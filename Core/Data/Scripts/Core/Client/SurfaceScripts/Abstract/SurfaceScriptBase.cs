using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Generated;
using LcdMod.Client.Animation;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Config;
using LcdMod.Client.FactionColors;
using LcdMod.Client.Extensions;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Gui.UserControls;
using LcdMod.Client.Helpers;
using LcdMod.Client.ScreenAreas;
using LcdMod.Client.Terminal.Controls;
using LcdMod.Client.Utility;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Models;
using LcdMod.Common.Helpers;
using Sandbox.Game.Entities;
using Sandbox.Game.Components;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;
using IMyCockpit = Sandbox.ModAPI.IMyCockpit;
using IMyShipController = Sandbox.ModAPI.IMyShipController;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using NotImplementedException = LcdMod.Common.Exceptions.NotImplementedException;

namespace LcdMod.Client.SurfaceScripts.Abstract
{
    public abstract class SurfaceScriptBase : MyTSSCommon, IAppHost
    {
        public static SurfaceCollection Instances = new SurfaceCollection();
        public Dictionary<string, Color> Theme { get; set; }
        readonly List<MySprite> _backgroundGrids = new List<MySprite>();
        Grid _footerRoot;
        Color _backgroundColor;
        Color _foregroundColor;

        public IMyFaction Faction { get; protected set; }
        protected string Icon { get; set; }
        public new IMyCubeBlock Block { get; }

        public virtual bool AsyncRender => false;

        protected long WaitForFrame;
        protected long LastRenderFrame;
        public long LastRunTick { get; private set; } = long.MinValue;

        public Vector2 TextureSize => Surface.TextureSize;

        /// <summary>
        /// Relative area of the <see cref="Sandbox.ModAPI.IMyTextSurface.TextureSize"/> That is Visible
        /// </summary>
        public virtual RectangleF ViewBox { get; protected set; }

        GridLogic _gridLogic;
        long _appGridEntityId;
        public GridLogic GridLogic
        {
            get
            {
                var cubeGrid = Block?.CubeGrid;
                if (cubeGrid == null || cubeGrid.Closed || cubeGrid.MarkedForClose)
                {
                    _gridLogic = null;
                    return null;
                }

                if (_gridLogic == null || _gridLogic.TargetGrid != cubeGrid.EntityId || !_gridLogic.IsAlive)
                    _gridLogic = LcdModSessionComponent.GetOrCreateGridLogic(cubeGrid);

                return _gridLogic;
            }
        }

        bool _init;
        int _rotationOrSurfaceIndex;

        protected float CaretY;
        protected float FooterHeight;

        public const float TITLE_BAR_HEIGHT_BASE = 40f;

        protected string LocalizedTitleCache = string.Empty;

        string _customInfo;

        public virtual string Title
        {
            get
            {
                if (string.IsNullOrEmpty(LocalizedTitleCache))
                    LocalizedTitleCache = StripScriptListPrefix(MyTexts.GetString(DefaultTitle));

                return LocalizedTitleCache;
            }
        }

        protected virtual string DefaultTitle => "<Title not Set>";

        public const string SCRIPT_LIST_PREFIX = "[SD] ";

        public static string StripScriptListPrefix(string title) =>
            title != null && title.StartsWith(SCRIPT_LIST_PREFIX, System.StringComparison.Ordinal)
                ? title.Substring(SCRIPT_LIST_PREFIX.Length)
                : title;

        public float ConfiguredScale => GeneralComponent.GetScale();
        protected float FontScale => _userFontScale <= 0f ? 1f : _userFontScale;
        protected float LayoutScale => ConfiguredScale * FontScale;

        float _userScale;
        float _userFontScale;
        string _userFont;
        protected float _userPadding;
        string _cachedTitleSource;
        string _cachedTitleText;
        float _cachedTitleAvailableWidth = -1f;
        float _cachedTitleFontSize = -1f;
        bool _cachedTitleLocalized;
        public bool TitleCanBeVisible { get; private set; } = true;
        public bool TitleVisible { get; private set; } = true;
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public SurfaceConfig Config { get; protected set; }
        IComponentContainer IAppHost.Config => Config;
        public GeneralConfigComponent GeneralComponent => Config.GetComponent<GeneralConfigComponent>();
        public ColorConfigComponent ColorComponent => Config.GetComponent<ColorConfigComponent>();
        /// <summary>Fonte escolhida no terminal, com o padrão do jogo como reserva.</summary>
        public string TextFont => string.IsNullOrEmpty(Surface.Font) ? "White" : Surface.Font;
        public InteractiveConfigComponent InteractionComponent => Config.GetComponent<InteractiveConfigComponent>();
        protected abstract AppType AppType { get; }

        public bool Dirty => _dirty;
        bool _dirty;

        /// <summary>Layout, fonte, cores ou facção mudaram desde a última vez que a tela zerou este sinal.</summary>
        protected bool LayoutDirty { get; set; } = true;
        bool _indexConflict;
        long _nextIndexConflictCheck;
        bool _disposed;
        volatile bool _asyncRenderPending;

        public AnimationController Animations { get; private set; }
        public ScreenProviderConfig ProviderConfig { get; private set; }
        protected bool IsScreenReadyToRender { get; private set; }
        public event Action<SurfaceScriptBase> OnRender;
        public event Action<SurfaceScriptBase> OnUpdateStart;
        public event Action<SurfaceScriptBase> OnUpdateEnd;

        protected SurfaceScriptBase(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block,
            size)
        {
            Animations = new AnimationController(
                GetAnimationFrame,
                ScheduleAnimationFrame,
                RenderSprites);

            WaitForFrame = MyAPIGateway.Session.GameplayFrameCounter + 6 * 5; // minimum of 5 frames splash screen 
            Block = (IMyCubeBlock)base.Block;
            var terminalBlock = (IMyTerminalBlock)Block;
            terminalBlock.AppendingCustomInfo += OnAppendingCustomInfo;
            terminalBlock.SetDetailedInfoDirty();
            terminalBlock.RefreshCustomInfo();
            
            _textureSize = (Vector2I)Surface.TextureSize;
            var surfaceSize = Surface.SurfaceSize;
            _renderComp = (MyRenderComponentScreenAreas)Block.Render;

            _aspectRatio = surfaceSize.X > surfaceSize.Y
                ? new Vector2(1f, 1f * surfaceSize.Y / surfaceSize.X)
                : new Vector2(1f * surfaceSize.X / surfaceSize.Y, 1f);

            Block.OnMarkForClose += HandleBlockMarkedForClose;
            UpdateFaction(FactionHelperCommon.GetOwnerFaction(Block as IMyTerminalBlock));
            DrawSplash();
            LcdModSessionComponent.OnLanguageChanged += LayoutChanged;
        }

        public int RotationOrSurfaceIndex => _rotationOrSurfaceIndex;
        public int SurfaceIndex => Config == null ? RotationOrSurfaceIndex : Config.SurfaceIndex;

        public abstract IApp App { get; }

        protected Grid FooterRoot
        {
            get
            {
                if (_footerRoot == null)
                    _footerRoot = new Grid(default(RectangleF));

                return _footerRoot;
            }
        }

        protected bool HasFooterRoot => _footerRoot != null && _footerRoot.Visible && _footerRoot.HasChildren;

        protected int ResolveRotationOrSurfaceIndex()
        {
            if (Block.CubeGrid.Physics == null)
                return -1; // we can ignore surface that does not exist

            if (Block is IMyTextPanel)
            {
                foreach (var component in Block.Components)
                {
                    _lcdSurfaceComponent = component as IMyLcdSurfaceComponent;
                    if (_lcdSurfaceComponent == null)
                        continue;

                    return _lcdSurfaceComponent.SelectedRotationIndex;
                }
            }
            else
            {
                var surfaceProvider = Block as IMyTextSurfaceProvider;
                if (surfaceProvider != null)
                {
                    var currentSurfaceName = Surface.Name;

                    for (int i = 0; i < surfaceProvider.SurfaceCount; i++)
                    {
                        if (surfaceProvider.GetSurface(i).Name != currentSurfaceName)
                            continue;

                        return i;
                    }
                }
            }

            LogHelper.Log(MyLogSeverity.Warning, "Failed to find surface {0} for {1}", Surface.Name, Block);
            return -1;
        }

        static long GetAnimationFrame()
        {
            return MyAPIGateway.Session != null
                ? MyAPIGateway.Session.GameplayFrameCounter
                : 0L;
        }

        static void ScheduleAnimationFrame(Action action)
        {
            if (action != null)
                LcdModClientComponent.RunNextFrame.Add(action);
        }

        void DrawSplash()
        {
            if (ViewBox.Size == Vector2.Zero)
                UpdateViewBox();

            var offset = Math.Min(ViewBox.Width, ViewBox.Height) / 5;
            var frame = Surface.DrawFrame();
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", ViewBox.Center,
                new Vector2(Math.Max(ViewBox.Width, ViewBox.Height) * 2), FactionHelperCommon.GetBackgroundColor(Faction)));
            frame.Add(new MySprite(SpriteType.TEXTURE, Icon,
                new Vector2(ViewBox.Center.X, ViewBox.Center.Y - offset / 2),
                new Vector2(Math.Min(ViewBox.Width, ViewBox.Height) / 1.5f), FactionHelperCommon.EnsureReadable(FactionHelperCommon.GetIconColor(Faction))));
            frame.Add(new MySprite(SpriteType.TEXT, Title, new Vector2(ViewBox.Center.X, ViewBox.Center.Y + offset),
                null, FactionHelperCommon.EnsureReadable(FactionHelperCommon.GetIconColor(Faction)), TextFont, rotation: 1.6f * FontScale));
            frame.Dispose();
        }

        protected void AddEmptyWithFiltersSprites(List<MySprite> sprites)
        {
            AddBackground(sprites);
            DrawTitle(sprites);
            DrawMessage(sprites, LocHelper.GetLoc("ScreenBlueprintsRew_NoBlueprints"),
"Warning", GetWarningColor(), ConfiguredScale);
            DrawFooter(sprites);
        }

        protected void AddEmptySprites(List<MySprite> sprites)
        {
            AddBackground(sprites);
            DrawTitle(sprites);
            DrawMessage(sprites, LocHelper.Empty,
"Warning", GetWarningColor(), ConfiguredScale);
            DrawFooter(sprites);
        }

        public virtual void RequestRedraw()
        {
            LayoutChanged();
            _dirty = true;
            Run();
            _dirty = false;
        }

        public void UseProviderConfig(ScreenProviderConfig providerConfig)
        {
            if (providerConfig == null)
                return;

            ProviderConfig = providerConfig;

            var index = Config == null ? RotationOrSurfaceIndex : Config.SurfaceIndex;
            if (index < 0)
                return;

            var persistedSurface = ConfigManager.EnsureSurfaceApp(Block, providerConfig, index, AppType);
            if (persistedSurface == null)
                return;

            Config = providerConfig.CanWriteConfig(persistedSurface)
                ? persistedSurface
                : persistedSurface.Clone();
        }

        public override void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try
            {
                CloseApp(App);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }

            try
            {
                if (Animations != null)
                    Animations.Dispose();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }

            try
            {
                if (Block != null)
                {
                    Block.OnMarkForClose -= HandleBlockMarkedForClose;
                    var terminalBlock = (IMyTerminalBlock)Block;
                    terminalBlock.AppendingCustomInfo -= OnAppendingCustomInfo;
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }

            try
            {
                if (Block != null && ProviderConfig != null && ConfigManager.TakeUnsaved(Block.EntityId))
                    ConfigManager.Save(Block, ProviderConfig);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }

            Instances.Remove(this);
            LcdModSessionComponent.OnLanguageChanged -= LayoutChanged;
            OnRender = null;
            OnUpdateStart = null;
            OnUpdateEnd = null;
            base.Dispose();
        }

        void HandleBlockMarkedForClose(IMyEntity entity)
        {
            Dispose();
        }

        static void CloseApp(IApp app)
        {
            var appBase = app as App;
            if (appBase == null)
                return;

            try
            {
                appBase.Close();
            }
            finally
            {
                appBase.CancelAnimationTree();
            }
        }

        protected virtual void UpdateViewBox()
        {
            var sizeOffset = (Surface.TextureSize - Surface.SurfaceSize) / 2;

            _userPadding = Surface.TextPadding;

            var padding = (Surface.TextPadding / 100) * Surface.SurfaceSize;
            sizeOffset += padding / 2;

            ViewBox = new RectangleF(
                sizeOffset.X,
                sizeOffset.Y,
                Surface.SurfaceSize.X - padding.X,
                Surface.SurfaceSize.Y - padding.Y);
        }

        public override void Run()
        {
            var session = MyAPIGateway.Session;
            if (session == null || _disposed)
                return;

            var currentFrame = session.GameplayFrameCounter;
            if (currentFrame < WaitForFrame)
                return;

            LastRunTick = currentFrame;

            base.Run();

            if (ViewBox.Size == Vector2.Zero)
                UpdateViewBox();

            if (!_init)
            {
                if (_indexConflict && currentFrame < _nextIndexConflictCheck)
                    return;

                Init();
                if (_indexConflict)
                {
                    _nextIndexConflictCheck = currentFrame + 600;
                    return;
                }
            }

            IsScreenReadyToRender = false;

            if (Config == null)
            {
                GetSettings((IMyTextSurface)Surface, Block);
                return;
            }
            
            var titleCanBeVisible = SurfaceAspectRatioHelper.CanShowTitle(Surface);
            var titleVisible = GeneralComponent.TitleVisible && titleCanBeVisible;

            if (TitleCanBeVisible != titleCanBeVisible)
                (Block as IMyTerminalBlock)?.RefreshTerminal();

            if (Math.Abs(_userPadding - Surface.TextPadding) > .01f ||
                Math.Abs(_userScale - GeneralComponent.GetScale()) > .001f ||
                Math.Abs(_userFontScale - Surface.FontSize) > .001f ||
                !string.Equals(_userFont, Surface.Font, StringComparison.Ordinal) ||
                BackgroundColor != _backgroundColor ||
                ForegroundColor != _foregroundColor ||
                TitleCanBeVisible != titleCanBeVisible ||
                TitleVisible != titleVisible)
                LayoutChanged();


            var cubeGrid = Block?.CubeGrid;
            if (cubeGrid == null || cubeGrid.Closed || cubeGrid.MarkedForClose)
            {
                _gridLogic = null;
                DrawLoadingScreen(GeneralComponent.GetScale());
                return;
            }

            if (_gridLogic == null || _gridLogic.TargetGrid != cubeGrid.EntityId || !_gridLogic.IsAlive)
                _gridLogic = LcdModSessionComponent.GetOrCreateGridLogic(cubeGrid);

            if (_gridLogic == null)
            {
                DrawLoadingScreen(GeneralComponent.GetScale());
                return;
            }

            ReplaceGridBoundAppIfNeeded(cubeGrid.EntityId);

            IsScreenReadyToRender = true;

            RaiseUpdateStart();
            try
            {
                SafeRun();
            }
            catch (Exception e)
            {
                OnException(e);
            }
            finally
            {
                RaiseUpdateEnd();
            }
        }

        void ReplaceGridBoundAppIfNeeded(long gridEntityId)
        {
            var previousGridEntityId = _appGridEntityId;
            _appGridEntityId = gridEntityId;
            if (previousGridEntityId == 0L || previousGridEntityId == gridEntityId)
                return;

            IApp detachedApp;
            try
            {
                detachedApp = DetachGridBoundApp();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
                return;
            }

            if (detachedApp == null)
                return;

            try
            {
                CloseApp(detachedApp);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        /// <summary>
        /// Detaches an app whose subscriptions belong to the grid on which it was created.
        /// SafeRun creates its replacement against the current grid.
        /// </summary>
        protected virtual IApp DetachGridBoundApp()
        {
            return null;
        }

        void Init()
        {
            _rotationOrSurfaceIndex = ResolveRotationOrSurfaceIndex();
            var panel = Block as IMyTextPanel;
            
            if (panel != null && Instances.GetInstance(panel, _rotationOrSurfaceIndex) != null)
            {
                _customInfo = LocHelper.GetLoc(MOD_PREFIX + "IndexConflict");
                DrawIndexConflict(_customInfo);
                _customInfo += "\n"+ LocHelper.GetLoc(MOD_PREFIX + "IndexConflictDetails");
                panel.RefreshCustomInfo();
                _indexConflict = true;
                return;
            }

            if (_indexConflict && panel != null)
            {
                _customInfo = null;
                panel.RefreshCustomInfo();
            }
            _indexConflict = false;
            Instances.Add(this);
            _init = true;
        }

        /// <summary>Só texto, sem Config: o conflito acontece antes de a configuração existir.</summary>
        void DrawIndexConflict(string message)
        {
            float scale = 0.9f * FontScale;
            var measured = Surface.MeasureStringInPixels(new StringBuilder(message), TextFont, 1f);
            if (measured.X > 0f)
                scale = Math.Min(scale, ViewBox.Width * 0.95f / measured.X);
            var position = new Vector2(ViewBox.Center.X, ViewBox.Center.Y - measured.Y * scale / 2f);
            using (var frame = Surface.DrawFrame())
            {
                frame.Add(new MySprite(SpriteType.TEXT, message, position, null, Surface.ScriptForegroundColor, TextFont,
                    TextAlignment.CENTER, scale));
            }
        }

        protected virtual string GetDetailedInfoCustomText() => string.Empty;

        void OnAppendingCustomInfo(IMyTerminalBlock block, StringBuilder detailedInfo)
        {
            if (_disposed)
                return;

            if (detailedInfo.Length > 0) 
                detailedInfo.AppendLine();

            if (!(block is IMyTextPanel)) 
                detailedInfo.AppendLine(Surface.DisplayName + ": ");
            
            if (!string.IsNullOrWhiteSpace(_customInfo))
                detailedInfo.AppendLine(_customInfo);

            var customText = GetDetailedInfoCustomText() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(customText))
                return;

            detailedInfo.AppendLine(customText);
        }

        public void OnException(Exception e)
        {
            try
            {
                var bSoD = BSoD.ShowBSoD(this, e);

                _renderComp.RenderSpritesToTexture(RotationOrSurfaceIndex, bSoD.Frame, _textureSize, _aspectRatio,
                    Surface.ScriptBackgroundColor, Surface.BackgroundAlpha);
                
                NotifyRendered();
            }
            catch (Exception e2)
            {
                ErrorHandlerHelper.LogError(e, this);
                ErrorHandlerHelper.LogError(e2, this);
            }

            var session = MyAPIGateway.Session;
            WaitForFrame = (session != null ? session.GameplayFrameCounter : 0L) + 600;
        }

        void GetSettings(IMyTextSurface surface, IMyCubeBlock block)
        {
            var index = 0;
            IMyTextSurfaceProvider surfaceProvider = (IMyTextSurfaceProvider)block;
            while (index < surfaceProvider.SurfaceCount)
            {
                if (surface.Equals(surfaceProvider.GetSurface(index)))
                {
                    SurfaceConfig config;
                    var providerConfig = ProviderConfig;
                    ConfigManager.LoadSettings(block, index, AppType, ref providerConfig, out config);
                    ProviderConfig = providerConfig;
                    Config = config;
                    return;
                }

                index++;
            }
        }

        /// <summary>
        /// Resets the <see cref="CaretY"/> to the Top of the screen, if <see cref="TitleVisible"/>, draws the Tittle 
        /// </summary>
        /// <param name="frame"></param>
        public virtual void DrawTitle(List<MySprite> frame)
        {
            const float margin = 0f;
            float headerScale = LayoutScale;
            float titleBarHeight = TITLE_BAR_HEIGHT_BASE * headerScale;
            Vector2 position = ViewBox.Position;
            position.X += margin;

            CaretY = position.Y;

            if (!TitleVisible)
                return;

            AddHeaderSprite(frame, new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = Icon,
                Position = position + new Vector2(20f) * headerScale,
                Size = new Vector2(40f * headerScale),
                Color = GetHeaderColor(),
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;

            frame.Add(MySprite.CreateClipRect(new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + ViewBox.X),
                (int)(40f * headerScale))));

            var availableWidth = ViewBox.Width - position.X + ViewBox.X;
            var titleText = GetCachedTitleText(availableWidth, 1.3f, true);

            AddHeaderSprite(frame, new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = titleText,
                Position = position,
                RotationOrScale = ConfiguredScale * 1.3f * FontScale,
                Color = GetHeaderColor(),
                Alignment = TextAlignment.LEFT,
                FontId = TextFont            });

            frame.Add(MySprite.CreateClearClipRect());

            CaretY += titleBarHeight;
        }

        protected virtual void DrawFooter(List<MySprite> frame)
        {
            if (!HasFooterRoot)
                return;

            RenderFooterRoot(frame, FooterRoot.Rect);
        }

        protected void RenderFooterRoot(List<MySprite> frame, RectangleF bounds)
        {
            if (frame == null || _footerRoot == null || !_footerRoot.Visible)
                return;

            IVisualStyleScope scope = App;
            _footerRoot.SetStyleParent(scope);
            _footerRoot.SetDataContext(App);
            _footerRoot.SetRect(bounds);
            _footerRoot.Render(frame);
        }

        protected static readonly Regex RxGroup = new Regex("\\(\\s*G\\s*:\\s*(.+?)\\s*\\)", RegexOptions.IgnoreCase);
        protected static readonly Regex RxContainer = new Regex("\\(\\s*(?!G\\s*:)(.+?)\\s*\\)", RegexOptions.IgnoreCase);

        protected static int GetScrollStep(int secondsPerStep)
        {
            return GetTimeStep(secondsPerStep);
        }

        protected static int GetTimeStep(float secondsPerStep)
        {
            try
            {
                var sess = MyAPIGateway.Session;
                if (sess == null) return 0;
                if (secondsPerStep <= 0f) secondsPerStep = 1f / 60f;

                // SE runs at 60 game ticks per second.
                int ticksPerStep = Math.Max(1, (int)Math.Round(secondsPerStep * 60f));
                long frameCounter = sess.GameplayFrameCounter;
                return (int)(frameCounter / ticksPerStep);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"[LcdMod] GetTimeStep error: {ex.Message}");
                return 0;
            }
        }

        public bool TryGetReferenceWorldMatrix(ReferenceMode mode, out MatrixD world,
            bool useBlockWorldForCockpitAuto = false)
        {
            world = MatrixD.Identity;

            switch (mode)
            {
                case ReferenceMode.Screen:
                    return ScreenAreaGeometry.TryGetScreenWorldMatrix(this, out world);
                case ReferenceMode.Controller:
                    return TryGetControllerWorldMatrix(out world);
                case ReferenceMode.Auto:
                default:
                    if (Block is IMyCockpit)
                    {
                        if (useBlockWorldForCockpitAuto)
                        {
                            world = Block.WorldMatrix;
                            return true;
                        }

                        if (TryGetCockpitWorldMatrix(out world))
                            return true;
                    }

                    return ScreenAreaGeometry.TryGetScreenWorldMatrix(this, out world);
            }
        }

        public bool TryGetReferenceWorldMatrix(int referenceModeValue, out MatrixD world,
            bool useBlockWorldForCockpitAuto = false)
        {
            var mode = (ReferenceMode)referenceModeValue;
            return TryGetReferenceWorldMatrix(mode, out world, useBlockWorldForCockpitAuto);
        }

        public bool TryGetCockpitWorldMatrix(out MatrixD world)
        {
            world = MatrixD.Identity;
            var cockpit = Block as IMyCockpit;
            if (cockpit == null)
                return false;

            world = cockpit.WorldMatrix;
            return true;
        }

        public bool TryGetControllerWorldMatrix(out MatrixD world)
        {
            world = MatrixD.Identity;
            var controller = ResolveShipController();
            if (controller == null)
                return false;

            world = controller.WorldMatrix;
            return true;
        }

        public IMyShipController ResolveShipController()
        {
            var myGrid = Block?.CubeGrid as MyCubeGrid;
            if (myGrid == null)
                return null;

            if (myGrid.MainCockpit != null)
                return myGrid.MainCockpit as IMyShipController;

            if (myGrid.MainRemoteControl != null)
                return myGrid.MainRemoteControl as IMyShipController;

            return null;
        }


        protected virtual RectangleF GetCellViewBox(float xStart, float xEnd, float yStart, float cellHeight,
            float cellPadding)
        {
            var innerLeft = xStart + cellPadding;
            var innerRight = xEnd - cellPadding;
            var innerTop = yStart + cellPadding;
            var innerBottom = yStart + cellHeight - cellPadding;
            return new RectangleF(innerLeft, innerTop, innerRight - innerLeft, innerBottom - innerTop);
        }

        public virtual void DrawMessage(List<MySprite> sprites, string message, string icon, Color color,
            float scale = 1f)
        {
            float contentTop = CaretY;
            float contentBottom = ViewBox.Bottom - FooterHeight;
            float contentHeight = Math.Max(0f, contentBottom - contentTop);
            if (contentHeight <= 0f)
                return;

            var center = new Vector2(ViewBox.Center.X, contentTop + contentHeight * 0.45f);
            float iconSize = Math.Min(ViewBox.Width, contentHeight) * .4f * Math.Min(scale, 2f);

            var iconSprite = new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = icon,
                Position = center,
                Size = new Vector2(iconSize),
                Color = color,
                Alignment = TextAlignment.CENTER
            };

            var text = new StringBuilder(message ?? string.Empty);
            TrimText(ref text, ViewBox.Width - 8f * ConfiguredScale);
            var textSprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text.ToString(),
                Position = new Vector2(center.X, center.Y + (iconSize / 2)),
                Color = color,
                Alignment = TextAlignment.CENTER,
                FontId = TextFont,
                RotationOrScale = 1f * ConfiguredScale * FontScale
            };

            sprites.Add(iconSprite.Shadow(2 * ConfiguredScale));
            sprites.Add(iconSprite);

            sprites.Add(textSprite.Shadow(2 * ConfiguredScale));
            sprites.Add(textSprite);
        }

        public virtual void DrawLoading(List<MySprite> sprites, float scale = 1f)
        {
            DrawLoadingFrame(sprites, scale);
        }

        protected Color GetHeaderColor()
        {
            return Readable(ColorComponent.ResolveHeaderColor(Block as IMyTerminalBlock));
        }

        protected Color GetWarningColor()
        {
            return Readable(ColorComponent.ResolveWarningColor(Block as IMyTerminalBlock));
        }

        protected Color GetErrorColor()
        {
            return Readable(ColorComponent.ResolveErrorColor(Block as IMyTerminalBlock));
        }

        /// <summary>Cor padrão (não escolhida pelo usuário) ganha contraste mínimo de 3:1 com o fundo real da tela.</summary>
        Color Readable(Color color)
        {
            return color.ReadableFor(ColorComponent, Block as IMyTerminalBlock, BackgroundColor);
        }

        protected static void ParseFilter(IMyTerminalBlock lcd, out string mode, out string token)
        {
            mode = null;
            token = null;
            if (lcd == null) return;
            var name = lcd.CustomName ?? string.Empty;

            var mg = RxGroup.Match(name);
            if (mg.Success)
            {
                mode = "group";
                token = mg.Groups[1].Value.Trim();
                return;
            }

            var mc = RxContainer.Match(name);
            if (mc.Success)
            {
                mode = "container";
                token = mc.Groups[1].Value.Trim();
            }
        }

        public void TrimText(ref StringBuilder sb, float availableWidth, float fontSize = 1)
        {
            Vector2 textSize = Surface.MeasureStringInPixels(sb, TextFont, fontSize * ConfiguredScale * FontScale);

            if (textSize.X > availableWidth)
            {
                var source = sb.ToString();
                for (int i = source.Length - 1; i > 0; i--)
                {
                    sb.Clear();
                    sb.Append(FormatingHelper.TrimName(source, i));
                    textSize = Surface.MeasureStringInPixels(sb, TextFont, fontSize * ConfiguredScale * FontScale);

                    if (textSize.X <= availableWidth)
                        break;
                }
            }
        }

        protected Vector2 ToScreenMargin(Vector2 absoluteCenterInViewBox)
        {
            return new Vector2(absoluteCenterInViewBox.X, 512f - absoluteCenterInViewBox.Y);
        }

        protected MySprite Text(string s, Vector2 p, float scale)
        {
            return new MySprite
            {
                Type = SpriteType.TEXT, Data = s, Position = p,
                Color = Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT,
                RotationOrScale = scale * FontScale
            };
        }

        protected virtual void LayoutChanged()
        {
            if (_disposed || Surface == null)
                return;

            _userPadding = Surface.TextPadding;
            _userScale = GeneralComponent.GetScale();
            _userFontScale = Surface.FontSize;
            _userFont = Surface.Font;
            _backgroundColor = BackgroundColor;
            _foregroundColor = ForegroundColor;
            LocalizedTitleCache = string.Empty;
            TitleCanBeVisible = SurfaceAspectRatioHelper.CanShowTitle(Surface);
            TitleVisible = GeneralComponent.TitleVisible && TitleCanBeVisible;
            InvalidateTitleCache();
            UpdateViewBox();
            _backgroundGrids.Clear();
            LayoutDirty = true;
        }


        protected void DrawLoadingScreen(float scale = 1f, bool drawTitle = true)
        {
            using (var frame = Surface.DrawFrame())
            {
                var sprites = new List<MySprite>();
                AddLoadingScreenSprites(sprites, scale, drawTitle);
                frame.AddRange(sprites);
            }
        }

        protected void AddLoadingScreenSprites(List<MySprite> sprites, float scale = 1f, bool drawTitle = true)
        {
            AddBackground(sprites);
            if (drawTitle)
                DrawTitle(sprites);
            DrawLoadingFrame(sprites, scale);
        }

        protected virtual void DrawLoadingFrame(List<MySprite> sprites, float scale = 1f)
        {
            float contentTop = CaretY;
            float contentBottom = ViewBox.Bottom - FooterHeight;
            float contentHeight = Math.Max(0f, contentBottom - contentTop);
            if (contentHeight <= 0f)
                return;

            var center = new Vector2(ViewBox.Center.X, contentTop + contentHeight * 0.45f);
            float wheelScale = Math.Max(0.05f, scale);
            float outerSize = Math.Min(ViewBox.Width, contentHeight) * 0.28f * wheelScale;
            float innerSize = outerSize * 0.6f;

            var session = MyAPIGateway.Session;
            double seconds = session != null ? session.GameplayFrameCounter / 60.0 : 0.0;
            float outerRotation = (float)(seconds * 2.4);
            float innerRotation = -outerRotation;

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = center,
                Size = new Vector2(outerSize),
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER,
                RotationOrScale = outerRotation
            });

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = center,
                Size = new Vector2(innerSize),
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER,
                RotationOrScale = innerRotation
            });

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = LocHelper.GetLoc("LoadingPleaseWait"),
                Position = new Vector2(center.X, center.Y + outerSize * 0.9f),
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER,
                FontId = TextFont,
                RotationOrScale = ConfiguredScale * FontScale
            });
        }

        protected string GetCachedTitleText(float availableWidth, float fontSize = 1.3f, bool localizeTitle = false)
        {
            var rawTitle = Title;
            availableWidth = Math.Max(0f, availableWidth);

            if (_cachedTitleText != null &&
                _cachedTitleSource == rawTitle &&
                _cachedTitleLocalized == localizeTitle &&
                Math.Abs(_cachedTitleAvailableWidth - availableWidth) <= 0.1f &&
                Math.Abs(_cachedTitleFontSize - fontSize) <= 0.0001f)
            {
                return _cachedTitleText;
            }

            var source = localizeTitle ? MyTexts.GetString(rawTitle) : rawTitle;
            var sb = new StringBuilder(source ?? string.Empty);
            if (availableWidth > 0f)
                TrimText(ref sb, availableWidth, fontSize);

            _cachedTitleSource = rawTitle;
            _cachedTitleLocalized = localizeTitle;
            _cachedTitleAvailableWidth = availableWidth;
            _cachedTitleFontSize = fontSize;
            _cachedTitleText = sb.ToString();
            return _cachedTitleText;
        }

        protected void InvalidateTitleCache()
        {
            _cachedTitleSource = null;
            _cachedTitleText = null;
            _cachedTitleAvailableWidth = -1f;
            _cachedTitleFontSize = -1f;
            _cachedTitleLocalized = false;
        }


        Color _backgroundGridsColor;

        public void AddBackground(List<MySprite> frame, Color? color = null)
        {
            var requested = color ?? BackgroundColor;
            if (_backgroundGrids.Count == 0 || _backgroundGridsColor != requested)
            {
                _backgroundGrids.Clear();
                _backgroundGridsColor = requested;
                var backgroundColor = new Color(requested, 0.66f);
                var frameTemp = Surface.DrawFrame();
                AddTiledBackground(frameTemp, backgroundColor);
                frameTemp.AddToList(_backgroundGrids);
            }

            frame.AddRange(_backgroundGrids);
        }

        void AddTiledBackground(MySpriteDrawFrame frame, Color color)
        {
            var tile = MyTextSurfaceHelper.DEFAULT_BACKGROUND;
            tile.Color = color;

            var tileSize = tile.Size ?? new Vector2(MyTextSurfaceHelper.BACKGROUND_SIZE);
            if (tileSize.X <= 0f || tileSize.Y <= 0f)
                tileSize = new Vector2(MyTextSurfaceHelper.BACKGROUND_SIZE);

            var shift = MyTextSurfaceHelper.BACKGROUND_SHIFT;
            float stepX = Math.Abs(shift.X) > 0.001f ? Math.Abs(shift.X) : tileSize.X;
            float stepY = Math.Abs(shift.Y) > 0.001f ? Math.Abs(shift.Y) : tileSize.Y;

            if (stepX <= 0f || stepY <= 0f)
                return;

            var basePosition = (tile.Position ?? Vector2.Zero) + Surface.TextureSize / 2f;
            var bounds = GetBackgroundBounds();
            var halfSize = tileSize * 0.5f;

            float startX = GetFirstBackgroundTileCenter(bounds.X, basePosition.X, halfSize.X, stepX);
            float startY = GetFirstBackgroundTileCenter(bounds.Y, basePosition.Y, halfSize.Y, stepY);

            for (float y = startY; y - halfSize.Y < bounds.Bottom; y += stepY)
            {
                for (float x = startX; x - halfSize.X < bounds.Right; x += stepX)
                {
                    tile.Position = new Vector2(x, y);
                    frame.Add(tile);
                }
            }
        }

        RectangleF GetBackgroundBounds()
        {
            var textureSize = Surface.TextureSize;
            var bounds = new RectangleF(
                0f,
                0f,
                Math.Max(1f, textureSize.X),
                Math.Max(1f, textureSize.Y));

            var viewBox = ViewBox;
            if (viewBox.Width <= 0f || viewBox.Height <= 0f)
                return bounds;

            float left = Math.Min(bounds.X, viewBox.X);
            float top = Math.Min(bounds.Y, viewBox.Y);
            float right = Math.Max(bounds.Right, viewBox.Right);
            float bottom = Math.Max(bounds.Bottom, viewBox.Bottom);

            return new RectangleF(left, top, Math.Max(1f, right - left), Math.Max(1f, bottom - top));
        }

        static float GetFirstBackgroundTileCenter(float boundsStart, float baseCenter, float halfSize, float step)
        {
            float baseStart = baseCenter - halfSize;
            return baseCenter + (float)Math.Floor((boundsStart - baseStart) / step) * step;
        }


        protected static void AddHeaderSprite(List<MySprite> frame, MySprite sprite)
        {
            frame.Add(sprite.Shadow(1f));
            frame.Add(sprite);
        }

        public void UpdateFaction(IMyFaction faction)
        {
            Faction = faction;
            Icon = FactionHelperCommon.GetIcon(faction);
            LayoutDirty = true;
        }

        readonly Vector2I _textureSize;
        readonly Vector2 _aspectRatio;
        readonly MyRenderComponentScreenAreas _renderComp;
        IMyLcdSurfaceComponent _lcdSurfaceComponent;

        /// <summary>
        /// Calling this break the regular rendering of the Text surface, ensure ALL render call is routed here if the app needs to use it
        /// </summary>
        public void RenderSprites()
        {
            RenderSprites(false);
        }

        public void RenderSprites(bool force)
        {
            var session = MyAPIGateway.Session;
            if (session == null || _disposed || _asyncRenderPending)
                return;

            var currentFrame = session.GameplayFrameCounter;
            if ((!force && LastRenderFrame == currentFrame) || WaitForFrame > currentFrame)
                return;
            try
            {
                var spriteList = PrepareSpritesForRender(RenderFrame(GetSprites));
                LastRenderFrame = currentFrame;
                NotifyRendered();

                var backgroundColor = Surface.ScriptBackgroundColor;
                var renderOpacity = GetRenderOpacity();
                var textureChanged = ShouldRenderTexture(spriteList, backgroundColor, renderOpacity, currentFrame);
                if (!force && !textureChanged)
                    return;

                if (AsyncRender)
                {
                    _asyncBuffer.Clear();
                    _asyncBuffer.AddRange(spriteList);
                    _asyncRenderPending = true;

                    var surfaceIndex = RotationOrSurfaceIndex;
                    LcdModClientComponent.Blocker.Add(MyAPIGateway.Parallel.Start(() =>
                    {
                        try
                        {
                            _renderComp.RenderSpritesToTexture(
                                surfaceIndex, _asyncBuffer, _textureSize, _aspectRatio,
                                backgroundColor, renderOpacity);
                        }
                        finally
                        {
                            OnAsyncRendered();
                        }
                    }));
                }
                else
                {
                    _renderComp.RenderSpritesToTexture(RotationOrSurfaceIndex, spriteList, _textureSize, _aspectRatio,
                        backgroundColor, renderOpacity);
                }

            }
            catch (Exception e)
            {
                OnException(e);
            }
        }

        void OnAsyncRendered()
        {
            _asyncBuffer?.Clear();
            _asyncRenderPending = false;
        }

        readonly List<MySprite> _asyncBuffer = new List<MySprite>();

        // Rasterizar a textura é o passo caro do render. Quando a lista de sprites não muda, a textura
        // anterior continua válida; o refresh periódico cobre texturas recriadas pelo jogo.
        const long TEXTURE_REFRESH_FRAMES = 300;
        int _lastTextureSignature;
        long _lastTextureRenderFrame = -1;

        bool ShouldRenderTexture(List<MySprite> sprites, Color backgroundColor, byte renderOpacity, long currentFrame)
        {
            int signature = ComputeTextureSignature(sprites, backgroundColor, renderOpacity, _textureSize);
            bool refreshDue = _lastTextureRenderFrame < 0 ||
                              currentFrame - _lastTextureRenderFrame >= TEXTURE_REFRESH_FRAMES;
            if (!refreshDue && signature == _lastTextureSignature)
                return false;

            _lastTextureSignature = signature;
            _lastTextureRenderFrame = currentFrame;
            return true;
        }

        static int ComputeTextureSignature(List<MySprite> sprites, Color backgroundColor, byte renderOpacity, Vector2I textureSize)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + sprites.Count;
                hash = hash * 31 + (int)backgroundColor.PackedValue;
                hash = hash * 31 + renderOpacity;
                hash = hash * 31 + textureSize.X;
                hash = hash * 31 + textureSize.Y;
                for (int i = 0; i < sprites.Count; i++)
                {
                    var sprite = sprites[i];
                    hash = hash * 31 + (int)sprite.Type;
                    hash = hash * 31 + (sprite.Data != null ? sprite.Data.GetHashCode() : 0);
                    hash = hash * 31 + (sprite.FontId != null ? sprite.FontId.GetHashCode() : 0);
                    hash = hash * 31 + (int)sprite.Alignment;
                    hash = hash * 31 + sprite.RotationOrScale.GetHashCode();
                    if (sprite.Position.HasValue)
                    {
                        hash = hash * 31 + sprite.Position.Value.X.GetHashCode();
                        hash = hash * 31 + sprite.Position.Value.Y.GetHashCode();
                    }

                    if (sprite.Size.HasValue)
                    {
                        hash = hash * 31 + sprite.Size.Value.X.GetHashCode();
                        hash = hash * 31 + sprite.Size.Value.Y.GetHashCode();
                    }

                    if (sprite.Color.HasValue)
                        hash = hash * 31 + (int)sprite.Color.Value.PackedValue;
                }

                return hash;
            }
        }

        bool _transparentAreaResolved;
        bool _isTransparentArea;

        byte GetRenderOpacity()
        {
            if (!_transparentAreaResolved && _init)
            {
                _isTransparentArea = ScreenAreaGeometry.IsTransparentScreenArea(Block, RotationOrSurfaceIndex);
                _transparentAreaResolved = true;
            }

            if (_transparentAreaResolved ? _isTransparentArea : ScreenAreaGeometry.IsTransparentScreenArea(Block, RotationOrSurfaceIndex))
                return GetDefaultOpacity();

            return GeneralComponent.BackgroundAlpha.Get(GetDefaultOpacity);
        }

        byte GetDefaultOpacity() => Surface.BackgroundAlpha;

        void NotifyRendered()
        {
            RaiseSurfaceEvent(OnRender);
        }

        void RaiseUpdateStart()
        {
            RaiseSurfaceEvent(OnUpdateStart);
        }

        void RaiseUpdateEnd()
        {
            RaiseSurfaceEvent(OnUpdateEnd);
        }


        void RaiseSurfaceEvent(Action<SurfaceScriptBase> handlers)
        {
            if (handlers == null)
                return;

            foreach (var @delegate in handlers.GetInvocationList())
            {
                var handler = (Action<SurfaceScriptBase>)@delegate;
                try
                {
                    handler(this);
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, this);
                }
            }
        }

        static List<MySprite> PrepareSpritesForRender(List<MySprite> sprites)
        {
            if (sprites == null || sprites.Count == 0)
                return sprites ?? new List<MySprite>();

            List<MySprite> prepared = null;
            for (int i = 0; i < sprites.Count; i++)
            {
                var sprite = sprites[i];
                if (sprite.Type == SpriteType.TEXTURE && !string.IsNullOrWhiteSpace(sprite.Data) && char.IsNumber(sprite.Data[0]))
                    ResolveCustomTexture(ref sprite);

                if (CanRenderSprite(sprite))
                {
                    prepared?.Add(sprite);
                    continue;
                }

                if (prepared == null)
                {
                    prepared = new List<MySprite>(sprites.Count);
                    for (int j = 0; j < i; j++)
                        prepared.Add(sprites[j]);
                }
            }

            return prepared ?? sprites;
        }

        /// <summary>Imagem de outro jogador ("steamId-nome") ainda desconhecida: pede pela rede e mostra o ícone de ausente enquanto isso.</summary>
        static void ResolveCustomTexture(ref MySprite sprite)
        {
            if (TextureHelper.IsKnownTexture(sprite.Data))
                return;

            ulong ownerId;
            string textureName;
            if (TextureHelper.HasTextureParseFailed(sprite.Data) ||
                !TextureFileHelper.TryParseTextureKey(sprite.Data, out ownerId, out textureName))
            {
                TextureHelper.MarkTextureParseFailed(sprite.Data);
                ApplyMissingTextureFallback(ref sprite);
                return;
            }

            if (!TextureHelper.HasPendingTextureRequest(sprite.Data))
                TextureHelper.TryQueueTextureRequest(ownerId, textureName, sprite.Data);
            if (!TextureHelper.IsKnownTexture(sprite.Data))
                ApplyMissingTextureFallback(ref sprite);
        }

        static void ApplyMissingTextureFallback(ref MySprite sprite)
        {
            sprite.Data = "MissingIcon";
            if (!sprite.Size.HasValue)
                return;

            var size = sprite.Size.Value;
            var uniform = Math.Min(Math.Abs(size.X), Math.Abs(size.Y));
            sprite.Size = new Vector2(uniform, uniform);
        }

        static bool CanRenderSprite(MySprite sprite)
        {
            if (!IsFinite(sprite.RotationOrScale))
                return false;

            if (sprite.Position.HasValue && !IsFinite(sprite.Position.Value))
                return false;

            if (sprite.Size.HasValue && !IsFinite(sprite.Size.Value))
                return false;

            return true;
        }

        static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.X) && IsFinite(value.Y);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }


        public virtual List<MySprite> GetSprites()
        {
            throw new NotImplementedException();
        }

        protected virtual List<MySprite> RenderFrame(Func<List<MySprite>> sprites)
        {
            return sprites();
        }

        public abstract void SafeRun();
    }
}
