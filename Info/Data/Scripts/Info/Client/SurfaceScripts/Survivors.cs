using System.Collections.Generic;
using LcdMod.Common.Config.Components;
using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Helpers;
using LcdMod.Client.Modules.Survivors;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Terminal.Controls.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.IMyTextSurface;
using static LcdMod.Common.Helpers.Constants;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.SurfaceScripts
{
    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class SurvivorsSurfaceScript : SurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "Survivors";
        public const string TITLE = SurvivorsLocalization.TITLE_KEY;

        SurvivorsApp _app;
        protected override string DefaultTitle => TITLE;
        public override IApp App => _app;

        public SurvivorsSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
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
                _app = new SurvivorsApp(this);

            UpdateViewBox();
            _app.Update();
            RenderSprites();
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = new List<MySprite>();
            AddBackground(sprites);
            DrawTitle(sprites);
            if (_app == null)
                return sprites;

            if (!SurvivorsClient.HasData)
                DrawLoading(sprites, GeneralComponent.GetScale());
            else if (!_app.HasVisibleItems())
                DrawMessage(sprites, LocHelper.Empty, "Warning", GetWarningColor(), GeneralComponent.GetScale());
            else
                sprites.AddRange(_app.GetSprites());
            return sprites;
        }
    }
}
