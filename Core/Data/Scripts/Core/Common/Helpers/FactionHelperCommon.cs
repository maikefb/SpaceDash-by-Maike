using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace LcdMod.Common.Helpers
{
    public static class FactionHelperCommon
    {
        public static Color DefaultColor => new Color(170, 120, 190);
        const float MIN_READABLE_VALUE = 0.6f;
        const string DEFAULT_ICON = "Textures\\FactionLogo\\Others\\OtherIcon_18.dds";

        public static string GetIcon(IMyTerminalBlock block)
        {
            return block != null ? GetIcon(GetOwnerFaction(block)) : DEFAULT_ICON;
        }

        public static string GetIcon(IMyFaction faction)
        {
            return faction?.FactionIcon?.ToString() ?? DEFAULT_ICON;
        }

        public static Color GetBackgroundColor(IMyFaction faction)
        {
            if (faction?.CustomColor == null)
                return Color.Black;

            return MyColorPickerConstants.HSVOffsetToHSV(faction.CustomColor).HSVtoColor();
        }

        public static IMyFaction GetOwnerFaction(IMyTerminalBlock block)
        {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction(block?.OwnerId ?? 0);
        }

        public static Color GetIconColor(IMyFaction faction)
        {
            if (faction == null)
                return DefaultColor;

            return MyColorPickerConstants.HSVOffsetToHSV(faction.IconColor).HSVtoColor();
        }

        public static Color GetCustomColor(IMyFaction faction)
        {
            if (faction == null)
                return Color.White;

            return MyColorPickerConstants.HSVOffsetToHSV(faction.CustomColor).HSVtoColor();
        }

        public static Color GetAccent(IMyTerminalBlock block)
        {
            if (block == null)
                return DefaultColor;

            var icon = GetIconColor(GetOwnerFaction(block));
            if (icon.ColorToHSV().Y <= 0.01)
            {
                var background = GetCustomColor(GetOwnerFaction(block));
                if (background.ColorToHSV().Y > 0.01)
                    icon = background;
            }
            return EnsureReadable(icon);
        }

        /// <summary>Cor de facção escura demais some sobre o fundo preto padrão: levanta o valor HSV até um mínimo legível.</summary>
        public static Color EnsureReadable(Color color)
        {
            var hsv = color.ColorToHSV();
            if (hsv.Z >= MIN_READABLE_VALUE)
                return color;
            hsv.Z = MIN_READABLE_VALUE + 0.15f;
            return hsv.HSVtoColor();
        }


        public static Color GetIconColor(IMyTerminalBlock block)
        {
            return block != null ? GetIconColor(GetOwnerFaction(block)) : DefaultColor;
        }

    }
}