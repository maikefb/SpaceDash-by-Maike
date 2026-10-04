using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using VRageMath;

namespace LcdMod.Client.Gui.Styling.Styles
{
    public static class DefaultStyleBuilder
    {
        public static StyleTree Build()
        {
            StyleTree styles = new StyleTree();

            Style control = styles.For<ControlTemplate>()
                .Set(ControlTemplate.TextColorProperty, ThemeResources.FontColor)
                .Set(ControlTemplate.TextFontProperty, ThemeResources.TextFont)
                .Set(ControlTemplate.LayoutScaleProperty, ThemeResources.LayoutScale)
                .Set(ControlTemplate.FontScaleProperty, ThemeResources.FontScale)
                .Set(ControlTemplate.OpacityProperty, 1f)
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.SurfaceColor)
                .Set(ControlTemplate.BorderColorProperty, ThemeResources.BorderVariantColor)
                .Set(ControlTemplate.BorderRadiusPixelsProperty, BorderRenderer.DEFAULT_RADIUS_PIXELS)
                .Set(ControlTemplate.BorderThicknessPixelsProperty, 0f)
                .Set(ControlTemplate.PaddingProperty, Vector4.Zero);

            styles.For<ScrollPanel>()
                .Set(ScrollPanel.ScrollBarTrackColorProperty, ThemeResources.ScrollBarTrackColor)
                .Set(ScrollPanel.ScrollBarThumbColorProperty, ThemeResources.ScrollBarThumbColor);

            control.State(StyleState.Disabled)
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.SurfaceContainerLowColor)
                .Set(ControlTemplate.TextColorProperty, ThemeResources.DisabledColor);

            styles.For<Border>()
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.AccentContainerColor)
                .Set(ControlTemplate.BorderThicknessPixelsProperty, 0);

            ButtonStyle.Build(styles);
            
            return styles;
        }
    }
}
