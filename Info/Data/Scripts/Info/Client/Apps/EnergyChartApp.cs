using System;
using System.Collections.Generic;
using System.Linq;
using LcdMod.Client.Animation;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Custom.Energy;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Helpers;
using LcdMod.Client.Modules.Energy;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Apps
{
    /// <summary>
    ///     Port do "Power Chart" (PowerGraph): produzida e usada no tempo com eixo compartilhado, uso da capacidade e
    ///     energia armazenada em anéis e o painel de baterias com quantidade, status e previsão.
    /// </summary>
    internal sealed class EnergyChartApp : EnergyAppBase
    {
        const int EASE_FRAMES = 12;
        const int MAX_Y_DIVISIONS = 5;
        const float MIN_PLOT_UNITS = 6f;
        const string USED_CHANNEL = "EnergyUsed";
        const string STORED_CHANNEL = "EnergyStored";

        /// <summary>Rótulos do eixo numa só unidade, escolhida pelo teto do eixo.</summary>
        static readonly Func<float, string> TickMegaWatts = mw => mw.ToString("0.##", FormatingHelper.Culture) + " MW";
        static readonly Func<float, string> TickKiloWatts = mw => (mw * 1000f).ToString("0.##", FormatingHelper.Culture) + " kW";
        static readonly Func<float, string> TickGigaWatts = mw => (mw / 1000f).ToString("0.##", FormatingHelper.Culture) + " GW";

        readonly List<MySprite> _sprites = new List<MySprite>(420);
        readonly List<EnergyRow> _rows = new List<EnergyRow>(3);
        float _displayUsed;
        float _displayStored;
        EnergySnapshot _animatedSnapshot;
        long _animatedVersion = -1L;

        public EnergyChartApp(IAppHost host) : base(host)
        {
        }

        public override void Update()
        {
            base.Update();
            var snapshot = Snapshot;
            if (!snapshot.HasData || ReferenceEquals(snapshot, _animatedSnapshot) && snapshot.Version == _animatedVersion)
                return;

            _animatedSnapshot = snapshot;
            _animatedVersion = snapshot.Version;
            PrepareFrame();
            Animate(USED_CHANNEL, () => _displayUsed, value => _displayUsed = value, snapshot.UsedRatio);
            Animate(STORED_CHANNEL, () => _displayStored, value => _displayStored = value, snapshot.StoredRatio);
        }

        /// <summary>Só anima quando os anéis aparecem e a diferença muda ao menos meio passo do anel; senão o valor salta direto.</summary>
        void Animate(string channel, Func<float> getter, Action<float> setter, float target)
        {
            if (Mode == EnergyLayoutMode.Tiny || Math.Abs(getter() - target) < 0.5f / EnergyTheme.DONUT_STEPS)
            {
                setter(target);
                return;
            }

            this.RunAnimation(channel, AnimationConflict.Replace,
                new Keyframe(getter, setter, target, EASE_FRAMES, EasingMode.EaseOutCubic));
        }

        public override List<MySprite> GetSprites()
        {
            PrepareFrame();
            _sprites.Clear();
            var snapshot = Snapshot;
            if (Mode == EnergyLayoutMode.Tiny)
                DrawTiny(snapshot);
            else
                DrawFull(snapshot);
            return _sprites;
        }

        void DrawFull(EnergySnapshot s)
        {
            float gap = Metrics.U;
            float leftWidth = Body.Width * (Mode == EnergyLayoutMode.Wide ? EnergyTheme.CHART_LEFT_WIDE : EnergyTheme.CHART_LEFT_SQUARE) - gap * 0.5f;
            var left = new RectangleF(Body.X, Body.Y, leftWidth, Body.Height);
            var right = new RectangleF(left.Right + gap, Body.Y, Body.Width - leftWidth - gap, Body.Height);

            float axisMax = AxisMax(s);
            float blockHeight = (left.Height - gap) * 0.5f;
            DrawChartBlock(new RectangleF(left.X, left.Y, left.Width, blockHeight), Loc("Produced"), s.ProducedMw,
                s.ProducedHistory, s.HistoryHead, axisMax, Palette.ValueNeutral);
            DrawChartBlock(new RectangleF(left.X, left.Y + blockHeight + gap, left.Width, blockHeight), Loc("Used"), s.ConsumedMw,
                s.UsedHistory, s.HistoryHead, axisMax, Palette.ChartUsed);

            float donutHeight = right.Height * EnergyTheme.CHART_DONUT_CELL;
            DrawDonutCell(new RectangleF(right.X, right.Y, right.Width, donutHeight), Loc("Used"), _displayUsed,
                FormatingHelper.PercentageToString(s.UsedRatio), EnergyTheme.UsedColor(ref Palette, s.UsedRatio));
            DrawDonutCell(new RectangleF(right.X, right.Y + donutHeight, right.Width, donutHeight), Loc("Stored"), _displayStored,
                s.HasBatteries ? FormatingHelper.PercentageToString(s.StoredRatio) : NotAvailable,
                s.HasBatteries ? EnergyTheme.ChargeColor(ref Palette, s.StoredRatio) : Palette.LabelMuted);
            DrawBatteryPanel(new RectangleF(right.X, right.Y + donutHeight * 2f, right.Width, right.Height - donutHeight * 2f), s);
        }

        /// <summary>
        ///     Plot ao lado ou acima da tabela, no arranjo que dá ao plot o maior menor lado. A tabela fica com ao menos o que as
        ///     duas linhas pedem na escala mínima naquele arranjo, e o plot sai quando sobra menos de
        ///     <see cref="MIN_PLOT_UNITS"/> unidades.
        /// </summary>
        void DrawTiny(EnergySnapshot s)
        {
            _rows.Clear();
            _rows.Add(PowerRow(Loc("Produced"), s.ProducedMw, Palette.ValueNeutral, float.MaxValue));
            _rows.Add(PowerRow(Loc("Used"), s.ConsumedMw, Palette.ChartUsed, float.MaxValue));

            float gap = Metrics.U;
            float inset = Metrics.U * 0.3f;
            float share = 1f - EnergyTheme.CHART_LEFT_SQUARE;
            Vector2 stacked;
            Vector2 sideBySide;
            EnergyCardRenderer.MinTableSizes(Host.Surface, TextFont, _rows, ref Metrics, out stacked, out sideBySide);
            float tableWidth = Math.Max(Body.Width * share - gap * 0.5f, Body.Height >= stacked.Y ? stacked.X : sideBySide.X);
            var besidePlot = new RectangleF(Body.X, Body.Y + inset, Body.Width - tableWidth - gap, Body.Height - inset * 2f);
            float tableHeight = Math.Max(Body.Height * share - gap * 0.5f, Body.Width >= sideBySide.X ? sideBySide.Y : stacked.Y);
            var abovePlot = new RectangleF(Body.X + inset, Body.Y, Body.Width - inset * 2f, Body.Height - tableHeight - gap);

            bool above = MinSide(abovePlot) >= MinSide(besidePlot);
            var plot = above ? abovePlot : besidePlot;
            var table = above
                ? new RectangleF(Body.X, Body.Bottom - tableHeight, Body.Width, tableHeight)
                : new RectangleF(Body.Right - tableWidth, Body.Y, tableWidth, Body.Height);
            if (MinSide(plot) >= Metrics.U * MIN_PLOT_UNITS)
                DrawPlotWithoutAxis(plot, s.ProducedHistory, s.HistoryHead, AxisMax(s), Palette.ValueNeutral);
            else
                table = Body;
            EnergyCardRenderer.DrawValueTable(_sprites, Host.Surface, TextFont, table, _rows, ref Palette, ref Metrics);
        }

        static float MinSide(RectangleF rect)
        {
            return Math.Min(rect.Width, rect.Height);
        }

        float LineThickness => Math.Max(2f, 2.5f * Metrics.S);

        /// <summary>Mesma escala nos dois gráficos: teto redondo acima do maior valor de ambas as séries.</summary>
        static float AxisMax(EnergySnapshot s)
        {
            return LineChartPanel.NiceMax(Math.Max(s.ProducedHistory.Max(), s.UsedHistory.Max()));
        }

        void DrawPlotWithoutAxis(RectangleF plot, float[] history, int head, float axisMax, Color seriesColor)
        {
            if (plot.Height <= 2f || plot.Width <= 2f)
                return;

            LineChartPanel.DrawFrame(_sprites, plot, Palette.PlotBackground, Palette.Divider, 2, Metrics.DividerThickness);
            LineChartPanel.DrawSeries(_sprites, plot, history, head, axisMax, LineThickness, seriesColor, Palette.Bracket);
        }

        /// <summary>
        ///     Faixa rótulo | valor atual e, abaixo, o plot com tantas divisões redondas quantas os rótulos do eixo permitirem;
        ///     sem altura para um rótulo de eixo, o plot fica sem eixo em vez de sumir.
        /// </summary>
        void DrawChartBlock(RectangleF rect, string label, float current, float[] history, int head, float axisMax, Color seriesColor)
        {
            var surface = Host.Surface;
            string font = TextFont;
            float valueScale = Metrics.Text(EnergyTheme.VALUE);
            float stripHeight = FormatingHelper.LineHeight(valueScale, surface, font) + Metrics.U * 0.4f;
            float stripCenter = rect.Y + stripHeight * 0.5f;
            float valueWidth;
            float gap;
            float labelScale;
            StripLayout(rect.Width, ref valueScale, out valueWidth, out gap, out labelScale);
            string valueText = PowerText(current, false, valueWidth);
            EnergyCardRenderer.DrawText(_sprites, surface, font, label.ToUpperInvariant(), rect.X, stripCenter, rect.Width - valueWidth - gap, stripHeight,
                labelScale, Palette.LabelMuted, TextAlignment.LEFT);
            EnergyCardRenderer.DrawText(_sprites, surface, FormatingHelper.BoldFont(font, valueText), valueText, rect.Right, stripCenter, valueWidth, stripHeight,
                valueScale, seriesColor, TextAlignment.RIGHT);

            float axisScale = Metrics.Text(EnergyTheme.AXIS);
            float axisLine = FormatingHelper.LineHeight(axisScale, surface, font);
            float plotTop = rect.Y + stripHeight + Math.Max(Metrics.U * 0.5f, axisLine * 0.5f + 2f);
            float plotBottom = rect.Bottom - axisLine * 0.5f;
            if (plotBottom - plotTop < axisLine)
            {
                float top = rect.Y + stripHeight + Metrics.U * 0.3f;
                DrawPlotWithoutAxis(new RectangleF(rect.X, top, rect.Width, rect.Bottom - top - Metrics.U * 0.3f), history, head, axisMax, seriesColor);
                return;
            }

            var ticks = axisMax >= 1000f ? TickGigaWatts : axisMax >= 1f ? TickMegaWatts : TickKiloWatts;
            int maxDivisions = MathHelper.Clamp((int)Math.Floor((plotBottom - plotTop) / (axisLine * 1.6f)), 1, MAX_Y_DIVISIONS);
            int divisions = LineChartPanel.NiceDivisions(axisMax, maxDivisions);
            float gutter = LineChartPanel.MeasureYLabelGutter(surface, font, axisScale, axisMax, divisions, ticks) + Metrics.U * 0.5f;
            var plot = new RectangleF(rect.X + gutter, plotTop, Math.Max(1f, rect.Width - gutter), plotBottom - plotTop);
            LineChartPanel.DrawFrame(_sprites, plot, Palette.PlotBackground, Palette.Divider, divisions, Metrics.DividerThickness);
            LineChartPanel.DrawYLabels(_sprites, surface, plot, plot.X - Metrics.U * 0.4f, axisMax, divisions, font, axisScale, Palette.LabelMuted, ticks);
            LineChartPanel.DrawSeries(_sprites, plot, history, head, axisMax, LineThickness, seriesColor, Palette.Bracket);
        }

        /// <summary>
        ///     Faixa rótulo | valor igual nos dois blocos e estável entre amostras: medida com os dois rótulos e com
        ///     <see cref="EnergyAppBase.WidestPower"/>, que só reserva largura enquanto cabe ao lado do rótulo; no aperto, o piso é o
        ///     maior valor atual, que perde casas para caber ao lado do rótulo. O espaçamento encolhe até U/4 e o valor até a escala
        ///     mínima antes de o rótulo virar reticências, e o rótulo nunca fica maior que o valor.
        /// </summary>
        void StripLayout(float width, ref float valueScale, out float valueWidth, out float gap, out float labelScale)
        {
            var surface = Host.Surface;
            string font = TextFont;
            string template = WidestPower;
            string templateFont = FormatingHelper.BoldFont(font, template);
            string produced = Loc("Produced").ToUpperInvariant();
            string used = Loc("Used").ToUpperInvariant();
            float producedMin = MinWidth(produced, false);
            float usedMin = MinWidth(used, false);
            float labelMin = Math.Max(producedMin, usedMin);
            var s = Snapshot;
            float valueRoom = width - Metrics.U * 0.25f - labelMin;
            float currentMin = Math.Max(MinWidth(PowerText(s.ProducedMw, false, valueRoom), true), MinWidth(PowerText(s.ConsumedMw, false, valueRoom), true));
            float valueMin = Math.Max(Math.Min(MinWidth(template, true), width - Metrics.U - labelMin), currentMin);
            gap = MathHelper.Clamp(width - labelMin - valueMin, Metrics.U * 0.25f, Metrics.U);
            valueWidth = Math.Min(width * 0.6f, FormatingHelper.GetSizeInPixel(template, templateFont, valueScale, surface).X);
            valueWidth = Math.Max(valueMin, Math.Min(valueWidth, width - gap - labelMin));
            FormatingHelper.FitText(surface, template, templateFont, valueScale, valueWidth, 0f, EnergyMetrics.MIN_TEXT, out valueScale);
            FormatingHelper.FitText(surface, producedMin >= usedMin ? produced : used, font, Math.Min(Metrics.Text(EnergyTheme.LABEL), valueScale),
                width - valueWidth - gap, 0f, EnergyMetrics.MIN_TEXT, out labelScale);
        }

        /// <summary>
        ///     Anel com o percentual no centro; sem largura para o percentual dentro do anel, só o rótulo e o percentual. O
        ///     tamanho do percentual vem do modelo "100%", para não mudar a cada amostra, e o rótulo nunca fica maior que ele.
        /// </summary>
        void DrawDonutCell(RectangleF cell, string label, float displayFraction, string percent, Color color)
        {
            var surface = Host.Surface;
            string font = TextFont;
            string valueFont = FormatingHelper.BoldFont(font, percent);
            float labelScale;
            string upper = FormatingHelper.FitText(surface, label.ToUpperInvariant(), font, Metrics.Text(EnergyTheme.LABEL), cell.Width, cell.Height * 0.25f,
                EnergyMetrics.MIN_TEXT, out labelScale);
            float labelHeight = FormatingHelper.GetSizeInPixel(upper, font, labelScale, surface).Y;

            float available = cell.Height - labelHeight - Metrics.U * 0.4f;
            float radius = Math.Min(cell.Width, available) * 0.5f * 0.92f;
            var center = new Vector2(cell.Center.X, cell.Y + labelHeight + Metrics.U * 0.4f + available * 0.5f);
            string template = FormatingHelper.PercentageToString(1f);
            string templateFont = FormatingHelper.BoldFont(font, template);
            float minTextWidth = Math.Max(MinWidth(percent, true), MinWidth(template, true));
            bool ring = radius * 1.3f >= minTextWidth && radius * 0.9f >= FormatingHelper.LineHeight(EnergyMetrics.MIN_TEXT, surface, font);
            float boxWidth = ring ? radius * 1.3f : cell.Width;
            float boxHeight = ring ? radius * 0.9f : Math.Max(1f, available);
            float valueScale;
            FormatingHelper.FitText(surface, template, templateFont, Metrics.Text(EnergyTheme.VALUE) * 1.1f, boxWidth, boxHeight, EnergyMetrics.MIN_TEXT,
                out valueScale);
            if (ring)
                DonutPanel.DrawDonut(_sprites, center, radius * (1f - EnergyTheme.RING_THICKNESS), radius, displayFraction, color, Palette.GaugeTrack,
                    EnergyTheme.DONUT_STEPS, 0f, 2f * Metrics.S);
            valueScale = EnergyCardRenderer.DrawText(_sprites, surface, valueFont, percent, center.X, center.Y, boxWidth, boxHeight, valueScale, color,
                TextAlignment.CENTER);
            _sprites.Add(EnergyCardRenderer.Text(upper, new Vector2(cell.Center.X, cell.Y), Math.Min(labelScale, valueScale), Palette.LabelMuted,
                TextAlignment.CENTER, font));
        }

        /// <summary>Tabela secundária: não cresce além do tamanho natural para não competir com as leituras do gráfico.</summary>
        void DrawBatteryPanel(RectangleF cell, EnergySnapshot s)
        {
            var surface = Host.Surface;
            string font = TextFont;
            float titleScale;
            string title = FormatingHelper.FitText(surface, KindName(EnergyKind.Battery).ToUpperInvariant(), font, Metrics.Text(EnergyTheme.LABEL), cell.Width,
                cell.Height * 0.25f, EnergyMetrics.MIN_TEXT, out titleScale);
            float titleHeight = FormatingHelper.GetSizeInPixel(title, font, titleScale, surface).Y;
            _sprites.Add(EnergyCardRenderer.Text(title, new Vector2(cell.X, cell.Y + Metrics.U * 0.3f), titleScale, Palette.LabelMuted, TextAlignment.LEFT, font));
            float tableTop = cell.Y + Metrics.U * 0.6f + titleHeight;
            _sprites.Add(LineRenderer.Rect(new Vector2(cell.Center.X, tableTop), new Vector2(cell.Width, Metrics.DividerThickness), Palette.Divider));

            var batteries = s.Kinds[(int)EnergyKind.Battery];
            _rows.Clear();
            _rows.Add(new EnergyRow(Loc("Units"), WorkingText(batteries.Working, batteries.Total), EnergyTheme.StateColor(ref Palette, batteries.State)));
            _rows.Add(new EnergyRow(MyTexts.GetString("TerminalStatus"), BatteryStatusText(s.BatteryStatus), BatteryStatusColor(s.BatteryStatus)));
            _rows.Add(EtaRow(s));
            var table = new RectangleF(cell.X, tableTop + Metrics.DividerThickness, cell.Width,
                Math.Max(1f, cell.Bottom - tableTop - Metrics.DividerThickness));
            EnergyCardRenderer.DrawValueTable(_sprites, surface, font, table, _rows, ref Palette, ref Metrics, 1f);
        }
    }
}
