using LcdMod.Common.Config.Components;
using System;
using System.Collections.Generic;
using System.Text;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Extensions;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Panels.WrapPanel;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Helpers;
using LcdMod.Client.Terminal.Controls;
using LcdMod.Common.Helpers;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using VisualStackPanel = LcdMod.Client.Gui.ControlsTemplates.Panels.StackPanel.StackPanel;
using VisualWrapPanel = LcdMod.Client.Gui.ControlsTemplates.Panels.WrapPanel.WrapPanel;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.Apps
{
    
    
    public sealed partial class GasApp : App
    {
        const int LINE_HEIGHT = 40;

        readonly Dictionary<string, string> _gasDisplayNameCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<MyDefinitionId, string> _gasSubtypeByDefinition = new Dictionary<MyDefinitionId, string>();
        readonly Dictionary<long, TankName> _tankNames = new Dictionary<long, TankName>();
        readonly List<MySprite> _sprites = new List<MySprite>();
        static readonly StringBuilder TrimBuffer = new StringBuilder();
        string _filterSourceName;
        string _filterToken;

        sealed class TankName
        {
            public string CustomName;
            public string Display;
        }
        List<Entry> _entries = new List<Entry>();
        List<Entry> _previousEntries = new List<Entry>();
        readonly LinkedTypedBlockSourceSet<IMyGasTank> _tanks =
            new LinkedTypedBlockSourceSet<IMyGasTank>(delegate(TypedBlockCollection blocks)
            {
                return blocks.GasTanks;
            });
        readonly List<RectangleControl> _entryControls = new List<RectangleControl>();
        readonly ScrollPanel _scrollPanel;
        readonly VisualStackPanel _listPanel;
        readonly VisualWrapPanel _gridPanel;

        // todo: convert to interactive app
        public override IReadOnlyList<Control> VisualChildren { get; } = new Control[]{};
        public bool HasEntries => _entries.Count > 0;

        /// <summary>true quando a última leitura difere da anterior em nome, ordem ou percentual.</summary>
        public bool DataChanged { get; private set; } = true;

        public bool IsScrollable => _scrollPanel.IsScrollable;

        public GasApp(IAppHost host) : base(host)
        {
            _scrollPanel = AddLogicalChild(new ScrollPanel(dataContext: this));
            _scrollPanel.SetVisible(false);
            _listPanel = new VisualStackPanel();
            _gridPanel = new VisualWrapPanel();
            _gridPanel.CustomRender = RenderGridPanelContent;
            _tanks.Changed += OnTanksChanged;
        }

        void OnTanksChanged()
        {
            _tankNames.Clear();
        }

        public override void Update()
        {
            var next = _previousEntries;
            _previousEntries = _entries;
            _entries = next;

            var count = 0;
            ReadEntries(_entries, ref count);
            if (_entries.Count > count)
                _entries.RemoveRange(count, _entries.Count - count);
            _entries.Sort((a, b) =>
            {
                var cmp = b.Percentage.CompareTo(a.Percentage);
                if (cmp != 0) return cmp;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            DataChanged = !SameEntries(_previousEntries, _entries);
        }

        static bool SameEntries(List<Entry> previous, List<Entry> current)
        {
            if (previous.Count != current.Count)
                return false;

            for (int i = 0; i < current.Count; i++)
            {
                if (previous[i].Percentage != current[i].Percentage ||
                    !string.Equals(previous[i].Name, current[i].Name, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = _sprites;
            sprites.Clear();
            ClearControls();
            switch (GeneralComponent.DisplayMode)
            {
                case (int)DisplayMode.Grid:
                    DrawGrid(sprites);
                    break;
                default:
                    DrawList(sprites);
                    break;
            }

            ClearDirtyAfterRender();
            return sprites;
        }

        public override void Close()
        {
            _tanks.Dispose();
            base.Close();
        }

        void DrawList(List<MySprite> sprites)
        {
            if (_entries.Count <= 0)
                return;

            var rowHeight = LINE_HEIGHT * GeneralComponent.GetScale();
            _scrollPanel.SetContent(_listPanel);
            _listPanel.RowHeight = rowHeight;
            _listPanel.Gap = 0f;
            SyncPanelChildren(_listPanel, false);
            ConfigureScrollPanel(rowHeight);
            _scrollPanel.Render(sprites);
        }

        void DrawGrid(List<MySprite> sprites)
        {
            if (_entries.Count <= 0)
                return;

            var rowHeight = 2f * LINE_HEIGHT * GeneralComponent.GetScale();
            _scrollPanel.SetContent(_gridPanel);
            _gridPanel.RowHeight = rowHeight;
            _gridPanel.MinimumColumnWidth = Host.ViewBox.Width + 1f;
            _gridPanel.ForceSingleColumn = true;
            _gridPanel.HorizontalGap = 0f;
            _gridPanel.VerticalGap = 0f;
            SyncPanelChildren(_gridPanel, true);
            ConfigureScrollPanel(rowHeight);
            _scrollPanel.Render(sprites);
        }

        void ClearControls()
        {
            _scrollPanel.SetVisible(false);
            for (int i = 0; i < _entryControls.Count; i++)
            {
                if (_entryControls[i] != null)
                    _entryControls[i].SetVisible(false);
            }
        }

        void ConfigureScrollPanel(float rowHeight)
        {
            var contentTop = GetContentTop();
            var viewportHeight = Math.Max(0f, Host.ViewBox.Bottom - contentTop);
            _scrollPanel.ConfigureAutomatic(
                new RectangleF(Host.ViewBox.X, contentTop, Host.ViewBox.Width, viewportHeight),
                ScrollPanel.DEFAULT_SCROLLER_WIDTH_PIXELS * GeneralComponent.GetScale(),
                rowHeight);
            _scrollPanel.SetVisible(true);
        }

        void SyncPanelChildren(Panel panel, bool renderAsGrid)
        {
            if (panel == null)
                return;

            EnsureEntryControlCount(_entries.Count);
            RemoveExtraPanelChildren(panel, _entries.Count);

            var children = panel.VisualChildren;
            bool changed = false;
            for (int i = 0; i < _entries.Count; i++)
            {
                var control = _entryControls[i];
                control.SetDataContext(_entries[i]);
                control.CustomRender = renderAsGrid ? RenderGridEntryControl : (InteractiveRenderHandler)RenderListEntryControl;
                control.SetVisible(true);

                if (!ReferenceEquals(control.Parent, panel))
                {
                    panel.AddChild(control);
                    children = panel.VisualChildren;
                    changed = true;
                }

                if (children == null || i >= children.Count || ReferenceEquals(children[i], control))
                    continue;

                int currentIndex = IndexOfChild(children, control);
                if (currentIndex < 0)
                    continue;

                if (panel.MoveChild(control, i))
                    changed = true;
            }

            if (changed)
                panel.InvalidateLayout();
        }

        void EnsureEntryControlCount(int count)
        {
            while (_entryControls.Count < count)
            {
                _entryControls.Add(new RectangleControl(default(RectangleF))
                {
                    CustomRender = RenderListEntryControl
                });
            }
        }

        void RemoveExtraPanelChildren(Panel panel, int desiredCount)
        {
            var children = panel.VisualChildren;
            if (children == null)
                return;

            for (int i = children.Count - 1; i >= desiredCount; i--)
                panel.RemoveChild(children[i] as ControlTemplate);
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

        void RenderListEntryControl(ControlTemplate control, List<MySprite> frame)
        {
            var entry = control?.DataContext as Entry;
            if (entry == null)
                return;

            DrawRow(frame, entry, control.Bounds);
        }

        void RenderGridEntryControl(ControlTemplate control, List<MySprite> frame)
        {
            var entry = control?.DataContext as Entry;
            if (entry == null)
                return;

            DrawGridCell(frame, entry, control.Bounds.X, control.Bounds.Right, control.Bounds.Y, control.Bounds.Height);
        }

        void RenderGridPanelContent(ControlTemplate control, List<MySprite> sprites)
        {
            var children = control?.VisualChildren;
            if (children == null)
                return;

            if (GeneralComponent.DrawLines)
            {
                var layout = WrapPanelLayout.Create(
                    control.Bounds,
                    _gridPanel.RowHeight,
                    _gridPanel.MinimumColumnWidth,
                    children.Count,
                    0,
                    true);
                DrawGridLines(sprites, layout);
            }

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i] as ControlTemplate;
                if (child != null)
                    child.Render(sprites);
            }
        }

        void DrawGridLines(List<MySprite> sprites, WrapPanelLayout layout)
        {
            var lineColor = GetHeaderColor();
            var contentStart = _scrollPanel.ContentBounds.X;
            var contentEnd = _scrollPanel.ContentBounds.Right;
            var gridHeight = _scrollPanel.ContentBounds.Height;

            for (int row = 0; row <= _scrollPanel.MaxVisibleRows; row++)
            {
                var y = _scrollPanel.ContentBounds.Y + row * layout.RowHeight;
                sprites.Add(new MySprite { Type = SpriteType.TEXTURE, Data = "SquareSimple", Position = new Vector2((contentStart + contentEnd) / 2f, y), Size = new Vector2(contentEnd - contentStart, 2f), Color = lineColor, Alignment = TextAlignment.CENTER });
            }

            var lineCenterY = _scrollPanel.ContentViewportBounds.Y + gridHeight / 2f;
            sprites.Add(new MySprite { Type = SpriteType.TEXTURE, Data = "SquareSimple", Position = new Vector2(contentStart, lineCenterY), Size = new Vector2(2f, gridHeight), Color = lineColor, Alignment = TextAlignment.CENTER });
            sprites.Add(new MySprite { Type = SpriteType.TEXTURE, Data = "SquareSimple", Position = new Vector2(contentEnd, lineCenterY), Size = new Vector2(2f, gridHeight), Color = lineColor, Alignment = TextAlignment.CENTER });
        }

        void DrawRow(List<MySprite> frame, Entry entry, RectangleF bounds)
        {
            var pct = MathHelper.Clamp(entry.Percentage, 0f, 1f);
            Vector2 position = bounds.Position;

            if (GeneralComponent.DrawLines)
                frame.Add(new MySprite { Type = SpriteType.TEXTURE, Data = "SquareSimple", Position = new Vector2(bounds.Center.X, position.Y), Size = new Vector2(bounds.Width, 2f), Color = Host.ForegroundColor, Alignment = TextAlignment.CENTER });

            var barMargin = 8 * GeneralComponent.GetScale();
            Vector2 size = new Vector2(bounds.Width, bounds.Height) - barMargin;
            var rowClip = new RectangleF(
                bounds.X,
                bounds.Y,
                Math.Max(0f, bounds.Width - 145f * GeneralComponent.GetScale()),
                bounds.Height);

            if (BeginNestedClip(frame, rowClip))
            {
                var activeRowClip = Intersect(rowClip, _scrollPanel.ContentViewportBounds);
                DrawClippedProgressBar(
                    frame,
                    new Vector2(position.X, position.Y + GeneralComponent.GetScale()) + barMargin / 2f,
                    size,
                    pct,
                    activeRowClip);
                position.X += 16 * GeneralComponent.GetScale();
                position.Y += 4 * GeneralComponent.GetScale();
                frame.Add(new MySprite { Type = SpriteType.TEXT, Data = entry.Name, Position = position, RotationOrScale = GeneralComponent.GetScale(), Color = Host.Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, FontId = TextFont });
                EndNestedClipAndRestoreScrollClip(frame);
            }

            position.X = bounds.Right;
            frame.Add(new MySprite { Type = SpriteType.TEXT, Data = FormatingHelper.PercentageToString(pct), Position = position, RotationOrScale = GeneralComponent.GetScale(), Color = Host.Surface.ScriptForegroundColor, Alignment = TextAlignment.RIGHT, FontId = TextFont });
        }

        void DrawClippedProgressBar(
            List<MySprite> frame,
            Vector2 topLeft,
            Vector2 size,
            float pct,
            RectangleF rowClip)
        {
            var bgColor = Host.BackgroundColor.DeriveAccentColor();
            var fillColor = GetHeaderColor();
            var fillOverride = GetEntryUsageColor(pct);

            BarPanel.CreateSprites(frame, topLeft, size, fillColor, bgColor, 0f);

            var fillWidth = MathHelper.Clamp(pct, 0f, 1f) * Math.Max(1f, size.X);
            if (fillWidth <= 0.001f)
                return;

            var fillClip = Intersect(
                new RectangleF(topLeft.X, topLeft.Y, fillWidth, Math.Max(1f, size.Y)),
                rowClip);
            if (fillClip.Width <= 0f || fillClip.Height <= 0f)
                return;

            AddClip(frame, fillClip);
            BarPanel.CreateSprites(frame, topLeft, size, fillColor, bgColor, 1f, fillOverride);
            EndNestedClipAndRestoreClip(frame, rowClip);
        }

        bool BeginNestedClip(List<MySprite> sprites, RectangleF bounds)
        {
            var clip = Intersect(bounds, _scrollPanel.ContentViewportBounds);
            if (clip.Width <= 0f || clip.Height <= 0f)
                return false;

            AddClip(sprites, clip);
            return true;
        }

        void EndNestedClipAndRestoreScrollClip(List<MySprite> sprites)
        {
            if (sprites == null)
                return;

            sprites.Add(MySprite.CreateClearClipRect());
            AddClip(sprites, _scrollPanel.ContentViewportBounds);
        }

        static void EndNestedClipAndRestoreClip(List<MySprite> sprites, RectangleF clip)
        {
            if (sprites == null)
                return;

            sprites.Add(MySprite.CreateClearClipRect());
            AddClip(sprites, clip);
        }

        static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            float x = Math.Max(a.X, b.X);
            float y = Math.Max(a.Y, b.Y);
            float right = Math.Min(a.Right, b.Right);
            float bottom = Math.Min(a.Bottom, b.Bottom);
            return new RectangleF(x, y, Math.Max(0f, right - x), Math.Max(0f, bottom - y));
        }

        static void AddClip(List<MySprite> sprites, RectangleF bounds)
        {
            if (sprites == null)
                return;

            int x = (int)Math.Floor(bounds.X);
            int y = (int)Math.Floor(bounds.Y);
            int right = (int)Math.Ceiling(bounds.Right);
            int bottom = (int)Math.Ceiling(bounds.Bottom);
            sprites.Add(MySprite.CreateClipRect(new Rectangle(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y))));
        }

        void DrawGridCell(List<MySprite> frame, Entry entry, float xStart, float xEnd, float yStart, float rowHeight)
        {
            var cellPadding = (LINE_HEIGHT * GeneralComponent.GetScale()) / 3f;
            var pct = MathHelper.Clamp(entry.Percentage, 0f, 1f);
            var cellView = GetCellViewBox(xStart, xEnd, yStart, rowHeight, cellPadding);

            if (!GeneralComponent.DrawLines)
            {
                var backgroundColor = GetHeaderColor();
                var hsv = VRageMath.ColorExtensions.ColorToHSV(backgroundColor);
                hsv.Z *= 0.2f;
                var cellRect = new RectangleF(xStart + cellPadding / 2f, yStart + cellPadding / 2f, (xEnd - xStart) - cellPadding, rowHeight - cellPadding);
                var dropShadow = new RectangleF(cellRect.Position + 2, cellRect.Size);
                BorderRenderer.CreateSpritesFromRect(dropShadow, frame, hsv.HSVtoColor(),
                    radiusScale: GeneralComponent.GetScale());
                BorderRenderer.CreateSpritesFromRect(cellRect, frame, backgroundColor,
                    radiusScale: GeneralComponent.GetScale());
            }

            var nameHeight = Math.Max(0f, cellView.Height * .45f);
            var nameRect = new RectangleF(cellView.X, cellView.Y, cellView.Width, nameHeight);
            var bottomRect = new RectangleF(cellView.X, nameRect.Bottom, cellView.Width, Math.Max(0f, cellView.Bottom - nameRect.Bottom));
            var name = TrimmedName(entry, nameRect.Width);
            frame.Add(new MySprite { Type = SpriteType.TEXT, Data = name, Position = new Vector2(nameRect.X + 2f * GeneralComponent.GetScale(), nameRect.Y + 2f * GeneralComponent.GetScale()), RotationOrScale = .9f * GeneralComponent.GetScale(), Color = Host.Surface.ScriptForegroundColor, Alignment = TextAlignment.LEFT, FontId = TextFont });

            var barWidth = bottomRect.Width * (2f / 3f);
            var textRect = new RectangleF(bottomRect.X + barWidth, bottomRect.Y, bottomRect.Width - barWidth, bottomRect.Height);
            var barRect = new RectangleF(bottomRect.X, bottomRect.Y, barWidth, bottomRect.Height);
            var barInnerPaddingX = 2f * GeneralComponent.GetScale();
            var barInnerPaddingY = bottomRect.Height * 0.2f;
            var fillColor = Extensions.ColorExtensions.DeriveAccentColor(GetHeaderColor(), .4f, 0.5);
            BarPanel.CreateSprites(frame, new Vector2(barRect.X + barInnerPaddingX, barRect.Y + barInnerPaddingY + (2f * GeneralComponent.GetScale())), new Vector2(Math.Max(1f, barRect.Width - 2f * barInnerPaddingX), Math.Max(1f, barRect.Height - 2f * barInnerPaddingY)), fillColor, fillColor.DeriveAccentColor(.6f, 0.7), pct, GetEntryUsageColor(pct));
            frame.Add(new MySprite { Type = SpriteType.TEXT, Data = FormatingHelper.PercentageToString(pct), Position = new Vector2(textRect.Right - (2f * GeneralComponent.GetScale()), textRect.Y + 2f * GeneralComponent.GetScale()), RotationOrScale = .95f * GeneralComponent.GetScale(), Color = Host.Surface.ScriptForegroundColor, Alignment = TextAlignment.RIGHT, FontId = TextFont });
        }

        Color? GetEntryUsageColor(float pct)
        {
            if (pct <= .10f)
                return GetErrorColor();
            if (pct <= .25f)
                return GetWarningColor();
            return null;
        }

        float GetContentTop()
        {
            return Host.TitleVisible ? Host.ViewBox.Y + (40f * GeneralComponent.GetScale() * Host.Surface.FontSize) : Host.ViewBox.Y;
        }

        RectangleF GetCellViewBox(float xStart, float xEnd, float yStart, float cellHeight, float cellPadding)
        {
            var innerLeft = xStart + cellPadding;
            var innerRight = xEnd - cellPadding;
            var innerTop = yStart + cellPadding;
            var innerBottom = yStart + cellHeight - cellPadding;
            return new RectangleF(innerLeft, innerTop, innerRight - innerLeft, innerBottom - innerTop);
        }

        /// <summary>
        /// Nome recortado para caber na largura, com busca binária no comprimento e cache no próprio Entry.
        /// </summary>
        string TrimmedName(Entry entry, float availableWidth, float fontSize = 1f)
        {
            var name = entry.Name ?? string.Empty;
            var scale = fontSize * GeneralComponent.GetScale();
            if (entry.TrimmedSource == name && entry.TrimmedWidth == availableWidth && entry.TrimmedScale == scale)
                return entry.TrimmedName;

            var result = name;
            if (Measure(name, scale) > availableWidth)
            {
                int low = 0, high = name.Length - 1;
                result = string.Empty;
                while (low <= high)
                {
                    int mid = (low + high) / 2;
                    var candidate = mid == 0 ? string.Empty : FormatingHelper.TrimName(name, mid);
                    if (Measure(candidate, scale) <= availableWidth)
                    {
                        result = candidate;
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid - 1;
                    }
                }
            }

            entry.TrimmedSource = name;
            entry.TrimmedWidth = availableWidth;
            entry.TrimmedScale = scale;
            entry.TrimmedName = result;
            return result;
        }

        float Measure(string text, float scale)
        {
            TrimBuffer.Clear();
            TrimBuffer.Append(text);
            return Host.Surface.MeasureStringInPixels(TrimBuffer, TextFont, scale).X;
        }

        void ReadEntries(List<Entry> entries, ref int count)
        {
            ReadEntries(Host.GridLogic, Host.Block as IMyTerminalBlock, entries, ref count, Host.GetType());
        }

        static void SetEntry(List<Entry> entries, ref int count, string name, float percentage)
        {
            Entry entry;
            if (count < entries.Count)
            {
                entry = entries[count];
            }
            else
            {
                entry = new Entry();
                entries.Add(entry);
            }

            entry.Name = name;
            entry.Percentage = percentage;
            count++;
        }

        string ResolveFilterToken(IMyTerminalBlock sourceBlock)
        {
            var sourceName = sourceBlock != null ? sourceBlock.CustomName : null;
            if (!string.Equals(sourceName, _filterSourceName, StringComparison.Ordinal))
            {
                string mode;
                ParseFilter(sourceBlock, out mode, out _filterToken);
                _filterSourceName = sourceName;
            }

            return _filterToken;
        }

        string ResolveTankDisplayName(IMyTerminalBlock terminal, Type logType)
        {
            var customName = terminal.CustomName;
            TankName cached;
            if (_tankNames.TryGetValue(terminal.EntityId, out cached) &&
                string.Equals(cached.CustomName, customName, StringComparison.Ordinal))
                return cached.Display;

            var tankName = customName;
            if (string.IsNullOrEmpty(tankName))
                tankName = terminal.DisplayNameText;
            if (string.IsNullOrEmpty(tankName))
                tankName = terminal.BlockDefinition.SubtypeName;
            if (string.IsNullOrEmpty(tankName))
                tankName = "Gas Tank";

            var gasSubtype = GetStoredGasSubtype(terminal, logType);
            var gasName = GetGasDisplayNameCached(gasSubtype, logType);
            var display = string.IsNullOrEmpty(gasName) ? tankName : gasName + " - "+ tankName;

            if (cached == null)
            {
                cached = new TankName();
                _tankNames[terminal.EntityId] = cached;
            }

            cached.CustomName = customName;
            cached.Display = display;
            return display;
        }

        void ReadEntries(GridLogic gridLogic, IMyTerminalBlock sourceBlock, List<Entry> entries, ref int count, Type logType)
        {
            var token = ResolveFilterToken(sourceBlock);

            if (gridLogic == null)
                return;

            _tanks.Bind(gridLogic, (GridLinkTypeEnum)BlockSelectionComponent.GridLinkTypeInternal);
            var sources = _tanks.Sources;
            for (var sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                var tanks = sources[sourceIndex];
                for (var i = 0; i < tanks.Count; i++)
                {
                    var tank = tanks[i];
                    if (tank == null)
                        continue;

                    var terminal = (IMyTerminalBlock)tank;

                    if (!string.IsNullOrEmpty(token))
                    {
                        var customName = terminal.CustomName ?? string.Empty;
                        if (customName.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                    }

                    float ratio;
                    try
                    {
                        ratio = (float)tank.FilledRatio;
                    }
                    catch (Exception e)
                    {
                        ErrorHandlerHelper.LogError(e, logType);
                        continue;
                    }

                    SetEntry(entries, ref count, ResolveTankDisplayName(terminal, logType), ratio);
                }
            }
        }

        string GetStoredGasSubtype(IMyTerminalBlock tank, Type logType)
        {
            var definitionId = tank.BlockDefinition;
            string subtype;
            if (_gasSubtypeByDefinition.TryGetValue(definitionId, out subtype))
                return subtype;

            subtype = string.Empty;
            try
            {
                var defBase = MyDefinitionManager.Static.GetCubeBlockDefinition(definitionId);
                var gasDef = defBase as MyGasTankDefinition;
                if (gasDef != null && !string.IsNullOrEmpty(gasDef.StoredGasId.SubtypeName))
                    subtype = gasDef.StoredGasId.SubtypeName;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, logType);
            }

            _gasSubtypeByDefinition[definitionId] = subtype;
            return subtype;
        }

        string GetGasDisplayNameCached(string subtype, Type logType)
        {
            if (string.IsNullOrEmpty(subtype))
                return string.Empty;

            string display;
            if (_gasDisplayNameCache.TryGetValue(subtype, out display))
                return display;

            display = GetGasDisplayName(subtype, logType);
            _gasDisplayNameCache[subtype] = display;
            return display;
        }

        static string GetGasDisplayName(string subtype, Type logType)
        {
            try
            {
                var id = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), subtype);

                MyGasProperties def;
                if (MyDefinitionManager.Static.TryGetDefinition(id, out def))
                {
                    var s = def.DisplayNameString;
                    if (!string.IsNullOrEmpty(s))
                        return s;

                    if (def.DisplayNameEnum.HasValue)
                    {
                        var sb = MyTexts.Get(def.DisplayNameEnum.Value);
                        if (sb != null)
                        {
                            s = sb.ToString();
                            if (!string.IsNullOrEmpty(s))
                                return s;
                        }
                    }

                    if (!string.IsNullOrEmpty(def.DisplayNameText))
                        return def.DisplayNameText;
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, logType);
            }

            return subtype;
        }

        static readonly System.Text.RegularExpressions.Regex RxGroup =
            new System.Text.RegularExpressions.Regex("\\(\\s*G\\s*:\\s*(.+?)\\s*\\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        static readonly System.Text.RegularExpressions.Regex RxContainer =
            new System.Text.RegularExpressions.Regex("\\(\\s*(?!G\\s*:)(.+?)\\s*\\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        static void ParseFilter(IMyTerminalBlock block, out string mode, out string token)
        {
            mode = null;
            token = null;
            if (block == null)
                return;

            var name = block.CustomName ?? string.Empty;
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

        public class Entry
        {
            public string Name;
            public float Percentage;
            public string TrimmedSource;
            public string TrimmedName;
            public float TrimmedWidth;
            public float TrimmedScale;
        }
    }
}
