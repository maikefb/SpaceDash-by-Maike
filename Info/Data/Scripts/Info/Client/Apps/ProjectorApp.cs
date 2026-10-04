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
        const string LOC_INGOTS_LABEL = MOD_PREFIX + "Projector_Ingots";
        const string LOC_COMPONENTS_LABEL = "DisplayName_InventoryConstraint_Components";

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

            frame.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Position = new Vector2(ViewBox.X + ViewBox.Width * 0.5f, footerTop + FooterHeight * 0.5f),
                Size = new Vector2(ViewBox.Width, FooterHeight),
                Color = new Color(BackgroundColor.MulValue(0.8f), 0.5f),
                Alignment = TextAlignment.CENTER
            });

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
                Color = Surface.ScriptForegroundColor,
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
                Color = Surface.ScriptForegroundColor,
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
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER,
            });

            DonutDualPanel.CreateSprites(
                frame,
"",
                (IMyTextSurface)Surface,
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

        public override void Update()
        {
            IsLoading = false;
            base.Update();
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
