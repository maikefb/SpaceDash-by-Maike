using System.Collections.Generic;
using LcdMod.Common.Config.Components;
using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Gui;
using LcdMod.Client.Helpers;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;
using LcdMod.Client.SurfaceScripts.Abstract;
using IMyTextSurface = Sandbox.ModAPI.IMyTextSurface;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.SurfaceScripts
{
    
    [MyTextSurfaceScript(ID, TITLE)]
    public partial class FarmSurfaceScript : SurfaceScriptBase
    {
        public const string ID = ID_PREFIX + "Farm";
        public const string TITLE = MOD_PREFIX + "Farm";

        protected override string DefaultTitle => TITLE;

        public override IApp App => _app;
        IApp _app;

        public FarmSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        protected override void LayoutChanged()
        {
            base.LayoutChanged();
            if (_app != null)
                _app.LayoutChanged();
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
                _app = new FarmApp(this);

            _app.Update();
            if (_app.IsDirty || Dirty || LayoutDirty)
            {
                RenderSprites();
                LayoutDirty = false;
            }
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = new List<MySprite>();
            AddBackground(sprites);
            DrawTitle(sprites);
            if (_app == null)
                DrawLoading(sprites, GeneralComponent.GetScale());
            else if (!_app.HasVisibleItems())
            {
                DrawMessage(sprites, LocHelper.Empty, "Warning", GetWarningColor(), GeneralComponent.GetScale());
                (_app as FarmApp)?.CompleteHostRender();
            }
            else
                sprites.AddRange(_app.GetSprites());
            return sprites;
        }
    }
}
