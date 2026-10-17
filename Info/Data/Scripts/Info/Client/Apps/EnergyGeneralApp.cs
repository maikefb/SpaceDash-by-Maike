using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Gui.ControlsTemplates.Custom.Energy;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Client.Modules.Energy;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Apps
{
    /// <summary>Painel geral: produção, consumo, saldo, um resumo por tipo e a carga das baterias.</summary>
    internal sealed class EnergyGeneralApp : EnergyAppBase
    {
        const float KIND_VALUE = EnergyTheme.VALUE * 1.2f;
        const string ICON_POWER = "IconEnergy";
        const string ICON_BATTERY = "Battery";
        const float MINIMAL_HERO_FONT_MUL = 3f;
        const float RING_TRACK_MIX = 0.15f;
        const float USED_WARN_RATIO = 0.75f;
        const float USED_ERROR_RATIO = 0.9f;

        readonly List<MySprite> _sprites = new List<MySprite>(170);
        readonly List<BareLayout.Tile> _tiles = new List<BareLayout.Tile>();

        public EnergyGeneralApp(IAppHost host) : base(host)
        {
        }

        public override bool OwnsTitle => true;

        public override List<MySprite> GetSprites()
        {
            PrepareFrame();
            if (Mode != EnergyLayoutMode.Tiny && !FullLayoutFits())
                Mode = EnergyLayoutMode.Tiny;
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
            {
                DrawFramed(snapshot);
                return _sprites;
            }

            switch (Mode)
            {
                case EnergyLayoutMode.Tiny:
                    DrawTiny(snapshot);
                    break;
                case EnergyLayoutMode.Wide:
                    DrawWide(snapshot);
                    break;
                default:
                    DrawSquare(snapshot);
                    break;
            }

            return _sprites;
        }

        void DrawMinimal(List<MySprite> sprites)
        {
            var s = Snapshot;
            var view = Host.ViewBox;
            Color fg = GetForegroundColor();
            Color track = Color.Lerp(Host.BackgroundColor, fg, RING_TRACK_MIX);
            bool valid = s.HasData && s.ProducedMw > EnergySnapshot.EPSILON_MW;
            float fraction = valid ? MathHelper.Clamp(s.ConsumedMw / s.ProducedMw, 0f, 1f) : 0f;
            Color fill = !valid ? fg : fraction > USED_ERROR_RATIO ? GetErrorColor() : fraction > USED_WARN_RATIO ? GetWarningColor() : GetHeaderColor();
            string center = !s.HasData ? null : valid ? FormatingHelper.PercentageToString(fraction) : NotAvailable;
            string aux = s.HasData
                ? "P " + FormatingHelper.MegaWattsToString(s.ProducedMw) + "  C " + FormatingHelper.MegaWattsToString(s.ConsumedMw) + "  " + PowerText(s.NetMw, true, float.MaxValue)
                : null;
            float margin = Bare.Margin;
            float lineHeight = Bare.LineHeight(1f);

            if (BareLayout.IsWide(view))
            {
                var ringArea = new RectangleF(view.X, view.Y, view.Width * 0.5f, view.Height);
                Bare.Ring(sprites, ringArea, fraction, fill, track, center, fill, MINIMAL_HERO_FONT_MUL);
                float left = ringArea.Right + margin;
                Bare.Text(sprites, aux, new Vector2(left, view.Center.Y - lineHeight * 0.5f), TextAlignment.LEFT, 1f, fg, view.Right - left, view.Height);
                return;
            }

            var ring = new RectangleF(view.X, view.Y, view.Width, view.Height - lineHeight - margin);
            Bare.Ring(sprites, ring, fraction, fill, track, center, fill, MINIMAL_HERO_FONT_MUL);
            Bare.Text(sprites, aux, new Vector2(view.Center.X, ring.Bottom + margin), TextAlignment.CENTER, 1f, fg, view.Width, lineHeight);
        }

        void DrawCompact(List<MySprite> sprites)
        {
            var s = Snapshot;
            if (!s.HasData)
            {
                Host.DrawMessage(sprites, NotAvailable, "Warning", GetWarningColor());
                return;
            }

            Color fg = GetForegroundColor();
            Color accent = GetHeaderColor();
            bool hasBatteries = s.HasBatteries;

            _tiles.Clear();
            _tiles.Add(new BareLayout.Tile { Icon = ICON_POWER, Label = Loc("Production"), Value = FormatingHelper.MegaWattsToString(s.ProducedMw), ValueColor = fg });
            _tiles.Add(new BareLayout.Tile { Icon = ICON_POWER, Label = Loc("Consumption"), Value = FormatingHelper.MegaWattsToString(s.ConsumedMw), ValueColor = fg });
            _tiles.Add(new BareLayout.Tile
            {
                Icon = ICON_BATTERY,
                Label = Loc("Charge"),
                Value = hasBatteries ? FormatingHelper.PercentageToString(s.StoredRatio) : NotAvailable,
                ValueColor = hasBatteries ? BatteryStatusColor(s.BatteryStatus) : fg
            });
            _tiles.Add(new BareLayout.Tile
            {
                Icon = ICON_POWER,
                Label = Loc("Balance"),
                Value = PowerText(s.NetMw, true, float.MaxValue),
                ValueColor = s.NetMw >= 0f ? accent : GetErrorColor()
            });
            Bare.Tiles(sprites, Host.ViewBox, _tiles, 2, 2, fg, accent, Theme);
        }

        /// <summary>
        ///     As caixas mais apertadas do layout cheio (Carga: rótulo, secundário e barra; no Wide também o Saldo, com rótulo e
        ///     barra numa faixa menor) ainda dão ao valor a altura de uma linha no tamanho pedido? Com zoom alto não dão, e o
        ///     painel cai para as 4 caixas grandes do Tiny, para texto maior virar valor maior.
        /// </summary>
        bool FullLayoutFits()
        {
            float gap = Gap;
            float need = FormatingHelper.LineHeight(Metrics.Text(EnergyTheme.BIG_VALUE), Host.Surface, TextFont);
            if (Mode != EnergyLayoutMode.Wide)
                return EnergyCardRenderer.ValueBoxSlice(Host.Surface, TextFont, (Body.Height - gap * 5f) / 6f, true, true, true, ref Metrics) >= need;

            float height = Body.Height - gap * 3f;
            return EnergyCardRenderer.ValueBoxSlice(Host.Surface, TextFont, height * 0.23f, true, true, true, ref Metrics) >= need &&
                   EnergyCardRenderer.ValueBoxSlice(Host.Surface, TextFont, height * 0.17f, true, false, true, ref Metrics) >= need;
        }

        /// <summary>Espaço entre caixas limitado pela altura, para o zoom do usuário não comer as caixas.</summary>
        float Gap => Math.Min(Metrics.U * 0.6f, Body.Height * 0.015f);

        void DrawWide(EnergySnapshot s)
        {
            float gap = Gap;
            float height = Body.Height - gap * 3f;
            var totals = new RectangleF(Body.X, Body.Y, Body.Width, height * 0.3f);
            var balance = new RectangleF(Body.X, totals.Bottom + gap, Body.Width, height * 0.17f);
            var kinds = new RectangleF(Body.X, balance.Bottom + gap, Body.Width, height * 0.3f);
            var charge = new RectangleF(Body.X, kinds.Bottom + gap, Body.Width, height * 0.23f);

            DrawProduction(Cell(totals, 0, 2, gap), s);
            DrawConsumption(Cell(totals, 1, 2, gap), s);
            DrawBalance(balance, s);
            DrawKind(Cell(kinds, 0, 5, gap), EnergyKind.Battery, s);
            DrawKind(Cell(kinds, 1, 5, gap), EnergyKind.Solar, s);
            DrawKind(Cell(kinds, 2, 5, gap), EnergyKind.Wind, s);
            DrawKind(Cell(kinds, 3, 5, gap), EnergyKind.Reactor, s);
            DrawKind(Cell(kinds, 4, 5, gap), EnergyKind.Hydrogen, s);
            DrawCharge(charge, s, true);
        }

        void DrawSquare(EnergySnapshot s)
        {
            float gap = Gap;
            float rowHeight = (Body.Height - gap * 5f) / 6f;
            DrawProduction(Cell(Row(0, rowHeight, gap), 0, 2, gap), s);
            DrawConsumption(Cell(Row(0, rowHeight, gap), 1, 2, gap), s);
            DrawBalance(Row(1, rowHeight, gap), s);
            DrawKind(Cell(Row(2, rowHeight, gap), 0, 2, gap), EnergyKind.Battery, s);
            DrawKind(Cell(Row(2, rowHeight, gap), 1, 2, gap), EnergyKind.Solar, s);
            DrawKind(Cell(Row(3, rowHeight, gap), 0, 2, gap), EnergyKind.Wind, s);
            DrawKind(Cell(Row(3, rowHeight, gap), 1, 2, gap), EnergyKind.Reactor, s);
            DrawKind(Cell(Row(4, rowHeight, gap), 0, 2, gap), EnergyKind.Hydrogen, s);
            var stored = Cell(Row(4, rowHeight, gap), 1, 2, gap);
            DrawBox(stored, Fits(Loc("StoredEnergy"), stored) ? Loc("StoredEnergy") : Loc("Stored"), FormatingHelper.MegaWattHoursToString(s.StoredMwh),
                Palette.ValueNeutral, string.Format(Loc("MaxFormat"), FormatingHelper.MegaWattHoursToString(s.CapacityMwh)), EnergyTheme.BIG_VALUE,
                FormatingHelper.MegaWattHoursToString(888.88f));
            DrawCharge(Row(5, rowHeight, gap), s, true);
        }

        /// <summary>
        ///     Quatro caixas em linha, com rótulos curtos, quando cabem rótulo e valor no piso e o valor fica ao menos do tamanho
        ///     que teria na grade 2×2; senão a grade, ou uma coluna em retrato quando a grade não cabe.
        /// </summary>
        void DrawTiny(EnergySnapshot s)
        {
            float gap = Metrics.U * 0.5f;
            float minWidth = Math.Max(
                Math.Max(MinWidth(Loc("Produced").ToUpperInvariant(), false), MinWidth(Loc("Used").ToUpperInvariant(), false)),
                Math.Max(MinWidth(Loc("Balance").ToUpperInvariant(), false), MinWidth(Loc("Charge").ToUpperInvariant(), false)));
            minWidth = Math.Max(minWidth, Math.Max(
                Math.Max(MinWidth(PowerText(s.ProducedMw, false, 0f), true), MinWidth(PowerText(s.ConsumedMw, false, 0f), true)),
                Math.Max(MinWidth(PowerText(s.NetMw, true, 0f), true), MinWidth(ChargeText(s), true))));
            float boxMinWidth = minWidth + Metrics.Padding * 2f;
            float rowBoxWidth = (Body.Width - gap * 3f) / 4f;
            float gridBoxWidth = (Body.Width - gap) * 0.5f;
            bool inRow = rowBoxWidth >= boxMinWidth &&
                         TinyValueScale(rowBoxWidth, Body.Height) >= TinyValueScale(gridBoxWidth, (Body.Height - gap) * 0.5f);
            int columns = inRow ? 4 : Body.Height > Body.Width && gridBoxWidth < boxMinWidth ? 1 : 2;
            int rows = 4 / columns;
            float rowHeight = (Body.Height - gap * (rows - 1)) / rows;

            DrawProduction(Box(0, columns, rowHeight, gap), s);
            DrawConsumption(Box(1, columns, rowHeight, gap), s);
            var balance = Box(2, columns, rowHeight, gap);
            DrawBox(balance, Loc("Balance"), PowerText(s.NetMw, true, balance), EnergyTheme.BalanceColor(ref Palette, s.NetMw, s.BatteryEmptySoon), null,
                EnergyTheme.BIG_VALUE, "+" + WidestPower);
            DrawCharge(Box(3, columns, rowHeight, gap), s, false);
        }

        /// <summary>Escala do valor mais largo (o Saldo) numa caixa do Tiny, pela mesma conta do DrawValueBox.</summary>
        float TinyValueScale(float width, float height)
        {
            var surface = Host.Surface;
            string template = "+" + WidestPower;
            float unitLine = FormatingHelper.LineHeight(1f, surface, TextFont);
            float valueHeight = EnergyCardRenderer.ValueBoxSlice(surface, TextFont, height, true, false, false, ref Metrics);
            float requested = Math.Min(Math.Max(Metrics.Text(EnergyTheme.BIG_VALUE), valueHeight / unitLine * 0.9f), Metrics.Text(EnergyTheme.BIG_VALUE) * 2f);
            float byWidth = EnergyCardRenderer.ValueBoxInnerWidth(new RectangleF(0f, 0f, width, height), ref Metrics) /
                            FormatingHelper.GetSizeInPixel(template, FormatingHelper.BoldFont(TextFont, template), 1f, surface).X;
            return Math.Min(requested, Math.Min(byWidth, valueHeight / unitLine));
        }

        RectangleF Box(int index, int columns, float rowHeight, float gap)
        {
            return Cell(Row(index / columns, rowHeight, gap), index % columns, columns, gap);
        }

        RectangleF Row(int index, float rowHeight, float gap)
        {
            return new RectangleF(Body.X, Body.Y + (rowHeight + gap) * index, Body.Width, rowHeight);
        }

        static RectangleF Cell(RectangleF row, int index, int count, float gap)
        {
            float width = (row.Width - gap * (count - 1)) / count;
            return new RectangleF(row.X + (width + gap) * index, row.Y, width, row.Height);
        }

        void DrawProduction(RectangleF box, EnergySnapshot s)
        {
            bool tiny = Mode == EnergyLayoutMode.Tiny;
            DrawBox(box, LongTotals(box) ? Loc("Production") : Loc("Produced"), PowerText(s.ProducedMw, false, box), Palette.ValueNeutral,
                tiny ? null : string.Format(Loc("MaxFormat"), FormatingHelper.MegaWattsToString(s.MaxProducedMw)), EnergyTheme.BIG_VALUE, WidestPower);
        }

        void DrawConsumption(RectangleF box, EnergySnapshot s)
        {
            bool tiny = Mode == EnergyLayoutMode.Tiny;
            DrawBox(box, LongTotals(box) ? Loc("Consumption") : Loc("Used"), PowerText(s.ConsumedMw, false, box), EnergyTheme.UsedColor(ref Palette, s.UsedRatio),
                tiny ? null : string.Format(Loc("OfCapacityFormat"), FormatingHelper.PercentageToString(s.UsedRatio)), EnergyTheme.BIG_VALUE, WidestPower);
        }

        /// <summary>Rótulos longos de Produção e Consumo só quando os dois cabem: as caixas são irmãs, da mesma largura.</summary>
        bool LongTotals(RectangleF box)
        {
            return Mode != EnergyLayoutMode.Tiny && Fits(Loc("Production"), box) && Fits(Loc("Consumption"), box);
        }

        /// <summary>O rótulo cabe na caixa na escala mínima?</summary>
        bool Fits(string label, RectangleF box)
        {
            return MinWidth(label.ToUpperInvariant(), false) <= EnergyCardRenderer.ValueBoxInnerWidth(box, ref Metrics);
        }

        void DrawBalance(RectangleF box, EnergySnapshot s)
        {
            float reference = Math.Max(Math.Max(s.ProducedMw, s.ConsumedMw), EnergySnapshot.EPSILON_MW);
            float fraction = MathHelper.Clamp(s.NetMw / reference, -1f, 1f);
            EnergyCardRenderer.DrawValueBox(_sprites, Host.Surface, TextFont, box, Loc("Balance"), PowerText(s.NetMw, true, box),
                EnergyTheme.BalanceColor(ref Palette, s.NetMw, s.BatteryEmptySoon), null, fraction, true, EnergyTheme.BIG_VALUE, ref Palette, ref Metrics,
                "+" + WidestPower);
        }

        /// <summary>Estado em texto antes do detalhe, para sobreviver ao corte em caixas estreitas.</summary>
        void DrawKind(RectangleF box, EnergyKind kind, EnergySnapshot s)
        {
            var stats = s.Kinds[(int)kind];
            string sub;
            if (stats.Total == 0)
                sub = Loc("NoUnits");
            else
            {
                string prefix = stats.State != EnergyUnitState.Online ? StateText(stats.State) + " · " : string.Empty;
                if (kind == EnergyKind.Battery)
                    sub = prefix + BatteryStatusText(s.BatteryStatus);
                else
                {
                    float inner = EnergyCardRenderer.ValueBoxInnerWidth(box, ref Metrics);
                    string power = PowerText(stats.CurrentOutputMw, false, inner - MinWidth(prefix, false));
                    sub = prefix.Length > 0 && MinWidth(prefix + power, false) > inner ? StateText(stats.State) : prefix + power;
                }
            }

            DrawBox(box, KindName(kind), WorkingText(stats.Working, stats.Total), EnergyTheme.StateColor(ref Palette, stats.State), sub, KIND_VALUE);
        }

        void DrawCharge(RectangleF box, EnergySnapshot s, bool withDetails)
        {
            bool hasBatteries = s.HasBatteries;
            Color color = hasBatteries ? EnergyTheme.ChargeColor(ref Palette, s.StoredRatio) : Palette.LabelMuted;
            string value = ChargeText(s);
            string sub = null;
            if (withDetails)
            {
                sub = hasBatteries
                    ? string.Format(Loc("Of"), FormatingHelper.MegaWattHoursToString(s.StoredMwh), FormatingHelper.MegaWattHoursToString(s.CapacityMwh))
                    : Loc("NoUnits");
                if (hasBatteries && s.EtaSeconds >= 0f)
                    sub = Loc(s.BatteryStatus == EnergyBatteryStatus.Charging ? "FullIn" : "EmptyIn") + " " + EtaText(s.EtaSeconds) + " · " + sub;
            }

            EnergyCardRenderer.DrawValueBox(_sprites, Host.Surface, TextFont, box, Loc("Charge"), value, color, sub, hasBatteries ? s.StoredRatio : -1f, false,
                EnergyTheme.BIG_VALUE, ref Palette, ref Metrics, FormatingHelper.PercentageToString(1f));
        }

        void DrawFramed(EnergySnapshot s)
        {
            var f = Host.Frame;
            Color neutral = Palette.ValueNeutral;
            Color muted = Palette.LabelMuted;
            FramedHeader(_sprites);
            f.Text(_sprites, "producao_total.valor", FramedPower("producao_total.valor", s.ProducedMw, false), neutral);
            f.Text(_sprites, "producao_total.detalhe", string.Format(Loc("MaxFormat"), FormatingHelper.MegaWattsToString(s.MaxProducedMw)), muted, false);
            f.Text(_sprites, "consumo_total.valor", FramedPower("consumo_total.valor", s.ConsumedMw, false), EnergyTheme.UsedColor(ref Palette, s.UsedRatio));
            f.Text(_sprites, "consumo_total.detalhe", string.Format(Loc("OfCapacityFormat"), FormatingHelper.PercentageToString(s.UsedRatio)), muted, false);
            float reference = Math.Max(Math.Max(s.ProducedMw, s.ConsumedMw), EnergySnapshot.EPSILON_MW);
            Color balance = EnergyTheme.BalanceColor(ref Palette, s.NetMw, s.BatteryEmptySoon);
            f.BipolarBar(_sprites, "saldo.barra", s.NetMw / reference, balance);
            f.Text(_sprites, "saldo.valor", FramedPower("saldo.valor", s.NetMw, true), balance);
            DrawFramedKind("baterias", EnergyKind.Battery, s);
            DrawFramedKind("paineis_solares", EnergyKind.Solar, s);
            DrawFramedKind("turbinas_eolicas", EnergyKind.Wind, s);
            bool hasBatteries = s.HasBatteries;
            Color charge = hasBatteries ? EnergyTheme.ChargeColor(ref Palette, s.StoredRatio) : muted;
            f.Text(_sprites, "armazenado.valor", hasBatteries ? FormatingHelper.MegaWattHoursToString(s.StoredMwh) : NotAvailable, neutral);
            f.Text(_sprites, "armazenado.detalhe", hasBatteries ? string.Format(Loc("MaxFormat"), FormatingHelper.MegaWattHoursToString(s.CapacityMwh)) : Loc("NoUnits"), muted, false);
            f.Text(_sprites, "carga.valor", ChargeText(s), charge);
            string detail = !hasBatteries ? Loc("NoUnits")
                : s.EtaSeconds >= 0f ? Loc(s.BatteryStatus == EnergyBatteryStatus.Charging ? "FullIn" : "EmptyIn") + " " + EtaText(s.EtaSeconds)
                : BatteryStatusText(s.BatteryStatus);
            f.Text(_sprites, "carga.detalhe", detail, muted, false);
        }

        void DrawFramedKind(string prefix, EnergyKind kind, EnergySnapshot s)
        {
            var stats = s.Kinds[(int)kind];
            Host.Frame.Text(_sprites, prefix + ".valor", WorkingText(stats.Working, stats.Total), EnergyTheme.StateColor(ref Palette, stats.State));
            string detail = stats.Total == 0 ? Loc("NoUnits")
                : kind == EnergyKind.Battery ? BatteryStatusText(s.BatteryStatus)
                : FramedPower(prefix + ".detalhe", stats.CurrentOutputMw, false);
            Host.Frame.Text(_sprites, prefix + ".detalhe", detail, Palette.LabelMuted, false);
        }

        void DrawBox(RectangleF box, string label, string value, Color color, string sub, float valueType, string widthTemplate = null)
        {
            EnergyCardRenderer.DrawValueBox(_sprites, Host.Surface, TextFont, box, label, value, color, sub, -1f, false, valueType, ref Palette, ref Metrics,
                widthTemplate);
        }

        /// <summary>Potência que cabe na largura útil da caixa na escala mínima, perdendo casas antes da unidade.</summary>
        string PowerText(float megaWatts, bool signed, RectangleF box)
        {
            return PowerText(megaWatts, signed, EnergyCardRenderer.ValueBoxInnerWidth(box, ref Metrics));
        }

        string ChargeText(EnergySnapshot s)
        {
            return s.HasBatteries ? FormatingHelper.PercentageToString(s.StoredRatio) : NotAvailable;
        }
    }
}
