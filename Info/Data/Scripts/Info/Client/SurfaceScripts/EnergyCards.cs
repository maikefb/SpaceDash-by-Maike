using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Modules.Energy;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.IMyTextSurface;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.SurfaceScripts
{
    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class EnergyBatteriesSurfaceScript : EnergySurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "EnergyBatteries";
        public const string TITLE = MOD_PREFIX + "EnergyBatteries";

        protected override string DefaultTitle => TITLE;

        public EnergyBatteriesSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        internal override EnergyAppBase CreateApp()
        {
            return new EnergyCardApp(this, EnergyKind.Battery);
        }
    }

    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class EnergySolarSurfaceScript : EnergySurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "EnergySolar";
        public const string TITLE = MOD_PREFIX + "EnergySolar";

        protected override string DefaultTitle => TITLE;

        public EnergySolarSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        internal override EnergyAppBase CreateApp()
        {
            return new EnergyCardApp(this, EnergyKind.Solar);
        }
    }

    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class EnergyWindSurfaceScript : EnergySurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "EnergyWind";
        public const string TITLE = MOD_PREFIX + "EnergyWind";

        protected override string DefaultTitle => TITLE;

        public EnergyWindSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        internal override EnergyAppBase CreateApp()
        {
            return new EnergyCardApp(this, EnergyKind.Wind);
        }
    }

    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class EnergyHydrogenSurfaceScript : EnergySurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "EnergyHydrogen";
        public const string TITLE = MOD_PREFIX + "EnergyHydrogen";

        protected override string DefaultTitle => TITLE;

        public EnergyHydrogenSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        internal override EnergyAppBase CreateApp()
        {
            return new EnergyCardApp(this, EnergyKind.Hydrogen);
        }
    }

    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class EnergyReactorsSurfaceScript : EnergySurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "EnergyReactors";
        public const string TITLE = MOD_PREFIX + "EnergyReactors";

        protected override string DefaultTitle => TITLE;

        public EnergyReactorsSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        internal override EnergyAppBase CreateApp()
        {
            return new EnergyCardApp(this, EnergyKind.Reactor);
        }
    }
}
