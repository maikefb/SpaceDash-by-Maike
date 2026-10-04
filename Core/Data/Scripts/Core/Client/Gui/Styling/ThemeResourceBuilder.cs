using System.Collections.Generic;
using LcdMod.Client.Gui.Styling.DataTemplates;
using LcdMod.Common.Helpers;
using VRageMath;

namespace LcdMod.Client.Gui.Styling
{
    public static class ThemeResourceBuilder
    {
        public static ResourceTree FromThemeDictionary(IReadOnlyDictionary<string, Color> theme)
        {
            ResourceTree resources = new ResourceTree();
            ItemDataTemplate.AddResources(resources);

            resources.Set(ThemeResources.AccentColor, theme[Constants.PRIMARY]);
            resources.Set(ThemeResources.AccentContainerColor, theme[Constants.PRIMARY_CONTAINER]);
            resources.Set(ThemeResources.OnAccentContainerColor, theme[Constants.ON_PRIMARY_CONTAINER]);
            resources.Set(ThemeResources.WarningColor, theme[Constants.TERTIARY]);
            resources.Set(ThemeResources.ErrorColor, theme[Constants.ERROR]);
            resources.Set(ThemeResources.SuccessColor, theme[Constants.SUCCESS]);
            resources.Set(ThemeResources.MutedTextColor, theme[Constants.ON_SURFACE_VARIANT]);
            resources.Set(ThemeResources.DividerColor, theme[Constants.OUTLINE_VARIANT]);
            resources.Set(ThemeResources.DisabledColor, theme[Constants.DISABLED_FOREGROUND]);
            resources.Set(ThemeResources.BackgroundColor, theme[Constants.BACKGROUND]);
            resources.Set(ThemeResources.FontColor, theme[Constants.ON_BACKGROUND]);
            resources.Set(ThemeResources.SurfaceColor, theme[Constants.SURFACE]);
            resources.Set(ThemeResources.OnSurfaceColor, theme[Constants.ON_SURFACE]);
            resources.Set(ThemeResources.OnSurfaceVariantColor, theme[Constants.ON_SURFACE_VARIANT]);
            resources.Set(ThemeResources.SurfaceContainerLowColor, theme[Constants.SURFACE_CONTAINER_LOW]);
            resources.Set(ThemeResources.SurfaceContainerColor, theme[Constants.SURFACE_CONTAINER]);
            resources.Set(ThemeResources.SurfaceContainerHighestColor, theme[Constants.SURFACE_CONTAINER_HIGHEST]);
            resources.Set(ThemeResources.SecondaryContainerColor, theme[Constants.SECONDARY_CONTAINER]);
            resources.Set(ThemeResources.OnSecondaryContainerColor, theme[Constants.ON_SECONDARY_CONTAINER]);
            resources.Set(ThemeResources.BorderColor, theme[Constants.OUTLINE]);
            resources.Set(ThemeResources.BorderVariantColor, theme[Constants.OUTLINE_VARIANT]);
            resources.Set(ThemeResources.ScrollBarTrackColor, new Color(theme[Constants.OUTLINE_VARIANT], 127));
            resources.Set(ThemeResources.ScrollBarThumbColor, new Color(theme[Constants.PRIMARY], 250));

            resources.Set(ThemeResources.TextFont, "White");
            resources.Set(ThemeResources.PictureTransitionFrames, 24);

            return resources;
        }
    }
}
