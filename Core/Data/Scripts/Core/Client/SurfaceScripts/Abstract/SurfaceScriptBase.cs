using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Generated;
using LcdMod.Client.Animation;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Config;
using LcdMod.Client.Extensions;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Frames;
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
        readonly List<MySprite> _backgroundGrids = new List<MySprite>();
        readonly Dictionary<long, Vector2> _registeredProxyOffsets = new Dictionary<long, Vector2>();
        readonly List<MySprite> _cachedFrame = new List<MySprite>();
        Grid _footerRoot;
        Color _backgroundColor;
        Color _foregroundColor;

        public IMyFaction Faction { get; protected set; }
        protected string Icon { get; set; }
        public new IMyCubeBlock Block { get; }

        protected virtual bool ClipToBounds => _registeredProxyOffsets.Count > 0;

        protected long WaitForFrame;
        protected long LastRenderFrame;

        public Vector2 TextureSize => Surface.TextureSize;
        public Vector2 SurfaceSize => Surface.SurfaceSize;

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

        public float ConfiguredScale => Config == null ? 1f : GeneralComponent.GetScale();
        public float FontScale => (_userFontScale <= 0f ? 1f : _userFontScale) * ThemeChrome.FontBoost(_userTheme);
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
        LcdTheme _cachedTitleTheme;
        public bool TitleCanBeVisible { get; private set; } = true;
        public bool TitleVisible { get; private set; } = true;
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public SurfaceConfig Config { get; protected set; }
        IComponentContainer IAppHost.Config => Config;
        public GeneralConfigComponent GeneralComponent => Config.GetComponent<GeneralConfigComponent>();
        public virtual LcdTheme ThemeKind => GeneralComponent.GetTheme();
        /// <summary>Moldura do tema para esta tela (textura, rótulos e campos); null quando a tela não tem moldura.</summary>
        public FrameRenderer Frame { get; private set; }
        /// <summary>ID da tela na tabela de molduras; null quando a tela não tem moldura dedicada.</summary>
        protected virtual string FrameId => null;
        protected virtual bool FrameTiny => Surface.SurfaceSize.Y / Surface.SurfaceSize.X < 0.375f;
        LcdTheme _userTheme;
        /// <summary>Cor de texto do LCD já filtrada pelo tema (Café usa a paleta fixa).</summary>
        public new Color ForegroundColor => ThemeChrome.Foreground(_userTheme, Surface.ScriptForegroundColor);
        /// <summary>Cor de fundo do LCD já filtrada pelo tema (Café usa a paleta fixa).</summary>
        public new Color BackgroundColor => ThemeChrome.Background(_userTheme, Surface.ScriptBackgroundColor);
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

        public AnimationController Animations { get; private set; }
        public ScreenProviderConfig ProviderConfig { get; private set; }
        public event Action<SurfaceScriptBase> OnRender;

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
            Vector2 frameInset = Frame != null ? new Vector2(FrameThickness) : ThemeChrome.FrameInset(_userTheme, Surface.SurfaceSize, FrameThickness, ConfiguredScale);

            ViewBox = ApplyProxyOffsets(new RectangleF(
                sizeOffset.X + frameInset.X,
                sizeOffset.Y + frameInset.Y,
                Surface.SurfaceSize.X - padding.X - frameInset.X * 2f,
                Surface.SurfaceSize.Y - padding.Y - frameInset.Y * 2f));
        }

        protected RectangleF ApplyProxyOffsets(RectangleF viewBox)
        {
            if (_registeredProxyOffsets.Count <= 0)
                return viewBox;

            var min = Vector2.Zero;
            var max = Vector2.Zero;
            foreach (var entry in _registeredProxyOffsets)
            {
                min = Vector2.Min(min, entry.Value);
                max = Vector2.Max(max, entry.Value);
            }

            return new RectangleF(
                viewBox.X + min.X,
                viewBox.Y + min.Y,
                viewBox.Width + (max.X - min.X),
                viewBox.Height + (max.Y - min.Y));
        }


        public override void Run()
        {
            var session = MyAPIGateway.Session;
            if (session == null || _disposed)
                return;

            var currentFrame = session.GameplayFrameCounter;
            if (currentFrame < WaitForFrame)
                return;

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
                TitleVisible != titleVisible ||
                _userTheme != ThemeKind)
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

            try
            {
                SafeRun();
            }
            catch (Exception e)
            {
                OnException(e);
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
                frame.Add(new MySprite(SpriteType.TEXT, message, position, null, ForegroundColor, TextFont,
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
                    BackgroundColor, Surface.BackgroundAlpha);
                
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
            const float titleFontSize = 1.3f;
            float headerScale = LayoutScale;
            float titleBarHeight = TITLE_BAR_HEIGHT_BASE * headerScale;
            float iconSize = 40f * headerScale;
            Vector2 position = ViewBox.Position;
            position.X += margin;

            CaretY = position.Y;

            if (ThemeChrome.IsBare(_userTheme))
                return;

            if (_userTheme == LcdTheme.Small)
                return;

            if (Frame != null || !TitleVisible)
                return;

            Color accent = GetHeaderColor();
            Color titleColor = ThemeChrome.TitleColor(_userTheme, accent, BackgroundColor);
            ThemeChrome.DrawTitleAccent(frame, _userTheme, new RectangleF(ViewBox.X, ViewBox.Y, ViewBox.Width, titleBarHeight), ViewBox, accent, ForegroundColor, headerScale);

            AddHeaderSprite(frame, new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = Icon,
                Position = position + new Vector2(iconSize * 0.5f),
                Size = new Vector2(iconSize),
                Color = titleColor,
                Alignment = TextAlignment.CENTER
            });
            position.X += ViewBox.Width / 8f;

            frame.Add(MySprite.CreateClipRect(new Rectangle((int)position.X, (int)position.Y,
                (int)(ViewBox.Width - position.X + ViewBox.X),
                (int)titleBarHeight)));

            TextAlignment alignment = ThemeChrome.TitleAlignment(_userTheme);
            bool centered = alignment == TextAlignment.CENTER;
            var availableWidth = centered ? ViewBox.Width * 0.75f : ViewBox.Width - position.X + ViewBox.X;
            var titleText = GetCachedTitleText(availableWidth, titleFontSize, true);

            AddHeaderSprite(frame, new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = titleText,
                Position = centered ? new Vector2(ViewBox.Center.X, position.Y) : position,
                RotationOrScale = ConfiguredScale * titleFontSize * FontScale,
                Color = titleColor,
                Alignment = alignment,
                FontId = ThemeChrome.TitleFont(_userTheme, TextFont, titleText)
            });

            frame.Add(MySprite.CreateClearClipRect());

            CaretY += titleBarHeight;
        }

        protected virtual void DrawFooter(List<MySprite> frame)
        {
            if (!HasFooterRoot)
                return;

            if (ThemeChrome.IsBare(_userTheme))
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

            var textSprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = TextWrappingHelper.TrimToWidth(message, Surface, TextFont, ConfiguredScale * FontScale, ViewBox.Width - 8f * ConfiguredScale),
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
            return Readable(ColorComponent.ResolveWarningColor());
        }

        protected Color GetErrorColor()
        {
            return Readable(ColorComponent.ResolveErrorColor());
        }

        /// <summary>Cor padrão (não escolhida pelo usuário) ganha contraste mínimo de 3:1 com o fundo real da tela.</summary>
        Color Readable(Color color)
        {
            return color.ReadableFor(ColorComponent, BackgroundColor);
        }

        public void TrimText(ref StringBuilder sb, float availableWidth, float fontSize = 1)
        {
            var source = sb.ToString();
            var trimmed = TextWrappingHelper.TrimToWidth(source, Surface, TextFont, fontSize * ConfiguredScale * FontScale, availableWidth);
            if (ReferenceEquals(trimmed, source))
                return;

            sb.Clear();
            sb.Append(trimmed);
        }

        protected virtual void LayoutChanged()
        {
            if (_disposed || Surface == null || Config == null)
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
            ResolveFrame();
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
                Color = ForegroundColor,
                Alignment = TextAlignment.CENTER,
                RotationOrScale = outerRotation
            });

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = center,
                Size = new Vector2(innerSize),
                Color = ForegroundColor,
                Alignment = TextAlignment.CENTER,
                RotationOrScale = innerRotation
            });

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = LocHelper.GetLoc("LoadingPleaseWait"),
                Position = new Vector2(center.X, center.Y + outerSize * 0.9f),
                Color = ForegroundColor,
                Alignment = TextAlignment.CENTER,
                FontId = TextFont,
                RotationOrScale = ConfiguredScale * FontScale
            });
        }

        protected string GetCachedTitleText(float availableWidth, float fontSize = 1.3f, bool localizeTitle = false)
        {
            var rawTitle = Title;
            availableWidth = Math.Max(0f, availableWidth);

            var theme = ThemeKind;
            if (_cachedTitleText != null &&
                _cachedTitleSource == rawTitle &&
                _cachedTitleLocalized == localizeTitle &&
                _cachedTitleTheme == theme &&
                Math.Abs(_cachedTitleAvailableWidth - availableWidth) <= 0.1f &&
                Math.Abs(_cachedTitleFontSize - fontSize) <= 0.0001f)
            {
                return _cachedTitleText;
            }

            var source = ThemeChrome.LabelText(theme, localizeTitle ? MyTexts.GetString(rawTitle) : rawTitle);
            var sb = new StringBuilder(source ?? string.Empty);
            if (availableWidth > 0f)
                TrimText(ref sb, availableWidth, fontSize);

            _cachedTitleSource = rawTitle;
            _cachedTitleLocalized = localizeTitle;
            _cachedTitleTheme = theme;
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

        void ResolveFrame()
        {
            _userTheme = ThemeKind;
            var layout = FrameLayouts.Resolve(_userTheme, FrameId, FrameTiny);
            Frame = layout == null ? null : new FrameRenderer(layout, Surface, TextFont);
        }

        public void AddBackground(List<MySprite> frame, Color? color = null)
        {
            if (Frame != null)
            {
                Frame.Static(frame, GetHeaderColor());
                return;
            }

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
            Vector2 inset = ThemeChrome.FrameInset(_userTheme, Surface.SurfaceSize, FrameThickness, ConfiguredScale) * 0.5f;
            var outer = new RectangleF(ViewBox.X - inset.X, ViewBox.Y - inset.Y, ViewBox.Width + inset.X * 2f, ViewBox.Height + inset.Y * 2f);
            if (!ThemeChrome.DrawFrame(frame, _userTheme, outer, BackgroundColor, GetHeaderColor(), ForegroundColor, LayoutScale))
                DrawChrome(frame);
        }

        const float FRAME_RATIO = 0.04f;

        protected float FrameThickness => (float)Math.Round(Math.Min(Surface.SurfaceSize.X, Surface.SurfaceSize.Y) * FRAME_RATIO);

        /// <summary>Moldura neon 9-slice em volta do ViewBox (canto + borda, rotacionados).</summary>
        void DrawChrome(List<MySprite> frame)
        {
            float t = FrameThickness;
            if (t < 4f)
                return;

            RectangleF box = ViewBox;
            float half = t * 0.5f;
            float left = box.X - half;
            float right = box.Right + half;
            float top = box.Y - half;
            float bottom = box.Bottom + half;
            var corner = new Vector2(t);
            AddChromeSprite(frame, "SD_FrameCorner", new Vector2(left, top), corner, 0f);
            AddChromeSprite(frame, "SD_FrameCorner", new Vector2(right, top), corner, MathHelper.PiOver2);
            AddChromeSprite(frame, "SD_FrameCorner", new Vector2(right, bottom), corner, MathHelper.Pi);
            AddChromeSprite(frame, "SD_FrameCorner", new Vector2(left, bottom), corner, MathHelper.Pi + MathHelper.PiOver2);
            AddChromeSprite(frame, "SD_FrameEdge", new Vector2(box.Center.X, top), new Vector2(box.Width, t), 0f);
            AddChromeSprite(frame, "SD_FrameEdge", new Vector2(right, box.Center.Y), new Vector2(box.Height, t), MathHelper.PiOver2);
            AddChromeSprite(frame, "SD_FrameEdge", new Vector2(box.Center.X, bottom), new Vector2(box.Width, t), MathHelper.Pi);
            AddChromeSprite(frame, "SD_FrameEdge", new Vector2(left, box.Center.Y), new Vector2(box.Height, t), MathHelper.Pi + MathHelper.PiOver2);
        }

        static void AddChromeSprite(List<MySprite> frame, string texture, Vector2 position, Vector2 size, float rotation)
        {
            frame.Add(new MySprite(SpriteType.TEXTURE, texture, position, size, Color.White, null, TextAlignment.CENTER, rotation));
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
            if (session == null || _disposed)
                return;

            var currentFrame = session.GameplayFrameCounter;
            if ((!force && LastRenderFrame == currentFrame) || WaitForFrame > currentFrame)
                return;
            try
            {
                var spriteList = PrepareSpritesForRender(RenderFrame(GetSpritesDelegate));
                CacheFrameForProxies(spriteList);
                var renderList = ClipToBounds ? ClipSpriteListToSurfaceBounds(spriteList) : spriteList;
                LastRenderFrame = currentFrame;
                NotifyRendered();

                var backgroundColor = BackgroundColor;
                var renderOpacity = GetRenderOpacity();
                var textureChanged = ShouldRenderTexture(renderList, backgroundColor, renderOpacity, currentFrame);
                if (!force && !textureChanged)
                    return;

                _renderComp.RenderSpritesToTexture(RotationOrSurfaceIndex, renderList, _textureSize, _aspectRatio,
                    backgroundColor, renderOpacity);
            }
            catch (Exception e)
            {
                OnException(e);
            }
        }

        List<MySprite> ClipSpriteListToSurfaceBounds(List<MySprite> sprites)
        {
            if (!ClipToBounds || sprites == null || sprites.Count == 0)
                return sprites;

            var renderBounds = new RectangleF(0f, 0f, _textureSize.X, _textureSize.Y);
            var renderSprites = _clipBuffer;
            renderSprites.Clear();
            bool skipClippedSprites = false;

            for (int i = 0; i < sprites.Count; i++)
            {
                var sprite = sprites[i];
                if (sprite.Type == SpriteType.CLIP_RECT)
                {
                    skipClippedSprites = !TryClipRectToRenderBounds(sprite, renderBounds, out sprite);
                    if (skipClippedSprites)
                        continue;
                }
                else if (skipClippedSprites)
                {
                    continue;
                }

                renderSprites.Add(sprite);
            }

            return renderSprites;
        }

        static bool TryClipRectToRenderBounds(MySprite sprite, RectangleF renderBounds, out MySprite clippedSprite)
        {
            clippedSprite = sprite;
            if (!sprite.Position.HasValue || !sprite.Size.HasValue)
                return true;

            var clip = NormalizeRect(new RectangleF(sprite.Position.Value, sprite.Size.Value));
            RectangleF clipped;
            RectangleF.Intersect(ref clip, ref renderBounds, out clipped);
            if (clipped.Width <= 0f || clipped.Height <= 0f)
                return false;

            clippedSprite = new MySprite(
                SpriteType.CLIP_RECT,
                sprite.Data,
                clipped.Position,
                clipped.Size,
                sprite.Color,
                sprite.FontId,
                sprite.Alignment,
                sprite.RotationOrScale);
            return true;
        }

        static RectangleF NormalizeRect(RectangleF rect)
        {
            float left = Math.Min(rect.X, rect.Right);
            float top = Math.Min(rect.Y, rect.Bottom);
            float right = Math.Max(rect.X, rect.Right);
            float bottom = Math.Max(rect.Y, rect.Bottom);
            return new RectangleF(left, top, right - left, bottom - top);
        }



        readonly List<MySprite> _clipBuffer = new List<MySprite>();

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
            var handlers = OnRender;
            if (handlers == null)
                return;

            try
            {
                handlers(this);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
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

        public static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.X) && IsFinite(value.Y);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }


        public virtual List<MySprite> GetSprites()
        {
            throw new NotImplementedException();
        }

        Func<List<MySprite>> _getSprites;
        Func<List<MySprite>> GetSpritesDelegate => _getSprites ?? (_getSprites = GetSprites);

        protected virtual List<MySprite> RenderFrame(Func<List<MySprite>> sprites)
        {
            return sprites();
        }

        void CacheFrameForProxies(IEnumerable<MySprite> spriteList)
        {
            if (_registeredProxyOffsets.Count <= 0 || spriteList == null)
                return;

            _cachedFrame.Clear();
            _cachedFrame.AddRange(spriteList);
        }

        public List<MySprite> GetCachedFrame() => _cachedFrame;

        public void RegisterProxy(long proxyKey, Vector2 offset)
        {
            Vector2 existing;
            if (_registeredProxyOffsets.TryGetValue(proxyKey, out existing) && existing == offset)
                return;

            _registeredProxyOffsets[proxyKey] = offset;
            LayoutChanged();
        }

        public void UnregisterProxy(long proxyKey)
        {
            if (_registeredProxyOffsets.Remove(proxyKey))
                LayoutChanged();
        }


        public abstract void SafeRun();
    }
}
