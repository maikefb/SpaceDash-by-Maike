using LcdMod.Common.Config.Components;
using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Extensions;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.ControlsTemplates.Basic;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.SurfaceScripts.Abstract;
using StackPanelControl = LcdMod.Client.Gui.ControlsTemplates.Panels.StackPanel.StackPanel;
using LcdMod.Client.Helpers;
using LcdMod.Common.Helpers;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;
using SliderRadarRange = LcdMod.Client.Terminal.Controls.Generic.SliderRadarRange;
using static LcdMod.Common.Helpers.Constants;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.Apps
{
    internal class ContactRecord
    {
        public long EntityId;
        public string Name;
        public MyRelationsBetweenPlayerAndBlock Relationship;
        public string IconTexture;
        public Vector3D WorldPosition;
        public float Distance;
        public bool IsTargeted;
        public float TargetLockPercent;
        public int MissedFrames;
    }

    

    

    public partial class RadarApp : App, IApp
    {
        public const string ID = ID_PREFIX + "Radar";
        public const string TITLE = MOD_PREFIX + "Radar";


        // Fixed ring distances (meters) drawn regardless of dynamic range
        const float RING_1_M = 800f;
        const float RING_2_M = 1400f;
        const float RING_3_M = 2000f;
        const float DEFAULT_RANGE = 3000f;
        const string LONG_RANGE_WARNING_KEY = MOD_PREFIX + "Radar_AntennaRangeWarning";

        // Timing intervals (Run() calls; each call ≈ 166 ms at Update10)
        // ponytail: varredura de entidades a cada 3 Update10 (≈ 0,5 s); timeout em varreduras (≈ 6 s).
        const int CONTACT_COLLECT_EVERY = 3;
        const int CONTACT_TIMEOUT = 12;

        const int MAX_CONTACTS = 32;

        // Visual sizes in logical pixels scaled by Scale at render time
        const float RADAR_MARGIN_PX = 14f;

        const float RANGE_TEXT_SIZE = 1.2f;
        const float DOT_SIZE = 16f;
        const float QUADRANT_LABEL_TEXT_SIZE = 0.6f;
        const float QUADRANT_LABEL_MARGIN_PX = 10f;
        const float SIZE_TO_PX = 28.8f;
        const float TGT_ELEVATION_LINE_WIDTH = 4f;
        const float RADAR_RANGE_LINE_WIDTH = 8f;
        const float QUADRANT_LINE_WIDTH = 4f;
        const float QUADRANT_LINE_COVERAGE_PER_SIDE = .8f;
        const float PROJECTION_ANGLE_DEG = 55f;
        const float CAMERA_MIN_ZOOM_SCALE = 0.1f;
        const float CAMERA_MAX_ZOOM_SCALE = 10f;
        const float CAMERA_DEFAULT_ZOOM_SCALE = 1f;
        const float CAMERA_MAX_PAN_RANGE_RATIO = 1f;
        const int FOOTER_MAX_ROWS = 4;
        const float FOOTER_ROW_HEIGHT_PX = 20f;
        const float FOOTER_BOTTOM_PADDING_PX = 6f;
        const float FOOTER_WARNING_TEXT_SIZE = 0.46f;
        const float FOOTER_WARNING_ICON_SIZE_PX = 13f;
        const float FOOTER_WARNING_ICON_GAP_PX = 5f;
        const int FOOTER_SCROLL_STEP_SECONDS = 2;
        const long LONG_RANGE_SIGNAL_CACHE_FRAMES = 120L;
        const int COMPACT_MAX_ROWS = 4;
        const float COMPACT_ROW_RATIO = 1.5f;
        const float COMPACT_SWATCH_PX = 6f;
        const float MINIMAL_DOT_RADIUS_PX = 3f;
        const float MINIMAL_RING_WIDTH_PX = 2f;
        const float SCOPE_RING_ALPHA = 0.3f;
        const float HOSTILE_DIM_ALPHA = 0.3f;
        static readonly string[] _sectorNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        sealed class CompactContactOrder : IComparer<ContactRecord>
        {
            public static readonly CompactContactOrder Instance = new CompactContactOrder();

            public int Compare(ContactRecord a, ContactRecord b)
            {
                bool hostileA = a.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies;
                bool hostileB = b.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies;
                if (hostileA != hostileB)
                    return hostileA ? -1 : 1;
                return a.Distance.CompareTo(b.Distance);
            }
        }


        readonly Dictionary<long, ContactRecord> _contacts = new Dictionary<long, ContactRecord>();
        readonly HashSet<long> _seenThisFrame = new HashSet<long>();
        readonly HashSet<long> _processedGroupGridIds = new HashSet<long>();
        readonly List<long> _toRemove = new List<long>();
        readonly List<ContactRecord> _sortedContacts = new List<ContactRecord>();
        readonly List<ContactRecord> _compactList = new List<ContactRecord>();
        readonly List<BareLayout.KvRow> _compactRows = new List<BareLayout.KvRow>();
        readonly List<IMyCubeGrid> _tempGroupGrids = new List<IMyCubeGrid>();
        readonly List<IMySlimBlock> _tempSlimBlocks = new List<IMySlimBlock>();
        readonly Dictionary<long, SignalCacheEntry> _longRangeSignalCache = new Dictionary<long, SignalCacheEntry>();
        readonly IMyEntity[] _lockCandidates = new IMyEntity[6];
        float _maxRange = DEFAULT_RANGE;
        Vector2 _cameraPanMeters;
        float _cameraZoomScale = CAMERA_DEFAULT_ZOOM_SCALE;
        int _collectTick;
        RadarLayout _layout;
        bool _hasLayout;
        string _longRangeWarningText;
        string _detectedEntitiesText;
        string _rangeLabelText;
        string _footerRangeText;
        float _footerRangeTextRange = float.NaN;

        readonly List<IMyEntity> _tempEntities = new List<IMyEntity>();
        readonly Vector2 _tgtIconSize = new Vector2(20f, 20f);
        readonly Vector2 _shipIconSize = new Vector2(32f, 16f);
        readonly Vector2 _borderPadding = new Vector2(16f, 64f);
        readonly List<TargetInfo> _targetsBelowPlane = new List<TargetInfo>();
        readonly List<TargetInfo> _targetsAbovePlane = new List<TargetInfo>();
        readonly List<MySprite> _backgroundSprites = new List<MySprite>();
        readonly List<Control> _children = new List<Control>();
        readonly List<ControlTemplate> _footerRows = new List<ControlTemplate>();
        readonly List<IconTextRowControl> _footerContactControls = new List<IconTextRowControl>();
        readonly Panel _cameraControl;
        readonly RadarSceneControl _radarSceneControl;
        readonly RadarFooterControl _footerControl;
        readonly FooterHeaderControl _footerHeaderControl;
        readonly IconTextRowControl _footerWarningControl;
        long _cachedCharacterId;
        Sandbox.Game.EntityComponents.MyTargetLockingComponent _cachedCharacterTargetLocking;
        static readonly float _radarProjectionCos = (float)Math.Cos(MathHelper.ToRadians(PROJECTION_ANGLE_DEG));
        static readonly float _radarProjectionSin = (float)Math.Sin(MathHelper.ToRadians(PROJECTION_ANGLE_DEG));
        float _footerHeight;

        struct SignalCacheEntry
        {
            public long Frame;
            public bool Result;
        }

        struct RadarLayout
        {
            public Vector2 ViewCenter;
            public Vector2 ContentCenter;
            public Vector2 PlaneSize;
            public RectangleF ViewportBounds;
            public float RadarScale;
            public float ContentScale;
            public float WorldRange;
        }

        sealed class RadarSceneControl : RectangleControl
        {
            readonly RadarApp _owner;

            public RadarSceneControl(RadarApp owner)
                : base(default(RectangleF))
            {
                _owner = owner;
            }

            protected override void RenderDefault(List<MySprite> sprites)
            {
                _owner.RenderRadar(sprites);
            }
        }

        sealed class RadarFooterControl : Panel
        {
            readonly StackPanelControl _rowsPanel;

            public RadarFooterControl()
                : base(default(RectangleF))
            {
                _rowsPanel = new StackPanelControl();
                _rowsPanel.Gap = 0f;
                AddChild(_rowsPanel);
            }

            public Color FooterBackgroundColor { get; set; }

            public void SetRowHeight(float rowHeight)
            {
                _rowsPanel.RowHeight = rowHeight;
            }

            public void SetRows(IList<ControlTemplate> rows)
            {
                var children = _rowsPanel.VisualChildren;
                bool changed = children == null || rows == null || children.Count != rows.Count;
                if (!changed)
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (!ReferenceEquals(children[i], rows[i]))
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                if (!changed)
                    return;

                _rowsPanel.ClearChildren();
                if (rows == null)
                    return;

                for (int i = 0; i < rows.Count; i++)
                    _rowsPanel.AddChild(rows[i]);
            }

            protected override void ArrangeChildren()
            {
                _rowsPanel.SetRect(Rect);
            }

            protected override void RenderDefault(List<MySprite> sprites)
            {
                var rect = Rect;
                if (rect.Width <= 0f || rect.Height <= 0f)
                    return;

                if (!ThemeChrome.DrawFooterBand(sprites, ResolveTheme(), rect, ResolveColor(ThemeResources.BackgroundColor), ResolveColor(ThemeResources.AccentColor), ResolveColor(ThemeResources.FontColor), LayoutScale))
                {
                    sprites.Add(new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = rect.Center,
                        Size = rect.Size,
                        Color = FooterBackgroundColor,
                        Alignment = TextAlignment.CENTER
                    });
                }

                sprites.Add(MySprite.CreateClipRect(ToClipRectangle(rect)));
                RenderChildren(sprites);
                sprites.Add(MySprite.CreateClearClipRect());
            }
        }

        sealed class SpriteImageControl : RectangleControl
        {
            public SpriteImageControl()
                : base(default(RectangleF))
            {
                SpriteName = "MissingIcon";
                SizeRatio = 0.8f;
            }

            public string SpriteName { get; set; }
            public Color Tint { get; set; }
            public float SizeRatio { get; set; }

            protected override void RenderDefault(List<MySprite> sprites)
            {
                var rect = GetViewBox();
                if (rect.Width <= 0f || rect.Height <= 0f)
                    return;

                float size = Math.Max(
                    1f,
                    Math.Min(rect.Width, rect.Height) * MathHelper.Clamp(SizeRatio, 0.1f, 1f));
                sprites.Add(new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = string.IsNullOrWhiteSpace(SpriteName) ? "MissingIcon": SpriteName,
                    Position = rect.Center,
                    Size = new Vector2(size),
                    Color = Tint,
                    Alignment = TextAlignment.CENTER
                });
            }
        }

        class IconTextRowControl : RectangleControl
        {
            readonly SpriteImageControl _image;
            readonly TextBlock _text;

            public IconTextRowControl()
                : base(default(RectangleF))
            {
                _image = new SpriteImageControl();
                _text = new TextBlock(default(RectangleF))
                {
                    Ellipsize = true,
                    HorizontalAlignment = TextAlignment.LEFT,
                    VerticalAlignment = TextBlockVerticalAlignment.Center
                };

                IconSizePixels = 12f;
                GapPixels = 6f;
                HorizontalPaddingPixels = 6f;

                AddChild(_image);
                AddChild(_text);
            }

            public float IconSizePixels { get; set; }
            public float GapPixels { get; set; }
            public float HorizontalPaddingPixels { get; set; }

            public void SetContent(
                string spriteName,
                string text,
                Color imageTint,
                Color textColor,
                float fontScale,
                float imageSizeRatio)
            {
                _image.SpriteName = spriteName;
                _image.Tint = imageTint;
                _image.SizeRatio = imageSizeRatio;
                _text.Text = text ?? string.Empty;
                _text.TextColor = textColor;
                _text.FontScale = fontScale;
            }

            public override void Arrange(RectangleF bounds)
            {
                base.Arrange(bounds);
                ArrangeChildren();
            }

            protected override void RenderDefault(List<MySprite> sprites)
            {
                ArrangeChildren();
                _image.Render(sprites);
                _text.Render(sprites);
            }

            void ArrangeChildren()
            {
                var rect = GetViewBox();
                if (rect.Width <= 0f || rect.Height <= 0f)
                    return;

                float layoutScale = Math.Max(0.01f, LayoutScale);
                float pad = HorizontalPaddingPixels * layoutScale;
                float gap = GapPixels * layoutScale;
                float iconSize = Math.Max(1f, Math.Min(rect.Height, IconSizePixels * layoutScale));
                var iconRect = new RectangleF(
                    rect.X + pad,
                    rect.Center.Y - iconSize * 0.5f,
                    iconSize,
                    iconSize);
                var textRect = new RectangleF(
                    iconRect.Right + gap,
                    rect.Y,
                    Math.Max(1f, rect.Right - iconRect.Right - gap - pad),
                    rect.Height);

                _image.Arrange(iconRect);
                _text.Arrange(textRect);
            }
        }

        sealed class FooterHeaderControl : RectangleControl
        {
            readonly TextBlock _title;
            readonly TextBlock _range;

            public FooterHeaderControl()
                : base(default(RectangleF))
            {
                _title = new TextBlock(default(RectangleF))
                {
                    Ellipsize = true,
                    FontScale = 0.55f,
                    HorizontalAlignment = TextAlignment.LEFT,
                    VerticalAlignment = TextBlockVerticalAlignment.Center
                };
                _range = new TextBlock(default(RectangleF))
                {
                    Ellipsize = true,
                    FontScale = 0.55f,
                    HorizontalAlignment = TextAlignment.RIGHT,
                    VerticalAlignment = TextBlockVerticalAlignment.Center
                };

                AddChild(_title);
                AddChild(_range);
            }

            public void SetHeader(string title, string range, Color textColor)
            {
                _title.Text = title ?? string.Empty;
                _range.Text = range ?? string.Empty;
                _title.TextColor = textColor;
                _range.TextColor = textColor;
            }

            public override void Arrange(RectangleF bounds)
            {
                base.Arrange(bounds);
                ArrangeChildren();
            }

            protected override void RenderDefault(List<MySprite> sprites)
            {
                ArrangeChildren();
                _title.Render(sprites);
                _range.Render(sprites);
            }

            void ArrangeChildren()
            {
                var rect = GetViewBox();
                if (rect.Width <= 0f || rect.Height <= 0f)
                    return;

                float pad = 6f * Math.Max(0.01f, LayoutScale);
                float gap = 8f * Math.Max(0.01f, LayoutScale);
                float contentWidth = Math.Max(1f, rect.Width - pad * 2f);
                float rangeWidth = Math.Max(1f, contentWidth * 0.45f);
                float titleWidth = Math.Max(1f, contentWidth - rangeWidth - gap);

                _title.Arrange(new RectangleF(rect.X + pad, rect.Y, titleWidth, rect.Height));
                _range.Arrange(new RectangleF(
                    rect.Right - pad - rangeWidth,
                    rect.Y,
                    rangeWidth,
                    rect.Height));
            }
        }

        readonly IAppHost _host;
        IMyCubeBlock Block => _host.Block;
        Sandbox.ModAPI.Ingame.IMyTextSurface Surface => _host.Surface;
        RectangleF ViewBox => _host.ViewBox;
        float Scale => _host.ConfiguredScale;
        float FontScale => _host.FontScale;
        float LayoutScale => Scale * FontScale;
        Color ForegroundColor => _host.ForegroundColor;
        Color BackgroundColor => _host.BackgroundColor;
        public override IReadOnlyList<Control> VisualChildren => _children;

        struct TargetInfo
        {
            public Vector3 Position;
            public Color IconColor;
            public string IconTexture;
            public Color ElevationColor;
            public bool TargetLock;
            public float TargetLockPercent;
            public bool AbovePlane;
            public bool OutOfRange;
            public MyRelationsBetweenPlayerAndBlock Relationship;
        }


        public RadarApp(IAppHost host)
            : base(host)
        {
            _host = host;
            _cameraControl = AddLogicalChild(new Panel(default(RectangleF)));

            _radarSceneControl = new RadarSceneControl(this);
            _cameraControl.AddChild(_radarSceneControl);

            _footerControl = AddLogicalChild(new RadarFooterControl());
            _footerHeaderControl = new FooterHeaderControl();
            _footerWarningControl = new IconTextRowControl
            {
                IconSizePixels = FOOTER_WARNING_ICON_SIZE_PX,
                GapPixels = FOOTER_WARNING_ICON_GAP_PX
            };

            _children.Add(_cameraControl);
            _children.Add(_footerControl);
            RefreshLocTexts();
        }

        public override void Update()
        {
            _maxRange = GetConfiguredRange();
            ApplyRadarConfig();
            if (_collectTick++ % CONTACT_COLLECT_EVERY == 0)
            {
                CollectContacts();
                PurgeStaleContacts();
            }
            BuildSortedContacts();
            UpdateFooterHeights();
            UpdateFooterControls();
        }

        public override void LayoutChanged()
        {
            RefreshLocTexts();
            UpdateCameraControlBounds();
            UpdateFooterControlBounds();
        }

        void RefreshLocTexts()
        {
            _longRangeWarningText = LocHelper.GetLoc(LONG_RANGE_WARNING_KEY);
            _detectedEntitiesText = LocHelper.GetLoc(MOD_PREFIX + "Radar_DetectedEntities");
            _rangeLabelText = string.Format(
                FormatingHelper.Culture,
                LocHelper.GetLoc(MOD_PREFIX + "Common_Label_WithColon"),
                MyTexts.GetString("BlockPropertyTitle_OreDetectorRange")) + " ";
            _footerRangeTextRange = float.NaN;
        }

        public override List<MySprite> GetSprites()
        {
            _backgroundSprites.Clear();
            if (IsMinimal)
            {
                DrawMinimal(_backgroundSprites);
                return _backgroundSprites;
            }
            if (IsCompact)
            {
                DrawCompact(_backgroundSprites);
                return _backgroundSprites;
            }
            UpdateCameraControlBounds();
            UpdateFooterControlBounds();
            _cameraControl.Render(_backgroundSprites);
            _footerControl.Render(_backgroundSprites);
            return _backgroundSprites;
        }

        void CollectContacts()
        {
            _seenThisFrame.Clear();
            _maxRange = GetConfiguredRange();

            CollectEntityContacts();

            foreach (var kv in _contacts)
            {
                if (_seenThisFrame.Contains(kv.Key))
                    kv.Value.MissedFrames = 0;
                else
                    kv.Value.MissedFrames++;
            }
        }

        void CollectEntityContacts()
        {
            try
            {
                var session = MyAPIGateway.Session;
                if (session == null) return;

                var shipPos = Block.WorldMatrix.Translation;
                float range = _maxRange;
                var ownGrid = Block.CubeGrid;
                if (ownGrid == null) return;

                long ownGridId = ownGrid.EntityId;
                long ownGridOwnerId = GetPrimaryGridOwner(ownGrid);
                long lockedTargetEntityId;
                float lockedTargetPercent;
                long lockedTargetGridId = GetLockedTargetGridId(out lockedTargetEntityId, out lockedTargetPercent);
                var normalizedLockPercent = NormalizeLockPercent(lockedTargetPercent);

                _tempEntities.Clear();
                var sphere = new BoundingSphereD(shipPos, range);
                var entitiesInSphere = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                if (entitiesInSphere != null)
                {
                    _tempEntities.AddRange(entitiesInSphere);
                    entitiesInSphere.Clear();
                }

                _processedGroupGridIds.Clear();
                foreach (var entity in _tempEntities)
                {
                    if (TryCollectSignalProxyContact(entity, shipPos, ownGrid, ownGridOwnerId, lockedTargetGridId,
                            lockedTargetEntityId, normalizedLockPercent))
                        continue;

                    var grid = entity as IMyCubeGrid;
                    if (grid == null || grid.Physics == null)
                        continue;
                    if (grid.EntityId == ownGridId)
                        continue;

                    if (_processedGroupGridIds.Contains(grid.EntityId))
                        continue;

                    _tempGroupGrids.Clear();
                    try
                    {
                        MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Logical, _tempGroupGrids);
                    }
                    catch (Exception e)
                    {
                        ErrorHandlerHelper.LogError(e, this);
                    }

                    if (_tempGroupGrids.Count == 0)
                        _tempGroupGrids.Add(grid);

                    IMyCubeGrid selectedGrid = null;
                    double selectedScore = double.MinValue;
                    bool ownGroup = false;
                    bool groupHasLockedTarget = false;

                    foreach (var groupGrid in _tempGroupGrids)
                    {
                        _processedGroupGridIds.Add(groupGrid.EntityId);
                        if (lockedTargetGridId != 0 && groupGrid.EntityId == lockedTargetGridId)
                            groupHasLockedTarget = true;

                        if (groupGrid.EntityId == ownGridId)
                        {
                            ownGroup = true;
                            continue;
                        }

                        if (groupGrid.Physics == null) continue;

                        var groupPos = groupGrid.WorldMatrix.Translation;
                        if (Vector3D.Distance(groupPos, shipPos) > range) continue;

                        var score = GetGridVolumeScore(groupGrid);
                        if (score > selectedScore)
                        {
                            selectedScore = score;
                            selectedGrid = groupGrid;
                        }
                    }

                    if (ownGroup || selectedGrid == null)
                        continue;

                    var pos = selectedGrid.WorldMatrix.Translation;
                    if (Vector3D.Distance(pos, shipPos) > DEFAULT_RANGE &&
                        !GridGroupHasLongRangeSignalCached(selectedGrid.EntityId, _tempGroupGrids, shipPos, ownGrid))
                        continue;

                    long entityId = selectedGrid.EntityId;
                    if (!_seenThisFrame.Add(entityId)) continue;

                    ContactRecord rec;
                    if (!_contacts.TryGetValue(entityId, out rec))
                    {
                        rec = new ContactRecord { EntityId = entityId };
                        _contacts[entityId] = rec;
                    }

                    long selectedOwnerId = GetPrimaryGridOwner(selectedGrid);
                    rec.Name = selectedGrid.DisplayName ?? string.Empty;
                    rec.Relationship = GetOwnerRelationship(selectedOwnerId, ownGridOwnerId);
                    rec.IconTexture = GetFactionIconForOwner(selectedOwnerId);
                    rec.WorldPosition = pos;
                    rec.IsTargeted = groupHasLockedTarget;
                    rec.TargetLockPercent = groupHasLockedTarget ? normalizedLockPercent : 0f;
                    rec.MissedFrames = 0;
                }
            }
            catch (Exception ex)
            {
                ErrorHandlerHelper.LogError(ex, this);
            }
        }

        bool TryCollectSignalProxyContact(
            IMyEntity entity,
            Vector3D receiverPosition,
            IMyCubeGrid receiverGrid,
            long receiverOwnerId,
            long lockedTargetGridId,
            long lockedTargetEntityId,
            float normalizedLockPercent)
        {
            if (entity == null || receiverGrid == null)
                return false;

            var block = entity as IMyCubeBlock;
            var signalGrid = block?.CubeGrid;
            if (signalGrid != null && signalGrid.EntityId == receiverGrid.EntityId)
                return false;

            if (!IsLongRangeSignalEntityInRange(entity, receiverPosition, receiverGrid))
                return false;

            long entityId = signalGrid?.EntityId ?? entity.EntityId;
            if (_seenThisFrame.Contains(entityId))
                return true;

            var pos = entity.WorldMatrix.Translation;
            if (Vector3D.Distance(pos, receiverPosition) > _maxRange)
                return true;

            _seenThisFrame.Add(entityId);

            long ownerId = signalGrid != null
                ? GetPrimaryGridOwner(signalGrid)
                : (block as IMyTerminalBlock)?.OwnerId ?? 0L;

            ContactRecord rec;
            if (!_contacts.TryGetValue(entityId, out rec))
            {
                rec = new ContactRecord { EntityId = entityId };
                _contacts[entityId] = rec;
            }

            rec.Name = GetSignalProxyName(entity, block, signalGrid);
            rec.Relationship = GetOwnerRelationship(ownerId, receiverOwnerId);
            rec.IconTexture = GetFactionIconForOwner(ownerId);
            rec.WorldPosition = pos;
            rec.IsTargeted = entity.EntityId == lockedTargetEntityId ||
                             (signalGrid != null && signalGrid.EntityId == lockedTargetGridId);
            rec.TargetLockPercent = rec.IsTargeted ? normalizedLockPercent : 0f;
            rec.MissedFrames = 0;
            return true;
        }

        static string GetSignalProxyName(IMyEntity entity, IMyCubeBlock block, IMyCubeGrid signalGrid)
        {
            var radio = entity as IMyRadioAntenna;
            if (radio != null && !string.IsNullOrWhiteSpace(radio.HudText))
                return radio.HudText;

            var beacon = entity as IMyBeacon;
            if (beacon != null && !string.IsNullOrWhiteSpace(beacon.HudText))
                return beacon.HudText;

            var terminalBlock = block as IMyTerminalBlock;
            if (terminalBlock != null && !string.IsNullOrWhiteSpace(terminalBlock.CustomName))
                return terminalBlock.CustomName;

            if (signalGrid != null && !string.IsNullOrWhiteSpace(signalGrid.DisplayName))
                return signalGrid.DisplayName;

            return entity.DisplayName ?? string.Empty;
        }

        static bool IsLongRangeSignalEntityInRange(
            IMyEntity entity,
            Vector3D receiverPosition,
            IMyCubeGrid receiverGrid)
        {
            var functional = entity as IMyFunctionalBlock;
            if (functional == null || !functional.IsFunctional || !functional.Enabled)
                return false;

            var radio = entity as IMyRadioAntenna;
            if (radio != null)
                return radio.IsBroadcasting && BroadcastRangeReaches(radio.WorldMatrix.Translation, radio.Radius,
                    receiverPosition);

            var beacon = entity as IMyBeacon;
            if (beacon != null)
                return BroadcastRangeReaches(beacon.WorldMatrix.Translation, beacon.Radius, receiverPosition);

            var laser = entity as IMyLaserAntenna;
            if (laser == null || laser.Other == null || receiverGrid == null)
                return false;

            return laser.Other.CubeGrid != null &&
                   laser.Other.CubeGrid.EntityId == receiverGrid.EntityId &&
                   laser.IsInRange(laser.Other);
        }

        void ApplyRadarConfig()
        {
            _cameraPanMeters = ClampCameraPan(GetConfigCameraPan(RadarComponent));
            _cameraZoomScale = ClampCameraZoomScale(RadarComponent.CameraZoomScale);
        }

        static Vector2 GetConfigCameraPan(RadarConfigComponent config)
        {
            return new Vector2(
                (float)config.CameraPanX,
                (float)config.CameraPanY);
        }

        static float ClampCameraZoomScale(float zoomScale)
        {
            if (!SurfaceScriptBase.IsFinite(zoomScale) || zoomScale <= 0f)
                zoomScale = CAMERA_DEFAULT_ZOOM_SCALE;

            return MathHelper.Clamp(zoomScale, CAMERA_MIN_ZOOM_SCALE, CAMERA_MAX_ZOOM_SCALE);
        }

        float GetConfiguredRange()
        {
            return SliderRadarRange.GetRangeMeters(RadarComponent.RangeScale);
        }

        float GetWorldRange()
        {
            return SurfaceScriptBase.IsFinite(_maxRange) && _maxRange > 0f ? _maxRange : DEFAULT_RANGE;
        }

        // Varrer todos os blocos de um grid distante é caro; o resultado vale por 2 s por grupo.
        bool GridGroupHasLongRangeSignalCached(long groupKey, List<IMyCubeGrid> grids, Vector3D receiverPosition, IMyCubeGrid receiverGrid)
        {
            var frame = MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
            SignalCacheEntry entry;
            if (_longRangeSignalCache.TryGetValue(groupKey, out entry) && frame - entry.Frame < LONG_RANGE_SIGNAL_CACHE_FRAMES)
                return entry.Result;

            bool result = false;
            for (int i = 0; i < grids.Count && !result; i++)
                result = GridHasLongRangeSignal(grids[i], receiverPosition, receiverGrid);

            _longRangeSignalCache[groupKey] = new SignalCacheEntry { Frame = frame, Result = result };
            return result;
        }

        bool GridHasLongRangeSignal(IMyCubeGrid grid, Vector3D receiverPosition, IMyCubeGrid receiverGrid)
        {
            if (grid == null)
                return false;

            try
            {
                _tempSlimBlocks.Clear();
                grid.GetBlocks(_tempSlimBlocks,
                    slimBlock => IsLongRangeSignalEntityInRange(slimBlock?.FatBlock, receiverPosition, receiverGrid));
                return _tempSlimBlocks.Count > 0;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
                return false;
            }
            finally
            {
                _tempSlimBlocks.Clear();
            }
        }

        static bool BroadcastRangeReaches(Vector3D broadcastPosition, float radius, Vector3D receiverPosition)
        {
            if (radius <= 0f)
                return false;

            double radiusSq = radius;
            radiusSq *= radiusSq;
            return Vector3D.DistanceSquared(broadcastPosition, receiverPosition) <= radiusSq;
        }

        double GetGridVolumeScore(IMyCubeGrid grid)
        {
            var aabb = grid.WorldAABB;
            var size = aabb.Max - aabb.Min;
            return size.X * size.Y * size.Z;
        }

        long GetPrimaryGridOwner(IMyCubeGrid grid)
        {
            var owners = grid.BigOwners;
            return owners != null && owners.Count > 0 ? owners[0] : 0;
        }

        long GetLockedTargetGridId(out long targetEntityId, out float targetLockPercent)
        {
            targetEntityId = 0;
            targetLockPercent = 0f;

            long lockedEntityId;
            float lockedPercent;

            var player = MyAPIGateway.Session?.LocalHumanPlayer;
            RefreshCachedCharacterTargetLocking(player);

            var lockedGridId = GetLockedTargetGridIdFromTargetLockingComponent(_cachedCharacterTargetLocking,
                out lockedEntityId,
                out lockedPercent);
            if (lockedEntityId != 0)
            {
                targetEntityId = lockedEntityId;
                targetLockPercent = lockedPercent;
            }

            if (lockedGridId != 0)
                return lockedGridId;

            var myGrid = Block.CubeGrid as Sandbox.Game.Entities.MyCubeGrid;
            _lockCandidates[0] = player?.Character;
            _lockCandidates[1] = player?.Controller?.ControlledEntity?.Entity;
            _lockCandidates[2] = MyAPIGateway.Session?.CameraController?.Entity;
            _lockCandidates[3] = Block as IMyCockpit;
            _lockCandidates[4] = myGrid?.MainCockpit as IMyCockpit;
            _lockCandidates[5] = myGrid;

            for (int i = 0; i < _lockCandidates.Length; i++)
            {
                lockedGridId = GetLockedTargetGridIdFromEntity(_lockCandidates[i], out lockedEntityId, out lockedPercent);
                if (lockedEntityId != 0 && targetEntityId == 0)
                {
                    targetEntityId = lockedEntityId;
                    targetLockPercent = lockedPercent;
                }

                if (lockedGridId != 0)
                    break;
            }

            Array.Clear(_lockCandidates, 0, _lockCandidates.Length);
            return lockedGridId;
        }

        void RefreshCachedCharacterTargetLocking(IMyPlayer player)
        {
            var characterEntity = player?.Character as IMyEntity;
            var currentCharacterId = characterEntity?.EntityId ?? 0;
            if (currentCharacterId == _cachedCharacterId)
                return;

            _cachedCharacterId = currentCharacterId;
            _cachedCharacterTargetLocking = characterEntity?.Components?
                .Get<Sandbox.Game.EntityComponents.MyTargetLockingComponent>();
        }

        static long GetLockedTargetGridIdFromEntity(IMyEntity entity, out long targetEntityId,
            out float targetLockPercent)
        {
            targetEntityId = 0;
            targetLockPercent = 0f;
            if (entity == null || entity.Components == null)
                return 0;

            var targetLockingBlock =
                entity.Components.Get<Sandbox.Game.EntityComponents.MyTargetLockingBlockComponent>();
            if (targetLockingBlock != null)
            {
                var blockTargetEntity = targetLockingBlock.TargetEntity;
                if (blockTargetEntity != null)
                {
                    targetEntityId = blockTargetEntity.EntityId;
                    targetLockPercent = targetLockingBlock.LockingProgressPercent;
                    var blockTargetGrid = blockTargetEntity as IMyCubeGrid;
                    if (blockTargetGrid != null)
                        return blockTargetGrid.EntityId;
                }
            }

            long lockedEntityId;
            float lockedPercent;
            long lockedGridId = GetLockedTargetGridIdFromTargetLockingComponent(
                entity.Components.Get<Sandbox.Game.EntityComponents.MyTargetLockingComponent>(),
                out lockedEntityId,
                out lockedPercent);
            if (lockedEntityId != 0)
            {
                targetEntityId = lockedEntityId;
                targetLockPercent = lockedPercent;
            }

            return lockedGridId;
        }

        static long GetLockedTargetGridIdFromTargetLockingComponent(
            Sandbox.Game.EntityComponents.MyTargetLockingComponent targetLocking,
            out long targetEntityId,
            out float targetLockPercent)
        {
            targetEntityId = 0;
            targetLockPercent = 0f;
            if (targetLocking == null)
                return 0;

            var targetEntity = targetLocking.TargetEntity;
            if (targetEntity == null)
                return 0;

            targetEntityId = targetEntity.EntityId;
            targetLockPercent = targetLocking.LockingProgressPercent;
            var targetGrid = targetEntity as IMyCubeGrid;
            return targetGrid?.EntityId ?? 0;
        }

        static float NormalizeLockPercent(float rawPercent)
        {
            if (rawPercent > 1f)
                rawPercent *= 0.01f;
            return MathHelper.Clamp(rawPercent, 0f, 1f);
        }

        static MyRelationsBetweenPlayerAndBlock GetOwnerRelationship(long ownerId, long ownGridOwnerId)
        {
            if (ownerId == 0)
                return MyRelationsBetweenPlayerAndBlock.Neutral;

            if (ownGridOwnerId != 0 && ownerId == ownGridOwnerId)
                return MyRelationsBetweenPlayerAndBlock.FactionShare; // Allied

            if (ownGridOwnerId == 0)
                return MyRelationsBetweenPlayerAndBlock.Neutral;

            switch (Sandbox.Game.Entities.MyIDModule.GetRelationPlayerPlayer(ownGridOwnerId, ownerId))
            {
                case VRage.Game.Entity.MyRelationsBetweenPlayers.Enemies:
                    return MyRelationsBetweenPlayerAndBlock.Enemies;
                case VRage.Game.Entity.MyRelationsBetweenPlayers.Allies:
                    return MyRelationsBetweenPlayerAndBlock.FactionShare;
                default:
                    return MyRelationsBetweenPlayerAndBlock.Neutral;
            }
        }

        static string GetFactionIconForOwner(long ownerId)
        {
            if (ownerId == 0)
                return null;

            var icon = MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(ownerId)?.FactionIcon?.ToString();
            return string.IsNullOrEmpty(icon) ? null : icon;
        }


        void PurgeStaleContacts()
        {
            _toRemove.Clear();
            foreach (var kv in _contacts)
                if (kv.Value.MissedFrames > CONTACT_TIMEOUT)
                    _toRemove.Add(kv.Key);

            foreach (var t in _toRemove)
                _contacts.Remove(t);

            if (_longRangeSignalCache.Count == 0)
                return;

            _toRemove.Clear();
            var frame = MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
            foreach (var kv in _longRangeSignalCache)
                if (frame - kv.Value.Frame > LONG_RANGE_SIGNAL_CACHE_FRAMES * 10)
                    _toRemove.Add(kv.Key);
            foreach (var key in _toRemove)
                _longRangeSignalCache.Remove(key);
        }


        void RenderRadar(List<MySprite> sprites)
        {
            var lineColor = ForegroundColor;
            var backColor = BackgroundColor;
            var warnColor = GetWarningColor();
            var errColor = GetErrorColor();
            var allyColor = GetHeaderColor();
            var planeColor = new Color(ForegroundColor, 0.12f);

            if (!_hasLayout)
            {
                _cameraControl.SetVisible(false);
                return;
            }

            var layout = _layout;
            sprites.Add(MySprite.CreateClipRect(ToClipRectangle(layout.ViewportBounds)));

            DrawRadarPlaneBackground(
                sprites,
                layout.ContentCenter,
                layout.PlaneSize,
                layout.WorldRange,
                layout.ContentScale,
                lineColor,
                backColor,
                planeColor);
            DrawRadarPlane(sprites, layout.ContentCenter, layout.PlaneSize, layout.ContentScale, lineColor);

            BuildTargetLayers(errColor, warnColor, allyColor, layout.WorldRange);
            foreach (var t in _targetsBelowPlane)
                DrawTargetIcon(sprites, layout.ContentCenter, layout.PlaneSize, t, layout.ContentScale);

            foreach (var t in _targetsAbovePlane)
                DrawTargetIcon(sprites, layout.ContentCenter, layout.PlaneSize, t, layout.ContentScale);

            sprites.Add(MySprite.CreateClearClipRect());
        }

        bool TryCalculateRadarLayout(out RadarLayout layout)
        {
            layout = default(RadarLayout);

            float radarScale = Scale;
            float cameraZoomScale = ClampCameraZoomScale(_cameraZoomScale);
            float cappedScale = GetRadarCappedScale();
            float margin = RADAR_MARGIN_PX * cappedScale;
            float worldRange = GetWorldRange();
            RectangleF area = ViewBox;
            float areaTop = area.Y + margin;
            float areaBottom = area.Bottom - margin;
            float areaHeight = areaBottom - areaTop;
            float areaWidth = area.Width - margin * 2f;
            if (areaWidth <= 0f || areaHeight <= 0f)
                return false;

            Vector2 viewportCropped = new Vector2(
                areaWidth,
                areaHeight - (RANGE_TEXT_SIZE * SIZE_TO_PX + _borderPadding.Y) * cappedScale);
            if (viewportCropped.X <= 0f || viewportCropped.Y <= 0f)
                return false;

            float sideLength = viewportCropped.X * _radarProjectionCos < viewportCropped.Y
                ? viewportCropped.X
                : viewportCropped.Y / _radarProjectionCos;

            Vector2 viewCenter = new Vector2(area.Center.X, areaTop + viewportCropped.Y * 0.5f);
            Vector2 planeSize = new Vector2(sideLength, sideLength * _radarProjectionCos) *
                                GeneralComponent.GetScale() *
                                cameraZoomScale;
            if (planeSize.X <= 0f || planeSize.Y <= 0f)
                return false;

            Vector2 cameraOffsetPixels = ProjectCameraPanToScreen(planeSize, worldRange);

            layout.ViewCenter = viewCenter;
            layout.ContentCenter = viewCenter - cameraOffsetPixels;
            layout.PlaneSize = planeSize;
            layout.ViewportBounds = new RectangleF(
                area.X,
                area.Y,
                area.Width,
                Math.Max(1f, area.Height));
            layout.RadarScale = radarScale;
            layout.ContentScale = radarScale * cameraZoomScale;
            layout.WorldRange = worldRange;
            return true;
        }

        Vector2 ProjectCameraPanToScreen(Vector2 radarPlaneSize, float worldRange)
        {
            if (worldRange <= 0f)
                return Vector2.Zero;

            float scale = radarPlaneSize.X * 0.5f / worldRange;
            return new Vector2(
                _cameraPanMeters.X * scale,
                _cameraPanMeters.Y * scale * _radarProjectionCos);
        }

        void UpdateCameraControlBounds()
        {
            _hasLayout = TryCalculateRadarLayout(out _layout);
            _cameraControl.SetVisible(_hasLayout);
            if (!_hasLayout)
                return;

            _cameraControl.SetRect(ViewBox);
            _radarSceneControl.SetRect(ViewBox);
        }

        bool ShouldShowLongRangeWarning()
        {
            return _maxRange > DEFAULT_RANGE + 0.5f;
        }

        void BuildTargetLayers(Color errColor, Color warnColor, Color allyColor, float worldRange)
        {
            _targetsBelowPlane.Clear();
            _targetsAbovePlane.Clear();

            MatrixD gridMatrix;
            if (!TryGetReferenceWorldMatrix(out gridMatrix))
                gridMatrix = Block.WorldMatrix;
            int shown = Math.Min(_sortedContacts.Count, MAX_CONTACTS);
            for (int i = 0; i < shown; i++)
            {
                TargetInfo info;
                if (!TryBuildTargetInfo(_sortedContacts[i], gridMatrix, errColor, warnColor, allyColor, worldRange, out info))
                    continue;

                if (info.AbovePlane) _targetsAbovePlane.Add(info);
                else _targetsBelowPlane.Add(info);
            }

            _targetsBelowPlane.Sort((a, b) => a.Position.Y.CompareTo(b.Position.Y));
            _targetsAbovePlane.Sort((a, b) => a.Position.Y.CompareTo(b.Position.Y));
        }

        bool TryGetReferenceWorldMatrix(out MatrixD world)
        {
            return _host.TryGetReferenceWorldMatrix(InteractionComponent.ReferenceMode, out world, true);
        }

        bool TryBuildTargetInfo(ContactRecord contact, MatrixD gridMatrix, Color errColor, Color warnColor,
            Color allyColor,
            float worldRange,
            out TargetInfo targetInfo)
        {
            if (worldRange <= 0f)
            {
                targetInfo = default(TargetInfo);
                return false;
            }

            var transformedDirection =
                Vector3D.TransformNormal(contact.WorldPosition - gridMatrix.Translation, MatrixD.Transpose(gridMatrix));
            var position = new Vector3((float)transformedDirection.X, (float)transformedDirection.Z,
                (float)transformedDirection.Y);

            bool inRange = position.X * position.X + position.Y * position.Y < worldRange * worldRange;
            bool above = position.Z >= 0f;

            if (inRange)
            {
                position /= worldRange;
            }
            else
            {
                Vector3 directionFlat = position;
                directionFlat.Z = 0f;
                if (directionFlat.LengthSquared() < 1e-6f)
                {
                    targetInfo = default(TargetInfo);
                    return false;
                }

                position = Vector3.Normalize(directionFlat);
            }

            Color iconColor = ContactColor(contact, errColor, warnColor, allyColor);
            targetInfo = new TargetInfo
            {
                Position = position,
                IconColor = iconColor,
                IconTexture = contact.IconTexture,
                ElevationColor = new Color(iconColor, 0.7f),
                TargetLock = contact.IsTargeted,
                TargetLockPercent = contact.TargetLockPercent,
                AbovePlane = above,
                OutOfRange = !inRange,
                Relationship = contact.Relationship
            };
            return true;
        }

        void DrawRadarPlaneBackground(List<MySprite> sprites, Vector2 centerPos, Vector2 radarPlaneSize,
            float worldRange, float scale,
            Color lineColor, Color backColor, Color planeColor)
        {
            float lineWidth = RADAR_RANGE_LINE_WIDTH * scale;
            AddTexture(sprites, "Circle", centerPos, radarPlaneSize, lineColor);
            AddTexture(sprites, "Circle", centerPos, radarPlaneSize - lineWidth * Vector2.One, backColor);

            DrawRangeRing(sprites, centerPos, radarPlaneSize, worldRange, RING_3_M, lineWidth, lineColor, backColor);
            DrawRangeRing(sprites, centerPos, radarPlaneSize, worldRange, RING_2_M, lineWidth, lineColor, backColor);
            DrawRangeRing(sprites, centerPos, radarPlaneSize, worldRange, RING_1_M, lineWidth, lineColor, backColor);

            AddTexture(sprites, "Circle", centerPos, radarPlaneSize, planeColor);
        }

        void DrawRangeRing(List<MySprite> sprites, Vector2 centerPos, Vector2 radarPlaneSize, float worldRange,
            float ringDistance, float lineWidth, Color lineColor, Color backColor)
        {
            if (worldRange <= 0f || ringDistance <= 0f)
                return;

            float ratio = ringDistance / worldRange;
            if (ratio <= 0f || ratio >= 1f)
                return;

            Vector2 ringSize = radarPlaneSize * ratio;
            if (ringSize.X <= lineWidth || ringSize.Y <= lineWidth)
                return;

            AddTexture(sprites, "Circle", centerPos, ringSize, new Color(lineColor, 0.65f));
            AddTexture(sprites, "Circle", centerPos, ringSize - lineWidth * Vector2.One, backColor);
        }

        void DrawRadarPlane(List<MySprite> sprites, Vector2 radarScreenCenter, Vector2 radarPlaneSize, float scale,
            Color lineColor)
        {
            var iconSize = _shipIconSize * scale;
            AddTexture(sprites, "Triangle", radarScreenCenter + new Vector2(0f, -0.2f * iconSize.Y), iconSize,
                lineColor);

            float lineWidth = QUADRANT_LINE_WIDTH * scale;
            Color quadrantLineColor = new Color(lineColor, 0.5f);
            Vector2 halfHorizontal = new Vector2(radarPlaneSize.X * 0.5f, 0f);
            Vector2 halfVertical = new Vector2(0f, radarPlaneSize.Y * 0.5f);
            Vector2 horizontalInner = halfHorizontal * (1f - QUADRANT_LINE_COVERAGE_PER_SIDE);
            Vector2 verticalInner = halfVertical * (1f - QUADRANT_LINE_COVERAGE_PER_SIDE);
            LineRenderer.DrawLine(sprites, radarScreenCenter - halfHorizontal, radarScreenCenter - horizontalInner, lineWidth,
                quadrantLineColor);
            LineRenderer.DrawLine(sprites, radarScreenCenter + horizontalInner, radarScreenCenter + halfHorizontal, lineWidth,
                quadrantLineColor);
            LineRenderer.DrawLine(sprites, radarScreenCenter - halfVertical, radarScreenCenter - verticalInner, lineWidth,
                quadrantLineColor);
            LineRenderer.DrawLine(sprites, radarScreenCenter + verticalInner, radarScreenCenter + halfVertical, lineWidth,
                quadrantLineColor);

            float angleTextSize = QUADRANT_LABEL_TEXT_SIZE * scale * FontScale;
            float labelMargin = QUADRANT_LABEL_MARGIN_PX * scale;
            Color angleColor = new Color(ForegroundColor, 0.72f);
            float angleLabelHalfHeight = FormatingHelper.GetSizeInPixel("180", "Debug", angleTextSize, Surface).Y * 0.5f;
            Vector2 angleLabelOffset = new Vector2(0f, -angleLabelHalfHeight);
            float sideLabelInwardOffset = labelMargin * 0.5f;
            sprites.Add(new MySprite(
                SpriteType.TEXT,
"0º",
                radarScreenCenter - halfVertical - new Vector2(0f, labelMargin) + angleLabelOffset,
                null,
                angleColor,
                TextFont,
                TextAlignment.CENTER,
                angleTextSize));
            sprites.Add(new MySprite(
                SpriteType.TEXT,
"90º",
                radarScreenCenter + halfHorizontal + new Vector2(labelMargin - sideLabelInwardOffset, 0f) +
                angleLabelOffset,
                null,
                angleColor,
                TextFont,
                TextAlignment.LEFT,
                angleTextSize));
            sprites.Add(new MySprite(
                SpriteType.TEXT,
"180º",
                radarScreenCenter + halfVertical + new Vector2(0f, labelMargin) + angleLabelOffset,
                null,
                angleColor,
                TextFont,
                TextAlignment.CENTER,
                angleTextSize));
            sprites.Add(new MySprite(
                SpriteType.TEXT,
"270º",
                radarScreenCenter - halfHorizontal - new Vector2(labelMargin - sideLabelInwardOffset, 0f) +
                angleLabelOffset,
                null,
                angleColor,
                TextFont,
                TextAlignment.RIGHT,
                angleTextSize));
        }

        void DrawTargetIcon(List<MySprite> sprites, Vector2 screenCenter, Vector2 radarPlaneSize, TargetInfo targetInfo,
            float scale)
        {
            Vector3 targetPosPixels = targetInfo.Position * new Vector3(1f, _radarProjectionCos, _radarProjectionSin) *
                                      radarPlaneSize.X * 0.5f;
            var targetPosPlane = new Vector2(targetPosPixels.X, targetPosPixels.Y);
            Vector2 iconPos = targetPosPlane - targetPosPixels.Z * Vector2.UnitY;

            RoundVector2(ref iconPos);
            RoundVector2(ref targetPosPlane);

            // ponytail: profundidade pela altura no plano; fundo fica a 0,75× da frente.
            scale *= 0.875f + 0.125f * MathHelper.Clamp(targetInfo.Position.Y, -1f, 1f);
            float elevationLineWidth = Math.Max(1f, TGT_ELEVATION_LINE_WIDTH * scale);
            var elevationSprite = new MySprite(SpriteType.TEXTURE, "SquareSimple",
                screenCenter + (iconPos + targetPosPlane) * 0.5f,
                new Vector2(elevationLineWidth, Math.Abs(targetPosPixels.Z)),
                targetInfo.ElevationColor);
            RoundVector2(ref elevationSprite.Position);
            RoundVector2(ref elevationSprite.Size);

            Vector2 iconDrawPos = screenCenter + iconPos;
            RoundVector2(ref iconDrawPos);

            float shadowThickness = 2f * (float)Math.Max(1f, Math.Round(scale * 4f));
            float shadowScale = scale * (_tgtIconSize.X + shadowThickness) / _tgtIconSize.X;

            Vector2 iconSize = _tgtIconSize * scale;
            iconSize.Y *= _radarProjectionCos;
            var projectedIconSprite = new MySprite(SpriteType.TEXTURE, "Circle",
                screenCenter + targetPosPlane, iconSize, targetInfo.ElevationColor);
            RoundVector2(ref projectedIconSprite.Position);

            bool showProjectedElevation = Math.Abs(iconPos.Y - targetPosPlane.Y) > iconSize.Y;
            if (targetInfo.AbovePlane)
            {
                if (showProjectedElevation)
                {
                    sprites.Add(projectedIconSprite);
                    sprites.Add(elevationSprite);
                }

                DrawContactIcon(sprites, iconDrawPos, targetInfo, shadowScale, 0f);
            }
            else
            {
                if (showProjectedElevation) sprites.Add(elevationSprite);
                DrawContactIcon(sprites, iconDrawPos, targetInfo, shadowScale, 0f);
                if (showProjectedElevation) sprites.Add(projectedIconSprite);
            }

            if (targetInfo.TargetLock)
            {
                Vector2 targetBoxSize = (_tgtIconSize + 20f) * scale;
                if (targetInfo.TargetLockPercent >= 0.999f)
                    DrawRotatedSquareOutline(sprites, iconDrawPos, (_tgtIconSize.X + 8f) * scale, 2f * scale,
                        targetInfo.IconColor, MathHelper.PiOver4);
                DrawBoxCorners(sprites, targetBoxSize, iconDrawPos, 12f * scale, 4f * scale, targetInfo.IconColor);
            }
        }


        static void DrawRotatedSquareOutline(List<MySprite> sprites, Vector2 center, float size, float thickness,
            Color color, float rotation)
        {
            float half = size * 0.5f;
            var p1 = RotateVector(new Vector2(-half, -half), rotation) + center;
            var p2 = RotateVector(new Vector2(half, -half), rotation) + center;
            var p3 = RotateVector(new Vector2(half, half), rotation) + center;
            var p4 = RotateVector(new Vector2(-half, half), rotation) + center;

            LineRenderer.DrawLine(sprites, p1, p2, thickness, color, thickness * 0.5f);
            LineRenderer.DrawLine(sprites, p2, p3, thickness, color, thickness * 0.5f);
            LineRenderer.DrawLine(sprites, p3, p4, thickness, color, thickness * 0.5f);
            LineRenderer.DrawLine(sprites, p4, p1, thickness, color, thickness * 0.5f);
        }

        static Vector2 RotateVector(Vector2 v, float radians)
        {
            float c = (float)Math.Cos(radians);
            float s = (float)Math.Sin(radians);
            return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }

        static void DrawBoxCorners(List<MySprite> sprites, Vector2 boxSize, Vector2 centerPos, float lineLength,
            float lineWidth, Color color)
        {
            var horizontalSize = new Vector2(lineLength, lineWidth);
            var verticalSize = new Vector2(lineWidth, lineLength);
            Vector2 horizontalOffset = 0.5f * horizontalSize;
            Vector2 verticalOffset = 0.5f * verticalSize;
            Vector2 boxHalfSize = 0.5f * boxSize;
            Vector2 boxTopLeft = centerPos - boxHalfSize;
            Vector2 boxBottomRight = centerPos + boxHalfSize;
            Vector2 boxTopRight = centerPos + new Vector2(boxHalfSize.X, -boxHalfSize.Y);
            Vector2 boxBottomLeft = centerPos + new Vector2(-boxHalfSize.X, boxHalfSize.Y);

            AddTexture(sprites, "SquareSimple", boxTopLeft + horizontalOffset, horizontalSize, color);
            AddTexture(sprites, "SquareSimple", boxTopLeft + verticalOffset, verticalSize, color);
            AddTexture(sprites, "SquareSimple", boxTopRight + new Vector2(-horizontalOffset.X, horizontalOffset.Y),
                horizontalSize, color);
            AddTexture(sprites, "SquareSimple", boxTopRight + new Vector2(-verticalOffset.X, verticalOffset.Y),
                verticalSize, color);
            AddTexture(sprites, "SquareSimple", boxBottomLeft + new Vector2(horizontalOffset.X, -horizontalOffset.Y),
                horizontalSize, color);
            AddTexture(sprites, "SquareSimple", boxBottomLeft + new Vector2(verticalOffset.X, -verticalOffset.Y),
                verticalSize, color);
            AddTexture(sprites, "SquareSimple", boxBottomRight - horizontalOffset, horizontalSize, color);
            AddTexture(sprites, "SquareSimple", boxBottomRight - verticalOffset, verticalSize, color);
        }

        static void AddTexture(List<MySprite> sprites, string texture, Vector2 position, Vector2 size, Color color,
            float rotation = 0f)
        {
            sprites.Add(new MySprite(SpriteType.TEXTURE, texture, position, size, color, null, TextAlignment.CENTER,
                rotation));
        }

        static void AddSpriteWithShadow(List<MySprite> sprites, MySprite sprite, float offset)
        {
            sprites.Add(sprite.Shadow(offset));
            sprites.Add(sprite);
        }

        void DrawContactIcon(List<MySprite> sprites, Vector2 position, TargetInfo targetInfo, float scale,
            float rotation)
        {
            if (!string.IsNullOrEmpty(targetInfo.IconTexture))
            {
                AddSpriteWithShadow(sprites, new MySprite(
                    SpriteType.TEXTURE,
                    targetInfo.IconTexture,
                    position,
                    new Vector2(DOT_SIZE * 1.5f) * scale,
                    targetInfo.IconColor,
                    null,
                    TextAlignment.CENTER,
                    rotation), scale);
                return;
            }

            if (targetInfo.OutOfRange)
            {
                DrawOutOfRangeIcon(sprites, position, targetInfo.IconColor, scale, rotation);
                return;
            }

            switch (targetInfo.Relationship)
            {
                case MyRelationsBetweenPlayerAndBlock.Owner:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    DrawSquare(sprites, position, targetInfo.IconColor, scale, rotation);
                    break;
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    DrawCircle(sprites, position, targetInfo.IconColor, scale, rotation);
                    break;
                default:
                    DrawTriangle(sprites, position, targetInfo.IconColor, scale, rotation);
                    break;
            }
        }

        static void DrawSquare(List<MySprite> frame, Vector2 centerPos, Color color, float scale,
            float rotation)
        {
            AddSpriteWithShadow(frame,
                new MySprite(SpriteType.TEXTURE, "SquareSimple", centerPos, new Vector2(DOT_SIZE) * scale, color, null,
                    TextAlignment.CENTER, rotation), scale);
        }

        static void DrawCircle(List<MySprite> frame, Vector2 centerPos, Color color, float scale,
            float rotation)
        {
            AddSpriteWithShadow(frame,
                new MySprite(SpriteType.TEXTURE, "Circle", centerPos, new Vector2(DOT_SIZE) * scale, color, null,
                    TextAlignment.CENTER, rotation), scale);
        }

        static void DrawOutOfRangeIcon(List<MySprite> frame, Vector2 centerPos, Color color,
            float scale,
            float rotation)
        {
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);
            scale *= 0.075f;

            AddTexture(frame, "SquareSimple",
                new Vector2(cos * 61f - sin * -12f, sin * 61f + cos * -12f) * scale + centerPos,
                new Vector2(180f, 350f) * scale, color.MulValue(0.2f), -0.7854f + rotation);
            AddTexture(frame, "SquareSimple",
                new Vector2(cos * -61f - sin * -12f, sin * -61f + cos * -12f) * scale + centerPos,
                new Vector2(180f, 350f) * scale, color.MulValue(0.2f), 0.7854f + rotation);
            AddTexture(frame, "SquareSimple",
                new Vector2(cos * 61f - sin * -12f, sin * 61f + cos * -12f) * scale + centerPos,
                new Vector2(80f, 250f) * scale, color, -0.7854f + rotation);
            AddTexture(frame, "SquareSimple",
                new Vector2(cos * -61f - sin * -12f, sin * -61f + cos * -12f) * scale + centerPos,
                new Vector2(80f, 250f) * scale, color, 0.7854f + rotation);
        }

        static void DrawTriangle(List<MySprite> frame, Vector2 centerPos, Color color, float scale,
            float rotation)
        {
            Vector2 iconSize = new Vector2(DOT_SIZE) * scale;
            Vector2 shadowSize = iconSize * 1.2f;

            AddTexture(frame, "Triangle", centerPos, shadowSize, color.MulValue(0.2f), rotation);
            AddTexture(frame, "Triangle", centerPos + new Vector2(0, iconSize.Y * .05f), iconSize, color, rotation);
        }

        static void RoundVector2(ref Vector2 vec)
        {
            vec.X = (float)Math.Round(vec.X);
            vec.Y = (float)Math.Round(vec.Y);
        }

        Vector2 ClampCameraPan(Vector2 pan)
        {
            if (!SurfaceScriptBase.IsFinite(pan))
                return Vector2.Zero;

            float maxPan = Math.Max(0f, _maxRange) * CAMERA_MAX_PAN_RANGE_RATIO;
            if (maxPan <= 0f)
                return Vector2.Zero;

            float lengthSq = pan.LengthSquared();
            float maxPanSq = maxPan * maxPan;
            if (lengthSq <= maxPanSq)
                return pan;

            return pan * (maxPan / (float)Math.Sqrt(lengthSq));
        }

        static Rectangle ToClipRectangle(RectangleF bounds)
        {
            int x = (int)Math.Floor(bounds.X);
            int y = (int)Math.Floor(bounds.Y);
            int right = (int)Math.Ceiling(bounds.Right);
            int bottom = (int)Math.Ceiling(bounds.Bottom);
            return new Rectangle(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y));
        }

        static void RoundVector2(ref Vector2? vec)
        {
            if (!vec.HasValue) return;
            var value = vec.Value;
            value.X = (float)Math.Round(value.X);
            value.Y = (float)Math.Round(value.Y);
            vec = value;
        }

        void UpdateFooterHeights()
        {
            int entries = _sortedContacts.Count;
            _footerHeight = CalculateFooterHeight(entries, LayoutScale);
        }

        void UpdateFooterControls()
        {
            bool showWarning = ShouldShowLongRangeWarning();
            bool hasContacts = _sortedContacts.Count > 0;
            if (_footerHeight <= 0f || !showWarning && !hasContacts)
            {
                _footerRows.Clear();
                _footerControl.SetRows(_footerRows);
                _footerControl.SetVisible(false);
                return;
            }

            _footerControl.SetVisible(true);
            _footerControl.FooterBackgroundColor = new Color(BackgroundColor.MulValue(0.8f), 0.5f);
            _footerControl.SetRowHeight(GetFooterRowHeight(LayoutScale));
            _footerRows.Clear();

            if (showWarning)
            {
                Color warningColor = new Color(GetWarningColor(), 0.9f);
                _footerWarningControl.SetContent(
"Warning",
                    _longRangeWarningText,
                    warningColor,
                    warningColor,
                    FOOTER_WARNING_TEXT_SIZE,
                    0.9f);
                _footerRows.Add(_footerWarningControl);
            }

            if (hasContacts)
            {
                Color headerColor = new Color(ForegroundColor, 0.75f);
                _footerHeaderControl.SetHeader(
                    _detectedEntitiesText,
                    GetFooterRangeText(),
                    headerColor);
                _footerRows.Add(_footerHeaderControl);

                int visibleEntries = GetVisibleFooterEntryCount(_sortedContacts.Count);
                int startIndex = 0;
                if (_sortedContacts.Count > visibleEntries)
                    startIndex = GetScrollStep(FOOTER_SCROLL_STEP_SECONDS) % _sortedContacts.Count;

                var errColor = GetErrorColor();
                var warnColor = GetWarningColor();
                var allyColor = GetHeaderColor();
                for (int i = 0; i < visibleEntries; i++)
                {
                    ContactRecord contact = _sortedContacts[(startIndex + i) % _sortedContacts.Count];
                    Color iconColor = ContactColor(contact, errColor, warnColor, allyColor);
                    IconTextRowControl control = GetFooterContactControl(i);
                    control.SetContent(
                        FooterIconTexture(contact),
                        GetFooterContactText(contact),
                        iconColor,
                        ForegroundColor,
                        0.5f,
                        0.78f);
                    _footerRows.Add(control);
                }
            }

            _footerControl.SetRows(_footerRows);
            UpdateFooterControlBounds();
        }

        void UpdateFooterControlBounds()
        {
            if (_footerControl == null)
                return;

            if (!_footerControl.Visible || _footerHeight <= 0f)
            {
                _footerControl.SetVisible(false);
                return;
            }

            _footerControl.SetRect(new RectangleF(
                ViewBox.X,
                ViewBox.Bottom - _footerHeight,
                ViewBox.Width,
                _footerHeight));
        }

        float CalculateFooterHeight(int entries, float layoutScale)
        {
            int rowCount = GetFooterRowCount(entries);
            if (rowCount <= 0)
                return 0f;

            return rowCount * GetFooterRowHeight(layoutScale) + FOOTER_BOTTOM_PADDING_PX * layoutScale;
        }

        int GetFooterRowCount(int entries)
        {
            int rowCount = ShouldShowLongRangeWarning() ? 1 : 0;
            int visibleEntries = GetVisibleFooterEntryCount(entries);
            if (visibleEntries > 0)
                rowCount += 1 + visibleEntries;

            return rowCount;
        }

        static int GetVisibleFooterEntryCount(int entries)
        {
            return Math.Min(Math.Min(MAX_CONTACTS, Math.Max(0, entries)), FOOTER_MAX_ROWS);
        }

        static float GetFooterRowHeight(float layoutScale)
        {
            return Math.Max(1f, FOOTER_ROW_HEIGHT_PX * layoutScale);
        }

        IconTextRowControl GetFooterContactControl(int index)
        {
            while (_footerContactControls.Count <= index)
                _footerContactControls.Add(new IconTextRowControl());

            return _footerContactControls[index];
        }

        string GetFooterRangeText()
        {
            if (_footerRangeTextRange != _maxRange)
            {
                _footerRangeTextRange = _maxRange;
                _footerRangeText = _rangeLabelText + FormatingHelper.DistanceToString(_maxRange);
            }

            return _footerRangeText;
        }

        static string GetFooterContactText(ContactRecord contact)
        {
            string distance = FormatingHelper.DistanceToString(contact.Distance);
            string name = string.IsNullOrWhiteSpace(contact.Name) ? "Unknown": contact.Name;
            if (contact.IsTargeted)
                name = "[L] "+ name;

            return distance + "  "+ name;
        }

        float GetRadarCappedScale()
        {
            float configScale = Math.Max(GeneralComponent.GetScale(), 0.0001f);
            float autoScale = Scale / configScale;
            return autoScale * Math.Min(configScale, 1f);
        }

        void BuildSortedContacts()
        {
            _sortedContacts.Clear();
            var shipPos = Block.WorldMatrix.Translation;

            // Entity contacts: only those within detection range
            foreach (var kv in _contacts)
            {
                var c = kv.Value;
                c.Distance = (float)Vector3D.Distance(c.WorldPosition, shipPos);
                if (c.Distance > _maxRange)
                    continue;
                _sortedContacts.Add(c);
            }

            // Targeted contacts first, then closest-first
            _sortedContacts.Sort((a, b) =>
                a.IsTargeted != b.IsTargeted ? (a.IsTargeted ? -1 : 1) : a.Distance.CompareTo(b.Distance));
        }

        static Color ContactColor(ContactRecord c, Color errColor, Color warnColor, Color frdColor)
        {
            return RelationColor(c.Relationship, errColor, warnColor, frdColor);
        }

        static Color RelationColor(MyRelationsBetweenPlayerAndBlock relationship, Color errColor, Color warnColor, Color frdColor)
        {
            switch (relationship)
            {
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    return errColor;
                case MyRelationsBetweenPlayerAndBlock.Owner:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    return frdColor;
                default:
                    return warnColor;
            }
        }

        MatrixD GetReferenceMatrix()
        {
            MatrixD gridMatrix;
            if (!TryGetReferenceWorldMatrix(out gridMatrix))
                gridMatrix = Block.WorldMatrix;
            return gridMatrix;
        }

        static Vector2 PlanarOffset(ContactRecord contact, ref MatrixD gridMatrix, ref MatrixD inverse)
        {
            var local = Vector3D.TransformNormal(contact.WorldPosition - gridMatrix.Translation, inverse);
            return new Vector2((float)local.X, (float)local.Z);
        }

        static string SectorName(Vector2 planar)
        {
            if (planar.LengthSquared() < 1e-6f)
                return _sectorNames[0];
            double degrees = Math.Atan2(planar.X, -planar.Y) * 180d / Math.PI;
            if (degrees < 0d)
                degrees += 360d;
            return _sectorNames[(int)Math.Round(degrees / 45d) % _sectorNames.Length];
        }

        void DrawMinimal(List<MySprite> sprites)
        {
            RectangleF view = Host.ViewBox;
            float radius = Math.Min(view.Width, view.Height) * 0.5f - Bare.Margin;
            if (radius <= 0f)
                return;

            Vector2 center = view.Center;
            Color fg = GetForegroundColor();
            Color ringColor = ThemeChrome.WithAlpha(fg, SCOPE_RING_ALPHA);
            float ringWidth = Math.Max(1f, MINIMAL_RING_WIDTH_PX * Scale);
            float crossWidth = Math.Max(1f, Scale);
            AddTexture(sprites, "Circle", center, new Vector2(radius * 2f), ringColor);
            AddTexture(sprites, "Circle", center, new Vector2(Math.Max(0f, radius - ringWidth) * 2f), BackgroundColor);
            sprites.Add(LineRenderer.Rect(center, new Vector2(radius * 2f, crossWidth), ringColor));
            sprites.Add(LineRenderer.Rect(center, new Vector2(crossWidth, radius * 2f), ringColor));

            float worldRange = GetWorldRange();
            if (worldRange <= 0f || _sortedContacts.Count == 0)
                return;

            Color errColor = GetErrorColor();
            Color warnColor = GetWarningColor();
            Color allyColor = GetHeaderColor();
            Color hostileColor = BlinkOn ? errColor : ThemeChrome.WithAlpha(errColor, HOSTILE_DIM_ALPHA);
            MatrixD gridMatrix = GetReferenceMatrix();
            MatrixD inverse = MatrixD.Transpose(gridMatrix);
            float pixelsPerMeter = radius / worldRange;
            var dotSize = new Vector2(MINIMAL_DOT_RADIUS_PX * 2f * Scale);
            int shown = Math.Min(_sortedContacts.Count, MAX_CONTACTS);
            for (int i = 0; i < shown; i++)
            {
                ContactRecord contact = _sortedContacts[i];
                Vector2 planar = PlanarOffset(contact, ref gridMatrix, ref inverse);
                if (planar.LengthSquared() > worldRange * worldRange)
                    continue;
                Color color = contact.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies
                    ? hostileColor
                    : RelationColor(contact.Relationship, errColor, warnColor, allyColor);
                AddTexture(sprites, "Circle", center + planar * pixelsPerMeter, dotSize, color);
            }
        }

        void DrawCompact(List<MySprite> sprites)
        {
            if (_sortedContacts.Count == 0)
            {
                Host.DrawMessage(sprites, LocHelper.GetLoc(MOD_PREFIX + "NotAvailable"), "Warning", GetWarningColor());
                return;
            }

            _compactList.Clear();
            _compactList.AddRange(_sortedContacts);
            _compactList.Sort(CompactContactOrder.Instance);

            RectangleF view = Host.ViewBox;
            Color fg = GetForegroundColor();
            Color errColor = GetErrorColor();
            Color warnColor = GetWarningColor();
            Color allyColor = GetHeaderColor();
            MatrixD gridMatrix = GetReferenceMatrix();
            MatrixD inverse = MatrixD.Transpose(gridMatrix);
            float swatch = COMPACT_SWATCH_PX * Scale;
            float indent = swatch + Bare.Margin;
            var area = new RectangleF(view.X + indent, view.Y, view.Width - indent, view.Height);

            _compactRows.Clear();
            int count = Math.Min(_compactList.Count, COMPACT_MAX_ROWS);
            for (int i = 0; i < count; i++)
            {
                ContactRecord contact = _compactList[i];
                string name = string.IsNullOrWhiteSpace(contact.Name) ? "Unknown" : contact.Name;
                Vector2 planar = PlanarOffset(contact, ref gridMatrix, ref inverse);
                _compactRows.Add(new BareLayout.KvRow
                {
                    Label = name,
                    Value = FormatingHelper.DistanceToString(contact.Distance) + " " + SectorName(planar),
                    ValueColor = fg,
                    Bar = -1f
                });
            }

            int drawn = Bare.Kv(sprites, area, _compactRows, fg, Theme);
            float rowHeight = Bare.LineHeight(1f) * COMPACT_ROW_RATIO;
            for (int i = 0; i < drawn; i++)
            {
                Color color = RelationColor(_compactList[i].Relationship, errColor, warnColor, allyColor);
                var pos = new Vector2(view.X + swatch * 0.5f, area.Y + (i + 0.5f) * rowHeight);
                sprites.Add(LineRenderer.Rect(pos, new Vector2(swatch), color));
            }
        }

        static string FooterIconTexture(ContactRecord contact)
        {
            if (!string.IsNullOrEmpty(contact.IconTexture))
                return contact.IconTexture;

            switch (contact.Relationship)
            {
                case MyRelationsBetweenPlayerAndBlock.Owner:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    return "SquareSimple";
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    return "Circle";
                default:
                    return "Triangle";
            }
        }

        static int GetScrollStep(int secondsPerStep)
        {
            try
            {
                var sess = MyAPIGateway.Session;
                if (sess == null)
                    return 0;
                int ticksPerStep = Math.Max(1, secondsPerStep * 60);
                return sess.GameplayFrameCounter / ticksPerStep;
            }
            catch
            {
                return 0;
            }
        }
    }
}
