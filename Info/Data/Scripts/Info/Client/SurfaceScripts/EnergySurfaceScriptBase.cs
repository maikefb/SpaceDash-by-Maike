using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Common.Config.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.IMyTextSurface;

namespace LcdMod.Client.SurfaceScripts
{
    /// <summary>
    ///     Ciclo comum das telas de energia. Enquanto carrega, usa o cabeçalho e o spinner padrão do mod; com dados, o app
    ///     decide se o título é o cabeçalho padrão ou a barra dentro da moldura.
    /// </summary>
    public abstract class EnergySurfaceScriptBase : SurfaceScriptBase
    {
        EnergyAppBase _app;

        protected EnergySurfaceScriptBase(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
        }

        public override IApp App => _app;

        internal abstract EnergyAppBase CreateApp();

        protected override IApp DetachGridBoundApp()
        {
            var app = _app;
            _app = null;
            return app;
        }

        public override void SafeRun()
        {
            if (_app == null)
                _app = CreateApp();

            UpdateViewBox();
            _app.Update();
            RenderSprites();
        }

        public override void DrawTitle(List<MySprite> frame)
        {
            if (_app == null || !_app.OwnsTitle || !_app.HasData)
            {
                base.DrawTitle(frame);
                return;
            }

            CaretY = _app.DrawChrome(frame);
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = new List<MySprite>();
            AddBackground(sprites);
            DrawTitle(sprites);
            if (_app == null || !_app.HasData)
                DrawLoading(sprites, ConfiguredScale);
            else if (!_app.HasVisibleItems())
                DrawMessage(sprites, _app.EmptyMessage, "Warning", _app.OwnsTitle ? _app.MessageColor : GetWarningColor(), ConfiguredScale);
            else
                sprites.AddRange(_app.GetSprites());
            return sprites;
        }
    }
}
