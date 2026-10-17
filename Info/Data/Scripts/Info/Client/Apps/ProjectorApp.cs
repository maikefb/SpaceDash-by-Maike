using LcdMod.Common.Config.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Apps.ViewModel;
using LcdMod.Client.Config;
using LcdMod.Client.Extensions;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.ControlsTemplates.Basic;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Terminal.Controls;
using LcdMod.Common.Helpers;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.Apps
{
    
    
    
    internal sealed partial class ProjectorApp : ItemsApp
    {
        public const string TITLE = MOD_PREFIX + "Projector";

        protected override string DefaultTitle => _customTitle ?? TITLE;
        protected override ItemDisplayMode PresentationMode =>
            ItemDisplayComponent.ResolveDisplayMode(GeneralComponent);

        string _customTitle;
        readonly ProjectorViewModel _projectorViewModel;
        bool _showIngots;

        string _required = "Req";
        string _available = "Ava";

        float _requiredX;
        float _availableX;
        bool _projectorDataInitialized;

        const float PIE_RADIUS = 40;
        const float MINIMAL_PERCENT_FONT_MUL = 3f;
        const float COMPACT_ICON_BASE = 32f;
        const float COMPACT_GAP_BASE = 4f;
        const int COMPACT_PAGE_FRAMES = 300;
        const string LOC_INGOTS_LABEL = MOD_PREFIX + "Projector_Ingots";
        const string LOC_COMPONENTS_LABEL = "DisplayName_InventoryConstraint_Components";

        readonly Dictionary<string, string> _loc = new Dictionary<string, string>();
        int _compactPages = 1;
        int _lastCompactPage;

        public bool IsLoading { get; private set; }
        public int MissingComponents => _projectorViewModel.MissingComponents;
        public IReadOnlyDictionary<MyItemType, double> ComponentMissing => _projectorViewModel.ComponentMissing;

        public ProjectorApp(IAppHost host) : base(host, CreateViewModel)
        {
            _projectorViewModel = (ProjectorViewModel)ViewModel;

            if (!ItemDisplayComponent.MigrateLegacyDisplayMode(GeneralComponent))
                return;

            var block = Host.Block as IMyTerminalBlock;
            var provider = Host.ProviderConfig;
            if (block == null || provider == null || !provider.CanWrite)
                return;

            LcdModClientComponent.RunNextFrame.Add(delegate
            {
                ConfigManager.Sync(block, provider);
            });
        }

        static IItemsAppViewModel CreateViewModel(
            GridLogic gridLogic,
            ItemSelectionConfigComponent selection,
            BlockSelectionConfigComponent blockSelection)
        {
            return new ProjectorViewModel(gridLogic, selection, blockSelection);
        }

        IMyProjector Projector => _projectorViewModel.Projector;

        bool HasBlueprint => Projector != null && Projector.IsProjecting;

        int CompactPage
        {
            get
            {
                long frame = MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
                return (int)((frame / COMPACT_PAGE_FRAMES) % Math.Max(1, _compactPages));
            }
        }

        struct ProjectorFooterLayout
        {
            public float Height;
            public float Top;
            public float ContentTop;
            public float ContentLeft;
            public float TextRight;
            public Vector2 PieCenter;
        }

        public override void LayoutChanged()
        {
            base.LayoutChanged();
            _loc.Clear();
            _projectorDataInitialized = false;
            _customTitle = Projector?.CustomName;

            var raA = MyTexts.Get(MyStringId.GetOrCompute("ScreenTerminalProduction_RequiredAndAvailable")).ToString()
                .Split('/');
            if (raA.Length == 2)
            {
                _required = raA.First().Trim();
                _available = raA.Last().Trim();
            }

            _requiredX = Surface.MeasureStringInPixels(new StringBuilder(_required), TextFont, 1).X;
            _availableX = Surface.MeasureStringInPixels(new StringBuilder(_available), TextFont, 1).X;
        }

        protected override void DrawFooter(List<MySprite> frame)
        {
            if (Projector?.CustomName != _customTitle)
                LayoutChanged();

            if (Projector == null)
                return;

            // Guard on the component requirement (always tracked) so the footer — and its toggle
            // button — stays visible even when the active ore-bar view computes to zero.
            if (_projectorViewModel.TotalBlocks == 0 || _projectorViewModel.ComponentMissing.Count == 0)
                return;

            int built = Math.Max(_projectorViewModel.TotalBlocks - _projectorViewModel.RemainingBlocks, 0);
            float textScale = Scale * 0.9f * FontScale;
            var lineSpacer = GetFooterLineSpacer();
            var legendSize = GetFooterLegendSize();
            var pieSize = GetFooterPieSize();
            var layout = CreateFooterLayout();
            var pos = new Vector2(layout.ContentLeft, layout.ContentTop);

            FooterHeight = layout.Height;
            pos.X += pieSize.X;

            var footerTop = layout.Top;

            if (!ThemeChrome.DrawFooterBand(frame, Theme, new RectangleF(ViewBox.X, footerTop, ViewBox.Width, FooterHeight), BackgroundColor, GetHeaderColor(), ForegroundColor, Scale))
            {
                frame.Add(new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(ViewBox.X + ViewBox.Width * 0.5f, footerTop + FooterHeight * 0.5f),
                    Size = new Vector2(ViewBox.Width, FooterHeight),
                    Color = new Color(BackgroundColor.MulValue(0.8f), 0.5f),
                    Alignment = TextAlignment.CENTER
                });
            }

            float legendTextSpacing = GetFooterLegendTextSpacing();
            float pieToTextGap = 10f * Scale;

            var blocksString = MyTexts.GetString("TerminalTab_Info_Blocks");

            pos.X += legendSize.X + legendTextSpacing + pieToTextGap;

            var blocksPct = built / (float)_projectorViewModel.TotalBlocks;
            var componentsPct = _projectorViewModel.TotalMaterials > 0
                ? 1 - (float)_projectorViewModel.MissingMaterials / _projectorViewModel.TotalMaterials
                : 1f;

            StringBuilder sb = new StringBuilder(
                $"{blocksString}{blocksPct:P2}  ({built}/{_projectorViewModel.TotalBlocks} )");

            TrimText(ref sb, layout.TextRight - pos.X, 0.9f);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = sb.ToString(),
                Position = pos,
                RotationOrScale = textScale,
                Color = Host.ForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = TextFont
            });

            pos.Y += lineSpacer;

            var components = GetMaterialLabel();

            sb.Clear();
            sb.Append(
                $"{components}: {componentsPct:P2}  ({FormatingHelper.FormatItemQty(_projectorViewModel.TotalMaterials - _projectorViewModel.MissingMaterials)}" +
                $"/{FormatingHelper.FormatItemQty(_projectorViewModel.TotalMaterials)})");


            TrimText(ref sb, layout.TextRight - pos.X, 0.9f);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = sb.ToString(),
                Position = pos,
                RotationOrScale = textScale,
                Color = Host.ForegroundColor,
                Alignment = TextAlignment.LEFT,
                FontId = TextFont
            });

            pos.X -= legendSize.X + legendTextSpacing;

            pos.Y -= lineSpacer - (legendSize.Y + legendSize.Y / 2);

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = pos,
                Size = legendSize,
                Color = GetHeaderColor(),
                Alignment = TextAlignment.CENTER,
            });

            pos.Y += lineSpacer;

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = pos,
                Size = legendSize,
                Color = Host.ForegroundColor,
                Alignment = TextAlignment.CENTER,
            });

            DonutDualPanel.CreateSprites(
                frame,
"",
                Host.ForegroundColor,
                Host.BackgroundColor,
                ToScreenMargin(layout.PieCenter),
                pieSize,
                componentsPct,
                blocksPct,
                GetHeaderColor(),
                true,
                false);
        }

        string GetMaterialLabel()
        {
            return MyTexts.GetString(_showIngots ? LOC_INGOTS_LABEL : LOC_COMPONENTS_LABEL);
        }

        string Loc(string key)
        {
            string text;
            if (!_loc.TryGetValue(key, out text))
                _loc[key] = text = LocHelper.GetLoc(MOD_PREFIX + "Projector_" + key);
            return text;
        }

        protected override void DrawMinimal(List<MySprite> s)
        {
            var view = ViewBox;
            Color fg = ForegroundColor;
            Color accent = GetHeaderColor();
            Color track = Color.Lerp(BackgroundColor, fg, 0.15f);
            bool hasBlueprint = HasBlueprint;
            int total = hasBlueprint ? _projectorViewModel.TotalBlocks : 0;
            int remaining = hasBlueprint ? _projectorViewModel.RemainingBlocks : 0;
            float fraction = total > 0 ? 1f - remaining / (float)total : 0f;
            string center = total > 0 ? FormatingHelper.PercentageToString(fraction) : LocHelper.GetLoc(MOD_PREFIX + "NotAvailable");
            string remainingText = hasBlueprint ? string.Format(Loc("Remaining"), remaining, total) : null;
            string buildableText = hasBlueprint ? string.Format(Loc("Buildable"), Projector.BuildableBlocksCount) : null;
            float margin = Bare.Margin;
            float lineHeight = Bare.LineHeight(1f);

            if (BareLayout.IsWide(view))
            {
                var ringArea = new RectangleF(view.X, view.Y, view.Width * 0.5f, view.Height);
                Bare.Ring(s, ringArea, fraction, accent, track, center, fg, MINIMAL_PERCENT_FONT_MUL);
                float left = ringArea.Right + margin;
                float top = view.Center.Y - lineHeight;
                Bare.Text(s, remainingText, new Vector2(left, top), TextAlignment.LEFT, 1f, fg, view.Right - left, lineHeight);
                Bare.Text(s, buildableText, new Vector2(left, top + lineHeight), TextAlignment.LEFT, 1f, fg, view.Right - left, lineHeight);
                return;
            }

            var ring = new RectangleF(view.X, view.Y, view.Width, view.Height - lineHeight * 2f - margin);
            Bare.Ring(s, ring, fraction, accent, track, center, fg, MINIMAL_PERCENT_FONT_MUL);
            Bare.Text(s, remainingText, new Vector2(view.Center.X, ring.Bottom + margin), TextAlignment.CENTER, 1f, fg, view.Width, lineHeight);
            Bare.Text(s, buildableText, new Vector2(view.Center.X, ring.Bottom + margin + lineHeight), TextAlignment.CENTER, 1f, fg, view.Width, lineHeight);
        }

        protected override void DrawCompact(List<MySprite> s)
        {
            var items = DisplayItems;
            _compactPages = 1;
            if (!HasBlueprint || items.Count == 0)
            {
                Host.DrawMessage(s, LocHelper.Empty, "Warning", GetWarningColor());
                return;
            }

            var area = ViewBox;
            if (area.Width <= 0f || area.Height <= 0f)
                return;

            float scale = Bare.Scale;
            float icon = COMPACT_ICON_BASE * scale;
            float gap = COMPACT_GAP_BASE * scale;
            float lineHeight = Bare.LineHeight(1f);
            float cellWidth = icon + gap;
            float cellHeight = icon + lineHeight + gap;
            int cols = Math.Max(1, (int)Math.Floor(area.Width / cellWidth));
            int rows = Math.Max(1, (int)Math.Floor((area.Height + gap) / cellHeight));
            int cells = cols * rows;
            int count = items.Count;
            _compactPages = (count + cells - 1) / cells;
            int start = CompactPage * cells;
            int end = Math.Min(count, start + cells);
            float originX = area.X + Math.Max(0f, (area.Width - cols * cellWidth + gap) * 0.5f);
            Color bg = BackgroundColor;
            Color accent = GetHeaderColor();
            Color error = GetErrorColor();
            string ok = Loc("Ok");

            for (int i = start; i < end; i++)
            {
                var item = items[i];
                if (item == null)
                    continue;

                int slot = i - start;
                float x = originX + (slot % cols) * cellWidth;
                float y = area.Y + (slot / cols) * cellHeight;
                double missing = (double)item.Amount;
                DrawItemIcon(s, ResolveSprite(item.ItemType), new Vector2(x + icon * 0.5f, y + icon * 0.5f), new Vector2(icon, icon), TextAlignment.CENTER, bg);
                Bare.Text(s, missing > 0d ? FormatingHelper.FormatItemQty(missing) : ok, new Vector2(x + icon * 0.5f, y + icon), TextAlignment.CENTER, 1f, missing > 0d ? error : accent, cellWidth, lineHeight);
            }
        }

        public override void Update()
        {
            IsLoading = false;
            base.Update();
            if (IsCompact && _compactPages > 1)
            {
                int page = CompactPage;
                if (page != _lastCompactPage)
                {
                    _lastCompactPage = page;
                    InvalidateContentSprites();
                }
            }

            var changed = _projectorViewModel.UpdateProjector(
                Block?.CubeGrid,
                ProjectorReferenceComponent.EntityId,
                _showIngots);
            if (Projector?.CustomName != _customTitle)
                LayoutChanged();

            if (!_projectorDataInitialized && ProjectorReferenceComponent.EntityId != 0 && Projector == null)
            {
                _projectorDataInitialized = true;
                IsLoading = true;
                return;
            }

            _projectorDataInitialized = true;
            if (changed)
                InvalidateContentAndFooterSprites();
        }

        ProjectorFooterLayout CreateFooterLayout()
        {
            var baseHeight = GetFooterBaseHeight();
            var footerPaddingX = GetFooterPaddingX() + GetFooterInnerPaddingX();
            var layout = new ProjectorFooterLayout
            {
                Height = baseHeight,
                ContentLeft = ViewBox.X + footerPaddingX,
                TextRight = ViewBox.Right - footerPaddingX
            };

            layout.Top = ViewBox.Bottom - layout.Height;
            layout.ContentTop = layout.Top + GetFooterPaddingY();
            layout.PieCenter = new Vector2(
                ViewBox.X + GetFooterInnerPaddingX() + GetFooterPieSize().X * 0.5f,
                layout.Top + baseHeight * 0.5f);
            return layout;
        }

        float GetFooterPaddingX()
        {
            return GetFooterLegendSize().X + GetFooterLegendTextSpacing();
        }

        float GetFooterInnerPaddingX()
        {
            return 6f * Scale;
        }

        float GetFooterPaddingY()
        {
            return GetFooterLegendSize().Y;
        }

        Vector2 GetFooterPieSize()
        {
            return new Vector2(PIE_RADIUS * Scale);
        }

        Vector2 GetFooterLegendSize()
        {
            return new Vector2(8f, 8f) * Scale * FontScale;
        }

        float GetFooterLegendTextSpacing()
        {
            return GetFooterLegendSize().X * 0.5f;
        }

        float GetFooterLineSpacer()
        {
            return 25f * LayoutScale;
        }

        float GetFooterTextHeight()
        {
            return 25f * 2f * LayoutScale;
        }

        float GetFooterBaseHeight()
        {
            var pieSize = GetFooterPieSize();
            return Math.Max(GetFooterTextHeight(), pieSize.Y) + GetFooterPaddingY() * 2f;
        }

    }
}
