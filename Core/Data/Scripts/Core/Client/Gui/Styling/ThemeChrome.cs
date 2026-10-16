using System;
using System.Collections.Generic;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.Styling
{
    /// <summary>
    ///     Ponto único das molduras, cartões, barras e divisórias por tema. Cada Draw* devolve false para
    ///     <see cref="LcdTheme.Default"/> sem tocar na lista, e o chamador mantém o desenho atual nesse caso.
    /// </summary>
    public static class ThemeChrome
    {
        const float FRAME_FILL_MIX = 0.05f;
        const float CLEAN_LINE_ALPHA = 0.15f;
        const float FUTURISTIC_LINE_ALPHA = 0.35f;
        const float OUTLINE_ALPHA = 0.6f;
        const float BRACKET_ARM = 10f;
        const float PERCENT_MIN_HEIGHT = 10f;
        const float PERCENT_TEXT_HEIGHT = 28.8f;
        const float PERCENT_TEXT_WIDTH = 78f;
        const float CHAMFER_RATIO = 0.18f;
        const float CHAMFER_MAX = 12f;
        const float FRAME_INSET_RATIO = 24f / 512f;
        const float SMALL_INSET = 2f;
        const float SMALL_FONT_BOOST = 1.25f;
        const float CAFE_FILL_MIX = 0.08f;

        public static Color FrameFill(LcdTheme theme, Color background, Color foreground)
        {
            switch (theme)
            {
                case LcdTheme.Futuristic: return Color.Lerp(background, foreground, FRAME_FILL_MIX);
                case LcdTheme.Cafe: return Color.Lerp(background, foreground, CAFE_FILL_MIX);
                default: return background;
            }
        }

        public static Color DividerColor(LcdTheme theme, Color foreground)
        {
            return WithAlpha(foreground, theme == LcdTheme.Futuristic ? FUTURISTIC_LINE_ALPHA : CLEAN_LINE_ALPHA);
        }

        public static string LabelText(LcdTheme theme, string text)
        {
            return theme == LcdTheme.Futuristic && text != null ? text.ToUpperInvariant() : text;
        }

        public static string TitleFont(LcdTheme theme, string font, string text)
        {
            return theme == LcdTheme.Futuristic ? FormatingHelper.BoldFont(font, text) : font;
        }

        /// <summary>Small amplia todo o texto dos apps para telas de cockpit.</summary>
        public static float FontBoost(LcdTheme theme)
        {
            return theme == LcdTheme.Small ? SMALL_FONT_BOOST : 1f;
        }

        /// <summary>Borda reservada à moldura: 9-slice no Default, faixa fina nos temas desenhados, mínima no Small.</summary>
        public static Vector2 FrameInset(LcdTheme theme, Vector2 surfaceSize, float defaultThickness)
        {
            switch (theme)
            {
                case LcdTheme.Default: return new Vector2(defaultThickness);
                case LcdTheme.Small: return new Vector2(SMALL_INSET);
                default: return surfaceSize * FRAME_INSET_RATIO;
            }
        }

        /// <summary>Moldura desenhada por código em volta de <paramref name="outer"/>; false no Default (o chamador desenha o 9-slice).</summary>
        public static bool DrawFrame(List<MySprite> sprites, LcdTheme theme, RectangleF outer, Color background, Color accent, Color foreground, float scale)
        {
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    DrawChamferedBox(sprites, outer, FrameFill(theme, background, foreground), WithAlpha(accent, OUTLINE_ALPHA), accent, scale);
                    return true;
                case LcdTheme.Cafe:
                    sprites.Add(LineRenderer.Rect(outer.Center, outer.Size, FrameFill(theme, background, foreground)));
                    LineRenderer.DrawRectOutline(sprites, outer, Math.Max(2f, 2f * scale), WithAlpha(accent, OUTLINE_ALPHA));
                    return true;
                case LcdTheme.Clean:
                case LcdTheme.Small:
                    LineRenderer.DrawRectOutline(sprites, outer, Math.Max(1f, scale), DividerColor(theme, foreground));
                    return true;
                default:
                    return false;
            }
        }

        public static bool DrawTitleAccent(List<MySprite> sprites, LcdTheme theme, RectangleF bar, RectangleF screen, Color accent, Color foreground, float scale)
        {
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    float thickness = Math.Max(1f, 1.5f * scale);
                    sprites.Add(LineRenderer.Rect(new Vector2(bar.Center.X, bar.Bottom - thickness * 0.5f), new Vector2(bar.Width, thickness), accent));
                    return true;
                case LcdTheme.Clean:
                case LcdTheme.Cafe:
                    DrawDivider(sprites, theme, new Vector2(bar.X, bar.Bottom - 1f), new Vector2(bar.Right, bar.Bottom - 1f), foreground, scale);
                    return true;
                default:
                    return false;
            }
        }

        public static bool DrawCard(List<MySprite> sprites, LcdTheme theme, RectangleF rect, Color background, Color accent, Color foreground, float scale)
        {
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    DrawChamferedBox(sprites, rect, FrameFill(theme, background, foreground), WithAlpha(accent, OUTLINE_ALPHA), accent, scale);
                    return true;
                case LcdTheme.Cafe:
                    sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, FrameFill(theme, background, foreground)));
                    goto case LcdTheme.Clean;
                case LcdTheme.Clean:
                    DrawDivider(sprites, theme, new Vector2(rect.X, rect.Bottom - 1f), new Vector2(rect.Right, rect.Bottom - 1f), foreground, scale);
                    return true;
                case LcdTheme.Small:
                    return true;
                default:
                    return false;
            }
        }

        public static bool DrawFooterBand(List<MySprite> sprites, LcdTheme theme, RectangleF rect, Color background, Color accent, Color foreground, float scale)
        {
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, FrameFill(theme, background, foreground)));
                    sprites.Add(LineRenderer.Rect(new Vector2(rect.Center.X, rect.Y + 0.5f), new Vector2(rect.Width, Math.Max(1f, scale)), accent));
                    return true;
                case LcdTheme.Cafe:
                    sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, FrameFill(theme, background, foreground)));
                    goto case LcdTheme.Clean;
                case LcdTheme.Clean:
                    DrawDivider(sprites, theme, new Vector2(rect.X, rect.Y), new Vector2(rect.Right, rect.Y), foreground, scale);
                    return true;
                case LcdTheme.Small:
                    return true;
                default:
                    return false;
            }
        }

        public static bool DrawDivider(List<MySprite> sprites, LcdTheme theme, Vector2 from, Vector2 to, Color foreground, float scale)
        {
            if (theme == LcdTheme.Default)
                return false;

            LineRenderer.DrawLine(sprites, from, to, Math.Max(1f, scale), DividerColor(theme, foreground));
            return true;
        }

        /// <summary>Barra na altura de <paramref name="rect"/> com o percentual em Monospace no fim. Vale para todos os temas.</summary>
        public static bool DrawBar(List<MySprite> sprites, LcdTheme theme, RectangleF rect, float fraction, Color fill, Color foreground, float scale)
        {
            if (rect.Width <= 0f || rect.Height <= 0f)
                return true;

            fraction = MathHelper.Clamp(fraction, 0f, 1f);
            float thickness = Math.Max(1f, scale);
            bool showPercent = rect.Height >= PERCENT_MIN_HEIGHT * scale;
            float textScale = showPercent ? rect.Height / PERCENT_TEXT_HEIGHT : 0f;
            float textWidth = showPercent ? PERCENT_TEXT_WIDTH * textScale + thickness * 2f : 0f;
            var bar = new RectangleF(rect.X, rect.Y, Math.Max(1f, rect.Width - textWidth), rect.Height);
            Color track = WithAlpha(foreground, CLEAN_LINE_ALPHA);
            switch (theme)
            {
                case LcdTheme.Futuristic:
                {
                    LineRenderer.DrawRectOutline(sprites, bar, thickness, WithAlpha(foreground, OUTLINE_ALPHA));
                    float width = (bar.Width - thickness * 2f) * fraction;
                    if (width > 0f)
                        sprites.Add(LineRenderer.Rect(new Vector2(bar.X + thickness + width * 0.5f, bar.Center.Y), new Vector2(width, bar.Height - thickness * 2f), fill));
                    break;
                }
                case LcdTheme.Clean:
                case LcdTheme.Cafe:
                case LcdTheme.Small:
                {
                    sprites.Add(LineRenderer.Rect(bar.Center, bar.Size, track));
                    float width = bar.Width * fraction;
                    if (width > 0f)
                        sprites.Add(LineRenderer.Rect(new Vector2(bar.X + width * 0.5f, bar.Center.Y), new Vector2(width, bar.Height), fill));
                    break;
                }
                default:
                    BarPanel.CreateSprites(sprites, bar.Position, bar.Size, fill, track, fraction, cornerRadius: bar.Height * 0.5f);
                    break;
            }

            if (showPercent)
                sprites.Add(new MySprite(SpriteType.TEXT, (int)Math.Round(fraction * 100f) + "%",
                    new Vector2(rect.Right, rect.Center.Y - PERCENT_TEXT_HEIGHT * textScale * 0.5f), null, foreground, "Monospace", TextAlignment.RIGHT, textScale));
            return true;
        }

        public static bool DrawIconCell(List<MySprite> sprites, LcdTheme theme, RectangleF cell, Color accent, float scale)
        {
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    sprites.Add(LineRenderer.Rect(new Vector2(cell.Right - 0.5f, cell.Center.Y), new Vector2(Math.Max(1f, scale), cell.Height), WithAlpha(accent, OUTLINE_ALPHA)));
                    return true;
                case LcdTheme.Clean:
                case LcdTheme.Cafe:
                case LcdTheme.Small:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Caixa de cantos chanfrados: preenchimento em cruz (fica de fora só o triângulo de cada chanfro) e contorno em
        ///     oito traços. As diagonais superior-esquerda e inferior-direita saem em <paramref name="accent"/> quando ela tem alfa.
        /// </summary>
        public static void DrawChamferedBox(List<MySprite> sprites, RectangleF rect, Color fill, Color line, Color accent, float scale)
        {
            if (rect.Width <= 0f || rect.Height <= 0f)
                return;

            float c = Math.Min(Math.Min(rect.Width, rect.Height) * CHAMFER_RATIO, CHAMFER_MAX * scale);
            if (fill.A > 0)
            {
                sprites.Add(LineRenderer.Rect(rect.Center, new Vector2(rect.Width - c * 2f, rect.Height), fill));
                sprites.Add(LineRenderer.Rect(rect.Center, new Vector2(rect.Width, rect.Height - c * 2f), fill));
            }

            if (line.A == 0)
                return;

            float t = Math.Max(1f, scale);
            float half = t * 0.5f;
            sprites.Add(LineRenderer.Rect(new Vector2(rect.Center.X, rect.Y + half), new Vector2(rect.Width - c * 2f, t), line));
            sprites.Add(LineRenderer.Rect(new Vector2(rect.Center.X, rect.Bottom - half), new Vector2(rect.Width - c * 2f, t), line));
            sprites.Add(LineRenderer.Rect(new Vector2(rect.X + half, rect.Center.Y), new Vector2(t, rect.Height - c * 2f), line));
            sprites.Add(LineRenderer.Rect(new Vector2(rect.Right - half, rect.Center.Y), new Vector2(t, rect.Height - c * 2f), line));
            Color diagonal = accent.A > 0 ? accent : line;
            LineRenderer.DrawLine(sprites, new Vector2(rect.X + half, rect.Y + c), new Vector2(rect.X + c, rect.Y + half), t, diagonal, half);
            LineRenderer.DrawLine(sprites, new Vector2(rect.Right - c, rect.Y + half), new Vector2(rect.Right - half, rect.Y + c), t, line, half);
            LineRenderer.DrawLine(sprites, new Vector2(rect.Right - half, rect.Bottom - c), new Vector2(rect.Right - c, rect.Bottom - half), t, diagonal, half);
            LineRenderer.DrawLine(sprites, new Vector2(rect.X + c, rect.Bottom - half), new Vector2(rect.X + half, rect.Bottom - c), t, line, half);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.R, color.G, color.B, (byte)MathHelper.Clamp(alpha * 255f, 0f, 255f));
        }
    }
}
