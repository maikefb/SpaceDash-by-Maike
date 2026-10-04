// ReSharper disable RedundantUsingDirective
using System;
using VRageMath;

namespace LcdMod.Common.Helpers
{
    /// <summary>
    /// Valores compartilhados pelos dois mods. Identidade por mod (ID_PREFIX, PORT, GUIDs de storage)
    /// fica na outra parte desta classe parcial, em Constants.Mod.cs de cada mod.
    /// </summary>
    public static partial class Constants
    {
        public static Version Version => Generated.Constants.Version;
        public static string BuildConfiguration => Generated.Constants.BuildConfiguration;
        public static string VersionName => Generated.Constants.VersionName;

        public const string MOD_PREFIX = "LcdMod_";

        public const string CONFIG_FILE = "LcdMod.local.xml";
        public const string TEXTURE_IMPORT_FILE = "import.txt";
        /// <summary>Teto por imagem: 2048x2048 em BC7 com mipmaps. Acima disso o servidor recusa.</summary>
        public const int MAX_TEXTURE_BYTES = 5330000;
        public const int MAX_TEXTURE_DIMENSION = 2048;
        public const int MAX_TEXTURES_PER_OWNER = 32;
        public const int MAX_TEXTURE_NAME_LENGTH = 64;
        public const int MAX_CONFIG_PACKET_BYTES = 64 * 1024;
        public const string GITHUB = "https://github.com/maikefb/SpaceDash-by-Maike";

        public const string BSOD_TITLE_FALLBACK =
"Your Station ran into a problem and needs to Restart. We're waiting for a while, and then we'll restart it for you";

        public const string BSOD_INFO1_FALLBACK = "For more information about this issue,";
        public const string BSOD_INFO2_FALLBACK = "Visit ";
        public const string BSOD_INFO4_FALLBACK = "If you call a support person, give them this info:";
        public const string BSOD_INFO5_FALLBACK = "Exception code:";

        public const float MIN_SCREEN_HEIGHT_TO_WIDTH_RATIO = 0.2f;

        public const string PRIMARY = "primary";
        public const string PRIMARY_CONTAINER = "primaryContainer";
        public const string ON_PRIMARY_CONTAINER = "onPrimaryContainer";

        public const string SECONDARY_CONTAINER = "secondaryContainer";
        public const string ON_SECONDARY_CONTAINER = "onSecondaryContainer";

        public const string TERTIARY = "tertiary";

        public const string ERROR = "error";

        public const string BACKGROUND = "background";
        public const string ON_BACKGROUND = "onBackground";

        public const string SURFACE = "surface";
        public const string ON_SURFACE = "onSurface";
        public const string ON_SURFACE_VARIANT = "onSurfaceVariant";

        public const string SURFACE_CONTAINER_LOW = "surfaceContainerLow";
        public const string SURFACE_CONTAINER = "surfaceContainer";
        public const string SURFACE_CONTAINER_HIGHEST = "surfaceContainerHighest";

        public const string OUTLINE = "outline";
        public const string OUTLINE_VARIANT = "outlineVariant";

        public const string SUCCESS = "success";

        public const string DISABLED_FOREGROUND = "disabledForeground";

        public const string GENERAL = "core.general";
        public const string COLORS = "core.colors";
        public const string INTERACTION = "core.interaction";
        public const string FILTERS = "data.filters";
        public const string BLOCKS = "data.blocks";
        public const string ITEMS = "data.items";
        public const string ITEM_DISPLAY = "view.items";
        public const string APP = "app.settings";

        // The slot, not the component CLR type, identifies the semantic use of a reference.
        public const string PROJECTOR_REFERENCE = "reference.projector";
        public const string DOCKABLE_REFERENCE = "reference.dockable";

        public static Color ColorCorrection { get; set; } = new Color(175,185,200);
    }
}
