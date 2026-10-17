using LcdMod.Common.Config.Components;
using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Gui.ControlsTemplates.Custom.Energy;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Styling;
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
        readonly List<BareLayout.Tile> _tiles = new List<BareLayout.Tile>(4);

        const float MINIMAL_ICON_BASE = 24f;
        const float MINIMAL_PERCENT_FONT_MUL = 3f;
        const float MINIMAL_POWER_FONT_MUL = 2.5f;
        const float MINIMAL_BAR_HEIGHT_RATIO = 0.5f;
        const string ICON_BATTERY = "Battery";
        const string ICON_HYDROGEN = "IconHydrogen";
        const string ICON_POWER = "IconEnergy";

        public EnergyCardApp(IAppHost host, EnergyKind kind) : base(host)
        {
            _kind = kind;
        }

        public override bool OwnsTitle => true;

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
            if (IsMinimal)
            {
                DrawMinimal(_sprites, snapshot, stats);
                return _sprites;
            }
            if (IsCompact)
            {
                DrawCompact(_sprites, snapshot, stats);
                return _sprites;
            }
            if (Host.Frame != null)
            {
                DrawFramed(snapshot, stats);
                return _sprites;
            }

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

        bool HeroIsPercent => _kind == EnergyKind.Battery || _kind == EnergyKind.Hydrogen;

        string HeroValue(EnergySnapshot s, EnergyKindStats stats)
        {
            if (stats.Total == 0)
                return "0";
            switch (_kind)
            {
                case EnergyKind.Battery:
                    return FormatingHelper.PercentageToString(s.StoredRatio);
                case EnergyKind.Hydrogen:
                    return FormatingHelper.PercentageToString(stats.LoadRatio);
                default:
                    return FormatingHelper.MegaWattsToString(stats.CurrentOutputMw);
            }
        }

        float HeroFraction(EnergySnapshot s, EnergyKindStats stats)
        {
            if (stats.Total == 0)
                return 0f;
            return _kind == EnergyKind.Battery ? s.StoredRatio : stats.LoadRatio;
        }

        string KindIcon(EnergyKindStats stats)
        {
            switch (_kind)
            {
                case EnergyKind.Battery:
                    return ICON_BATTERY;
                case EnergyKind.Hydrogen:
                    return ICON_HYDROGEN;
                default:
                    return TextureHelper.GetOrAddTextureForBlock(stats.RepresentativeDefinition, ICON_POWER);
            }
        }

        string MinimalLine(EnergySnapshot s, EnergyKindStats stats)
        {
            string text = WorkingText(stats.Working, stats.Total) + " · ";
            if (_kind != EnergyKind.Battery)
                return text + StateText(stats.State);
            text += BatteryModeText(s.BatteryMode);
            if (s.BatteryStatus == EnergyBatteryStatus.Charging)
                return text + " +";
            if (s.BatteryStatus == EnergyBatteryStatus.Discharging)
                return text + " -";
            return text;
        }

        void DrawMinimal(List<MySprite> sprites, EnergySnapshot s, EnergyKindStats stats)
        {
            RectangleF view = Host.ViewBox;
            Color fg = Host.ForegroundColor;
            bool empty = !HasData || stats.Total == 0;
            Color stateColor = empty ? fg : EnergyTheme.StateColor(ref Palette, stats.State);
            string hero = HasData ? HeroValue(s, stats) : null;
            string line = HasData ? MinimalLine(s, stats) : null;
            float fraction = HasData ? HeroFraction(s, stats) : 0f;
            float heroMul = HeroIsPercent ? MINIMAL_PERCENT_FONT_MUL : MINIMAL_POWER_FONT_MUL;
            float margin = Bare.Margin;
            float iconSide = MINIMAL_ICON_BASE * Bare.Scale;
            float lineHeight = Bare.LineHeight(1f);
            float barHeight = Math.Max(2f, lineHeight * MINIMAL_BAR_HEIGHT_RATIO);
            string icon = KindIcon(stats);

            if (BareLayout.IsWide(view))
            {
                var left = new RectangleF(view.X, view.Y, view.Width * 0.5f - margin * 0.5f, view.Height);
                var right = new RectangleF(view.Center.X + margin * 0.5f, view.Y, view.Width * 0.5f - margin * 0.5f, view.Height);
                sprites.Add(new MySprite(SpriteType.TEXTURE, icon, new Vector2(left.Center.X, left.Y + iconSide * 0.5f), new Vector2(iconSide, iconSide), fg, null, TextAlignment.CENTER));
                Bare.Hero(sprites, hero, new RectangleF(left.X, left.Y + iconSide + margin, left.Width, left.Height - iconSide - margin), heroMul, stateColor);
                float block = barHeight + margin + lineHeight;
                float top = right.Center.Y - block * 0.5f;
                ThemeChrome.DrawBar(sprites, Theme, new RectangleF(right.X, top, right.Width, barHeight), fraction, stateColor, fg, Bare.Scale);
                Bare.Text(sprites, line, new Vector2(right.X, top + barHeight + margin), TextAlignment.LEFT, 1f, fg, right.Width, lineHeight);
                return;
            }

            sprites.Add(new MySprite(SpriteType.TEXTURE, icon, new Vector2(view.Center.X, view.Y + iconSide * 0.5f), new Vector2(iconSide, iconSide), fg, null, TextAlignment.CENTER));
            float lineTop = view.Bottom - lineHeight;
            float barTop = lineTop - margin - barHeight;
            float heroTop = view.Y + iconSide + margin;
            Bare.Hero(sprites, hero, new RectangleF(view.X, heroTop, view.Width, barTop - margin - heroTop), heroMul, stateColor);
            ThemeChrome.DrawBar(sprites, Theme, new RectangleF(view.X, barTop, view.Width, barHeight), fraction, stateColor, fg, Bare.Scale);
            Bare.Text(sprites, line, new Vector2(view.Center.X, lineTop), TextAlignment.CENTER, 1f, fg, view.Width, lineHeight);
        }

        void DrawCompact(List<MySprite> sprites, EnergySnapshot s, EnergyKindStats stats)
        {
            if (!HasData)
            {
                Host.DrawMessage(sprites, NotAvailable, "Warning", GetWarningColor());
                return;
            }

            RectangleF view = Host.ViewBox;
            Color fg = Host.ForegroundColor;
            Color accent = GetHeaderColor();
            Color neutral = Palette.ValueNeutral;
            Color stateColor = stats.Total == 0 ? fg : EnergyTheme.StateColor(ref Palette, stats.State);
            string icon = KindIcon(stats);
            float lineHeight = Bare.LineHeight(1f);
            float margin = Bare.Margin;

            Bare.Text(sprites, KindName(_kind) + " " + WorkingText(stats.Working, stats.Total), new Vector2(view.X, view.Y), TextAlignment.LEFT, 1f, stateColor, view.Width, lineHeight);
            var grid = new RectangleF(view.X, view.Y + lineHeight + margin, view.Width, view.Height - lineHeight - margin);

            _tiles.Clear();
            if (_kind == EnergyKind.Battery)
            {
                Color charge = stats.Total == 0 ? fg : EnergyTheme.ChargeColor(ref Palette, s.StoredRatio);
                _tiles.Add(new BareLayout.Tile { Icon = icon, Label = Loc("Charge"), Value = FormatingHelper.PercentageToString(s.StoredRatio), ValueColor = charge });
                _tiles.Add(new BareLayout.Tile { Label = Loc("Mode"), Value = BatteryModeText(s.BatteryMode), ValueColor = neutral });
                _tiles.Add(new BareLayout.Tile { Icon = ICON_POWER, Label = Loc("Input"), Value = FormatingHelper.MegaWattsToString(s.BatteryInputMw), ValueColor = neutral });
                _tiles.Add(new BareLayout.Tile { Icon = ICON_POWER, Label = Loc("Output"), Value = FormatingHelper.MegaWattsToString(stats.CurrentOutputMw), ValueColor = neutral });
                Bare.Tiles(sprites, grid, _tiles, 2, 2, fg, accent, Theme);
                return;
            }

            bool environmental = _kind == EnergyKind.Solar || _kind == EnergyKind.Wind;
            string ratioLabel = Loc(environmental ? "Efficiency" : "Load");
            string ratioValue = stats.Working == 0 ? NotAvailable
                : FormatingHelper.PercentageToString(environmental ? stats.AvailabilityRatio : stats.LoadRatio);
            Color ratioColor = stats.Working == 0 ? Palette.LabelMuted
                : environmental ? neutral : EnergyTheme.LoadColor(ref Palette, stats.LoadRatio);
            var output = new BareLayout.Tile { Icon = ICON_POWER, Label = Loc("Output"), Value = FormatingHelper.MegaWattsToString(stats.CurrentOutputMw), ValueColor = neutral };
            var max = new BareLayout.Tile { Icon = ICON_POWER, Label = Loc("MaxOutput"), Value = FormatingHelper.MegaWattsToString(stats.MaxOutputMw), ValueColor = neutral };
            var ratio = new BareLayout.Tile { Icon = icon, Label = ratioLabel, Value = ratioValue, ValueColor = ratioColor };
            var state = new BareLayout.Tile { Label = MyTexts.GetString("TerminalStatus"), Value = StateText(stats.State), ValueColor = stateColor };
            if (_kind == EnergyKind.Hydrogen)
            {
                _tiles.Add(ratio);
                _tiles.Add(output);
                _tiles.Add(max);
            }
            else
            {
                _tiles.Add(output);
                _tiles.Add(max);
                _tiles.Add(ratio);
            }
            _tiles.Add(state);
            Bare.Tiles(sprites, grid, _tiles, 2, 2, fg, accent, Theme);
        }

        /// <summary>
        ///     O selo identifica o tipo só quando o título está oculto. Reatores e motores a hidrogênio, que entram em atenção
        ///     pela utilização, mostram essa utilização num anel em volta do ícone enquanto alguma unidade está em operação.
        /// </summary>
        void DrawIllustration(EnergyKindStats stats, RectangleF area)
        {
            EnergyUnitState state = stats.State;
            bool dimmed = state == EnergyUnitState.Offline || state == EnergyUnitState.Inoperative;
            string illustration = IllustrationTexture(_kind);
            bool showsLoad = illustration == null && (_kind == EnergyKind.Reactor || _kind == EnergyKind.Hydrogen) && stats.Working > 0;
            string icon = illustration ?? TextureHelper.GetOrAddTextureForBlock(stats.RepresentativeDefinition, "IconEnergy");
            Color iconColor = illustration != null
                ? Color.White
                : dimmed ? Palette.LabelMuted : Palette.Label;
            EnergyCardRenderer.DrawIllustration(_sprites, Host.Surface, TextFont, area, icon, iconColor, illustration != null ? ILLUSTRATION_ASPECT : 1f,
                Host.TitleVisible ? null : KindName(_kind),
                StateText(state), EnergyTheme.StateColor(ref Palette, state), showsLoad ? stats.LoadRatio : -1f, ref Palette, ref Metrics);
        }

        const float ILLUSTRATION_ASPECT = 2172f / 724f;

        static string IllustrationTexture(EnergyKind kind)
        {
            switch (kind)
            {
                case EnergyKind.Battery: return "SD_IllusBattery";
                case EnergyKind.Solar: return "SD_IllusSolar";
                case EnergyKind.Wind: return "SD_IllusWind";
                case EnergyKind.Reactor: return "SD_IllusReactor";
                case EnergyKind.Hydrogen: return "SD_IllusEngine";
                default: return null;
            }
        }

        void DrawFramed(EnergySnapshot s, EnergyKindStats stats)
        {
            var f = Host.Frame;
            Color neutral = Palette.ValueNeutral;
            Color state = EnergyTheme.StateColor(ref Palette, stats.State);
            string units = WorkingText(stats.Working, stats.Total);
            FramedHeader(_sprites);
            if (_kind == EnergyKind.Battery)
            {
                Color charge = EnergyTheme.ChargeColor(ref Palette, s.StoredRatio);
                f.Text(_sprites, "modo.valor", BatteryModeText(s.BatteryMode), neutral);
                f.Text(_sprites, "baterias.valor", units, state);
                f.Text(_sprites, "entrada.valor", FramedPower("entrada.valor", s.BatteryInputMw, false), neutral);
                f.Text(_sprites, "saida.valor", FramedPower("saida.valor", stats.CurrentOutputMw, false), neutral);
                f.Text(_sprites, "armazenado.valor", string.Format(Loc("Of"), FormatingHelper.MegaWattHoursToString(s.StoredMwh),
                    FormatingHelper.MegaWattHoursToString(s.CapacityMwh)), neutral);
                f.Bar(_sprites, "carga.barra", s.StoredRatio, charge);
                f.Text(_sprites, "carga.valor", FormatingHelper.PercentageToString(s.StoredRatio), charge);
                return;
            }

            string unitsField = _kind == EnergyKind.Solar ? "paineis.valor"
                : _kind == EnergyKind.Wind ? "turbinas.valor"
                : _kind == EnergyKind.Hydrogen ? "motores.valor" : "reatores.valor";
            f.Text(_sprites, "estado.valor", StateText(stats.State), state);
            f.Text(_sprites, unitsField, units, state);
            f.Text(_sprites, "saida_atual.valor", FramedPower("saida_atual.valor", stats.CurrentOutputMw, false), neutral);
            f.Text(_sprites, "saida_maxima.valor", FramedPower("saida_maxima.valor", stats.MaxOutputMw, false), neutral);
            Color load = EnergyTheme.LoadColor(ref Palette, stats.LoadRatio);
            f.Bar(_sprites, "utilizacao.barra", stats.LoadRatio, load);
            f.Text(_sprites, "utilizacao.valor", stats.Working == 0 ? NotAvailable : FormatingHelper.PercentageToString(stats.LoadRatio), load);
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
