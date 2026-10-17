using LcdMod.Common.Config.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using LcdMod.Client.Animation;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Custom.Energy;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Gui.Styling;
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
        const string ICON_POWER = "IconEnergy";
        const string ICON_BATTERY = "Battery";
        const float MINIMAL_HERO_FONT_MUL = 2.5f;
        const float MINIMAL_CURVE_SHARE = 1f / 3f;
        const string PEAK_KEY = "LcdMod_Frame_Peak1h";

        /// <summary>Rótulos do eixo numa só unidade, escolhida pelo teto do eixo.</summary>
        static readonly Func<float, string> TickMegaWatts = mw => mw.ToString("0.##", FormatingHelper.Culture) + " MW";
        static readonly Func<float, string> TickKiloWatts = mw => (mw * 1000f).ToString("0.##", FormatingHelper.Culture) + " kW";
        static readonly Func<float, string> TickGigaWatts = mw => (mw / 1000f).ToString("0.##", FormatingHelper.Culture) + " GW";

        readonly List<MySprite> _sprites = new List<MySprite>(420);
        readonly List<EnergyRow> _rows = new List<EnergyRow>(3);
        readonly List<BareLayout.KvRow> _kv = new List<BareLayout.KvRow>(6);
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
            if (IsMinimal)
            {
                DrawMinimal(_sprites);
                return _sprites;
            }
            if (IsCompact)
            {
                DrawCompact(_sprites);
                return _sprites;
            }
            var snapshot = Snapshot;
            if (Host.Frame != null)
                DrawFramed(snapshot);
            else if (Mode == EnergyLayoutMode.Tiny)
                DrawTiny(snapshot);
            else
                DrawFull(snapshot);
            return _sprites;
        }

        /// <summary>As duas séries no mesmo plot da moldura, que já traz o quadro e as linhas de grade.</summary>
        void DrawFramed(EnergySnapshot s)
        {
            var f = Host.Frame;
            Color neutral = Palette.ValueNeutral;
            FramedHeader(_sprites);
            RectangleF plot = f.Rect("grafico.series");
            float axisMax = AxisMax(s);
            LineChartPanel.DrawSeries(_sprites, plot, s.ProducedHistory, s.HistoryHead, axisMax, LineThickness, neutral, Palette.Bracket);
            LineChartPanel.DrawSeries(_sprites, plot, s.UsedHistory, s.HistoryHead, axisMax, LineThickness, Palette.ChartUsed, Palette.Bracket);
            f.Text(_sprites, "producao.valor", FramedPower("producao.valor", s.ProducedMw, false), neutral);
            f.Text(_sprites, "consumo.valor", FramedPower("consumo.valor", s.ConsumedMw, false), Palette.ChartUsed);
            f.Text(_sprites, "saldo.valor", FramedPower("saldo.valor", s.NetMw, true), EnergyTheme.BalanceColor(ref Palette, s.NetMw, s.BatteryEmptySoon));
            f.Text(_sprites, "pico_1_h.valor", FramedPower("pico_1_h.valor", s.ProducedHistory.Max(), false), neutral);
            f.Text(_sprites, "armazenado.valor", s.HasBatteries ? FormatingHelper.PercentageToString(s.StoredRatio) : NotAvailable,
                s.HasBatteries ? EnergyTheme.ChargeColor(ref Palette, s.StoredRatio) : Palette.LabelMuted);
            f.Text(_sprites, "autonomia.valor", s.HasBatteries ? EtaText(s.EtaSeconds) : NotAvailable, neutral);
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
            DrawDonutCell(new RectangleF(right.X, right.Y, right.Width, donutHeight), Loc("Used"), ICON_POWER, _displayUsed,
                FormatingHelper.PercentageToString(s.UsedRatio), EnergyTheme.UsedColor(ref Palette, s.UsedRatio));
            DrawDonutCell(new RectangleF(right.X, right.Y + donutHeight, right.Width, donutHeight), Loc("Stored"), ICON_BATTERY, _displayStored,
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
                DrawCurve(_sprites, plot, s.ProducedHistory, s.HistoryHead, AxisMax(s), Palette.ValueNeutral);
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

        void DrawCurve(List<MySprite> sprites, RectangleF plot, float[] history, int head, float axisMax, Color seriesColor)
        {
            if (plot.Height <= 2f || plot.Width <= 2f)
                return;

            LineChartPanel.DrawFrame(sprites, plot, Palette.PlotBackground, Palette.Divider, 2, Metrics.DividerThickness);
            LineChartPanel.DrawSeries(sprites, plot, history, head, axisMax, LineThickness, seriesColor, Palette.Bracket);
        }

        /// <summary>As duas séries no mesmo plot, sem eixo nem legenda.</summary>
        void DrawCurve(List<MySprite> sprites, RectangleF plot, EnergySnapshot s)
        {
            if (plot.Height <= 2f || plot.Width <= 2f)
                return;

            float axisMax = AxisMax(s);
            DrawCurve(sprites, plot, s.ProducedHistory, s.HistoryHead, axisMax, Palette.ValueNeutral);
            LineChartPanel.DrawSeries(sprites, plot, s.UsedHistory, s.HistoryHead, axisMax, LineThickness, Palette.ChartUsed, Palette.Bracket);
        }

        static float PeakMw(EnergySnapshot s)
        {
            float peak = 0f;
            for (int i = 0; i < s.ProducedHistory.Length; i++)
                if (s.ProducedHistory[i] > peak)
                    peak = s.ProducedHistory[i];
            for (int i = 0; i < s.UsedHistory.Length; i++)
                if (s.UsedHistory[i] > peak)
                    peak = s.UsedHistory[i];
            return peak;
        }

        Color BalanceColor(EnergySnapshot s)
        {
            return s.NetMw >= 0f ? GetHeaderColor() : GetErrorColor();
        }

        void DrawMinimal(List<MySprite> sprites)
        {
            var s = Snapshot;
            RectangleF view = Host.ViewBox;
            Color fg = Host.ForegroundColor;
            if (!s.HasData)
            {
                Bare.Hero(sprites, NotAvailable, view, MINIMAL_HERO_FONT_MUL, fg);
                return;
            }

            float margin = Bare.Margin;
            float lineHeight = Bare.LineHeight(1f);
            string hero = PowerText(s.NetMw, true, float.MaxValue);
            Color heroColor = BalanceColor(s);
            string summary = Loc("Produced") + " " + PowerText(s.ProducedMw, false, float.MaxValue)
                             + "  " + Loc("Used") + " " + PowerText(s.ConsumedMw, false, float.MaxValue)
                             + "  " + LocHelper.GetLoc(PEAK_KEY) + " " + PowerText(PeakMw(s), false, float.MaxValue);

            if (BareLayout.IsWide(view))
            {
                var heroArea = new RectangleF(view.X, view.Y, view.Width * 0.5f - margin * 0.5f, view.Height);
                Bare.Hero(sprites, hero, heroArea, MINIMAL_HERO_FONT_MUL, heroColor);
                float left = heroArea.Right + margin;
                float width = view.Right - left;
                Bare.Text(sprites, summary, new Vector2(left, view.Y), TextAlignment.LEFT, 1f, fg, width, lineHeight);
                float top = view.Y + lineHeight + margin;
                DrawCurve(sprites, new RectangleF(left, top, width, view.Bottom - top), s);
                return;
            }

            var curve = new RectangleF(view.X, view.Bottom - view.Height * MINIMAL_CURVE_SHARE, view.Width, view.Height * MINIMAL_CURVE_SHARE);
            var heroHalf = new RectangleF(view.X, view.Y, view.Width, view.Height * 0.5f);
            Bare.Hero(sprites, hero, heroHalf, MINIMAL_HERO_FONT_MUL, heroColor);
            float lineTop = heroHalf.Bottom + (curve.Y - margin - heroHalf.Bottom - lineHeight) * 0.5f;
            Bare.Text(sprites, summary, new Vector2(view.Center.X, lineTop), TextAlignment.CENTER, 1f, fg, view.Width, curve.Y - margin - heroHalf.Bottom);
            DrawCurve(sprites, curve, s);
        }

        void DrawCompact(List<MySprite> sprites)
        {
            var s = Snapshot;
            if (!s.HasData)
            {
                Host.DrawMessage(sprites, EmptyMessage, BareIcons.Warning, GetWarningColor());
                return;
            }

            RectangleF view = Host.ViewBox;
            Color fg = Host.ForegroundColor;
            float gap = Bare.Margin;
            float leftWidth = view.Width * 0.5f - gap * 0.5f;
            var left = new RectangleF(view.X, view.Y, leftWidth, view.Height);
            var right = new RectangleF(left.Right + gap, view.Y, view.Width - leftWidth - gap, view.Height);

            _kv.Clear();
            _kv.Add(KvRow(Loc("Produced"), BareIcons.Production, PowerText(s.ProducedMw, false, float.MaxValue), fg));
            _kv.Add(KvRow(Loc("Used"), BareIcons.Consumption, PowerText(s.ConsumedMw, false, float.MaxValue), fg));
            _kv.Add(KvRow(Loc("Balance"), BareIcons.Balance, PowerText(s.NetMw, true, float.MaxValue), BalanceColor(s)));
            _kv.Add(KvRow(LocHelper.GetLoc(PEAK_KEY), BareIcons.Peak, PowerText(PeakMw(s), false, float.MaxValue), fg));
            if (s.HasBatteries)
            {
                bool charging = s.BatteryStatus == EnergyBatteryStatus.Charging;
                _kv.Add(KvRow(Loc("Stored"), BareIcons.Storage, FormatingHelper.MegaWattHoursToString(s.StoredMwh), fg));
                _kv.Add(KvRow(Loc(charging ? "FullIn" : "EmptyIn"), charging ? BareIcons.Charging : BareIcons.Discharging,
                    s.EtaSeconds == EnergySnapshot.NO_ETA ? NotAvailable : EtaText(s.EtaSeconds), fg));
            }

            DrawCurve(sprites, left, s);
            Bare.Kv(sprites, right, _kv, fg, Theme);
        }

        static BareLayout.KvRow KvRow(string label, string icon, string value, Color color)
        {
            return new BareLayout.KvRow { Label = label, Icon = icon, Value = value, ValueColor = color, Bar = -1f };
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
            Bare.Label(_sprites, label.ToUpperInvariant(), ICON_POWER, new RectangleF(rect.X, rect.Y, rect.Width - valueWidth - gap, stripHeight),
                Palette.LabelMuted, labelScale / Host.FontScale);
            EnergyCardRenderer.DrawText(_sprites, surface, FormatingHelper.BoldFont(font, valueText), valueText, rect.Right, stripCenter, valueWidth, stripHeight,
                valueScale, seriesColor, TextAlignment.RIGHT);

            float axisScale = Metrics.Text(EnergyTheme.AXIS);
            float axisLine = FormatingHelper.LineHeight(axisScale, surface, font);
            float plotTop = rect.Y + stripHeight + Math.Max(Metrics.U * 0.5f, axisLine * 0.5f + 2f);
            float plotBottom = rect.Bottom - axisLine * 0.5f;
            if (plotBottom - plotTop < axisLine)
            {
                float top = rect.Y + stripHeight + Metrics.U * 0.3f;
                DrawCurve(_sprites, new RectangleF(rect.X, top, rect.Width, rect.Bottom - top - Metrics.U * 0.3f), history, head, axisMax, seriesColor);
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
        void DrawDonutCell(RectangleF cell, string label, string icon, float displayFraction, string percent, Color color)
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
            Bare.Label(_sprites, label.ToUpperInvariant(), icon, new RectangleF(cell.X, cell.Y, cell.Width, labelHeight), Palette.LabelMuted,
                Math.Min(labelScale, valueScale) / Host.FontScale);
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
