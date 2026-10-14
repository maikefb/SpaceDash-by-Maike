using System.Collections.Generic;
using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Common.Config.Generation;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.SurfaceScripts
{

    [MyTextSurfaceScript(ID, TITLE)]
    public partial class GasSurfaceScript : SurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "GasGraph";
        public const string TITLE = MOD_PREFIX + "GasDashboard";
        protected override string DefaultTitle => TITLE;

        public override IApp App => _app;
        GasApp _app;

        public GasSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        protected override IApp DetachGridBoundApp()
        {
            var app = _app;
            _app = null;
            return app;
        }

        public override void SafeRun()
        {
            if (_app == null)
                _app = new GasApp(this);

            UpdateViewBox();
            _app.Update();
            RenderSprites();
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = new List<MySprite>();
            if (_app == null || !_app.HasData)
            {
                AddEmptySprites(sprites);
                return sprites;
            }

            AddBackground(sprites);
            DrawTitle(sprites);
            sprites.AddRange(_app.GetSprites());
            return sprites;
        }
    }
}
