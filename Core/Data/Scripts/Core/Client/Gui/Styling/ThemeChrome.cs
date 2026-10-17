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
        const float BARE_INSET = 4f;
        const float BARE_CARD_MIX = 0.06f;
        const float SMALL_FONT_BOOST = 1.25f;
        const float CAFE_FILL_MIX = 0.08f;
        const float CAFE_BORDER_GAP = 2f;
        const float CAFE_CORNER_RADIUS = 4f;
        const float CAFE_CARD_MIX = 0.4f;
        const float CAFE_SHADOW_ALPHA = 0.2f;
        const float CAFE_DASH = 6f;
        const float CAFE_DASH_GAP = 4f;
        const float FUTURISTIC_FOOTER_ALPHA = 0.25f;

        static readonly Color CAFE_INK = new Color(62, 42, 28);
        static readonly Color CAFE_PAPER = new Color(242, 232, 214);
        static readonly Color CAFE_BROWN = new Color(120, 80, 50);

        /// <summary>Mínimo e Compacto: sem título, rodapé, moldura ou faixas; só fundo e margem.</summary>
        public static bool IsBare(LcdTheme theme)
        {
            return theme == LcdTheme.Minimal || theme == LcdTheme.Compact;
        }

        /// <summary>Cor de texto efetiva: Café troca a do usuário pela tinta marrom-escura.</summary>
        public static Color Foreground(LcdTheme theme, Color user)
        {
            return theme == LcdTheme.Cafe ? CAFE_INK : user;
        }

        /// <summary>Cor de fundo efetiva: Café troca a do usuário pelo papel creme.</summary>
        public static Color Background(LcdTheme theme, Color user)
        {
            return theme == LcdTheme.Cafe ? CAFE_PAPER : user;
        }

        /// <summary>Cor do texto do título: Futuristic escreve na cor de fundo sobre a faixa de acento.</summary>
        public static Color TitleColor(LcdTheme theme, Color accent, Color background)
        {
            return theme == LcdTheme.Futuristic ? background : accent;
        }

        /// <summary>Clean e Default à esquerda; Futuristic, Café e Small centrados.</summary>
        public static TextAlignment TitleAlignment(LcdTheme theme)
        {
            return theme == LcdTheme.Clean || theme == LcdTheme.Default ? TextAlignment.LEFT : TextAlignment.CENTER;
        }

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
            switch (theme)
            {
                case LcdTheme.Futuristic:
                case LcdTheme.Clean:
                case LcdTheme.Cafe:
                    return FormatingHelper.BoldFont(font, text);
                default:
                    return font;
            }
        }

        /// <summary>Small amplia todo o texto dos apps para telas de cockpit.</summary>
        public static float FontBoost(LcdTheme theme)
        {
            return theme == LcdTheme.Small ? SMALL_FONT_BOOST : 1f;
        }

        /// <summary>Borda reservada à moldura: 9-slice no Default, faixa fina nos temas desenhados, mínima no Small, 4 px × escala nos bare.</summary>
        public static Vector2 FrameInset(LcdTheme theme, Vector2 surfaceSize, float defaultThickness, float scale)
        {
            if (IsBare(theme))
                return new Vector2(BARE_INSET * scale);
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
            if (IsBare(theme))
                return true;
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    DrawChamferedBox(sprites, outer, FrameFill(theme, background, foreground), WithAlpha(accent, OUTLINE_ALPHA), accent, scale);
                    return true;
                case LcdTheme.Cafe:
                    sprites.Add(LineRenderer.Rect(outer.Center, outer.Size, FrameFill(theme, background, foreground)));
                    float gap = CAFE_BORDER_GAP * scale + Math.Max(1f, scale);
                    LineRenderer.DrawRectOutline(sprites, outer, Math.Max(1f, scale), CAFE_BROWN);
                    LineRenderer.DrawRectOutline(sprites, new RectangleF(outer.X + gap, outer.Y + gap, outer.Width - gap * 2f, outer.Height - gap * 2f), Math.Max(1f, scale), CAFE_BROWN);
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
            if (IsBare(theme))
                return true;
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    sprites.Add(LineRenderer.Rect(bar.Center, bar.Size, accent));
                    return true;
                case LcdTheme.Clean:
                    DrawDivider(sprites, theme, new Vector2(bar.X, bar.Bottom - 1f), new Vector2(bar.Right, bar.Bottom - 1f), foreground, scale);
                    return true;
                case LcdTheme.Cafe:
                    DrawDashedLine(sprites, bar.Center.X, bar.Bottom - 1f, bar.Width, CAFE_BROWN, scale);
                    return true;
                default:
                    return false;
            }
        }

        static void DrawDashedLine(List<MySprite> sprites, float centerX, float y, float width, Color color, float scale)
        {
            float dash = CAFE_DASH * scale;
            float gap = CAFE_DASH_GAP * scale;
            int count = (int)Math.Floor((width + gap) / (dash + gap));
            if (count <= 0)
                return;

            float thickness = Math.Max(1f, scale);
            float x = centerX - (count * dash + (count - 1) * gap) * 0.5f + dash * 0.5f;
            for (int i = 0; i < count; i++)
                sprites.Add(LineRenderer.Rect(new Vector2(x + i * (dash + gap), y), new Vector2(dash, thickness), color));
        }

        static void DrawRoundedRect(List<MySprite> sprites, RectangleF rect, float radius, Color color)
        {
            float r = Math.Min(radius, Math.Min(rect.Width, rect.Height) * 0.5f);
            if (r <= 0f)
            {
                sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, color));
                return;
            }

            sprites.Add(LineRenderer.Rect(rect.Center, new Vector2(rect.Width - r * 2f, rect.Height), color));
            sprites.Add(LineRenderer.Rect(rect.Center, new Vector2(rect.Width, rect.Height - r * 2f), color));
            var diameter = new Vector2(r * 2f);
            sprites.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(rect.X + r, rect.Y + r), diameter, color));
            sprites.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(rect.Right - r, rect.Y + r), diameter, color));
            sprites.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(rect.X + r, rect.Bottom - r), diameter, color));
            sprites.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(rect.Right - r, rect.Bottom - r), diameter, color));
        }

        public static bool DrawCard(List<MySprite> sprites, LcdTheme theme, RectangleF rect, Color background, Color accent, Color foreground, float scale)
        {
            if (IsBare(theme))
            {
                sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, Color.Lerp(background, foreground, BARE_CARD_MIX)));
                return true;
            }
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    DrawChamferedBox(sprites, rect, FrameFill(theme, background, foreground), WithAlpha(accent, OUTLINE_ALPHA), accent, scale);
                    return true;
                case LcdTheme.Cafe:
                    float radius = CAFE_CORNER_RADIUS * scale;
                    float offset = Math.Max(1f, scale);
                    DrawRoundedRect(sprites, new RectangleF(rect.X + offset, rect.Y + offset, rect.Width, rect.Height), radius, WithAlpha(CAFE_INK, CAFE_SHADOW_ALPHA));
                    DrawRoundedRect(sprites, rect, radius, Color.Lerp(CAFE_PAPER, Color.White, CAFE_CARD_MIX));
                    return true;
                case LcdTheme.Clean:
                case LcdTheme.Small:
                    return true;
                default:
                    return false;
            }
        }

        public static bool DrawFooterBand(List<MySprite> sprites, LcdTheme theme, RectangleF rect, Color background, Color accent, Color foreground, float scale)
        {
            if (IsBare(theme))
                return true;
            switch (theme)
            {
                case LcdTheme.Futuristic:
                    sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, WithAlpha(accent, FUTURISTIC_FOOTER_ALPHA)));
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
            if (IsBare(theme))
                return true;
            if (theme == LcdTheme.Default)
                return false;

            LineRenderer.DrawLine(sprites, from, to, Math.Max(1f, scale), DividerColor(theme, foreground));
            return true;
        }

        /// <summary>
        ///     Barra padrão de todos os temas: trilho em pílula a 15 % do primeiro plano, preenchimento em <paramref name="fill"/>,
        ///     sem borda; abaixo de 2 px só retângulos. <paramref name="showPercent"/> escreve o percentual em Monospace à direita.
        /// </summary>
        public static bool DrawBar(List<MySprite> sprites, LcdTheme theme, RectangleF rect, float fraction, Color fill, Color foreground, float scale, bool showPercent = false)
        {
            if (rect.Width <= 0f || rect.Height <= 0f)
                return true;

            fraction = MathHelper.Clamp(fraction, 0f, 1f);
            showPercent = showPercent && rect.Height >= PERCENT_MIN_HEIGHT * scale;
            float textScale = showPercent ? rect.Height / PERCENT_TEXT_HEIGHT : 0f;
            float textWidth = showPercent ? PERCENT_TEXT_WIDTH * textScale + Math.Max(1f, scale) * 2f : 0f;
            var bar = new RectangleF(rect.X, rect.Y, Math.Max(1f, rect.Width - textWidth), rect.Height);
            Color track = WithAlpha(foreground, CLEAN_LINE_ALPHA);
            if (bar.Height < 2f)
            {
                sprites.Add(LineRenderer.Rect(bar.Center, bar.Size, track));
                float width = bar.Width * fraction;
                if (width > 0f)
                    sprites.Add(LineRenderer.Rect(new Vector2(bar.X + width * 0.5f, bar.Center.Y), new Vector2(width, bar.Height), fill));
            }
            else
                BarPanel.CreateSprites(sprites, bar.Position, bar.Size, fill, track, fraction, cornerRadius: bar.Height * 0.5f);

            if (showPercent)
                sprites.Add(new MySprite(SpriteType.TEXT, (int)Math.Round(fraction * 100f) + "%",
                    new Vector2(rect.Right, rect.Center.Y - PERCENT_TEXT_HEIGHT * textScale * 0.5f), null, foreground, "Monospace", TextAlignment.RIGHT, textScale));
            return true;
        }

        public static bool DrawIconCell(List<MySprite> sprites, LcdTheme theme, RectangleF cell, Color accent, float scale)
        {
            if (IsBare(theme))
                return true;
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
