using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Gui.ControlsTemplates.Custom.Energy;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Helpers;
using LcdMod.Client.Modules.Energy;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Apps
{
    /// <summary>Cartão de um tipo de energia: ilustração com selo e estado de um lado, tabela rótulo | valor do outro.</summary>
    internal sealed class EnergyCardApp : EnergyAppBase
    {
        readonly EnergyKind _kind;
        readonly List<MySprite> _sprites = new List<MySprite>(140);
        readonly List<EnergyRow> _rows = new List<EnergyRow>(8);

        public EnergyCardApp(IAppHost host, EnergyKind kind) : base(host)
        {
            _kind = kind;
        }

        public override bool OwnsTitle => true;
        protected override string ShortTitle => KindName(_kind);

        public override bool HasVisibleItems()
        {
            return Snapshot.Kinds[(int)_kind].Total > 0;
        }

        public override List<MySprite> GetSprites()
        {
            PrepareFrame();
            _sprites.Clear();
            var snapshot = Snapshot;
            var stats = snapshot.Kinds[(int)_kind];

            float gap = Metrics.U;
            RectangleF illustration;
            RectangleF table;
            EnergyLayoutMode layout = Mode == EnergyLayoutMode.Tiny && Body.Height > Body.Width ? EnergyLayoutMode.Square : Mode;
            switch (layout)
            {
                case EnergyLayoutMode.Tiny:
                    float pillWidth = MinWidth(StateText(stats.State).ToUpperInvariant(), false) + Metrics.Padding * 2.8f + gap * 0.5f;
                    float leftWidth = Math.Max(Body.Width * 0.3f, Math.Min(Body.Width * 0.4f, pillWidth));
                    illustration = new RectangleF(Body.X, Body.Y, leftWidth - gap * 0.5f, Body.Height);
                    table = new RectangleF(Body.X + leftWidth + gap * 0.5f, Body.Y, Body.Width - leftWidth - gap * 0.5f, Body.Height);
                    break;
                case EnergyLayoutMode.Wide:
                    float illustrationWidth = Body.Width * EnergyTheme.CARD_ILLUSTRATION_WIDE;
                    _sprites.Add(LineRenderer.Rect(new Vector2(Body.X + illustrationWidth + gap * 0.5f, Body.Center.Y),
                        new Vector2(Metrics.DividerThickness, Body.Height), Palette.Divider));
                    illustration = new RectangleF(Body.X, Body.Y, illustrationWidth, Body.Height);
                    table = new RectangleF(Body.X + illustrationWidth + gap, Body.Y, Body.Width - illustrationWidth - gap, Body.Height);
                    break;
                default:
                    float illustrationHeight = Body.Height * EnergyTheme.CARD_ILLUSTRATION_SQUARE;
                    _sprites.Add(LineRenderer.Rect(new Vector2(Body.Center.X, Body.Y + illustrationHeight + gap * 0.5f),
                        new Vector2(Body.Width, Metrics.DividerThickness), Palette.Divider));
                    illustration = new RectangleF(Body.X, Body.Y, Body.Width, illustrationHeight);
                    table = new RectangleF(Body.X, Body.Y + illustrationHeight + gap, Body.Width, Body.Height - illustrationHeight - gap);
                    break;
            }

            BuildRows(stats, snapshot, table.Width - Metrics.Padding);
            DrawIllustration(stats, illustration);
            EnergyCardRenderer.DrawValueTable(_sprites, Host.Surface, TextFont, table, _rows, ref Palette, ref Metrics);
            return _sprites;
        }

        /// <summary>
        ///     O selo identifica o tipo só quando o título está oculto. Reatores e motores a hidrogênio, que entram em atenção
        ///     pela utilização, mostram essa utilização num anel em volta do ícone enquanto alguma unidade está em operação.
        /// </summary>
        void DrawIllustration(EnergyKindStats stats, RectangleF area)
        {
            EnergyUnitState state = stats.State;
            bool showsLoad = (_kind == EnergyKind.Reactor || _kind == EnergyKind.Hydrogen) && stats.Working > 0;
            EnergyCardRenderer.DrawIllustration(_sprites, Host.Surface, TextFont, area, TextureHelper.GetOrAddTextureForBlock(stats.RepresentativeDefinition, "IconEnergy"),
                state == EnergyUnitState.Offline || state == EnergyUnitState.Inoperative ? Palette.LabelMuted : Palette.Label, Host.TitleVisible ? null : KindName(_kind),
                StateText(state), EnergyTheme.StateColor(ref Palette, state), showsLoad ? stats.LoadRatio : -1f, ref Palette, ref Metrics);
        }

        /// <summary>Linhas em ordem de prioridade: a tabela descarta as do fim quando falta altura.</summary>
        void BuildRows(EnergyKindStats stats, EnergySnapshot s, float room)
        {
            _rows.Clear();
            Color neutral = Palette.ValueNeutral;
            var units = new EnergyRow(Loc("Units"), WorkingText(stats.Working, stats.Total), EnergyTheme.StateColor(ref Palette, stats.State));
            var output = PowerRow(Loc("Output"), stats.CurrentOutputMw, neutral, room);
            if (_kind == EnergyKind.Battery)
            {
                Color charge = EnergyTheme.ChargeColor(ref Palette, s.StoredRatio);
                _rows.Add(new EnergyRow(Loc("Charge"), FormatingHelper.PercentageToString(s.StoredRatio), charge, s.StoredRatio));
                _rows.Add(new EnergyRow(MyTexts.GetString("TerminalStatus"), BatteryStatusText(s.BatteryStatus), BatteryStatusColor(s.BatteryStatus)));
                _rows.Add(EtaRow(s));
                _rows.Add(new EnergyRow(Loc("Mode"), BatteryModeText(s.BatteryMode), neutral));
                _rows.Add(units);
                _rows.Add(PowerRow(Loc("Input"), s.BatteryInputMw, neutral, room));
                _rows.Add(output);
                string capacity = FormatingHelper.MegaWattHoursToString(s.CapacityMwh);
                string template = string.Format(Loc("Of"), FormatingHelper.MegaWattHoursToString(888.88f), capacity);
                if (MinWidth(template, true) <= room)
                    _rows.Add(new EnergyRow(Loc("Stored"), string.Format(Loc("Of"), FormatingHelper.MegaWattHoursToString(s.StoredMwh), capacity), neutral, -1f,
                        template));
                else
                    _rows.Add(new EnergyRow(Loc("Stored"), FormatingHelper.MegaWattHoursToString(s.StoredMwh), neutral, -1f,
                        FormatingHelper.MegaWattHoursToString(888.88f)));
                return;
            }

            _rows.Add(units);
            _rows.Add(output);
            _rows.Add(PowerRow(Loc("MaxOutput"), stats.MaxOutputMw, neutral, room));
            _rows.Add(PowerRow(Loc("Nominal"), stats.DefinedOutputMw, neutral, room));
            bool environmental = _kind == EnergyKind.Solar || _kind == EnergyKind.Wind;
            string ratioLabel = Loc(environmental ? "Efficiency" : "Load");
            if (stats.Working == 0)
                _rows.Add(new EnergyRow(ratioLabel, NotAvailable, Palette.LabelMuted));
            else if (environmental)
                _rows.Add(new EnergyRow(ratioLabel, FormatingHelper.PercentageToString(stats.AvailabilityRatio), neutral, stats.AvailabilityRatio));
            else
                _rows.Add(new EnergyRow(ratioLabel, FormatingHelper.PercentageToString(stats.LoadRatio), EnergyTheme.LoadColor(ref Palette, stats.LoadRatio),
                    stats.LoadRatio));
        }
    }
}
