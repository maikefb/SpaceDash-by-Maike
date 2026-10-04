using System;
using LcdMod.Client.Modules.Energy;
using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Custom.Energy
{
    public enum EnergyLayoutMode
    {
        Square,
        Wide,
        Tiny
    }

    /// <summary>Papéis de cor das telas de energia, resolvidos do tema da tela a cada quadro e legíveis sobre o fundo real.</summary>
    public struct EnergyPalette
    {
        /// <summary>Fundo sobre o qual os textos são desenhados: o painel opaco ou o fundo da tela.</summary>
        public Color Backdrop;

        public Color Panel;
        public Color Frame;
        public Color Bracket;
        public Color Label;
        public Color LabelMuted;
        public Color ValueNeutral;
        public Color ValueOk;
        public Color ValueAttention;
        public Color ValueAlert;
        public Color Divider;
        public Color BoxFrame;
        public Color GaugeTrack;
        public Color PlotBackground;
        public Color ChartUsed;
    }

    /// <summary>Medidas derivadas da escala configurada, do tamanho da tela e do tamanho da fonte.</summary>
    public struct EnergyMetrics
    {
        public const float MIN_TEXT = 0.4f;

        /// <summary>Fator do tamanho da tela, sem o zoom do usuário.</summary>
        public float ScreenFactor;

        public float S;
        public float F;
        public float U;
        public float FrameLine;
        public float BracketThickness;
        public float BracketArm;
        public float TitleBarHeight;
        public float DividerThickness;
        public float Padding;

        public static EnergyMetrics From(RectangleF view, float configuredScale, float fontSize)
        {
            float screenFactor = Math.Max(0.55f, Math.Min(view.Width, view.Height) / 512f);
            float s = MathHelper.Clamp(configuredScale * screenFactor, 0.45f, 2.5f);
            float f = fontSize > 0f ? fontSize : 1f;
            float u = 8f * s;
            return new EnergyMetrics
            {
                ScreenFactor = screenFactor,
                S = s,
                F = f,
                U = u,
                FrameLine = Math.Max(1f, 2f * s),
                BracketThickness = Math.Max(1.5f, 3f * s),
                BracketArm = 14f * s,
                TitleBarHeight = 4f * u * Math.Max(0.75f, f),
                DividerThickness = Math.Max(1f, s),
                Padding = u * 0.75f
            };
        }

        public float Text(float type)
        {
            return Math.Max(MIN_TEXT, type * S * F);
        }

        /// <summary>Escala de referência que não cresce com o zoom do usuário: decide o modo de layout, não o tamanho do texto.</summary>
        public float ReferenceText(float type)
        {
            return Math.Max(MIN_TEXT, type * Math.Min(1f, ScreenFactor) * Math.Min(1f, F));
        }
    }

    public static class EnergyTheme
    {
        public const float TITLE = 0.9f;
        public const float LABEL = 0.55f;
        public const float VALUE = 0.62f;
        public const float BIG_VALUE = 1.1f;
        public const float AXIS = 0.45f;

        /// <summary>Frações de layout compartilhadas pelas telas.</summary>
        public const float CHART_LEFT_SQUARE = 0.6f;
        public const float CHART_LEFT_WIDE = 0.68f;
        public const float CHART_DONUT_CELL = 0.3f;
        public const float CARD_ILLUSTRATION_WIDE = 0.34f;
        public const float CARD_ILLUSTRATION_SQUARE = 0.42f;
        public const float RING_THICKNESS = 0.22f;
        public const int DONUT_STEPS = 48;

        /// <summary>Linhas de texto que o menor lado precisa ter para Square/Wide: 16 na escala de referência e 9 na escala real (zoom).</summary>
        public const float MIN_SQUARE_LINES = 16f;
        public const float MIN_ZOOM_LINES = 9f;
        public const float MIN_ROW_LINES = 1.7f;
        const float USED_ATTENTION = 0.8f;
        const float USED_ALERT = 0.95f;
        const float CRITICAL_CHARGE_RATIO = 0.10f;
        const float TINY_HEIGHT_TO_WIDTH = 0.2f;
        const float WIDE_WIDTH_TO_HEIGHT = 1.3f;

        public static EnergyLayoutMode LayoutMode(RectangleF content, float referenceLineHeight, float labelLineHeight)
        {
            float side = Math.Min(content.Width, content.Height);
            if (content.Height / Math.Max(1f, content.Width) < TINY_HEIGHT_TO_WIDTH ||
                side < MIN_SQUARE_LINES * referenceLineHeight || side < MIN_ZOOM_LINES * labelLineHeight)
                return EnergyLayoutMode.Tiny;

            return content.Width / Math.Max(1f, content.Height) >= WIDE_WIDTH_TO_HEIGHT
                ? EnergyLayoutMode.Wide
                : EnergyLayoutMode.Square;
        }

        public static Color StateColor(ref EnergyPalette palette, EnergyUnitState state)
        {
            switch (state)
            {
                case EnergyUnitState.Online:
                    return palette.ValueOk;
                case EnergyUnitState.Warning:
                    return palette.ValueAttention;
                case EnergyUnitState.Offline:
                case EnergyUnitState.Inoperative:
                    return palette.ValueAlert;
                default:
                    return palette.LabelMuted;
            }
        }

        public static Color ChargeColor(ref EnergyPalette palette, float ratio)
        {
            if (ratio < CRITICAL_CHARGE_RATIO)
                return palette.ValueAlert;
            if (ratio < EnergySnapshot.LOW_CHARGE_RATIO)
                return palette.ValueAttention;
            return ratio >= EnergySnapshot.FULL_RATIO ? palette.ValueOk : palette.ValueNeutral;
        }

        /// <summary>Mesmo limiar e mesma severidade do estado "Atenção" dos reatores e motores.</summary>
        public static Color LoadColor(ref EnergyPalette palette, float ratio)
        {
            return ratio >= EnergySnapshot.ALERT_LOAD_RATIO ? palette.ValueAttention : palette.ValueNeutral;
        }

        /// <summary>Uso da capacidade disponível: atenção acima de 80 %, alerta perto do limite.</summary>
        public static Color UsedColor(ref EnergyPalette palette, float ratio)
        {
            if (ratio >= USED_ALERT)
                return palette.ValueAlert;
            return ratio > USED_ATTENTION ? palette.ValueAttention : palette.ValueNeutral;
        }

        public static Color BalanceColor(ref EnergyPalette palette, float netMw, bool emptySoon)
        {
            if (netMw > EnergySnapshot.EPSILON_MW)
                return palette.ValueOk;
            if (netMw < -EnergySnapshot.EPSILON_MW)
                return emptySoon ? palette.ValueAlert : palette.ValueAttention;
            return palette.ValueNeutral;
        }
    }
}
