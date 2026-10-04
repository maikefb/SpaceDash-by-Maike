using System;
using System.Collections.Generic;
using LcdMod.Client.Helpers;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Basic
{
    public enum TextBlockWrapping
    {
        NoWrap,
        Wrap
    }

    public enum TextBlockVerticalAlignment
    {
        Top,
        Center,
        Bottom
    }

    public sealed class TextBlock : RectangleControl
    {
        string _cachedText;
        string _cachedFont;
        float _cachedWidth;
        float _cachedHeight;
        float _cachedScale;
        float _cachedSpacing;
        bool _cachedEllipsize;
        string _cachedLine;
        List<string> _cachedLines;

        public TextBlock(RectangleF bounds) : base(bounds)
        {
            Text = string.Empty;
            FontId = null;
            FontScale = 0.58f;
            LineSpacingPixels = 0f;
            Wrapping = TextBlockWrapping.NoWrap;
            Ellipsize = true;
            HorizontalAlignment = TextAlignment.LEFT;
            VerticalAlignment = TextBlockVerticalAlignment.Center;
        }

        public string Text { get; set; }
        public string FontId { get; set; }
        public float LineSpacingPixels { get; set; }
        public TextBlockWrapping Wrapping { get; set; }
        public bool Ellipsize { get; set; }
        public TextAlignment HorizontalAlignment { get; set; }
        public TextBlockVerticalAlignment VerticalAlignment { get; set; }
        public new Color? TextColor { get; set; }

        protected override void RenderDefault(List<MySprite> sprites)
        {
            if (string.IsNullOrEmpty(Text) || TextSurface == null)
                return;

            var rect = GetViewBox();
            if (rect.Width <= 0f || rect.Height <= 0f)
                return;

            string fontId = string.IsNullOrEmpty(FontId) ? TextFont : FontId;
            float styledFontScale = ResolveStyleValue(FontScaleProperty);
            float scale = Math.Max(0.01f, LayoutScale * styledFontScale * FontScale);
            Color color = TextColor ?? base.TextColor;
            if (Wrapping == TextBlockWrapping.Wrap)
                RenderWrapped(rect, sprites, fontId, scale, color);
            else
                RenderSingleLine(rect, sprites, fontId, scale, color);
        }

        bool IsCached(string text, string fontId, float scale, float width, float height, float spacing)
        {
            return _cachedText == text &&
                   _cachedFont == fontId &&
                   _cachedScale == scale &&
                   _cachedWidth == width &&
                   _cachedHeight == height &&
                   _cachedSpacing == spacing &&
                   _cachedEllipsize == Ellipsize;
        }

        void RememberCacheKey(string text, string fontId, float scale, float width, float height, float spacing)
        {
            _cachedText = text;
            _cachedFont = fontId;
            _cachedScale = scale;
            _cachedWidth = width;
            _cachedHeight = height;
            _cachedSpacing = spacing;
            _cachedEllipsize = Ellipsize;
        }

        void RenderSingleLine(RectangleF rect, List<MySprite> sprites, string fontId, float scale, Color color)
        {
            string text = ResolveSingleLineText(rect, fontId, scale);

            if (string.IsNullOrEmpty(text))
                return;

            Vector2 size = MeasureTextSafe(text, fontId, scale);
            float y = GetTextY(rect, size.Y, 0f);
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = new Vector2(GetTextX(rect), y),
                RotationOrScale = scale,
                Color = color,
                Alignment = HorizontalAlignment,
                FontId = fontId
            });
        }

        string ResolveSingleLineText(RectangleF rect, string fontId, float scale)
        {
            string text = Text ?? string.Empty;
            if (_cachedLine != null && IsCached(text, fontId, scale, rect.Width, 0f, 0f))
                return _cachedLine;

            string result;
            if (MeasureTextSafe(text, fontId, scale).X <= rect.Width)
                result = text;
            else
                result = Ellipsize
                    ? EllipsizeToWidth(text, fontId, scale, rect.Width)
                    : TrimToWidth(text, fontId, scale, rect.Width);

            RememberCacheKey(text, fontId, scale, rect.Width, 0f, 0f);
            _cachedLine = result;
            _cachedLines = null;
            return result;
        }

        string EllipsizeToWidth(string text, string fontId, float scale, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
                return string.Empty;

            string suffix = FormatingHelper.ELLIPSIS.ToString();
            if (MeasureTextSafe(suffix, fontId, scale).X > maxWidth)
                return TrimToWidth(text, fontId, scale, maxWidth);

            // Busca binária no comprimento: a largura só cresce com mais caracteres.
            int low = 1, high = text.Length, best = 0;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                var candidate = text.Substring(0, mid).TrimEnd();
                if (candidate.Length > 0 && MeasureTextSafe(candidate + suffix, fontId, scale).X <= maxWidth)
                {
                    best = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            if (best <= 0)
                return string.Empty;

            var trimmed = text.Substring(0, best).TrimEnd();
            return trimmed.Length > 0 ? trimmed + suffix : string.Empty;
        }

        string TrimToWidth(string text, string fontId, float scale, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
                return string.Empty;

            int low = 1, high = text.Length, best = 0;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (MeasureTextSafe(text.Substring(0, mid), fontId, scale).X <= maxWidth)
                {
                    best = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return best <= 0 ? string.Empty : text.Substring(0, best);
        }

        Vector2 MeasureTextSafe(string text, string fontId, float scale)
        {
            var surface = TextSurface;
            if (surface == null || string.IsNullOrEmpty(text))
                return Vector2.Zero;

            var measured = FormatingHelper.GetSizeInPixel(text, fontId, scale, surface);
            float height = measured.Y > 0f ? measured.Y : Math.Max(1f, 30f * Math.Max(0.01f, scale));
            return new Vector2(Math.Max(0f, measured.X), height);
        }

        void RenderWrapped(RectangleF rect, List<MySprite> sprites, string fontId, float scale, Color color)
        {
            var surface = TextSurface;
            float spacing = LineSpacingPixels * LayoutScale;
            string text = Text ?? string.Empty;
            List<string> lines;
            if (_cachedLines != null && IsCached(text, fontId, scale, rect.Width, rect.Height, spacing))
            {
                lines = _cachedLines;
            }
            else
            {
                lines = TextWrappingHelper.WrapText(text, surface, fontId, scale, rect.Width, rect.Height, spacing, Ellipsize);
                RememberCacheKey(text, fontId, scale, rect.Width, rect.Height, spacing);
                _cachedLines = lines;
                _cachedLine = null;
            }

            if (lines == null || lines.Count == 0)
                return;

            float lineHeight = TextWrappingHelper.GetLineHeight(surface, fontId, scale, spacing);
            float totalHeight = lineHeight * lines.Count;
            float y = GetTextY(rect, totalHeight, 0f);
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrEmpty(line))
                    continue;

                sprites.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = line,
                    Position = new Vector2(GetTextX(rect), y + i * lineHeight),
                    RotationOrScale = scale,
                    Color = color,
                    Alignment = HorizontalAlignment,
                    FontId = fontId
                });
            }
        }

        float GetTextX(RectangleF rect)
        {
            switch (HorizontalAlignment)
            {
                case TextAlignment.RIGHT:
                    return rect.Right;
                case TextAlignment.CENTER:
                    return rect.Center.X;
                default:
                    return rect.X;
            }
        }

        float GetTextY(RectangleF rect, float textHeight, float topPadding)
        {
            switch (VerticalAlignment)
            {
                case TextBlockVerticalAlignment.Top:
                    return rect.Y + topPadding;
                case TextBlockVerticalAlignment.Bottom:
                    return rect.Bottom - textHeight;
                default:
                    return rect.Center.Y - textHeight * 0.5f;
            }
        }
    }
}
