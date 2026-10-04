using System;
using System.Collections.Generic;
using LcdMod.Client.Helpers;
using LcdMod.Client.SurfaceScripts.Abstract;
using VRage.Collections;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Gui.UserControls
{
    class BSoD
    {
        const string FONT_ID = "Debug";

        readonly Sandbox.ModAPI.Ingame.IMyTextSurface _surface;
        readonly List<MySprite> _frame = new List<MySprite>();
        public ListReader<MySprite> Frame { get; }

        BSoD(SurfaceScriptBase app, Exception exception)
        {
            var viewBox = app.ViewBox;
            _surface = app.Surface;
            var viewClip = new Rectangle(
                (int)viewBox.X,
                (int)viewBox.Y,
                Math.Max(1, (int)viewBox.Width),
                Math.Max(1, (int)viewBox.Height));

            var white = new Color(179, 237, 255);
            var blue = new Color(0, 88, 151);

            _frame.Add(MySprite.CreateClipRect(viewClip));
            _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", viewBox.Center, new Vector2(viewBox.Width, viewBox.Height), blue));

            string headerText = GetLocalizedText(MOD_PREFIX + "BSoD_Header", BSOD_TITLE_FALLBACK);
            var infoText = GetLocalizedText(MOD_PREFIX + "BSoD_InfoIntro", BSOD_INFO1_FALLBACK) + " " +
                           GetLocalizedText(MOD_PREFIX + "BSoD_InfoUrl", BSOD_INFO2_FALLBACK) + GITHUB;
            var exceptionText =
                GetLocalizedText(MOD_PREFIX + "BSoD_SupportIntro", BSOD_INFO4_FALLBACK) + "\n" +
                GetLocalizedText(MOD_PREFIX + "BSoD_ExceptionCode", BSOD_INFO5_FALLBACK) + "\n" + exception;

            float layoutScale = Math.Max(0.45f, Math.Min(viewBox.Width, viewBox.Height) / 512f);
            float titleGap = RoundToPixel(8f * layoutScale);
            float sectionGap = RoundToPixel(10f * layoutScale);
            float sadFace = 3f * layoutScale;
            float headerScale = 0.8f * layoutScale;
            float infoScale = 0.55f * layoutScale;
            float exceptionScale = 0.45f * layoutScale;

            float viewLeft = viewBox.X;
            float viewBottom = viewBox.Y + viewBox.Height;
            float availableWidth = Math.Max(8f, viewBox.Width);
            float cursorY = viewBox.Y;

            _frame.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = ":(",
                Color = white,
                FontId = FONT_ID,
                Alignment = TextAlignment.LEFT,
                Position = RoundToPixel(new Vector2(viewLeft, cursorY)),
                RotationOrScale = sadFace
            });

            cursorY += Math.Max(FormatingHelper.GetSizeInPixel(":(", FONT_ID, sadFace, _surface).Y,
                TextWrappingHelper.GetLineHeight(_surface, FONT_ID, sadFace, 0f)) + titleGap;

            cursorY += DrawWrappedText(headerText, viewLeft, cursorY, availableWidth, Math.Max(8f, viewBottom - cursorY), headerScale, white, false) + sectionGap;
            cursorY += DrawWrappedText(infoText, viewLeft, cursorY, availableWidth, Math.Max(8f, viewBottom - cursorY), infoScale, white, false) + sectionGap;
            DrawWrappedText(exceptionText, viewLeft, cursorY, availableWidth, Math.Max(8f, viewBottom - cursorY), exceptionScale, white, true);

            _frame.Add(MySprite.CreateClearClipRect());
            Frame = _frame;
        }

        static string GetLocalizedText(string loc, string fallback)
        {
            try
            {
                string localized = LocHelper.GetLoc(loc);
                if (localized != null && !string.Equals(localized, loc, StringComparison.Ordinal))
                    return localized;
            }
            catch
            {
                // Intentionally ignored: BSoD must be able to render even if localization breaks.
            }

            return fallback;
        }

        static Vector2 RoundToPixel(Vector2 value)
        {
            return new Vector2(RoundToPixel(value.X), RoundToPixel(value.Y));
        }

        static float RoundToPixel(float value)
        {
            return (float)Math.Round(value);
        }

        float DrawWrappedText(string text, float x, float y, float width, float height, float scale, Color color, bool trimEnd)
        {
            var lines = TextWrappingHelper.WrapText(text, _surface, FONT_ID, scale, width, height, 2f, trimEnd);
            if (lines == null || lines.Count == 0)
                return 0f;

            float lineHeight = TextWrappingHelper.GetLineHeight(_surface, FONT_ID, scale, 2f);
            for (int i = 0; i < lines.Count; i++)
            {
                _frame.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = lines[i],
                    Position = RoundToPixel(new Vector2(x, y + i * lineHeight)),
                    Color = color,
                    FontId = FONT_ID,
                    Alignment = TextAlignment.LEFT,
                    RotationOrScale = scale
                });
            }

            return lineHeight * lines.Count;
        }

        public static BSoD ShowBSoD(SurfaceScriptBase surfaceScriptBase, Exception exception) =>
            new BSoD(surfaceScriptBase, exception);
    }
}
