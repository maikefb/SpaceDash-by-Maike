using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Panels
{
    public static class LineRenderer
    {
        /// <summary>Segmento como SquareSimple rotacionado; <paramref name="extend"/> alonga cada ponta para fechar as junções.</summary>
        public static void DrawLine(List<MySprite> sprites, Vector2 point1, Vector2 point2, float width, Color color, float extend = 0f)
        {
            Vector2 diff = point2 - point1;
            float length = diff.Length();
            if (length <= 0f && extend <= 0f)
                return;

            float angle = length > 0f ? (float)Math.Atan2(diff.Y, diff.X) : 0f;
            sprites.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", 0.5f * (point1 + point2),
                new Vector2(length + extend * 2f, width), color, null, TextAlignment.CENTER, angle));
        }

        public static void DrawRectOutline(List<MySprite> sprites, RectangleF rect, float thickness, Color color)
        {
            if (thickness <= 0f || color.A == 0 || rect.Width <= 0f || rect.Height <= 0f)
                return;

            float half = thickness * 0.5f;
            sprites.Add(Rect(new Vector2(rect.Center.X, rect.Y + half), new Vector2(rect.Width, thickness), color));
            sprites.Add(Rect(new Vector2(rect.Center.X, rect.Bottom - half), new Vector2(rect.Width, thickness), color));
            sprites.Add(Rect(new Vector2(rect.X + half, rect.Center.Y), new Vector2(thickness, rect.Height), color));
            sprites.Add(Rect(new Vector2(rect.Right - half, rect.Center.Y), new Vector2(thickness, rect.Height), color));
        }

        /// <summary>Oito traços em L, um par por canto, por dentro da borda do retângulo.</summary>
        public static void DrawCornerBrackets(List<MySprite> sprites, RectangleF rect, float arm, float thickness, Color color)
        {
            if (arm <= 0f || thickness <= 0f || color.A == 0)
                return;

            arm = Math.Min(arm, Math.Min(rect.Width, rect.Height) * 0.5f);
            DrawBracket(sprites, new Vector2(rect.X, rect.Y), 1f, 1f, arm, thickness, color);
            DrawBracket(sprites, new Vector2(rect.Right, rect.Y), -1f, 1f, arm, thickness, color);
            DrawBracket(sprites, new Vector2(rect.X, rect.Bottom), 1f, -1f, arm, thickness, color);
            DrawBracket(sprites, new Vector2(rect.Right, rect.Bottom), -1f, -1f, arm, thickness, color);
        }

        static void DrawBracket(List<MySprite> sprites, Vector2 corner, float directionX, float directionY, float arm, float thickness, Color color)
        {
            float half = thickness * 0.5f;
            sprites.Add(Rect(new Vector2(corner.X + directionX * arm * 0.5f, corner.Y + directionY * half), new Vector2(arm, thickness), color));
            sprites.Add(Rect(new Vector2(corner.X + directionX * half, corner.Y + directionY * arm * 0.5f), new Vector2(thickness, arm), color));
        }

        public static MySprite Rect(Vector2 center, Vector2 size, Color color)
        {
            return new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, color);
        }
    }
}
