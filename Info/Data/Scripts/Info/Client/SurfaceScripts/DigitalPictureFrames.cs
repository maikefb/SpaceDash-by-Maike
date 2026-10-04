using System.Collections.Generic;
using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Helpers;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Terminal.Controls;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Helpers;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;
using IMyTextSurface = Sandbox.ModAPI.IMyTextSurface;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.SurfaceScripts
{
    [MyTextSurfaceScript(ID, TITLE)]
    public sealed partial class DigitalPictureFramesSurfaceScript : SurfaceScriptBase, IMultiDisplayMode
    {
        public const string ID = ID_PREFIX + "DigitalPictureFrames";
        public const string TITLE = MOD_PREFIX + "DigitalPictureFrames";

        static readonly List<MyTerminalControlComboBoxItem> DisplayModes = new List<MyTerminalControlComboBoxItem>
        {
            Mode(DigitalPictureFramesApp.PictureFrameDisplayMode.Center, "Center"),
            Mode(DigitalPictureFramesApp.PictureFrameDisplayMode.Fit, "Fit"),
            Mode(DigitalPictureFramesApp.PictureFrameDisplayMode.Fill, "Fill"),
            Mode(DigitalPictureFramesApp.PictureFrameDisplayMode.Stretch, "Stretch"),
            Mode(DigitalPictureFramesApp.PictureFrameDisplayMode.Tile, "Tile")
        };

        static MyTerminalControlComboBoxItem Mode(DigitalPictureFramesApp.PictureFrameDisplayMode mode, string key)
        {
            return new MyTerminalControlComboBoxItem { Key = (long)mode, Value = MyStringId.GetOrCompute(MOD_PREFIX + "PictureMode_" + key) };
        }

        DigitalPictureFramesApp _app;
        protected override string DefaultTitle => TITLE;
        public override IApp App => _app;

        public DigitalPictureFramesSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
        }

        public List<MyTerminalControlComboBoxItem> GetDisplayModes()
        {
            return DisplayModes;
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

        protected override string GetDetailedInfoCustomText()
        {
            return LocHelper.GetLoc(MOD_PREFIX + "PictureFrames_Folder") + " " + TextureFileHelper.StorageFolder();
        }

        public override void SafeRun()
        {
            if (_app == null)
                _app = new DigitalPictureFramesApp(this);

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

            if (!_app.HasVisibleItems())
                DrawMessage(sprites, LocHelper.GetLoc(MOD_PREFIX + "PictureFrames_Empty"), "Warning", GetWarningColor(), GeneralComponent.GetScale());
            else
                sprites.AddRange(_app.GetSprites());
            return sprites;
        }
    }
}
