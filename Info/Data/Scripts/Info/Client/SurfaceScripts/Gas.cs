using System.Collections.Generic;
using Generated;
using LcdMod.Client.Apps;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Terminal.Controls;
using LcdMod.Client.Terminal.Controls.Generic;
using LcdMod.Client.Utility;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.SurfaceScripts
{
    
    [MyTextSurfaceScript(ID, TITLE)]
    public partial class GasSurfaceScript : SurfaceScriptBase, IMultiDisplayMode
    {
        public const string ID = ID_PREFIX + "GasGraph";
        public const string TITLE = MOD_PREFIX + "GasFilled";
        protected override string DefaultTitle => TITLE;
        
        public override IApp App => _app;
        GasApp _app;
        bool _hasDrawn;
        int _lastScrollStep = -1;
        readonly List<MySprite> _frame = new List<MySprite>();

        public GasSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        public List<MyTerminalControlComboBoxItem> GetDisplayModes()
        {
            return DisplayModes.GridAndLegacy;
        }

        public override void SafeRun()
        {
            if (_app == null)
                _app = new GasApp(this);

            var viewBoxBefore = ViewBox;
            UpdateViewBox();
            _app.Update();

            // A textura só é redesenhada quando dados, layout, tema ou a rolagem automática mudam.
            var scrollStep = _app.IsScrollable ? GetTimeStep(InteractionComponent.AutoScrollStep) : 0;
            var needsRender = !_hasDrawn || Dirty || LayoutDirty || _app.IsDirty || _app.DataChanged ||
                              scrollStep != _lastScrollStep || !SameRect(viewBoxBefore, ViewBox);
            _lastScrollStep = scrollStep;
            if (!needsRender)
                return;

            RenderSprites();
            _hasDrawn = true;
            LayoutDirty = false;
        }

        static bool SameRect(RectangleF a, RectangleF b)
        {
            return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = _frame;
            sprites.Clear();
            if (_app == null || !_app.HasEntries)
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
