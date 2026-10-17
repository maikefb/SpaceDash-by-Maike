using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Generation;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Apps
{
    public sealed partial class GasApp : App
    {
        const float RATE_SMOOTHING = 0.25f;
        const double LOW_ATTENTION = 0.2d;
        const double LOW_CRITICAL = 0.05d;
        const float TITLE_HEIGHT_BASE = 40f;
        const float GAP_BASE = 6f;
        const float STATUS_HEIGHT_BASE = 28f;
        const float STATUS_MIN_CARD_BASE = 72f;
        const float ONE_LINE_CARD_BASE = 56f;
        const float BAR_HEIGHT_BASE = 6f;
        const float WIDE_ASPECT = 1.3f;
        const int CARD_COUNT = 8;
        const string ICON_H2 = "IconHydrogen";
        const string ICON_O2 = "IconOxygen";
        const string ICON_ICE = "MyObjectBuilder_Ore/Ice";
        const string ICON_BOTTLE = "MyObjectBuilder_GasContainerObject/HydrogenBottle";
        const string ICON_FALLBACK = "IconEnergy";
        static readonly string[] LABEL_ICONS = { ICON_H2, ICON_O2, ICON_H2, ICON_O2, ICON_H2, ICON_O2, ICON_O2, ICON_BOTTLE };
        public const int STATUS_NOMINAL = 0;
        public const int STATUS_ATTENTION = 1;
        public const int STATUS_CRITICAL = 2;

        sealed class GasStat
        {
            public double Stored, Capacity, RateLps, ProdLps;
            public int Tanks, Stockpile, AutoRefill, LowAttention, LowCritical;
            public bool HasRate;
            public double PreviousStored;

            public void Reset()
            {
                PreviousStored = Stored;
                Stored = 0d;
                Capacity = 0d;
                ProdLps = 0d;
                Tanks = Stockpile = AutoRefill = LowAttention = LowCritical = 0;
            }

            public void UpdateRate(double seconds)
            {
                if (seconds <= 0d) return;
                double raw = (Stored - PreviousStored) / seconds;
                RateLps = HasRate ? RateLps + RATE_SMOOTHING * (raw - RateLps) : raw;
                HasRate = true;
            }

            public double Level => Capacity > 0d ? Stored / Capacity : 0d;
            public double Consumption => ProdLps - RateLps;
        }

        sealed class GenInfo
        {
            public double H2Lps, O2Lps;
        }

        struct Row
        {
            public string Label, Value;
            public float Fraction;
            public Color Fill;
        }

        readonly GasStat _h2 = new GasStat();
        readonly GasStat _o2 = new GasStat();
        readonly Dictionary<MyDefinitionId, string> _tankGas = new Dictionary<MyDefinitionId, string>();
        readonly Dictionary<MyDefinitionId, GenInfo> _genInfo = new Dictionary<MyDefinitionId, GenInfo>();
        readonly LinkedTypedBlockSourceSet<IMyTerminalBlock> _blocks =
            new LinkedTypedBlockSourceSet<IMyTerminalBlock>(delegate(TypedBlockCollection blocks)
            {
                return blocks.TerminalBlocks;
            });
        static readonly MyDefinitionId IceId = new MyDefinitionId(typeof(MyObjectBuilder_Ore), "Ice");
        readonly Row[] _rows = new Row[2];

        int _gens, _gensActive, _gensAutoRefill;
        double _iceInGens, _iceOnGrid, _genVolumeUsed, _genVolumeMax;
        long _lastFrame;
        string _genIcon, _tankIcon;

        public int Status { get; private set; }
        public bool HasData { get; private set; }

        public override IReadOnlyList<Control> VisualChildren { get; } = new Control[] { };

        RectangleF ViewBox => Host.ViewBox;
        Color ForegroundColor => Host.ForegroundColor;
        Color BackgroundColor => Host.BackgroundColor;
        float LayoutScale => Math.Max(0.25f, GeneralComponent.GetScale()) * Math.Max(0.25f, Host.FontScale);

        public GasApp(IAppHost host) : base(host)
        {
            NeedGridData(GridCapability.Blocks);
        }

        public override void Update()
        {
            var gridLogic = Host.GridLogic;
            if (gridLogic == null)
            {
                HasData = false;
                return;
            }

            _h2.Reset();
            _o2.Reset();
            _gens = _gensActive = _gensAutoRefill = 0;
            _iceInGens = _iceOnGrid = _genVolumeUsed = _genVolumeMax = 0d;

            try
            {
                // ponytail: varre TerminalBlocks a cada Update; ok até ~2k blocos, mover para GridCapability.Items se pesar.
                _blocks.Bind(gridLogic, (GridLinkTypeEnum)BlockSelectionComponent.GridLinkTypeInternal);
                var sources = _blocks.Sources;
                for (int s = 0; s < sources.Count; s++)
                {
                    var list = sources[s];
                    for (int i = 0; i < list.Count; i++)
                    {
                        var block = list[i];
                        if (block == null) continue;

                        var tank = block as IMyGasTank;
                        if (tank != null)
                        {
                            ReadTank(tank);
                            continue;
                        }

                        var gen = block as IMyGasGenerator;
                        ReadInventories(block, gen != null);
                        if (gen != null)
                            ReadGenerator(gen);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"[LcdMod] GasDashboard error: {ex.Message}");
            }

            long frame = MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
            double seconds = _lastFrame > 0 && frame > _lastFrame ? (frame - _lastFrame) / 60d : 0d;
            _lastFrame = frame;
            _h2.UpdateRate(seconds);
            _o2.UpdateRate(seconds);

            HasData = _h2.Tanks + _o2.Tanks + _gens > 0;
            Status = ComputeStatus();
        }

        void ReadTank(IMyGasTank tank)
        {
            string gas;
            if (!_tankGas.TryGetValue(tank.BlockDefinition, out gas))
            {
                var def = MyDefinitionManager.Static.GetCubeBlockDefinition(tank.BlockDefinition) as MyGasTankDefinition;
                gas = def != null ? def.StoredGasId.SubtypeName : string.Empty;
                _tankGas[tank.BlockDefinition] = gas;
            }

            var stat = gas == "Hydrogen" ? _h2 : gas == "Oxygen" ? _o2 : null;
            if (stat == null) return;
            if (_tankIcon == null) _tankIcon = tank.BlockDefinition.ToString();

            double ratio = tank.FilledRatio;
            stat.Tanks++;
            stat.Capacity += tank.Capacity;
            stat.Stored += ratio * tank.Capacity;
            if (tank.Stockpile) stat.Stockpile++;
            if (tank.AutoRefillBottles) stat.AutoRefill++;
            if (ratio < LOW_CRITICAL) stat.LowCritical++;
            else if (ratio < LOW_ATTENTION) stat.LowAttention++;
        }

        void ReadGenerator(IMyGasGenerator gen)
        {
            _gens++;
            if (gen.AutoRefill) _gensAutoRefill++;
            if (_genIcon == null) _genIcon = gen.BlockDefinition.ToString();
            if (!gen.IsWorking) return;

            _gensActive++;
            var info = GetGenInfo(gen.BlockDefinition);
            _h2.ProdLps += info.H2Lps;
            _o2.ProdLps += info.O2Lps;
        }

        GenInfo GetGenInfo(MyDefinitionId id)
        {
            GenInfo info;
            if (_genInfo.TryGetValue(id, out info))
                return info;

            info = new GenInfo();
            try
            {
                var def = MyDefinitionManager.Static.GetCubeBlockDefinition(id) as MyOxygenGeneratorDefinition;
                if (def != null && def.ProducedGases != null)
                {
                    for (int i = 0; i < def.ProducedGases.Count; i++)
                    {
                        var gas = def.ProducedGases[i];
                        double lps = def.IceConsumptionPerSecond * gas.IceToGasRatio;
                        if (gas.Id.SubtypeName == "Hydrogen") info.H2Lps += lps;
                        else if (gas.Id.SubtypeName == "Oxygen") info.O2Lps += lps;
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"[LcdMod] GasDashboard generator definition error: {ex.Message}");
            }

            _genInfo[id] = info;
            return info;
        }

        void ReadInventories(IMyTerminalBlock block, bool isGenerator)
        {
            if (!block.HasInventory) return;
            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inv = block.GetInventory(i);
                if (inv == null) continue;
                double ice = (double)inv.GetItemAmount(IceId);
                if (isGenerator)
                {
                    _iceInGens += ice;
                    _genVolumeUsed += (double)inv.CurrentVolume;
                    _genVolumeMax += (double)inv.MaxVolume;
                }
                else
                {
                    _iceOnGrid += ice;
                }
            }
        }

        int ComputeStatus()
        {
            if (_h2.LowCritical + _o2.LowCritical > 0) return STATUS_CRITICAL;
            if ((_h2.ProdLps > 0d && _h2.Tanks == 0) || (_o2.ProdLps > 0d && _o2.Tanks == 0)) return STATUS_CRITICAL;
            if (_h2.LowAttention + _o2.LowAttention > 0) return STATUS_ATTENTION;
            if (_gens > 0 && _gensActive == 0) return STATUS_ATTENTION;
            return STATUS_NOMINAL;
        }

        /// <summary>Campo da moldura por cartão × linha; null quando a moldura não tem o dado (limite de reserva, nível de disparo).</summary>
        static readonly string[] FRAME_ROWS =
        {
            "h2_armazenado.nivel", "h2_armazenado.capacidade", "o2_armazenado.nivel", "o2_armazenado.capacidade",
            "geradores.ativos", "geradores.producao_total", "reserva_de_gelo.nivel", "reserva_de_gelo.capacidade",
            "saida_de_h2.vazao", "saida_de_h2.uso", "saida_de_o2.vazao", "saida_de_o2.uso",
            null, "estoque.total_armazenado", "reabastecimento.estado", null
        };

        void DrawFramed(List<MySprite> sprites)
        {
            var f = Host.Frame;
            Color fg = ForegroundColor;
            Color accent = GetHeaderColor();
            Color warn = GetWarningColor();
            Color error = GetErrorColor();
            string na = LocHelper.GetLoc(MOD_PREFIX + "NotAvailable");
            f.Header(sprites, Host.Block, BlockSelectionComponent.GridLinkTypeInternal, accent);
            for (int i = 0; i < CARD_COUNT; i++)
            {
                string icon, title;
                FillCard(i, out icon, out title, fg, accent, warn, error, na);
                for (int r = 0; r < 2; r++)
                {
                    string field = FRAME_ROWS[i * 2 + r];
                    if (field == null)
                        continue;
                    f.Text(sprites, field + ".valor", _rows[r].Value, fg);
                    f.Bar(sprites, field + ".barra", _rows[r].Fraction, _rows[r].Fill);
                }
            }

            string status = Status == STATUS_CRITICAL ? LocHelper.GetLoc(MOD_PREFIX + "Gas_Critical")
                : Status == STATUS_ATTENTION ? Loc("Attention") : Loc("Nominal");
            f.Text(sprites, "sistema.estado", status, Status == STATUS_CRITICAL ? error : Status == STATUS_ATTENTION ? warn : accent);
        }

        readonly List<MySprite> _sprites = new List<MySprite>();
        readonly Dictionary<string, string> _loc = new Dictionary<string, string>();

        public override void LayoutChanged()
        {
            base.LayoutChanged();
            _loc.Clear();
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = _sprites;
            sprites.Clear();
            if (Host.Frame != null)
            {
                DrawFramed(sprites);
                return sprites;
            }

            float ls = LayoutScale;
            float titleOffset = Host.TitleVisible ? TITLE_HEIGHT_BASE * ls : 0f;
            var view = ViewBox;
            float gap = GAP_BASE * ls;
            float width = view.Width;
            float height = Math.Max(0f, view.Height - titleOffset);
            float top = view.Y + titleOffset;

            int cols = width / Math.Max(1f, height) >= WIDE_ASPECT ? 4 : 2;
            int rows = CARD_COUNT / cols;
            float statusHeight = STATUS_HEIGHT_BASE * ls;
            float cardHeight = (height - statusHeight - gap * (rows + 2)) / rows;
            if (cardHeight < STATUS_MIN_CARD_BASE * ls)
            {
                statusHeight = 0f;
                cardHeight = (height - gap * (rows + 1)) / rows;
            }
            bool oneLine = cardHeight < ONE_LINE_CARD_BASE * ls;
            float cardWidth = (width - gap * (cols + 1)) / cols;

            Color fg = ForegroundColor;
            Color accent = GetHeaderColor();
            Color warn = GetWarningColor();
            Color error = GetErrorColor();
            string na = LocHelper.GetLoc(MOD_PREFIX + "NotAvailable");

            for (int i = 0; i < CARD_COUNT; i++)
            {
                var rect = new RectangleF(
                    view.X + gap + (i % cols) * (cardWidth + gap),
                    top + gap + (i / cols) * (cardHeight + gap),
                    cardWidth, cardHeight);
                string icon, title;
                FillCard(i, out icon, out title, fg, accent, warn, error, na);
                DrawCard(sprites, rect, icon, title, LABEL_ICONS[i], oneLine, fg, accent, ls);
            }

            if (statusHeight > 0f)
            {
                var rect = new RectangleF(view.X + gap, top + gap + rows * (cardHeight + gap), width - gap * 2f, statusHeight);
                DrawStatusBar(sprites, rect, fg, accent, warn, error, ls);
            }

            return sprites;
        }

        void FillCard(int index, out string icon, out string title, Color fg, Color accent, Color warn, Color error, string na)
        {
            switch (index)
            {
                case 0: FillStored(_h2, ICON_H2, "H2Stored", out icon, out title, fg, warn, error); return;
                case 1: FillStored(_o2, ICON_O2, "O2Stored", out icon, out title, fg, warn, error); return;
                case 2:
                    icon = _genIcon ?? ICON_FALLBACK;
                    title = Loc("Generators");
                    SetRow(0, Loc("Active"), _gensActive + " / " + _gens, Fraction(_gensActive, _gens), _gens > 0 && _gensActive == 0 ? warn : accent);
                    SetRow(1, Loc("Production"), FormatingHelper.VolumeToString(_h2.ProdLps + _o2.ProdLps) + "/s", Fraction(_gensActive, _gens), accent);
                    return;
                case 3:
                    icon = ICON_ICE;
                    title = Loc("IceReserve");
                    SetRow(0, Loc("IceInGenerators"), FormatingHelper.MassToString(_iceInGens), (float)Fraction(_genVolumeUsed, _genVolumeMax), _gens > 0 && _iceInGens <= 0d ? warn : accent);
                    SetRow(1, Loc("IceOnGrid"), FormatingHelper.MassToString(_iceOnGrid + _iceInGens), _iceOnGrid + _iceInGens > 0d ? 1f : 0f, fg);
                    return;
                case 4: FillOutput(_h2, ICON_H2, "H2Output", out icon, out title, fg, accent, warn, na); return;
                case 5: FillOutput(_o2, ICON_O2, "O2Output", out icon, out title, fg, accent, warn, na); return;
                case 6:
                {
                    icon = _tankIcon ?? ICON_O2;
                    title = Loc("Stockpile");
                    int tanks = _h2.Tanks + _o2.Tanks;
                    int stockpile = _h2.Stockpile + _o2.Stockpile;
                    double stored = _h2.Stored + _o2.Stored;
                    SetRow(0, Loc("StockpileTanks"), stockpile + " / " + tanks, Fraction(stockpile, tanks), accent);
                    SetRow(1, Loc("TotalStored"), FormatingHelper.VolumeToString(stored), (float)Fraction(stored, _h2.Capacity + _o2.Capacity), accent);
                    return;
                }
                default:
                {
                    icon = ICON_BOTTLE;
                    title = Loc("Refill");
                    int tanks = _h2.Tanks + _o2.Tanks;
                    int refill = _h2.AutoRefill + _o2.AutoRefill;
                    SetRow(0, Loc("GenAutoRefill"), _gensAutoRefill + " / " + _gens, Fraction(_gensAutoRefill, _gens), accent);
                    SetRow(1, Loc("TankAutoRefill"), refill + " / " + tanks, Fraction(refill, tanks), accent);
                    return;
                }
            }
        }

        void FillStored(GasStat stat, string iconId, string titleKey, out string icon, out string title, Color fg, Color warn, Color error)
        {
            icon = iconId;
            title = Loc(titleKey);
            float level = (float)stat.Level;
            Color fill = stat.LowCritical > 0 ? error : stat.LowAttention > 0 ? warn : fg;
            SetRow(0, Loc("Level"), FormatingHelper.PercentageToString(level), level, fill);
            SetRow(1, Loc("Capacity"), FormatingHelper.VolumeToString(stat.Capacity) + " (" + stat.Tanks + ")", stat.Tanks > 0 ? 1f : 0f, fg);
        }

        void FillOutput(GasStat stat, string iconId, string titleKey, out string icon, out string title, Color fg, Color accent, Color warn, string na)
        {
            icon = iconId;
            title = Loc(titleKey);
            string flow = stat.HasRate ? (stat.RateLps > 0d ? "+" : string.Empty) + FormatingHelper.VolumeToString(stat.RateLps) + "/s" : na;
            float flowFraction = stat.ProdLps > 0d ? (float)Math.Abs(stat.RateLps) / (float)stat.ProdLps : 0f;
            SetRow(0, Loc("Flow"), flow, flowFraction, stat.RateLps < 0d ? warn : accent);

            bool hasUsage = stat.HasRate && stat.ProdLps > 0d;
            float usage = hasUsage ? (float)Math.Max(0d, stat.Consumption / stat.ProdLps) : 0f;
            SetRow(1, Loc("Usage"), hasUsage ? FormatingHelper.PercentageToString(usage) : na, usage, usage > 1f ? warn : accent);
        }

        void SetRow(int index, string label, string value, float fraction, Color fill)
        {
            _rows[index].Label = label;
            _rows[index].Value = value;
            _rows[index].Fraction = MathHelper.Clamp(fraction, 0f, 1f);
            _rows[index].Fill = fill;
        }

        static float Fraction(double part, double total)
        {
            return total > 0d ? (float)(part / total) : 0f;
        }

        string Loc(string key)
        {
            string text;
            if (!_loc.TryGetValue(key, out text))
                _loc[key] = text = LocHelper.GetLoc(MOD_PREFIX + "Gas_" + key);
            return text;
        }

        void DrawCard(List<MySprite> sprites, RectangleF rect, string icon, string title, string labelIcon, bool oneLine, Color fg, Color accent, float ls)
        {
            if (!ThemeChrome.DrawCard(sprites, Theme, rect, BackgroundColor, accent, fg, ls))
                sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, Color.Lerp(BackgroundColor, fg, 0.08f)));

            float pad = 4f * ls;
            float cellWidth = Math.Min(rect.Height * 0.8f, rect.Width * 0.3f);
            var cell = new RectangleF(rect.X, rect.Y, cellWidth, rect.Height);
            ThemeChrome.DrawIconCell(sprites, Theme, cell, accent, ls);
            float iconSize = Math.Min(cellWidth, rect.Height) * 0.7f;
            sprites.Add(new MySprite(SpriteType.TEXTURE, icon, cell.Center, new Vector2(iconSize, iconSize), fg, null, TextAlignment.CENTER));

            float left = cell.Right + pad * 2f;
            float right = rect.Right - pad * 2f;
            float textWidth = Math.Max(1f, right - left);
            float titleMul = 0.6f * ls / Math.Max(0.25f, Host.FontScale);
            float titleHeight = Bare.LineHeight(titleMul);
            Bare.Label(sprites, ThemeChrome.LabelText(Theme, title), labelIcon, new RectangleF(left, rect.Y + pad, textWidth, titleHeight), fg, titleMul);

            int rowCount = oneLine ? 1 : 2;
            float rowsTop = rect.Y + pad + titleHeight;
            float rowHeight = (rect.Bottom - pad - rowsTop) / rowCount;
            float labelScale = 0.45f * ls;
            for (int i = 0; i < rowCount; i++)
            {
                var row = _rows[i];
                float rowTop = rowsTop + i * rowHeight;
                float textHeight = MeasureLineHeight(labelScale);
                float barHeight = Math.Max(BAR_HEIGHT_BASE * ls, rowHeight - textHeight - pad * 1.5f);
                string valueText = row.Value ?? string.Empty;
                float valueWidth = MeasureText(valueText, labelScale).X;
                string label = FormatingHelper.FitText(TextSurface, ThemeChrome.LabelText(Theme, row.Label), TextFont, labelScale, Math.Max(1f, textWidth - valueWidth - pad), 0f, 0.3f, out labelScale);
                sprites.Add(new MySprite(SpriteType.TEXT, label, new Vector2(left, rowTop), null, new Color(fg, 0.7f), TextFont, TextAlignment.LEFT, labelScale));
                sprites.Add(new MySprite(SpriteType.TEXT, valueText, new Vector2(right, rowTop), null, fg, TextFont, TextAlignment.RIGHT, labelScale));
                labelScale = 0.45f * ls;

                float barTop = rowTop + textHeight + pad * 0.5f;
                if (barTop + barHeight > rect.Bottom - pad * 0.5f) continue;
                var bar = new RectangleF(left, barTop, textWidth, barHeight);
                ThemeChrome.DrawBar(sprites, Theme, bar, row.Fraction, row.Fill, fg, ls);
            }
        }

        void DrawStatusBar(List<MySprite> sprites, RectangleF rect, Color fg, Color accent, Color warn, Color error, float ls)
        {
            Color statusColor = Status == STATUS_CRITICAL ? error : Status == STATUS_ATTENTION ? warn : accent;
            if (!ThemeChrome.DrawFooterBand(sprites, Theme, rect, BackgroundColor, statusColor, fg, ls))
                sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, Color.Lerp(BackgroundColor, fg, 0.08f)));

            float pad = 8f * ls;
            float dot = Math.Min(rect.Height * 0.4f, 12f * ls);
            sprites.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(rect.X + pad + dot * 0.5f, rect.Center.Y), new Vector2(dot, dot), statusColor, null, TextAlignment.CENTER));

            float scale = 0.5f * ls;
            float textHeight = MeasureLineHeight(scale);
            string label = ThemeChrome.LabelText(Theme, Loc("SystemStatus"));
            sprites.Add(new MySprite(SpriteType.TEXT, label, new Vector2(rect.X + pad * 2f + dot, rect.Center.Y - textHeight * 0.5f), null, new Color(fg, 0.7f), TextFont, TextAlignment.LEFT, scale));

            string status = Status == STATUS_CRITICAL ? LocHelper.GetLoc(MOD_PREFIX + "Gas_Critical")
                : Status == STATUS_ATTENTION ? Loc("Attention") : Loc("Nominal");
            string statusFont = ThemeChrome.TitleFont(Theme, TextFont, status);
            sprites.Add(new MySprite(SpriteType.TEXT, ThemeChrome.LabelText(Theme, status), new Vector2(rect.Right - pad, rect.Center.Y - textHeight * 0.5f), null, statusColor, statusFont, TextAlignment.RIGHT, scale));
        }

        public override void Close()
        {
            _blocks.Dispose();
            base.Close();
        }
    }
}
