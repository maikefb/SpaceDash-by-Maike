using System;
using System.Collections.Generic;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Helpers;
using LcdMod.Common.Helpers;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace LcdMod.Client.Gui.ControlsTemplates.Progress
{
    /// <summary>Gráfico de linha sobre um anel de amostras cheio: moldura, rótulos do eixo Y e série.</summary>
    public static class LineChartPanel
    {
        const float MIN_AXIS_MAX = 0.001f;
        static readonly double[] NiceSteps = { 1d, 1.2d, 1.5d, 2d, 2.5d, 3d, 4d, 5d, 6d, 8d };

        /// <summary>Passos aceitos entre linhas do eixo: os do teto mais 1,25 e 7,5, para tetos como 2,5 e 15 terem divisões.</summary>
        static readonly double[] NiceTicks = { 1d, 1.2d, 1.25d, 1.5d, 2d, 2.5d, 3d, 4d, 5d, 6d, 7.5d, 8d, 10d };

        /// <summary>Teto redondo do eixo: o primeiro de {1, 1.2, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10} vezes 10^n que cobre o valor.</summary>
        public static float NiceMax(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < MIN_AXIS_MAX)
                return MIN_AXIS_MAX;

            double exponent = Math.Floor(Math.Log10(value));
            double magnitude = Math.Pow(10d, exponent);
            double mantissa = value / magnitude;
            for (int i = 0; i < NiceSteps.Length; i++)
            {
                if (mantissa <= NiceSteps[i] * (1d + 1e-6))
                    return (float)(NiceSteps[i] * magnitude);
            }

            return (float)(10d * magnitude);
        }

        /// <summary>Maior número de divisões, até o máximo pedido, cujo passo também é redondo (evita "333.33 kW").</summary>
        public static int NiceDivisions(float axisMax, int maxDivisions)
        {
            for (int divisions = maxDivisions; divisions > 1; divisions--)
            {
                if (IsNiceStep(axisMax / (double)divisions))
                    return divisions;
            }

            return 1;
        }

        static bool IsNiceStep(double step)
        {
            if (step <= 0d)
                return false;

            double magnitude = Math.Pow(10d, Math.Floor(Math.Log10(step)));
            double mantissa = step / magnitude;
            for (int i = 0; i < NiceTicks.Length; i++)
            {
                if (Math.Abs(mantissa - NiceTicks[i]) < 1e-5)
                    return true;
            }

            return false;
        }

        public static bool SelfCheck()
        {
            float[] inputs = { 0.37f, 1f, 1.01f, 7.3f, 12.5f, 9.5f, 0.4f, 0.6f, 1.2f };
            float[] expected = { 0.4f, 1f, 1.2f, 8f, 15f, 10f, 0.4f, 0.6f, 1.2f };
            for (int i = 0; i < inputs.Length; i++)
            {
                float actual = NiceMax(inputs[i]);
                if (Math.Abs(actual - expected[i]) <= expected[i] * 1e-4f)
                    continue;

                LogHelper.Log(MyLogSeverity.Error, "LineChartPanel.NiceMax({0}) = {1}, expected {2}", inputs[i], actual, expected[i]);
                return false;
            }

            float[] axes = { 1f, 1.5f, 8f, 1.2f, 2.5f, 2.5f };
            int[] limits = { 5, 5, 5, 5, 5, 4 };
            int[] divisions = { 5, 5, 4, 4, 5, 2 };
            for (int i = 0; i < axes.Length; i++)
            {
                int actual = NiceDivisions(axes[i], limits[i]);
                if (actual == divisions[i])
                    continue;

                LogHelper.Log(MyLogSeverity.Error, "LineChartPanel.NiceDivisions({0}) = {1}, expected {2}", axes[i], actual, divisions[i]);
                return false;
            }

            return true;
        }

        public static void DrawFrame(List<MySprite> sprites, RectangleF plot, Color background, Color gridColor, int divisions, float thickness)
        {
            if (plot.Width <= 0f || plot.Height <= 0f)
                return;

            if (background.A > 0)
                sprites.Add(LineRenderer.Rect(plot.Center, plot.Size, background));

            divisions = Math.Max(1, divisions);
            for (int i = 0; i <= divisions; i++)
            {
                float y = plot.Bottom - plot.Height * i / divisions;
                sprites.Add(LineRenderer.Rect(new Vector2(plot.Center.X, y), new Vector2(plot.Width, thickness), gridColor));
            }
        }

        public static float MeasureYLabelGutter(IMyTextSurface surface, string font, float scale, float max, int divisions, Func<float, string> format)
        {
            float width = 0f;
            divisions = Math.Max(1, divisions);
            for (int i = 0; i <= divisions; i++)
                width = Math.Max(width, FormatingHelper.GetSizeInPixel(format(max * i / divisions), font, scale, surface).X);
            return width;
        }

        public static void DrawYLabels(List<MySprite> sprites, IMyTextSurface surface, RectangleF plot, float labelRight, float max,
            int divisions, string font, float scale, Color color, Func<float, string> format)
        {
            divisions = Math.Max(1, divisions);
            float lineHeight = FormatingHelper.LineHeight(scale, surface, font);
            for (int i = 0; i <= divisions; i++)
            {
                float y = plot.Bottom - plot.Height * i / divisions;
                sprites.Add(new MySprite(SpriteType.TEXT, format(max * i / divisions), new Vector2(labelRight, y - lineHeight * 0.5f), null, color, font,
                    TextAlignment.RIGHT, scale));
            }
        }

        /// <summary>Da amostra mais antiga (esquerda) à mais recente (direita, em <paramref name="head"/>), com marcador no último ponto.</summary>
        public static void DrawSeries(List<MySprite> sprites, RectangleF plot, float[] ring, int head, float max,
            float thickness, Color color, Color markerColor)
        {
            if (ring == null || ring.Length == 0 || max <= 0f || plot.Width <= 0f)
                return;

            int length = ring.Length;
            float stepX = length > 1 ? plot.Width / (length - 1) : 0f;
            Vector2 previous = Vector2.Zero;
            for (int i = 0; i < length; i++)
            {
                int age = length - 1 - i;
                int index = ((head - age) % length + length) % length;
                float ratio = MathHelper.Clamp(ring[index] / max, 0f, 1f);
                var point = new Vector2(plot.X + stepX * i, plot.Bottom - plot.Height * ratio);
                if (i > 0)
                    LineRenderer.DrawLine(sprites, previous, point, thickness, color, thickness * 0.5f);
                previous = point;
            }

            sprites.Add(new MySprite(SpriteType.TEXTURE, "Circle", previous, new Vector2(thickness * 2.5f), markerColor));
        }
    }
}
