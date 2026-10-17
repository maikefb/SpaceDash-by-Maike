using LcdMod.Common.Config.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Farm;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.ControlsTemplates.Custom;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Common.Helpers;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;
using VisualWrapPanel = LcdMod.Client.Gui.ControlsTemplates.Panels.WrapPanel.WrapPanel;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.Apps
{
    
    // ReSharper disable once PartialTypeWithSinglePart
    internal sealed partial class FarmApp : App, IApp
    {
        const float SLOT_W = 100f;
        const float SLOT_H = 100f;
        const float SCROLLER_W = 8f;
        const int DATA_REFRESH_RUN_INTERVAL = 6;
        const float MINIMAL_HERO_FONT_MUL = 3f;
        const float TRACK_MIX = 0.15f;
        const string SEPARATOR = " · ";
        const string ICON_WATER = "MyObjectBuilder_Ore/Ice";
        const string ICON_PLANT = "IconEnergy";
        static readonly Color WATER_COLOR = new Color(64, 156, 255);
        internal sealed class FarmEntry
        {
            public long EntryId { get; private set; }
            public FarmPlotEntry Plot { get; private set; }
            public float Ratio { get; private set; }
            public bool IsReady { get; private set; }
            public double RemainingSeconds { get; private set; } = -1d;
            public string PercentText { get; private set; }
            public string RemainingText { get; private set; }
            public string WaterText { get; private set; }
            public float WaterRatio { get; private set; }
            public string OutputSprite { get; private set; }
            public string OutputName { get; private set; }
            public Color StatusColor { get; private set; }
            public string DetailedInfo { get; private set; } = string.Empty;

            public bool Update(
                FarmPlotEntry plot,
                float ratio,
                bool isReady,
                double remainingSeconds,
                string percentText,
                string remainingText,
                string waterText,
                float waterRatio,
                string outputSprite,
                string outputName,
                Color statusColor,
                string detailedInfo)
            {
                ratio = MathHelper.Clamp(ratio, 0f, 1f);
                waterRatio = MathHelper.Clamp(waterRatio, 0f, 1f);
                percentText = percentText ?? string.Empty;
                remainingText = remainingText ?? string.Empty;
                waterText = waterText ?? string.Empty;
                outputSprite = outputSprite ?? string.Empty;
                outputName = outputName ?? string.Empty;
                detailedInfo = detailedInfo ?? string.Empty;

                long entryId = plot != null && plot.Block != null ? plot.Block.EntityId : 0L;
                bool presentationChanged =
                    EntryId != entryId ||
                    !Ratio.Equals(ratio) ||
                    IsReady != isReady ||
                    !RemainingSeconds.Equals(remainingSeconds) ||
                    !string.Equals(PercentText, percentText, StringComparison.Ordinal) ||
                    !string.Equals(RemainingText, remainingText, StringComparison.Ordinal) ||
                    !string.Equals(WaterText, waterText, StringComparison.Ordinal) ||
                    !WaterRatio.Equals(waterRatio) ||
                    !string.Equals(OutputSprite, outputSprite, StringComparison.Ordinal) ||
                    !string.Equals(OutputName, outputName, StringComparison.Ordinal) ||
                    !StatusColor.Equals(statusColor) ||
                    !string.Equals(DetailedInfo, detailedInfo, StringComparison.Ordinal);

                Plot = plot;
                EntryId = entryId;
                Ratio = ratio;
                IsReady = isReady;
                RemainingSeconds = remainingSeconds;
                PercentText = percentText;
                RemainingText = remainingText;
                WaterText = waterText;
                WaterRatio = waterRatio;
                OutputSprite = outputSprite;
                OutputName = outputName;
                StatusColor = statusColor;
                DetailedInfo = detailedInfo;

                return presentationChanged;
            }
        }

        readonly IAppHost _surfaceHost;
        readonly List<FarmEntry> _entries = new List<FarmEntry>();
        readonly List<FarmEntry> _nextEntries = new List<FarmEntry>();
        readonly List<Control> _children = new List<Control>();
        readonly Dictionary<long, FarmEntry> _entryById = new Dictionary<long, FarmEntry>();
        readonly Dictionary<long, FarmEntry> _entryModelsById = new Dictionary<long, FarmEntry>();
        readonly Dictionary<long, FarmPlotControl> _entryControlById = new Dictionary<long, FarmPlotControl>();
        readonly HashSet<long> _activeEntryIds = new HashSet<long>();
        readonly List<long> _entryIdsToRemove = new List<long>();
        readonly FarmGrowthHelper _growthDefinitions = new FarmGrowthHelper();
        readonly ScrollPanel _scrollPanel;
        readonly VisualWrapPanel _gridPanel;
        int _runsUntilDataRefresh;
        internal ColorConfigComponent FarmColors => ColorComponent;

        public FarmApp(IAppHost surfaceHost) : base(surfaceHost)
        {
            NeedGridData(GridCapability.Blocks);
            _surfaceHost = surfaceHost;
            _scrollPanel = AddLogicalChild(new ScrollPanel(dataContext: this));
            _scrollPanel.ScrollChanged = OnScrollPanelChanged;
            _scrollPanel.SetVisible(false);
            _gridPanel = new VisualWrapPanel();
            _children.Add(_scrollPanel);
        }

        public override IReadOnlyList<Control> VisualChildren => _children;

        public override void LayoutChanged()
        {
            _runsUntilDataRefresh = 0;
            _loc.Clear();
            _entries.Clear();
            _nextEntries.Clear();
            _entryById.Clear();
            _entryModelsById.Clear();
            ClearFarmEntryControls();
            MarkDirty();
        }

        public override void Update()
        {
            if (_scrollPanel.UpdateAutoScroll())
                MarkDirty();

            if (_runsUntilDataRefresh > 0)
            {
                _runsUntilDataRefresh--;
                return;
            }

            _runsUntilDataRefresh = DATA_REFRESH_RUN_INTERVAL - 1;
            RefreshEntries();
        }

        void RefreshEntries()
        {
            _nextEntries.Clear();
            _entryById.Clear();
            _activeEntryIds.Clear();
            bool changed = false;

            var gridLogic = _surfaceHost.GridLogic;
            if (gridLogic == null)
            {
                changed = _entries.Count > 0 || _entryModelsById.Count > 0;
                _entries.Clear();
                _entryModelsById.Clear();
                ClearFarmEntryControls();
                if (changed)
                    MarkDirty();
                return;
            }

            var farmPlots = gridLogic.FarmPlots;
            for (int i = 0; i < farmPlots.Count; i++)
            {
                var plot = farmPlots[i];
                if (plot == null || plot.Block == null || plot.Logic == null)
                    continue;

                bool entryChanged;
                var entry = GetOrUpdateEntry(plot, out entryChanged);
                changed |= entryChanged;
                _nextEntries.Add(entry);
                _entryById[entry.EntryId] = entry;
                _activeEntryIds.Add(entry.EntryId);
            }

            changed |= !EntryOrderEquals(_entries, _nextEntries);
            _entries.Clear();
            _entries.AddRange(_nextEntries);
            changed |= RemoveStaleEntryModels();

            if (_entries.Count == 0)
                _scrollPanel.SetVisible(false);

            if (changed)
                MarkDirty();
        }

        public override bool HasVisibleItems()
        {
            return _entries.Count > 0;
        }

        FarmEntry GetFarmEntry(long entryId)
        {
            FarmEntry entry;
            return _entryById.TryGetValue(entryId, out entry) ? entry : null;
        }

        readonly List<MySprite> _sprites = new List<MySprite>();
        readonly Dictionary<long, bool> _desiredEntryIds = new Dictionary<long, bool>();
        readonly List<Control> _desiredEntryControls = new List<Control>();

        public override List<MySprite> GetSprites()
        {
            BeginFarmEntryControlFrame();
            var sprites = _sprites;
            sprites.Clear();
            if (IsMinimal || IsCompact)
            {
                Aggregate();
                if (IsMinimal)
                    DrawMinimal(sprites);
                else
                    DrawCompact(sprites);
                ClearDirtyAfterRender();
                return sprites;
            }
            DrawFarmPlots(_surfaceHost, sprites);
            ClearDirtyAfterRender();
            return sprites;
        }

        readonly Dictionary<string, string> _loc = new Dictionary<string, string>();
        readonly List<BareLayout.KvRow> _kv = new List<BareLayout.KvRow>();
        float _avgGrowth, _avgWater;
        int _ready, _watered, _noWater;
        FarmEntry _next;
        string _noWaterName, _plantIcon;

        void Aggregate()
        {
            _avgGrowth = _avgWater = 0f;
            _ready = _watered = _noWater = 0;
            _next = null;
            _noWaterName = null;
            _plantIcon = null;
            int count = _entries.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = _entries[i];
                _avgGrowth += entry.Ratio;
                if (_plantIcon == null && !string.IsNullOrEmpty(entry.OutputSprite))
                    _plantIcon = entry.OutputSprite;
                if (entry.IsReady)
                    _ready++;
                else if (entry.RemainingSeconds >= 0d && (_next == null || entry.RemainingSeconds < _next.RemainingSeconds))
                    _next = entry;
                if (entry.Plot == null || entry.Plot.StorageComponent == null)
                    continue;
                _watered++;
                _avgWater += entry.WaterRatio;
                if (entry.WaterRatio > 0f)
                    continue;
                _noWater++;
                if (_noWaterName == null)
                    _noWaterName = entry.OutputName;
            }
            if (count > 0)
                _avgGrowth /= count;
            if (_watered > 0)
                _avgWater /= _watered;
        }

        string Loc(string key)
        {
            string text;
            if (!_loc.TryGetValue(key, out text))
                _loc[key] = text = LocHelper.GetLoc(MOD_PREFIX + "Farm_" + key);
            return text;
        }

        string Loc(string key, object arg)
        {
            return string.Format(FormatingHelper.Culture, Loc(key), arg);
        }

        void DrawMinimal(List<MySprite> sprites)
        {
            var view = Host.ViewBox;
            Color fg = Host.ForegroundColor;
            Color accent = GetHeaderColor();
            Color track = Color.Lerp(Host.BackgroundColor, fg, TRACK_MIX);
            int count = _entries.Count;
            string center = count > 0 ? FormatingHelper.PercentageToString(_avgGrowth) : null;
            string line = null;
            if (count > 0)
                line = _next != null ? Loc("Ready", _ready) + SEPARATOR + Loc("Next", _next.RemainingText) : Loc("Ready", _ready);
            string warning = _noWater > 0 ? Loc("NoWater") : null;
            float margin = Bare.Margin;
            float lineHeight = Bare.LineHeight(1f);
            int lines = (line != null ? 1 : 0) + (warning != null ? 1 : 0);
            float linesHeight = lines * lineHeight;

            if (BareLayout.IsWide(view))
            {
                var ringArea = new RectangleF(view.X, view.Y, view.Width * 0.5f, view.Height);
                Bare.Ring(sprites, ringArea, _avgGrowth, accent, track, center, fg, MINIMAL_HERO_FONT_MUL);
                float left = ringArea.Right + margin;
                float top = view.Center.Y - linesHeight * 0.5f;
                if (Bare.Text(sprites, line, new Vector2(left, top), TextAlignment.LEFT, 1f, fg, view.Right - left, lineHeight))
                    top += lineHeight;
                Bare.Text(sprites, warning, new Vector2(left, top), TextAlignment.LEFT, 1f, GetWarningColor(), view.Right - left, lineHeight);
                return;
            }

            float ringHeight = lines > 0 ? view.Height - linesHeight - margin : view.Height;
            var rings = new RectangleF(view.X, view.Y, view.Width, ringHeight);
            Bare.Ring(sprites, rings, _avgGrowth, accent, track, center, fg, MINIMAL_HERO_FONT_MUL);
            float lineTop = rings.Bottom + margin;
            if (Bare.Text(sprites, line, new Vector2(view.Center.X, lineTop), TextAlignment.CENTER, 1f, fg, view.Width, lineHeight))
                lineTop += lineHeight;
            Bare.Text(sprites, warning, new Vector2(view.Center.X, lineTop), TextAlignment.CENTER, 1f, GetWarningColor(), view.Width, lineHeight);
        }

        void DrawCompact(List<MySprite> sprites)
        {
            Color fg = Host.ForegroundColor;
            Color accent = GetHeaderColor();
            Color warn = GetWarningColor();
            int count = _entries.Count;
            if (count == 0)
            {
                Host.DrawMessage(sprites, LocHelper.Empty, "Warning", warn);
                return;
            }

            string plantIcon = _plantIcon ?? ICON_PLANT;
            _kv.Clear();
            _kv.Add(new BareLayout.KvRow
            {
                Label = Loc("ReadyToHarvest"),
                Icon = plantIcon,
                Value = _ready + "/" + count,
                ValueColor = _ready > 0 ? accent : fg,
                Bar = -1f
            });
            _kv.Add(new BareLayout.KvRow
            {
                Label = Loc("AvgGrowth"),
                Icon = plantIcon,
                Value = FormatingHelper.PercentageToString(_avgGrowth),
                ValueColor = fg,
                Bar = _avgGrowth,
                BarColor = accent
            });
            if (_next != null)
            {
                _kv.Add(new BareLayout.KvRow
                {
                    Label = _next.OutputName,
                    Icon = string.IsNullOrEmpty(_next.OutputSprite) ? plantIcon : _next.OutputSprite,
                    Value = Loc("Next", _next.RemainingText),
                    ValueColor = fg,
                    Bar = -1f
                });
            }
            if (_watered > 0)
            {
                _kv.Add(new BareLayout.KvRow
                {
                    Label = Loc("AvgWater"),
                    Icon = ICON_WATER,
                    Value = FormatingHelper.PercentageToString(_avgWater),
                    ValueColor = _noWater > 0 ? warn : fg,
                    Bar = _avgWater,
                    BarColor = WATER_COLOR
                });
            }
            if (_noWater > 0)
            {
                _kv.Add(new BareLayout.KvRow
                {
                    Label = Loc("NoWater"),
                    Icon = ICON_WATER,
                    Value = string.IsNullOrEmpty(_noWaterName) ? _noWater.ToString() : _noWater + SEPARATOR + _noWaterName,
                    ValueColor = GetErrorColor(),
                    Bar = -1f
                });
            }
            Bare.Kv(sprites, Host.ViewBox, _kv, fg, Theme);
        }

        public void CompleteHostRender()
        {
            ClearDirtyAfterRender();
        }

        FarmEntry GetOrUpdateEntry(FarmPlotEntry plot, out bool changed)
        {
            var entryId = plot.Block.EntityId;
            FarmEntry entry;
            if (!_entryModelsById.TryGetValue(entryId, out entry) || entry == null)
            {
                entry = new FarmEntry();
                _entryModelsById[entryId] = entry;
            }

            var detailedInfo = GetDetailedInfo(plot);
            float growthPercent;
            var ratio = GetGrowthRatio(plot, detailedInfo, out growthPercent);
            var outputItem = plot.Logic.OutputItem;
            var outputSprite = ResolveOutputSprite(outputItem);
            var outputName = ResolveOutputName(outputItem);
            var percentText = FormatingHelper.PercentageToString(ratio);
            var isReady = IsPlotReady(plot, ratio);
            var remainingSeconds = GetRemainingSeconds(plot, growthPercent, isReady);
            var remainingText = GetRemainingText(plot, remainingSeconds, percentText);
            var waterRatio = GetWaterRatio(plot);

            changed = entry.Update(
                plot,
                ratio,
                isReady,
                remainingSeconds,
                percentText,
                remainingText,
                GetWaterText(waterRatio, plot),
                waterRatio,
                outputSprite,
                outputName,
                GetStatusColor(plot, ratio),
                detailedInfo);
            return entry;
        }

        static string GetDetailedInfo(FarmPlotEntry plot)
        {
            try
            {
                return plot?.Logic?.GetDetailedInfoWithoutRequiredInput() ?? string.Empty;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(FarmApp));
                return string.Empty;
            }
        }

        static bool EntryOrderEquals(List<FarmEntry> current, List<FarmEntry> next)
        {
            if (ReferenceEquals(current, next))
                return true;
            if (current == null || next == null || current.Count != next.Count)
                return false;

            for (int i = 0; i < current.Count; i++)
            {
                if (!ReferenceEquals(current[i], next[i]))
                    return false;
            }

            return true;
        }

        static float GetWaterRatio(FarmPlotEntry plot)
        {
            var storage = plot?.StorageComponent;
            if (storage == null)
                return 0f;

            return MathHelper.Clamp((float)storage.FilledRatio, 0f, 1f);
        }

        static string GetWaterText(float ratio, FarmPlotEntry plot)
        {
            if (plot == null || plot.StorageComponent == null)
                return string.Empty;

            return string.Format(FormatingHelper.Culture, LocHelper.GetLoc(MOD_PREFIX + "Farm_Water"),
                FormatingHelper.PercentageToString(ratio));
        }

        string ResolveOutputSprite(MyDefinitionId outputItem)
        {
            if (outputItem.Equals(default(MyDefinitionId)) || MyDefinitionManager.Static == null)
                return string.Empty;

            MyPhysicalItemDefinition definition;
            if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(outputItem, out definition))
                return string.Empty;

            return TextureHelper.ResolveItemSprite(definition, _surfaceHost.Surface);
        }

        static string ResolveOutputName(MyDefinitionId outputItem)
        {
            if (outputItem.Equals(default(MyDefinitionId)) || MyDefinitionManager.Static == null)
                return string.Empty;

            MyPhysicalItemDefinition definition;
            if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(outputItem, out definition))
                return outputItem.SubtypeName;

            return !string.IsNullOrEmpty(definition.DisplayNameText) ? definition.DisplayNameText : outputItem.SubtypeName;
        }

        Color GetStatusColor(FarmPlotEntry plot, float ratio)
        {
            if (plot?.Logic == null || !plot.Logic.IsPlantPlanted)
                return _surfaceHost.ForegroundColor;

            if (!plot.Logic.IsAlive)
                return GetErrorColor();

            if (ratio >= 1f || plot.Logic.IsHarvestable)
                return GetHeaderColor();

            return _surfaceHost.ForegroundColor;
        }

        bool RemoveStaleEntryModels()
        {
            _entryIdsToRemove.Clear();
            foreach (var kv in _entryModelsById)
            {
                if (!_activeEntryIds.Contains(kv.Key))
                    _entryIdsToRemove.Add(kv.Key);
            }

            for (int i = 0; i < _entryIdsToRemove.Count; i++)
            {
                long entryId = _entryIdsToRemove[i];
                _entryModelsById.Remove(entryId);

                FarmPlotControl control;
                if (_entryControlById.TryGetValue(entryId, out control))
                {
                    if (control != null && ReferenceEquals(control.Parent, _gridPanel))
                        _gridPanel.RemoveChild(control);
                    if (control != null)
                        control.SetVisible(false);
                    _entryControlById.Remove(entryId);
                }
            }

            return _entryIdsToRemove.Count > 0;
        }

        void DrawFarmPlots(IAppHost owner, List<MySprite> sprites)
        {
            float minW = SLOT_W * owner.ConfiguredScale;
            float minH = SLOT_H * owner.ConfiguredScale;
            float contentTop = GetContentTop(owner) + 6f * owner.ConfiguredScale;

            int count = _entries.Count;
            if (count <= 0)
                return;

            _scrollPanel.SetContent(_gridPanel);
            _gridPanel.RowHeight = minH;
            _gridPanel.MinimumColumnWidth = minW;
            _gridPanel.HorizontalGap = 0f;
            _gridPanel.VerticalGap = 0f;
            SyncFarmEntryControls(_gridPanel);
            ConfigureFarmScrollPanel(owner, contentTop, minH);
            _scrollPanel.Render(sprites);
        }

        void ConfigureFarmScrollPanel(IAppHost owner, float contentTop, float rowHeight)
        {
            var viewportHeight = Math.Max(0f, owner.ViewBox.Bottom - contentTop);
            _scrollPanel.ConfigureAutomatic(
                new RectangleF(owner.ViewBox.X, contentTop, owner.ViewBox.Width, viewportHeight),
                SCROLLER_W * owner.ConfiguredScale,
                rowHeight);
            _scrollPanel.SetVisible(true);
            if (!_children.Contains(_scrollPanel))
                _children.Add(_scrollPanel);
        }

        void BeginFarmEntryControlFrame()
        {
            _children.Clear();
            _scrollPanel.SetVisible(false);
            foreach (var kv in _entryControlById)
                if (kv.Value != null)
                    kv.Value.SetVisible(false);
        }

        void ClearFarmEntryControls()
        {
            foreach (var kv in _entryControlById)
                if (kv.Value != null)
                    kv.Value.SetVisible(false);

            _entryControlById.Clear();
            _gridPanel.ClearChildren();
            _children.Clear();
        }

        void SyncFarmEntryControls(Panel panel)
        {
            if (panel == null)
                return;

            var desiredIds = _desiredEntryIds;
            var desired = _desiredEntryControls;
            desiredIds.Clear();
            desired.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry == null)
                    continue;

                desiredIds[entry.EntryId] = true;
                desired.Add(GetOrCreateFarmPlotControl(entry));
            }

            RemoveStalePanelChildren(panel, desiredIds);
            EnsurePanelChildOrder(panel, desired);
        }

        FarmPlotControl GetOrCreateFarmPlotControl(FarmEntry entry)
        {
            if (entry == null)
                return null;

            FarmPlotControl control;
            if (!_entryControlById.TryGetValue(entry.EntryId, out control) || control == null)
            {
                control = new FarmPlotControl(default(RectangleF), entry);
                _entryControlById[entry.EntryId] = control;
            }
            else
            {
                control.Bind(entry);
            }

            control.SetVisible(true);
            return control;
        }

        static void RemoveStalePanelChildren(Panel panel, Dictionary<long, bool> desiredIds)
        {
            var children = panel.VisualChildren;
            if (children == null)
                return;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                var child = children[i];
                var entry = child?.DataContext as FarmEntry;
                if (entry == null || desiredIds.ContainsKey(entry.EntryId))
                    continue;

                panel.RemoveChild(child);
            }
        }

        static void EnsurePanelChildOrder(Panel panel, List<Control> desired)
        {
            if (panel == null || desired == null)
                return;

            var children = panel.VisualChildren;
            bool changed = false;
            for (int i = 0; i < desired.Count; i++)
            {
                var child = desired[i] as ControlTemplate;
                if (child == null)
                    continue;

                if (!ReferenceEquals(child.Parent, panel))
                {
                    panel.AddChild(child);
                    children = panel.VisualChildren;
                    changed = true;
                }

                if (children == null || i >= children.Count || ReferenceEquals(children[i], child))
                    continue;

                int currentIndex = IndexOfChild(children, child);
                if (currentIndex < 0)
                    continue;

                if (panel.MoveChild(child, i))
                    changed = true;
            }

            if (changed)
                panel.InvalidateLayout();
        }

        static int IndexOfChild(IReadOnlyList<Control> children, ControlTemplate child)
        {
            if (children == null || child == null)
                return -1;

            for (int i = 0; i < children.Count; i++)
            {
                if (ReferenceEquals(children[i], child))
                    return i;
            }

            return -1;
        }

        void OnScrollPanelChanged(ScrollPanel panel)
        {
            Host.RenderSprites();
        }

        float GetContentTop(IAppHost owner)
        {
            return owner.TitleVisible ? owner.ViewBox.Y + (40f * owner.ConfiguredScale * owner.FontScale) : owner.ViewBox.Y;
        }

        static bool IsPlotReady(FarmPlotEntry plot, float ratio)
        {
            var logic = plot?.Logic;
            return logic != null && logic.IsPlantPlanted && logic.IsAlive &&
                   (logic.IsPlantFullyGrown || logic.IsHarvestable || ratio >= 0.999f);
        }

        double GetRemainingSeconds(FarmPlotEntry plot, float growthPercent, bool isReady)
        {
            var logic = plot?.Logic;
            if (logic == null || !logic.IsPlantPlanted || !logic.IsAlive)
                return -1d;

            if (isReady)
                return 0d;

            double remainingSeconds;
            return TryGetRemainingSeconds(plot, growthPercent, out remainingSeconds) ? remainingSeconds : -1d;
        }

        static string GetRemainingText(FarmPlotEntry plot, double remainingSeconds, string percentText)
        {
            var logic = plot?.Logic;
            if (logic == null || !logic.IsPlantPlanted)
                return LocHelper.Empty;

            if (!logic.IsAlive)
                return "--";

            return remainingSeconds >= 0d ? FormatRemainingSeconds(remainingSeconds) : percentText ?? string.Empty;
        }

        bool TryGetRemainingSeconds(
            FarmPlotEntry plot,
            float growthProgressPercent,
            out double seconds)
        {
            seconds = 0d;
            if (FarmGrowthHelper.TryGetRuntimeRemainingSeconds(plot, out seconds))
                return true;

            FarmGrowthProfile profile;
            return _growthDefinitions.TryResolveGrowthProfile(plot, out profile) &&
                   FarmGrowthHelper.TryGetRemainingSeconds(profile, growthProgressPercent, out seconds);
        }

        static string FormatRemainingSeconds(double remainingSeconds)
        {
            if (double.IsNaN(remainingSeconds) || double.IsInfinity(remainingSeconds))
                remainingSeconds = 0d;

            return FormatingHelper.FormatTimeHours((float)Math.Max(0d, remainingSeconds / 3600d));
        }

        static float GetGrowthRatio(FarmPlotEntry plot, string detailedInfo, out float growthPercent)
        {
            growthPercent = 0f;
            var logic = plot?.Logic;
            if (logic == null || !logic.IsPlantPlanted)
                return 0f;
            if (logic.IsPlantFullyGrown)
            {
                growthPercent = 100f;
                return 1f;
            }
            if (!logic.IsAlive)
                return 0f;

            if (!string.IsNullOrEmpty(detailedInfo))
            {
                float percent;
                int percentIndex;
                // Growth progress is not exposed as a typed IMyFarmPlotLogic property.
                if (TryParseFirstPercent(detailedInfo, out percent, out percentIndex))
                {
                    growthPercent = MathHelper.Clamp(percent, 0f, 100f);
                    return growthPercent / 100f;
                }
            }

            return 0f;
        }

        static bool TryParseFirstPercent(string text, out float value, out int percentIndex)
        {
            value = 0f;
            percentIndex = -1;
            if (string.IsNullOrEmpty(text))
                return false;

            percentIndex = text.IndexOf('%');
            if (percentIndex <= 0)
                return false;

            var start = percentIndex - 1;
            while (start >= 0)
            {
                var c = text[start];
                if (!char.IsDigit(c) && c != '.' && c != ',' && c != '-' && c != '+' && !char.IsWhiteSpace(c))
                    break;
                start--;
            }

            var token = text.Substring(start + 1, percentIndex - start - 1).Trim().Replace(" ", string.Empty);
            if (string.IsNullOrEmpty(token))
                return false;

            if (float.TryParse(token, NumberStyles.Float, FormatingHelper.Culture, out value))
                return true;

            token = token.Replace(',', '.');
            return float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

    }
}
