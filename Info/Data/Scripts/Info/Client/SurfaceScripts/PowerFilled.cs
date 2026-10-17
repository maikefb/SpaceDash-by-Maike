using System.Collections.Generic;
using LcdMod.Common.Config.Components;
using Generated;
using LcdMod.Client.Gui;
using LcdMod.Client.Helpers;
using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using LcdMod.Client.SurfaceScripts.Abstract;
using ComboboxLinkType = LcdMod.Client.Terminal.Controls.Generic.ComboboxLinkType;
using static LcdMod.Common.Helpers.Constants;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.SurfaceScripts
{
    
    [MyTextSurfaceScript(ID, TITLE)]
    public partial class PowerFilledSurfaceScript : SurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "BatteryGraph";
        public const string TITLE = MOD_PREFIX + "PowerFilled";

        protected override string DefaultTitle => TITLE;

        public override IApp App => _app;
        IApp _app;


        public PowerFilledSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                _app = new PowerFilledApp(this);

            _app.Update();
            RenderSprites();
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = new List<MySprite>();
            AddBackground(sprites);
            DrawTitle(sprites);
            if (_app == null)
                DrawLoading(sprites, GeneralComponent.GetScale());
            else if (!_app.HasVisibleItems())
                DrawMessage(sprites, LocHelper.Empty, "Warning", GetWarningColor(), GeneralComponent.GetScale());
            else
                sprites.AddRange(_app.GetSprites());
            return sprites;
        }
    }
}
