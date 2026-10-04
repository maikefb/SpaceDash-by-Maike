using System;
using System.Collections.Generic;
using LcdMod.Client.Gui.ControlsTemplates.Basic;
using LcdMod.Client.Gui.ControlsTemplates.Custom.Clock;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Custom
{
    /// <summary>
    /// Uma linha do ranking: marcador de medalha, posição e nome à esquerda, tempo vivo e recorde à direita.
    /// A fonte é ajustada à altura da linha e as colunas de tempo são medidas para "00d 00h 00m" caber sem reticências.
    /// </summary>
    internal sealed class SurvivorRowControl : RectangleControl
    {
        const string TIME_TEMPLATE = "00d 00h 00m";

        readonly SpriteIconControl _marker;
        readonly TextBlock _name;
        readonly TextBlock _alive;
        readonly TextBlock _record;
        bool _hasMarker;

        public SurvivorRowControl()
            : base(default(RectangleF))
        {
            _marker = new SpriteIconControl
            {
                SpriteName = "Circle",
                SizeRatio = 1f
            };
            _name = CreateText(TextAlignment.LEFT);
            _alive = CreateText(TextAlignment.RIGHT);
            _record = CreateText(TextAlignment.RIGHT);

            AddChild(_marker);
            AddChild(_name);
            AddChild(_alive);
            AddChild(_record);
        }

        public void Bind(string name, string alive, string record, Color nameColor, Color aliveColor, Color recordColor, Color? marker)
        {
            _name.Text = name ?? string.Empty;
            _name.TextColor = nameColor;
            _alive.Text = alive ?? string.Empty;
            _alive.TextColor = aliveColor;
            _record.Text = record ?? string.Empty;
            _record.TextColor = recordColor;
            _hasMarker = marker.HasValue;
            _marker.Tint = marker;
        }

        static TextBlock CreateText(TextAlignment alignment)
        {
            return new TextBlock(default(RectangleF))
            {
                FontScale = 1f,
                Ellipsize = true,
                HorizontalAlignment = alignment,
                VerticalAlignment = TextBlockVerticalAlignment.Center
            };
        }

        protected override void RenderDefault(List<MySprite> sprites)
        {
            if (TextSurface == null)
                return;

            var rect = GetViewBox();
            if (rect.Width <= 0f || rect.Height <= 0f)
                return;

            Vector2 unit = MeasureText(TIME_TEMPLATE, 1f);
            if (unit.X <= 0f || unit.Y <= 0f)
                return;

            float gap = 4f * LayoutScale;
            float markerSize = MathHelper.Max(1f, rect.Height * 0.4f);
            float columnsWidth = MathHelper.Max(1f, rect.Width - markerSize - 4f * gap);

            float byHeight = rect.Height * 0.72f / unit.Y;
            float byWidth = columnsWidth * 0.32f / unit.X;
            float effectiveScale = Math.Max(0.05f, Math.Min(byHeight, byWidth));
            float styledScale = Math.Max(0.01f, LayoutScale * ResolveStyleValue(FontScaleProperty));
            float fontScale = effectiveScale / styledScale;
            _name.FontScale = fontScale;
            _alive.FontScale = fontScale;
            _record.FontScale = fontScale;

            float timeWidth = unit.X * effectiveScale + gap;
            var markerRect = new RectangleF(rect.X + gap, rect.Center.Y - markerSize * 0.5f, markerSize, markerSize);
            float x = markerRect.Right + gap;
            float nameWidth = MathHelper.Max(1f, rect.Right - x - 2f * timeWidth - 2f * gap);

            _name.Arrange(new RectangleF(x, rect.Y, nameWidth, rect.Height));
            _alive.Arrange(new RectangleF(x + nameWidth + gap, rect.Y, timeWidth, rect.Height));
            _record.Arrange(new RectangleF(x + nameWidth + gap + timeWidth + gap, rect.Y, timeWidth, rect.Height));

            if (_hasMarker)
            {
                _marker.Arrange(markerRect);
                _marker.Render(sprites);
            }

            _name.Render(sprites);
            _alive.Render(sprites);
            _record.Render(sprites);
        }
    }
}
