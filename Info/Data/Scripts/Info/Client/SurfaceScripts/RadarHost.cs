using Generated;
using System.Collections.Generic;
using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Gui;
using LcdMod.Client.Terminal.Controls.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using SliderRadarRange = LcdMod.Client.Terminal.Controls.Generic.SliderRadarRange;
using static LcdMod.Common.Helpers.Constants;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.SurfaceScripts
{
    
    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class RadarSurfaceScript : SurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "Radar";
        public const string TITLE = MOD_PREFIX + "Radar";
        protected override string DefaultTitle => TITLE;

        public override IApp App => _app;       
        RadarApp _app;

        public RadarSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        protected override void LayoutChanged()
        {
            base.LayoutChanged();
            if (_app != null)
                _app.LayoutChanged();
        }

        public override void SafeRun()
        {
            if (_app == null)
                _app = new RadarApp(this);

            _app.Update();
            RenderSprites();
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = new List<MySprite>();
            AddBackground(sprites);
            if (_app != null)
            {
                sprites.AddRange(_app.GetSprites());
            }
            DrawTitle(sprites);
            return sprites;
        }
    }
}
