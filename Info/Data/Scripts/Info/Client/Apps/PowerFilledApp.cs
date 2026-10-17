using LcdMod.Common.Config.Components;
using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Extensions;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Panels.Virtualized;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Gui.UserControls.Power;
using LcdMod.Client.Helpers;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRageMath;

using LcdMod.Common.Config.Generation;
using LcdMod.Common.Helpers;

namespace LcdMod.Client.Apps
{
    
    
    internal sealed partial class PowerFilledApp : App, IApp
    {
        const float BATTERY_SLOT_W = 100f;
        const float BATTERY_SLOT_H = 100f;
        const float MINIMAL_HERO_FONT_MUL = 3f;
        const float MINIMAL_LINE_FONT_MUL = 1f;

        readonly IAppHost _surfaceHost;
        readonly List<PowerCollector> _collectors = new List<PowerCollector>();
        readonly List<BareLayout.KvRow> _kvRows = new List<BareLayout.KvRow>();
        readonly List<PowerEntry> _entries = new List<PowerEntry>();
        readonly List<Control> _children = new List<Control>();
        readonly Dictionary<long, PowerEntry> _entryById = new Dictionary<long, PowerEntry>();
        readonly ScrollPanel _scrollPanel;
        readonly VirtualizedWrapPanel<PowerEntry> _gridPanel;
        
        public override IReadOnlyList<Control> VisualChildren => _children;

        public PowerFilledApp(IAppHost surfaceHost) : base(surfaceHost)
        {
            _surfaceHost = surfaceHost;
            _scrollPanel = AddLogicalChild(new ScrollPanel(dataContext: this));
            _scrollPanel.ScrollChanged = OnScrollPanelChanged;
            _scrollPanel.SetVisible(false);
            _gridPanel = new VirtualizedWrapPanel<PowerEntry>
            {
                CreateControl = CreatePowerEntryHitbox,
                BindControl = BindPowerEntryHitbox
            };
        }

        public override void LayoutChanged()
        {
            CloseCollectors();
            _entries.Clear();
            _entryById.Clear();
        }

        public override void Update()
        {
            if (_scrollPanel.UpdateAutoScroll())
                MarkDirty();

            if (_collectors.Count == 0)
                BuildCollectors();

            var gridLogic = _surfaceHost.GridLogic;
            if (gridLogic == null)
                return;

            _entries.Clear();
            _entryById.Clear();

            for (int i = 0; i < _collectors.Count; i++)
                _collectors[i].Collect(gridLogic, _entries);

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry != null)
                    _entryById[entry.EntryId] = entry;
            }
        }

        public override bool HasVisibleItems()
        {
            for (int i = 0; i < _collectors.Count; i++)
            {
                if (_collectors[i] != null && _collectors[i].HasVisibleItems)
                    return true;
            }

            return false;
        }

        public PowerEntry GetPowerEntry(long entryId)
        {
            PowerEntry entry;
            return _entryById.TryGetValue(entryId, out entry) ? entry : null;
        }

        readonly List<MySprite> _sprites = new List<MySprite>();

        public override List<MySprite> GetSprites()
        {
            BeginPowerEntryHitboxFrame();
            var sprites = _sprites;
            sprites.Clear();
            if (IsMinimal)
            {
                DrawMinimal(sprites);
                return sprites;
            }
            if (IsCompact)
            {
                DrawCompact(sprites);
                return sprites;
            }
            DrawFooter(_surfaceHost, sprites);
            DrawBatteries(_surfaceHost, sprites);
            return sprites;
        }

        public override void Close()
        {
            CloseCollectors();
            base.Close();
        }

        void CloseCollectors()
        {
            for (int i = 0; i < _collectors.Count; i++)
                _collectors[i].Dispose();

            _collectors.Clear();
        }

        void BuildCollectors()
        {
            _collectors.Clear();

            var labelCharging = MyTexts.GetString("HudEnergyGroupCharging");
            var labelDischarging = MyTexts.GetString("BlockActionTitle_Discharge");
            var labelJumpNotReady = MyTexts.GetString("ScreenMedicals_RespawnShipNotReady");
            var labelJumpReady = MyTexts.GetString("ScreenMedicals_RespawnShipReady");
            var labelFull = MyTexts.GetString("RadialMenuAction_Signal_Full");

            _collectors.Add(new BatteryPowerCollector(Host, () => PowerComponent, () => ColorComponent)
            {
                ChargingLabel = labelCharging,
                DischargingLabel = labelDischarging,
                FullLabel = labelFull
            });

            _collectors.Add(new JumpDrivePowerCollector(Host, () => PowerComponent, () => ColorComponent)
            {
                ChargingLabel = labelCharging,
                ReadyLabel = labelJumpReady,
                NotReadyLabel = labelJumpNotReady
            });
        }

        void DrawBatteries(IAppHost owner, List<MySprite> sprites)
        {
            float minW = BATTERY_SLOT_W * owner.ConfiguredScale;
            float minH = BATTERY_SLOT_H * owner.ConfiguredScale;
            float contentTop = GetContentTop(owner) + 6f * owner.ConfiguredScale;
            float footerHeight = GetFooterHeight(owner);

            int count = _entries.Count;
            if (count <= 0)
                return;

            _scrollPanel.SetContent(_gridPanel);
            _gridPanel.RowHeight = minH;
            _gridPanel.MinimumColumnWidth = minW;
            _gridPanel.HorizontalGap = 0f;
            _gridPanel.VerticalGap = 0f;
            _gridPanel.ItemsSource = _entries;
            ConfigurePowerScrollPanel(owner, contentTop, footerHeight, minH);
            _scrollPanel.Render(sprites);
        }

        void ConfigurePowerScrollPanel(IAppHost owner, float contentTop, float footerHeight, float rowHeight)
        {
            var viewportHeight = Math.Max(0f, owner.ViewBox.Bottom - contentTop - Math.Max(0f, footerHeight));
            _scrollPanel.ConfigureAutomatic(
                new RectangleF(owner.ViewBox.X, contentTop, owner.ViewBox.Width, viewportHeight),
                ScrollPanel.DEFAULT_SCROLLER_WIDTH_PIXELS * owner.ConfiguredScale,
                rowHeight);
            _scrollPanel.SetVisible(true);
            if (!_children.Contains(_scrollPanel))
                _children.Add(_scrollPanel);
        }

        void BeginPowerEntryHitboxFrame()
        {
            _children.Clear();
            _scrollPanel.SetVisible(false);
        }

        RectangleControl CreatePowerEntryHitbox(PowerEntry entry)
        {
            var control = new RectangleControl(default(RectangleF), entry)
            {
                CustomRender = RenderPowerEntryHitbox
            };
            control.SetClass("ControlBase PowerFilledDetails");
            return control;
        }

        void BindPowerEntryHitbox(ControlTemplate control, PowerEntry entry, int index)
        {
            if (control == null)
                return;

            control.SetDataContext(entry);
            control.SetClass("ControlBase PowerFilledDetails");
            control.CustomRender = RenderPowerEntryHitbox;
            control.SetVisible(entry != null);
        }

        void OnScrollPanelChanged(ScrollPanel panel)
        {
            Host.RenderSprites();
        }

        void RenderPowerEntryHitbox(ControlTemplate hitbox, List<MySprite> sprites)
        {
            if (hitbox == null)
                return;

            var entry = hitbox.DataContext as PowerEntry;
            if (entry == null)
                return;

            entry = GetPowerEntry(entry.EntryId);
            if (entry == null)
                return;

            DrawPowerSlotVisual(_surfaceHost, sprites, entry, hitbox.Bounds);
        }

        void DrawFooter(IAppHost owner, List<MySprite> sprites)
        {
            int rows = 0;
            for (int i = 0; i < _collectors.Count; i++)
                if (_collectors[i] != null && _collectors[i].HasVisibleItems)
                    rows++;

            if (rows == 0)
            {
                return;
            }

            float rowHeight = 40f * owner.ConfiguredScale * owner.FontScale;
            float footerHeight = rowHeight * rows;
            float footerTop = owner.ViewBox.Bottom - footerHeight;
            float footerLeft = owner.ViewBox.X;
            float footerWidth = Math.Max(1f, owner.ViewBox.Width);
            float footerPad = 6f * owner.ConfiguredScale;
            float contentLeft = footerLeft + footerPad;
            float contentRight = footerLeft + footerWidth - footerPad;
            Color fg = owner.ForegroundColor;

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Position = new Vector2(owner.ViewBox.X + owner.ViewBox.Width * 0.5f, footerTop + footerHeight * 0.5f),
                Size = new Vector2(owner.ViewBox.Width, footerHeight),
                Color = new Color(owner.BackgroundColor.MulValue(0.8f), 0.5f),
                Alignment = TextAlignment.CENTER
            });

            float iconLeft = contentLeft;
            float textScale = owner.ConfiguredScale * 0.75f * owner.FontScale;
            float textLeftPad = Math.Max(2f, owner.ConfiguredScale * 2f);
            int rowIndex = 0;

            for (int i = 0; i < _collectors.Count; i++)
            {
                var collector = _collectors[i];
                if (collector == null || !collector.HasVisibleItems)
                    continue;
                DrawFooterRow(owner, sprites, collector, footerTop, rowHeight, rowIndex, iconLeft, contentRight, textScale, textLeftPad, fg);
                rowIndex++;
            }
        }

        void DrawFooterRow(IAppHost owner, List<MySprite> sprites, PowerCollector collector, float footerTop, float rowHeight,
            int rowIndex, float iconLeft, float contentRight, float textScale, float textLeftPad, Color fg)
        {
            float bandCy = footerTop + rowHeight * (rowIndex + 0.5f);
            float iconSize = rowHeight * 0.55f;
            var iconCenter = new Vector2(iconLeft + iconSize / 2f, bandCy);
            float displayedRatio = (float)Math.Round(collector.AverageCharge * 100f, MidpointRounding.AwayFromZero) / 100f;

            DrawFillableTexture(sprites, collector.FillableTexture, iconCenter, iconSize, displayedRatio, collector.StatusColor, fg,
                collector.DrawCenterIcon, 0f, collector.CenterIconScale);

            string avgText = FormatingHelper.PercentageToString(displayedRatio) + " "+ collector.FooterPrefix;
            float textLeft = iconLeft + iconSize + textLeftPad;
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = avgText,
                Position = new Vector2(textLeft, bandCy - FormatingHelper.GetSizeInPixel(avgText, this, textScale, owner.Surface).Y / 2f),
                RotationOrScale = textScale,
                Color = fg,
                Alignment = TextAlignment.LEFT,
                FontId = TextFont
            });

            if (!string.IsNullOrEmpty(collector.StatusText))
            {
                sprites.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = collector.StatusText,
                    Position = new Vector2(owner.ViewBox.Center.X, bandCy - FormatingHelper.GetSizeInPixel(collector.StatusText, this, textScale, owner.Surface).Y / 2f),
                    RotationOrScale = textScale,
                    Color = collector.StatusColor,
                    Alignment = TextAlignment.CENTER,
                    FontId = TextFont
                });
            }

            if (!collector.HasRightSideText)
                return;

            var rightText = collector.RightSideText;
            var size = FormatingHelper.GetSizeInPixel(rightText, this, textScale, owner.Surface);
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = rightText,
                Position = new Vector2(contentRight, bandCy - size.Y / 2f),
                RotationOrScale = textScale,
                Color = collector.RightSideColor,
                Alignment = TextAlignment.RIGHT,
                FontId = TextFont
            });
        }

        public void DrawPowerSlotVisual(IAppHost owner, List<MySprite> sprites, PowerEntry slot, RectangleF bounds)
        {
            float width = bounds.Width;
            float height = bounds.Height;
            float labelGap = Math.Max(1f, owner.ConfiguredScale * 2f);
            Vector2 pctRef = FormatingHelper.GetSizeInPixel(slot.PercentText, this, 1f, owner.Surface);
            float pctScale = Math.Min((width * 0.6f) / Math.Max(1f, pctRef.X), (height * 0.22f) / Math.Max(1f, pctRef.Y)) * Math.Min(owner.FontScale, 1f);
            float pctH = pctRef.Y * pctScale;
            float iconSize = Math.Max(0f, Math.Min(width, height - pctH - labelGap));
            float centerX = bounds.X + width / 2f;
            float centerY = bounds.Y + iconSize / 2f;

            DrawFillableTexture(sprites, slot.FillableTexture, new Vector2(centerX, centerY), iconSize, slot.Ratio, slot.FillColor,
                owner.ForegroundColor, slot.DrawCenterIcon, slot.CenterIconRotation, slot.CenterIconScale);

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = slot.PercentText,
                Position = new Vector2(centerX, bounds.Y + iconSize + labelGap),
                RotationOrScale = pctScale,
                Color = owner.ForegroundColor,
                Alignment = TextAlignment.CENTER,
                FontId = TextFont
            });
        }

        PowerCollector MinimalCollector()
        {
            for (int i = 0; i < _collectors.Count; i++)
            {
                var collector = _collectors[i];
                if (collector != null && collector.HasVisibleItems)
                    return collector;
            }
            return _collectors.Count > 0 ? _collectors[0] : null;
        }

        int CountEntries(PowerCollector collector)
        {
            int count = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry != null && entry.FillableTexture == collector.FillableTexture)
                    count++;
            }
            return count;
        }

        static float DisplayedRatio(PowerCollector collector)
        {
            return (float)Math.Round(collector.AverageCharge * 100f, MidpointRounding.AwayFromZero) / 100f;
        }

        void DrawMinimal(List<MySprite> sprites)
        {
            var collector = MinimalCollector();
            if (collector == null)
                return;

            var view = Host.ViewBox;
            Color fg = Host.ForegroundColor;
            float margin = Bare.Margin;
            float lineHeight = Bare.LineHeight(MINIMAL_LINE_FONT_MUL);
            bool hasItems = collector.HasVisibleItems;
            float ratio = hasItems ? DisplayedRatio(collector) : 0f;
            string percent = hasItems ? FormatingHelper.PercentageToString(ratio) : null;
            string line = null;
            if (hasItems)
            {
                string count = CountEntries(collector).ToString(FormatingHelper.Culture);
                line = string.IsNullOrEmpty(collector.StatusText) ? count : count + " · " + collector.StatusText;
            }

            RectangleF iconArea;
            Vector2 linePos;
            TextAlignment lineAlign;
            float lineMaxWidth;
            if (BareLayout.IsWide(view))
            {
                iconArea = new RectangleF(view.X, view.Y, view.Width * 0.5f, view.Height);
                float left = iconArea.Right + margin;
                linePos = new Vector2(left, view.Center.Y - lineHeight * 0.5f);
                lineAlign = TextAlignment.LEFT;
                lineMaxWidth = view.Right - left;
            }
            else
            {
                iconArea = new RectangleF(view.X, view.Y, view.Width, view.Height - lineHeight - margin);
                linePos = new Vector2(view.Center.X, iconArea.Bottom + margin);
                lineAlign = TextAlignment.CENTER;
                lineMaxWidth = view.Width;
            }

            float iconSize = Math.Min(iconArea.Width, iconArea.Height) - margin * 2f;
            if (iconSize <= 0f)
                return;

            Vector2 center = iconArea.Center;
            DrawFillableTexture(sprites, collector.FillableTexture, center, iconSize, ratio, hasItems ? collector.StatusColor : fg, fg,
                collector.DrawCenterIcon, 0f, collector.CenterIconScale);

            var inner = collector.FillableTexture.GetInnerRect(center, iconSize);
            float heroHeight = Bare.LineHeight(MINIMAL_HERO_FONT_MUL);
            Bare.Text(sprites, percent, new Vector2(inner.Center.X, inner.Center.Y - heroHeight * 0.5f), TextAlignment.CENTER, MINIMAL_HERO_FONT_MUL, fg, inner.Width, inner.Height);
            Bare.Text(sprites, line, linePos, lineAlign, MINIMAL_LINE_FONT_MUL, fg, lineMaxWidth, lineHeight);
        }

        void DrawCompact(List<MySprite> sprites)
        {
            Color fg = Host.ForegroundColor;
            _kvRows.Clear();
            for (int i = 0; i < _collectors.Count; i++)
            {
                var collector = _collectors[i];
                if (collector == null || !collector.HasVisibleItems)
                    continue;
                float ratio = DisplayedRatio(collector);
                _kvRows.Add(new BareLayout.KvRow
                {
                    Label = collector.FooterPrefix,
                    Icon = collector.FillableTexture.Name,
                    Value = FormatingHelper.PercentageToString(ratio),
                    ValueColor = collector.StatusColor,
                    Bar = collector.AverageCharge,
                    BarColor = collector.StatusColor
                });
                if (collector is BatteryPowerCollector && collector.HasRightSideText)
                {
                    _kvRows.Add(new BareLayout.KvRow
                    {
                        Label = LocHelper.GetLoc(Constants.MOD_PREFIX + "Frame_Autonomy"),
                        Icon = BareIcons.Hourglass,
                        Value = collector.RightSideText,
                        ValueColor = collector.RightSideColor,
                        Bar = -1f,
                        BarColor = fg
                    });
                }
            }

            if (_kvRows.Count == 0)
            {
                Host.DrawMessage(sprites, LocHelper.Empty, BareIcons.Warning, GetWarningColor());
                return;
            }

            Bare.Kv(sprites, Host.ViewBox, _kvRows, fg, Theme);
        }

        static void DrawFillableTexture(List<MySprite> sprites, FillableTexture texture, Vector2 center, float iconSize, float ratio, Color fillColor, Color iconColor,
            bool drawCenterIcon = true, float centerIconRotation = 0f, float centerIconScale = 1f)
        {
            ratio = MathHelper.Clamp(ratio, 0f, 1f);
            var innerRect = texture.GetInnerRect(center, iconSize);
            float innerLeft = innerRect.X;
            float innerTop = innerRect.Y;
            float innerRight = innerRect.Right;
            float innerBottom = innerRect.Bottom;
            float innerW = innerRect.Width;
            float innerH = innerRect.Height;

            if (ratio > 0.005f && innerW > 0f && innerH > 0f)
            {
                float fillH = innerH * ratio;
                sprites.Add(new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2((innerLeft + innerRight) / 2f, innerBottom - fillH / 2f),
                    Size = new Vector2(innerW, fillH),
                    Color = fillColor,
                    Alignment = TextAlignment.CENTER
                });
            }

            sprites.Add(new MySprite { Type = SpriteType.TEXTURE, Data = texture.Name, Position = center, Size = new Vector2(iconSize), Color = iconColor, Alignment = TextAlignment.CENTER });

            if (!drawCenterIcon || string.IsNullOrEmpty(texture.CenterIconTexture))
                return;

            float innerMin = Math.Min(innerW, innerH);
            float centerIconSize = innerMin > 0f ? innerMin * centerIconScale : iconSize * centerIconScale;
            Vector2 centerIconPos = innerMin > 0f ? new Vector2((innerLeft + innerRight) / 2f, (innerTop + innerBottom) / 2f) : center;
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = texture.CenterIconTexture,
                Position = centerIconPos,
                Size = new Vector2(centerIconSize),
                RotationOrScale = centerIconRotation,
                Color = iconColor,
                Alignment = TextAlignment.CENTER
            });
        }

        float GetContentTop(IAppHost owner)
        {
            return owner.TitleVisible ? owner.ViewBox.Y + (40f * owner.ConfiguredScale * owner.FontScale) : owner.ViewBox.Y;
        }

        float GetFooterHeight(IAppHost owner)
        {
            int rows = 0;
            for (int i = 0; i < _collectors.Count; i++)
                if (_collectors[i] != null && _collectors[i].HasVisibleItems)
                    rows++;
            if (rows == 0)
                return 0f;
            return (40f * owner.ConfiguredScale * owner.FontScale) * rows;
        }
    }
}
