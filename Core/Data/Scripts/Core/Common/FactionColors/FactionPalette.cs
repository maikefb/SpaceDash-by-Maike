using System;
using System.Globalization;
using VRageMath;

namespace LcdMod.Common.FactionColors
{
    /// <summary>Paleta compartilhada por facção: as três cores do mod mais fonte e fundo nativos da superfície.</summary>
    public sealed class FactionPalette
    {
        public Color Header;
        public Color Warning;
        public Color Error;
        public Color Foreground;
        public Color Background;

        public string Pack()
        {
            return string.Join(";", new[]
            {
                Header.PackedValue.ToString(CultureInfo.InvariantCulture),
                Warning.PackedValue.ToString(CultureInfo.InvariantCulture),
                Error.PackedValue.ToString(CultureInfo.InvariantCulture),
                Foreground.PackedValue.ToString(CultureInfo.InvariantCulture),
                Background.PackedValue.ToString(CultureInfo.InvariantCulture)
            });
        }

        public static bool TryParse(string raw, out FactionPalette palette)
        {
            palette = null;
            if (string.IsNullOrEmpty(raw))
                return false;

            var parts = raw.Split(';');
            if (parts.Length != 5)
                return false;

            var packed = new uint[5];
            for (int i = 0; i < 5; i++)
            {
                if (!uint.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out packed[i]))
                    return false;
            }

            palette = new FactionPalette
            {
                Header = new Color { PackedValue = packed[0] },
                Warning = new Color { PackedValue = packed[1] },
                Error = new Color { PackedValue = packed[2] },
                Foreground = new Color { PackedValue = packed[3] },
                Background = new Color { PackedValue = packed[4] }
            };
            return true;
        }
    }
}
