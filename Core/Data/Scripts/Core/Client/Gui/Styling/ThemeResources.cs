using VRageMath;

namespace LcdMod.Client.Gui.Styling
{
    public static class ThemeResources
    {
        public static readonly ResourceKey<Color> AccentColor =
            ResourceKey.Register<Color>("accentColor");

        public static readonly ResourceKey<Color> AccentContainerColor =
            ResourceKey.Register<Color>("accentContainerColor");

        public static readonly ResourceKey<Color> OnAccentContainerColor =
            ResourceKey.Register<Color>("onAccentContainerColor");

        public static readonly ResourceKey<Color> WarningColor =
            ResourceKey.Register<Color>("warningColor");

        public static readonly ResourceKey<Color> ErrorColor =
            ResourceKey.Register<Color>("errorColor");

        public static readonly ResourceKey<Color> SuccessColor =
            ResourceKey.Register<Color>("successColor");

        public static readonly ResourceKey<Color> MutedTextColor =
            ResourceKey.Register<Color>("mutedTextColor");

        public static readonly ResourceKey<Color> DividerColor =
            ResourceKey.Register<Color>("dividerColor");

        public static readonly ResourceKey<Color> DisabledColor =
            ResourceKey.Register<Color>("disabledColor");

        public static readonly ResourceKey<Color> BackgroundColor =
            ResourceKey.Register<Color>("backgroundColor");

        public static readonly ResourceKey<Color> FontColor =
            ResourceKey.Register<Color>("fontColor");

        public static readonly ResourceKey<Color> SurfaceColor =
            ResourceKey.Register<Color>("surfaceColor");

        public static readonly ResourceKey<Color> OnSurfaceColor =
            ResourceKey.Register<Color>("onSurfaceColor");

        public static readonly ResourceKey<Color> OnSurfaceVariantColor =
            ResourceKey.Register<Color>("onSurfaceVariantColor");

        public static readonly ResourceKey<Color> SurfaceContainerLowColor =
            ResourceKey.Register<Color>("surfaceContainerLowColor");

        public static readonly ResourceKey<Color> SurfaceContainerColor =
            ResourceKey.Register<Color>("surfaceContainerColor");

        public static readonly ResourceKey<Color> SurfaceContainerHighestColor =
            ResourceKey.Register<Color>("surfaceContainerHighestColor");

        public static readonly ResourceKey<Color> SecondaryContainerColor =
            ResourceKey.Register<Color>("secondaryContainerColor");

        public static readonly ResourceKey<Color> OnSecondaryContainerColor =
            ResourceKey.Register<Color>("onSecondaryContainerColor");

        public static readonly ResourceKey<Color> BorderColor =
            ResourceKey.Register<Color>("borderColor");

        public static readonly ResourceKey<Color> BorderVariantColor =
            ResourceKey.Register<Color>("borderVariantColor");

        public static readonly ResourceKey<Color> ScrollBarTrackColor =
            ResourceKey.Register<Color>("scrollBarTrackColor");

        public static readonly ResourceKey<Color> ScrollBarThumbColor =
            ResourceKey.Register<Color>("scrollBarThumbColor");

        public static readonly ResourceKey<float> LayoutScale =
            ResourceKey.Register<float>("layoutScale");

        public static readonly ResourceKey<float> FontScale =
            ResourceKey.Register<float>("fontScale");

        public static readonly ResourceKey<float> AutoScrollSecondsPerStep =
            ResourceKey.Register<float>("autoScrollSecondsPerStep");

        public static readonly ResourceKey<int> PictureTransitionFrames =
            ResourceKey.Register<int>("pictureTransitionFrames");
        
        public static readonly ResourceKey<string> TextFont =
            ResourceKey.Register<string>("textFont");
    }
}
