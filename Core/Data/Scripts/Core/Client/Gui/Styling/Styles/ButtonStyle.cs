using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Apps.ViewModel;
using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.ControlsTemplates.Basic;
using LcdMod.Client.Gui.ControlsTemplates.Lists;

namespace LcdMod.Client.Gui.Styling.Styles
{
    /// <summary>
    /// Estilos de botões e linhas. Os estados Hover, Pressed e Dragged não existem sem interação por clique,
    /// então só Active, Selected e Disabled são declarados.
    /// </summary>
    public static class ButtonStyle
    {
        public static void Build(StyleTree styles)
        {
            Style<Button> button = styles.For<Button>();
            ConfigureStandardButton(button);

            Style sort = button.ClassSelector("Sort")
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.SurfaceColor)
                .Set(ControlTemplate.TextColorProperty, ThemeResources.OnSurfaceVariantColor)
                .Set(ControlTemplate.BorderThicknessPixelsProperty, 0f);

            sort.State(StyleState.Selected)
                .Set(ControlTemplate.BorderThicknessPixelsProperty, 0f);

            sort.ClassSelector("SortAscending")
                .Set(ControlTemplate.TextColorProperty, ThemeResources.AccentColor);

            sort.ClassSelector("SortDescending")
                .Set(ControlTemplate.TextColorProperty, ThemeResources.AccentColor);

            Style listBoxItem = styles.For<ListBoxItem<ItemEntry>>();
            ConfigureListBoxItem(listBoxItem);

            Style listRow = styles.For<ControlTemplate>().ClassSelector("Row");
            ConfigureListRow(listRow);
        }

        static void ConfigureListRow(Style style)
        {
            style
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.SecondaryContainerColor)
                .Set(ControlTemplate.TextColorProperty, ThemeResources.OnSecondaryContainerColor);
        }

        static void ConfigureListBoxItem(Style style)
        {
            style
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.SecondaryContainerColor)
                .Set(ControlTemplate.TextColorProperty, ThemeResources.OnSecondaryContainerColor);
        }

        static void ConfigureStandardButton(Style style)
        {
            style
                .Set(ControlTemplate.BorderColorProperty, ThemeResources.BorderVariantColor)
                .Set(ControlTemplate.BorderThicknessPixelsProperty, 0f);

            style.State(StyleState.Selected)
                .Set(ControlTemplate.BorderColorProperty, ThemeResources.AccentColor)
                .Set(ControlTemplate.BorderThicknessPixelsProperty, 2f);
        }
    }
}
