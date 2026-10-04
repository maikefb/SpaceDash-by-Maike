using LcdMod.Common.FactionColors;
using LcdMod.Common.Helpers;
using Sandbox.ModAPI;
using VRageMath;

namespace LcdMod.Common.Config.Components
{
    public static class GeneralConfigComponentExtensions
    {
        public const float MIN_SCALE = 0.1f;
        public const float MAX_SCALE = 10f;

        public static float GetScale(this GeneralConfigComponent config)
        {
            return config == null ? 1f : MathHelper.Clamp(config.InternalScale, MIN_SCALE, MAX_SCALE);
        }

        public static void SetScale(this GeneralConfigComponent config, float value)
        {
            if (config != null)
                config.InternalScale = MathHelper.Clamp(value, MIN_SCALE, MAX_SCALE);
        }
    }

    public static class ColorConfigComponentExtensions
    {
        static readonly Color DefaultErrorColor =
            new Color(230, 70, 70);

        static readonly Color DefaultWarningColor =
            new Color { PackedValue = 0xFF10A0E0u };

        public static Color ResolveHeaderColor(this ColorConfigComponent config, IMyTerminalBlock block)
        {
            FactionPalette palette;
            if (TryGetSyncedPalette(config, block, out palette))
                return palette.Header;
            return config == null
                ? GetDefaultHeaderColor(block)
                : config.HeaderColor.Get(!config.CustomizedColors, () => GetDefaultHeaderColor(block));
        }

        public static Color ResolveErrorColor(this ColorConfigComponent config, IMyTerminalBlock block)
        {
            FactionPalette palette;
            return TryGetSyncedPalette(config, block, out palette) ? palette.Error : config.ResolveErrorColor();
        }

        public static Color ResolveWarningColor(this ColorConfigComponent config, IMyTerminalBlock block)
        {
            FactionPalette palette;
            return TryGetSyncedPalette(config, block, out palette) ? palette.Warning : config.ResolveWarningColor();
        }

        static bool TryGetSyncedPalette(ColorConfigComponent config, IMyTerminalBlock block, out FactionPalette palette)
        {
            palette = null;
            return config != null && config.SyncColors
                   && FactionColorsStore.TryGet(FactionColorsStore.FactionIdOf(block), out palette);
        }

        /// <summary>Cores escolhidas por alguém (personalizadas ou paleta da facção), que não devem ser ajustadas por contraste.</summary>
        public static bool HasExplicitColors(this ColorConfigComponent config, IMyTerminalBlock block)
        {
            FactionPalette palette;
            return config != null && (config.CustomizedColors || TryGetSyncedPalette(config, block, out palette));
        }

        public static Color ResolveErrorColor(this ColorConfigComponent config)
        {
            return config == null
                ? DefaultErrorColor
                : config.ErrorColor.Get(!config.CustomizedColors, () => DefaultErrorColor);
        }

        public static Color ResolveWarningColor(this ColorConfigComponent config)
        {
            return config == null
                ? DefaultWarningColor
                : config.WarningColor.Get(!config.CustomizedColors, () => DefaultWarningColor);
        }

        static Color GetDefaultHeaderColor(IMyTerminalBlock block)
        {
            return block == null
                ? FactionHelperCommon.DefaultColor
                : FactionHelperCommon.GetAccent(block);
        }
    }
}
