using System;
using Sandbox.ModAPI;

namespace LcdMod.Common.Helpers
{
    /// <summary>Quanto o mundo já rodou desde a criação: é o ElapsedGameTime do checkpoint, que o jogo persiste entre sessões e sincroniza nos clientes.</summary>
    public static class WorldClock
    {
        public static readonly DateTime Epoch = new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ElapsedTicks()
        {
            var session = MyAPIGateway.Session;
            return session == null ? 0L : Math.Max(0L, (session.GameDateTime - Epoch).Ticks);
        }

        public static double ToSeconds(long ticks)
        {
            return ticks <= 0L ? 0d : ticks / (double)TimeSpan.TicksPerSecond;
        }
    }
}
