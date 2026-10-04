using System.Collections.Generic;
using LcdMod.Common.Helpers;
using Sandbox.ModAPI;

namespace LcdMod.Common.FactionColors
{
    /// <summary>
    /// Paleta por facção guardada em variável do mundo, como o TerraformingPanels faz com as metas:
    /// o jogo salva junto com o mundo e entrega a quem entra, só mudanças ao vivo passam pela rede.
    /// </summary>
    public static class FactionColorsStore
    {
        const string PREFIX = "LcdMod.colors.";
        static readonly Dictionary<long, FactionPalette> Cache = new Dictionary<long, FactionPalette>();

        public static long FactionIdOf(IMyTerminalBlock block)
        {
            var faction = FactionHelperCommon.GetOwnerFaction(block);
            return faction == null ? 0L : faction.FactionId;
        }

        public static bool TryGet(long factionId, out FactionPalette palette)
        {
            palette = null;
            if (factionId == 0L)
                return false;
            if (Cache.TryGetValue(factionId, out palette))
                return true;

            string raw;
            if (!MyAPIGateway.Utilities.GetVariable(PREFIX + factionId, out raw) || !FactionPalette.TryParse(raw, out palette))
                return false;

            Cache[factionId] = palette;
            return true;
        }

        public static void Set(long factionId, FactionPalette palette)
        {
            if (factionId == 0L || palette == null)
                return;

            Cache[factionId] = palette;
            MyAPIGateway.Utilities.SetVariable(PREFIX + factionId, palette.Pack());
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}
