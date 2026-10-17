using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace LcdMod.Client.Gui.Styling
{
    /// <summary>Medição e blocos dos temas Mínimo e Compacto: texto que some em vez de encolher, anel, chave-valor, tiles e herói.</summary>
    public sealed class BareLayout
    {
        const float MARGIN_BASE = 4f;
        const float ICON_BASE = 16f;
        const float RING_THICKNESS_MIN = 4f;
        const float RING_THICKNESS_RATIO = 0.12f;
        const float RING_TEXT_WIDTH_RATIO = 1.6f;
        const float KV_ROW_RATIO = 1.5f;
        const float KV_DIVIDER_ALPHA = 0.15f;
        const float TILE_VALUE_FONT_MUL = 1.4f;
        const float ASPECT_RATIO = 1.5f;

        readonly IAppHost _host;
        readonly ITextStyleProvider _style;

        public BareLayout(IAppHost host, ITextStyleProvider style)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (style == null)
                throw new ArgumentNullException(nameof(style));
            _host = host;
            _style = style;
        }

        public float FontScale => _host.FontScale;
        public float Scale => _host.ConfiguredScale;
        public float Margin => MARGIN_BASE * Scale;
        IMyTextSurface Surface => _host.Surface;

        public float LineHeight(float fontMul)
        {
            return FormatingHelper.LineHeight(FontScale * fontMul, _style, Surface);
        }

        public bool Fits(string text, float fontMul, float maxWidth, float maxHeight)
        {
            if (string.IsNullOrEmpty(text) || FontScale <= 0f || maxWidth <= 0f || maxHeight <= 0f)
                return false;
            Vector2 size = Measure(text, fontMul, false);
            return size.X <= maxWidth && size.Y <= maxHeight;
        }

        public bool Fits(string text, float fontMul, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || FontScale <= 0f || maxWidth <= 0f)
                return false;
            return Measure(text, fontMul, false).X <= maxWidth;
        }

        /// <summary>Texto que some em vez de encolher; false quando não desenhou.</summary>
        public bool Text(List<MySprite> s, string text, Vector2 pos, TextAlignment align, float fontMul, Color color, float maxWidth, float maxHeight, bool bold = false)
        {
            if (s == null || string.IsNullOrEmpty(text) || FontScale <= 0f || maxWidth <= 0f || maxHeight <= 0f)
                return false;
            Vector2 size = Measure(text, fontMul, bold);
            if (size.X > maxWidth || size.Y > maxHeight)
                return false;
            s.Add(new MySprite(SpriteType.TEXT, text, pos, null, color, Font(text, bold), align, FontScale * fontMul));
            return true;
        }

        /// <summary>Rótulo com fallback texto → ícone → nada; devolve a largura usada.</summary>
        public float Label(List<MySprite> s, string text, string icon, RectangleF box, Color color, float fontMul = 1f)
        {
            if (s == null || box.Width <= 0f || box.Height <= 0f || FontScale <= 0f)
                return 0f;
            if (Fits(text, fontMul, box.Width, box.Height))
            {
                Vector2 size = Measure(text, fontMul, false);
                s.Add(new MySprite(SpriteType.TEXT, text, new Vector2(box.X, box.Center.Y - size.Y * 0.5f), null, color, Font(text, false), TextAlignment.LEFT, FontScale * fontMul));
                return size.X;
            }
            if (string.IsNullOrEmpty(icon))
                return 0f;
            float side = Math.Min(box.Height, ICON_BASE * Scale);
            if (side <= 0f)
                return 0f;
            s.Add(new MySprite(SpriteType.TEXTURE, icon, new Vector2(box.X + side * 0.5f, box.Center.Y), new Vector2(side, side), color, null, TextAlignment.CENTER));
            return side;
        }

        /// <summary>Anel de uma fatia sobre trilho; diâmetro = menor lado da área menos 2 margens.</summary>
        public void Ring(List<MySprite> s, RectangleF area, float fraction, Color fill, Color track, string center, Color centerColor, float centerFontMul)
        {
            float outer, inner;
            if (!RingRadii(s, area, out outer, out inner))
                return;
            DonutPanel.DrawDonut(s, area.Center, inner, outer, fraction, fill, track);
            RingCenter(s, area.Center, inner, center, centerColor, centerFontMul);
        }

        /// <summary>Anel de várias fatias sobre trilho; diâmetro = menor lado da área menos 2 margens.</summary>
        public void Ring(List<MySprite> s, RectangleF area, IReadOnlyList<DonutSegment> segments, Color track, string center, Color centerColor, float centerFontMul)
        {
            float outer, inner;
            if (segments == null || !RingRadii(s, area, out outer, out inner))
                return;
            DonutPanel.DrawSegmentedDonut(s, area.Center, inner, outer, segments, track);
            RingCenter(s, area.Center, inner, center, centerColor, centerFontMul);
        }

        public static float RingThickness(float diameter)
        {
            return Math.Max(RING_THICKNESS_MIN, diameter * RING_THICKNESS_RATIO);
        }

        public struct KvRow
        {
            public string Label, Icon, Value;
            public Color ValueColor;
            /// <summary>Fração da barra sob a linha; negativo = sem barra.</summary>
            public float Bar;
            public Color BarColor;
        }

        /// <summary>Linhas chave-valor de altura LineHeight(1)×1,5 com divisor fino; corta na última linha inteira que cabe e devolve as desenhadas.</summary>
        public int Kv(List<MySprite> s, RectangleF area, List<KvRow> rows, Color fg, LcdTheme theme)
        {
            if (s == null || rows == null || !CanDraw(area))
                return 0;
            float lineHeight = LineHeight(1f);
            float rowHeight = lineHeight * KV_ROW_RATIO;
            if (rowHeight <= 0f)
                return 0;
            int count = Math.Min(rows.Count, (int)Math.Floor(area.Height / rowHeight));
            Color divider = ThemeChrome.WithAlpha(fg, KV_DIVIDER_ALPHA);
            float margin = Margin;

            for (int i = 0; i < count; i++)
            {
                KvRow row = rows[i];
                float top = area.Y + i * rowHeight;
                if (i > 0)
                    s.Add(LineRenderer.Rect(new Vector2(area.Center.X, top), new Vector2(area.Width, 1f), divider));

                bool hasBar = row.Bar >= 0f;
                float textTop = hasBar ? top : top + (rowHeight - lineHeight) * 0.5f;
                float valueWidth = 0f;
                if (Text(s, row.Value, new Vector2(area.Right, textTop), TextAlignment.RIGHT, 1f, row.ValueColor, area.Width, lineHeight, true))
                    valueWidth = Measure(row.Value, 1f, true).X + margin;

                Label(s, row.Label, row.Icon, new RectangleF(area.X, textTop, area.Width - valueWidth, lineHeight), fg);

                if (!hasBar)
                    continue;
                float barHeight = (rowHeight - lineHeight) * 0.5f;
                float barTop = top + lineHeight + barHeight * 0.5f;
                ThemeChrome.DrawBar(s, theme, new RectangleF(area.X, barTop, area.Width, barHeight), row.Bar, row.BarColor, fg, Scale);
            }

            return count;
        }

        public struct Tile
        {
            public string Icon, Label, Value;
            public Color ValueColor;
        }

        /// <summary>Grade cols×rows de cartões: ícone e rótulo em cima, valor embaixo (some se não cabe).</summary>
        public void Tiles(List<MySprite> s, RectangleF area, List<Tile> tiles, int cols, int rows, Color fg, Color accent, LcdTheme theme)
        {
            if (s == null || tiles == null || cols <= 0 || rows <= 0 || !CanDraw(area))
                return;
            float gap = Margin;
            float tileWidth = (area.Width - gap * (cols - 1)) / cols;
            float tileHeight = (area.Height - gap * (rows - 1)) / rows;
            if (tileWidth <= 0f || tileHeight <= 0f)
                return;
            Color background = _host.BackgroundColor;
            float valueHeight = LineHeight(TILE_VALUE_FONT_MUL);
            int count = Math.Min(tiles.Count, cols * rows);

            for (int i = 0; i < count; i++)
            {
                Tile tile = tiles[i];
                var rect = new RectangleF(area.X + (i % cols) * (tileWidth + gap), area.Y + (i / cols) * (tileHeight + gap), tileWidth, tileHeight);
                ThemeChrome.DrawCard(s, theme, rect, background, accent, fg, Scale);

                var inner = new RectangleF(rect.X + gap, rect.Y + gap, rect.Width - gap * 2f, rect.Height - gap * 2f);
                if (inner.Width <= 0f || inner.Height <= 0f)
                    continue;
                float halfHeight = inner.Height * 0.5f;

                float labelLeft = inner.X;
                if (!string.IsNullOrEmpty(tile.Icon))
                {
                    float side = Math.Min(halfHeight, ICON_BASE * Scale);
                    if (side > 0f)
                    {
                        s.Add(new MySprite(SpriteType.TEXTURE, tile.Icon, new Vector2(inner.X + side * 0.5f, inner.Y + halfHeight * 0.5f), new Vector2(side, side), fg, null, TextAlignment.CENTER));
                        labelLeft += side + gap;
                    }
                }
                Label(s, tile.Label, null, new RectangleF(labelLeft, inner.Y, inner.Right - labelLeft, halfHeight), fg);

                Text(s, tile.Value, new Vector2(inner.Center.X, inner.Y + halfHeight * 1.5f - valueHeight * 0.5f), TextAlignment.CENTER, TILE_VALUE_FONT_MUL, tile.ValueColor, inner.Width, halfHeight);
            }
        }

        /// <summary>Valor-herói centrado com multiplicador fixo; devolve a altura ocupada ou 0 se não coube.</summary>
        public float Hero(List<MySprite> s, string text, RectangleF area, float fontMul, Color color)
        {
            if (s == null || !CanDraw(area))
                return 0f;
            float height = LineHeight(fontMul);
            return Text(s, text, new Vector2(area.Center.X, area.Center.Y - height * 0.5f), TextAlignment.CENTER, fontMul, color, area.Width, area.Height)
                ? height
                : 0f;
        }

        public static bool IsWide(RectangleF r)
        {
            return r.Width > ASPECT_RATIO * r.Height;
        }

        public static bool IsTall(RectangleF r)
        {
            return r.Height > ASPECT_RATIO * r.Width;
        }

        bool CanDraw(RectangleF area)
        {
            return area.Width > 0f && area.Height > 0f && FontScale > 0f;
        }

        bool RingRadii(List<MySprite> s, RectangleF area, out float outer, out float inner)
        {
            outer = 0f;
            inner = 0f;
            if (s == null || !CanDraw(area))
                return false;
            float diameter = Math.Min(area.Width, area.Height) - Margin * 2f;
            if (diameter <= 0f)
                return false;
            outer = diameter * 0.5f;
            inner = outer - RingThickness(diameter);
            return true;
        }

        void RingCenter(List<MySprite> s, Vector2 center, float inner, string text, Color color, float fontMul)
        {
            float maxWidth = inner * RING_TEXT_WIDTH_RATIO;
            if (!Fits(text, fontMul, maxWidth, inner))
                return;
            float height = Measure(text, fontMul, false).Y;
            Text(s, text, new Vector2(center.X, center.Y - height * 0.5f), TextAlignment.CENTER, fontMul, color, maxWidth, inner);
        }

        string Font(string text, bool bold)
        {
            string font = _style.ResolvedTextFont;
            if (string.IsNullOrEmpty(font))
                font = "White";
            return bold ? FormatingHelper.BoldFont(font, text) : font;
        }

        Vector2 Measure(string text, float fontMul, bool bold)
        {
            return FormatingHelper.GetSizeInPixel(text, Font(text, bold), FontScale * fontMul, Surface);
        }
    }
}
