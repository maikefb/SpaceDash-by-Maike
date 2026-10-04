using System;
using System.Collections.Generic;
using LcdMod.Client.Extensions;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Custom.Energy;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Client.Modules.Energy;
using LcdMod.Client.ScreenAreas;
using LcdMod.Client.SurfaceScripts.Abstract;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Apps.Abstract
{
    /// <summary>
    ///     Base das telas de energia: toma o lease do serviço compartilhado do grid, resolve paleta e medidas por quadro
    ///     e, quando <see cref="OwnsTitle"/>, desenha a moldura com a barra de título no lugar do cabeçalho padrão.
    /// </summary>
    internal abstract partial class EnergyAppBase : App
    {
        EnergyDataLease _lease;
        RectangleF _content;

        protected EnergyPalette Palette;
        protected EnergyMetrics Metrics;
        protected EnergyLayoutMode Mode;

        /// <summary>Área útil para o conteúdo: dentro da moldura e abaixo da barra de título.</summary>
        protected RectangleF Body;

        protected EnergyAppBase(IAppHost host) : base(host)
        {
        }

        public override IReadOnlyList<Control> VisualChildren { get; } = new Control[0];

        protected EnergySnapshot Snapshot => _lease != null ? _lease.Latest : EnergySnapshot.Empty;
        public bool HasData => Snapshot.HasData;
        public virtual bool OwnsTitle => false;
        public string EmptyMessage => Loc("NoUnits");

        /// <summary>Título alternativo, mais curto, usado quando o localizado não cabe inteiro na barra.</summary>
        protected virtual string ShortTitle => null;

        /// <summary>Cor de aviso legível sobre o painel, para a mensagem de tela vazia.</summary>
        public Color MessageColor => Palette.ValueAttention;

        public override bool HasVisibleItems()
        {
            return Snapshot.UnitsTotal > 0;
        }

        /// <summary>Energia só flui por juntas mecânicas e conectores acoplados: o vínculo físico vira elétrico aqui.</summary>
        public override void Update()
        {
            var module = LcdModSessionComponent.Client?.EnergyData;
            var requester = Host.GridLogic;
            if (module == null || requester == null)
                return;

            var linkType = (GridLinkTypeEnum)PowerComponent.GridLinkTypeInternal;
            if (linkType == GridLinkTypeEnum.Physical)
                linkType = GridLinkTypeEnum.Electrical;
            if (_lease != null && _lease.Service != null &&
                ReferenceEquals(_lease.Service.Requester, requester) && _lease.Service.LinkType == linkType)
                return;

            ReleaseLease();
            _lease = module.Capture(requester, linkType);
        }

        public override void Close()
        {
            ReleaseLease();
            base.Close();
        }

        /// <summary>Moldura e barra de título; devolve o topo da área de conteúdo para o caret da tela.</summary>
        public float DrawChrome(List<MySprite> sprites)
        {
            PrepareFrame();
            EnergyCardRenderer.DrawPanel(sprites, _content, ref Palette, ref Metrics);
            if (Host.TitleVisible)
            {
                var bar = new RectangleF(_content.X + Metrics.FrameLine, _content.Y + Metrics.FrameLine,
                    _content.Width - Metrics.FrameLine * 2f, Metrics.TitleBarHeight);
                EnergyCardRenderer.DrawTitleBar(sprites, Host.Surface, TextFont, bar, Host.Title, ShortTitle, ref Palette, ref Metrics);
            }

            return Body.Y;
        }

        protected void PrepareFrame()
        {
            RectangleF view = Host.ViewBox;
            float configuredScale = Host.ConfiguredScale;
            float fontSize = Host.Surface.FontSize;
            Metrics = EnergyMetrics.From(view, configuredScale, fontSize);
            Metrics.TitleBarHeight = Math.Max(Metrics.TitleBarHeight,
                FormatingHelper.LineHeight(Metrics.Text(EnergyTheme.TITLE), Host.Surface, TextFont) / 0.9f);

            float topInset = !OwnsTitle && Host.TitleVisible ? SurfaceScriptBase.TITLE_BAR_HEIGHT_BASE * configuredScale * fontSize : 0f;
            _content = new RectangleF(view.X, view.Y + topInset, view.Width, Math.Max(1f, view.Height - topInset));
            if (OwnsTitle)
            {
                float inset = Metrics.FrameLine + Metrics.Padding;
                float top = _content.Y + Metrics.FrameLine +
                            (Host.TitleVisible ? Metrics.TitleBarHeight + Metrics.Padding * 0.5f : Metrics.Padding);
                Body = new RectangleF(_content.X + inset, top, Math.Max(1f, _content.Width - inset * 2f),
                    Math.Max(1f, _content.Bottom - inset - top));
            }
            else
            {
                Body = _content;
            }

            Mode = EnergyTheme.LayoutMode(Body,
                FormatingHelper.LineHeight(Metrics.ReferenceText(EnergyTheme.LABEL), Host.Surface, TextFont),
                FormatingHelper.LineHeight(Metrics.Text(EnergyTheme.LABEL), Host.Surface, TextFont));
            Palette = BuildPalette();
        }

        /// <summary>
        ///     Cores do tema ajustadas ao fundo real onde serão desenhadas. Só o painel opaco força contraste mínimo: sobre o
        ///     fundo da tela (gráfico e LCD transparente) valem as cores como as outras telas do mod as usam, inclusive as
        ///     escolhidas pelo usuário.
        /// </summary>
        EnergyPalette BuildPalette()
        {
            Color background = Host.BackgroundColor;
            Color header = GetHeaderColor();
            Color accent = ResolveResource(ThemeResources.AccentColor, header);
            Color onSurface = ResolveResource(ThemeResources.OnSurfaceColor, Host.ForegroundColor);
            bool paintPanel = OwnsTitle && !ScreenAreaGeometry.IsTransparentScreenArea(Host.Block, Host.SurfaceIndex);
            Color panel = paintPanel ? new Color(ResolveResource(ThemeResources.SurfaceColor, background), 1f) : new Color(0, 0, 0, 0);
            Color backdrop = paintPanel ? panel : background;
            Color label = OnBackdrop(onSurface, paintPanel, panel);
            return new EnergyPalette
            {
                Backdrop = backdrop,
                Panel = panel,
                Frame = accent,
                Bracket = OnBackdrop(header, paintPanel, panel),
                Label = label,
                LabelMuted = Color.Lerp(label, backdrop, 0.35f).EnsureMinimalContrast(backdrop),
                ValueNeutral = OnBackdrop(accent, paintPanel, panel),
                ValueOk = OnBackdrop(ResolveResource(ThemeResources.SuccessColor, header), paintPanel, panel),
                ValueAttention = OnBackdrop(GetWarningColor(), paintPanel, panel),
                ValueAlert = OnBackdrop(GetErrorColor(), paintPanel, panel),
                Divider = ResolveResource(ThemeResources.DividerColor, onSurface).EnsureMinimalContrast(backdrop, 1.5),
                BoxFrame = ResolveResource(ThemeResources.BorderVariantColor, onSurface).EnsureMinimalContrast(backdrop, 1.5),
                GaugeTrack = ResolveResource(ThemeResources.SurfaceContainerHighestColor, onSurface),
                PlotBackground = ResolveResource(ThemeResources.SurfaceContainerLowColor, background),
                ChartUsed = ResolveResource(ThemeResources.OnSurfaceVariantColor, onSurface).EnsureMinimalContrast(backdrop)
            };
        }

        static Color OnBackdrop(Color color, bool paintPanel, Color panel)
        {
            return paintPanel ? color.EnsureMinimalContrast(panel) : color;
        }

        void ReleaseLease()
        {
            if (_lease == null)
                return;

            _lease.Dispose();
            _lease = null;
        }

        protected static string Loc(string suffix)
        {
            return LocHelper.GetLoc(MOD_PREFIX + "Energy_" + suffix);
        }

        protected static string NotAvailable => LocHelper.GetLoc(MOD_PREFIX + "NotAvailable");

        /// <summary>Valor em MW mais largo do formato (três dígitos e dois decimais): modelo de largura das leituras de potência.</summary>
        protected static string WidestPower => FormatingHelper.MegaWattsToString(888.88f);

        static readonly string[] PowerFormats = { "0", "0.#", "0.##" };

        protected float MinWidth(string text, bool bold)
        {
            return FormatingHelper.GetSizeInPixel(text, bold ? FormatingHelper.BoldFont(TextFont, text) : TextFont, EnergyMetrics.MIN_TEXT, Host.Surface).X;
        }

        /// <summary>Potência no formato do mod; se não couber em <paramref name="maxWidth"/> na escala mínima, com menos casas, para nunca perder a unidade.</summary>
        protected string PowerText(float megaWatts, bool signed, float maxWidth)
        {
            string text = null;
            for (int decimals = PowerFormats.Length - 1; decimals >= 0; decimals--)
            {
                text = FormatingHelper.WattsToString(megaWatts * 1e6, PowerFormats[decimals]);
                if (signed && megaWatts > EnergySnapshot.EPSILON_MW)
                    text = "+" + text;
                if (MinWidth(text, true) <= maxWidth)
                    break;
            }

            return text;
        }

        /// <summary>Linha de potência que reserva a largura de <see cref="WidestPower"/> e perde casas para caber em <paramref name="room"/> na escala mínima, sem perder a unidade.</summary>
        protected EnergyRow PowerRow(string label, float megaWatts, Color color, float room)
        {
            return new EnergyRow(label, PowerText(megaWatts, false, room), color, -1f, WidestPower);
        }

        protected static string KindName(EnergyKind kind)
        {
            switch (kind)
            {
                case EnergyKind.Battery:
                    return MyTexts.GetString("DisplayName_BlockGroup_Batteries");
                case EnergyKind.Solar:
                    return MyTexts.GetString("DisplayName_BlockGroup_SolarPanels");
                case EnergyKind.Wind:
                    return MyTexts.GetString("DisplayName_BlockGroup_WindTurbines");
                case EnergyKind.Reactor:
                    return MyTexts.GetString("DisplayName_BlockGroup_Reactors");
                case EnergyKind.Hydrogen:
                    return MyTexts.GetString("DisplayName_BlockGroup_HydrogenEngines");
                default:
                    return Loc("Other");
            }
        }

        protected static string StateText(EnergyUnitState state)
        {
            switch (state)
            {
                case EnergyUnitState.Online:
                    return Loc("Online");
                case EnergyUnitState.Warning:
                    return Loc("Warning");
                case EnergyUnitState.Offline:
                    return Loc("Offline");
                case EnergyUnitState.Inoperative:
                    return Loc("Inoperative");
                default:
                    return Loc("NoUnits");
            }
        }

        protected static string BatteryStatusText(EnergyBatteryStatus status)
        {
            switch (status)
            {
                case EnergyBatteryStatus.Idle:
                    return Loc("Idle");
                case EnergyBatteryStatus.Charging:
                    return MyTexts.GetString("HudEnergyGroupCharging");
                case EnergyBatteryStatus.Discharging:
                    return Loc("Discharging");
                case EnergyBatteryStatus.Full:
                    return MyTexts.GetString("RadialMenuAction_Signal_Full");
                default:
                    return NotAvailable;
            }
        }

        protected static string BatteryModeText(EnergyBatteryMode mode)
        {
            switch (mode)
            {
                case EnergyBatteryMode.Auto:
                    return MyTexts.GetString("BlockPropertyTitle_Auto");
                case EnergyBatteryMode.Recharge:
                    return MyTexts.GetString("BlockPropertyTitle_Recharge");
                case EnergyBatteryMode.Discharge:
                    return MyTexts.GetString("BlockPropertyTitle_Discharge");
                case EnergyBatteryMode.Mixed:
                    return Loc("Mixed");
                default:
                    return NotAvailable;
            }
        }

        protected Color BatteryStatusColor(EnergyBatteryStatus status)
        {
            switch (status)
            {
                case EnergyBatteryStatus.Charging:
                case EnergyBatteryStatus.Full:
                    return Palette.ValueOk;
                case EnergyBatteryStatus.Discharging:
                    return Palette.ValueAttention;
                case EnergyBatteryStatus.Idle:
                    return Palette.ValueNeutral;
                default:
                    return Palette.LabelMuted;
            }
        }

        /// <summary>Previsão compacta: segundos ou minutos, HH:MM:SS até dois dias, depois "2d 03h" e, acima de um ano, "> 365d".</summary>
        protected static string EtaText(float seconds)
        {
            if (seconds < 0f)
                return NotAvailable;
            if (seconds > 365f * 86400f)
                return "> 365d";
            if (seconds < 48f * 3600f)
                return FormatingHelper.FormatTimeHours(seconds / 3600f);

            var span = TimeSpan.FromSeconds(seconds);
            return span.Days + "d " + span.Hours.ToString("D2") + "h";
        }

        /// <summary>Linha "Cheia em" ou "Vazia em" conforme o status, com a previsão compacta.</summary>
        protected EnergyRow EtaRow(EnergySnapshot s)
        {
            return new EnergyRow(Loc(s.BatteryStatus == EnergyBatteryStatus.Charging ? "FullIn" : "EmptyIn"), EtaText(s.EtaSeconds),
                s.EtaSeconds >= 0f ? Palette.ValueNeutral : Palette.LabelMuted, -1f, "88:88:88");
        }

        protected static string WorkingText(int working, int total)
        {
            return string.Format(Loc("Working"), working, total);
        }
    }
}
