using System.Collections.Generic;
using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Terminal.Controls;
using LcdMod.Common.Config.Generation;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace LcdMod.Client.SurfaceScripts
{
    
    [MyTextSurfaceScript(ID, TITLE)]
    public partial class ConveyorMapSurfaceScript : SurfaceScriptBase, IReferenceBlockSelection
    {
        public const string ID = ID_PREFIX + "ConveyorMap";
        public const string TITLE = MOD_PREFIX + "ConveyorMap";

        ConveyorMapApp _app;

        public override IApp App => _app;
        protected override string DefaultTitle => TITLE;

        public ConveyorMapSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base((Sandbox.ModAPI.IMyTextSurface)surface, block, size)
        {
        }

        public bool IsReferenceBlockCandidate(Sandbox.ModAPI.IMyTerminalBlock block)
        {
            return _app != null && _app.IsConveyorBlock(block);
        }

        public bool TryGetReferenceBlockCandidates(List<Sandbox.ModAPI.IMyTerminalBlock> blocks)
        {
            if (_app != null)
                blocks.AddRange(_app.NetworkAnchors);
            return true;
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
                _app = new ConveyorMapApp(this);

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
