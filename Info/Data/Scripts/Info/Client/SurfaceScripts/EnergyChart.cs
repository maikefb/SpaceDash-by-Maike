using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.IMyTextSurface;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.SurfaceScripts
{
    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class EnergyChartSurfaceScript : EnergySurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "EnergyChart";
        public const string TITLE = MOD_PREFIX + "EnergyChart";

        protected override string DefaultTitle => TITLE;

        public EnergyChartSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        internal override EnergyAppBase CreateApp()
        {
            return new EnergyChartApp(this);
        }
    }
}
