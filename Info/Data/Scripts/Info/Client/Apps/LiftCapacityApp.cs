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
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Apps
{
    internal sealed partial class LiftCapacityApp : App
    {
        const float SAFETY_FACTOR = 0.85f;
        const float TITLE_HEIGHT_BASE = 40f;
        const float GAP_BASE = 6f;
        const float FOOTER_HEIGHT_BASE = 30f;
        const float BAR_HEIGHT_BASE = 6f;
        const float WIDE_ASPECT = 2.5f;
        const float TOP_CARDS_HEIGHT_RATIO = 0.28f;
        const float SIDE_COLUMN_WIDTH_RATIO = 0.38f;
        const double STANDARD_GRAVITY = 9.81d;
        const string ICON_GRAVITY = "IconTemperature";
        const string ICON_MASS = "IconEnergy";
        public const int STATE_SAFE = 0;
        public const int STATE_WARN = 1;
        public const int STATE_CRIT = 2;

        readonly double[] _maxThrust = new double[6];

        public bool HasData { get; private set; }
        public bool HasGravity { get; private set; }
        public int State { get; private set; }
        public double GravityG { get; private set; }
        public double ShipMassKg { get; private set; }
        public double LiftForceN { get; private set; }
        public double Usage { get; private set; }
        public double MaxMassKg { get; private set; }
        public double SafeMaxKg { get; private set; }
        public double CanCarryKg { get; private set; }

        public override IReadOnlyList<Control> VisualChildren { get; } = new Control[] { };

        RectangleF ViewBox => Host.ViewBox;
        Color ForegroundColor => Host.ForegroundColor;
        Color BackgroundColor => Host.BackgroundColor;
        float LayoutScale => Math.Max(0.25f, GeneralComponent.GetScale()) * Math.Max(0.25f, Host.FontScale);

        public LiftCapacityApp(IAppHost host) : base(host)
        {
            NeedGridData(GridCapability.Blocks);
        }

        public override void Update()
        {
            Array.Clear(_maxThrust, 0, _maxThrust.Length);
            bool hasThrust = false;
            HasGravity = false;

            try
            {
                var gridLogic = Host.GridLogic;
                if (gridLogic != null)
                    hasThrust = LiftPhysics.AccumulateMaxThrust(gridLogic.Blocks.Thrusts, _maxThrust, null);

                Vector3D gravity;
                HasGravity = LiftPhysics.TryGetNaturalGravity(Host.Block, out gravity);

                double mass;
                ShipMassKg = LiftPhysics.TryGetShipMass(Host.Block, out mass) ? mass : 0d;

                if (HasGravity)
                {
                    double g = gravity.Length();
                    GravityG = g / STANDARD_GRAVITY;
                    LiftForceN = LiftPhysics.AvailableForce(_maxThrust, -gravity / g, Host.Block.CubeGrid.WorldMatrix);
                    MaxMassKg = LiftForceN / g;
                    SafeMaxKg = MaxMassKg * SAFETY_FACTOR;
                    CanCarryKg = SafeMaxKg - ShipMassKg;
                    Usage = MaxMassKg > 0d ? ShipMassKg / MaxMassKg : 1d;
                    State = ShipMassKg <= SafeMaxKg ? STATE_SAFE : ShipMassKg <= MaxMassKg ? STATE_WARN : STATE_CRIT;
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"[LcdMod] LiftCapacity error: {ex.Message}");
            }

            HasData = hasThrust || HasGravity;
        }

        void DrawFramed(List<MySprite> sprites)
        {
            var f = Host.Frame;
            Color fg = ForegroundColor;
            Color accent = GetHeaderColor();
            Color stateColor = !HasGravity ? fg : State == STATE_CRIT ? GetErrorColor() : State == STATE_WARN ? GetWarningColor() : accent;
            string na = LocHelper.GetLoc(MOD_PREFIX + "NotAvailable");
            f.Header(sprites, Host.Block, 0, accent);
            f.Text(sprites, "gravidade.valor", HasGravity ? GravityG.ToString("0.00", FormatingHelper.Culture) + " g" : na, fg);
            f.Text(sprites, "massa_da_nave.valor", ShipMassKg > 0d ? FormatingHelper.MassToString(ShipMassKg) : na, fg);
            f.Text(sprites, "empuxo_de_elevacao.valor", HasGravity ? FormatingHelper.NewtonForceToString(LiftForceN) : na, fg);
            f.Bar(sprites, "uso_de_elevacao.barra", HasGravity ? (float)MathHelper.Clamp(Usage, 0d, 1d) : 0f, stateColor);
            f.Text(sprites, "uso_de_elevacao.valor", HasGravity ? FormatingHelper.PercentageToString((float)Usage) : na, stateColor);
            f.Text(sprites, "massa_maxima.valor", HasGravity ? FormatingHelper.MassToString(MaxMassKg) : na, fg);
            f.Text(sprites, "max_seguro.valor", HasGravity ? FormatingHelper.MassToString(SafeMaxKg) : na, fg);
            f.Text(sprites, "carga.capacidade", HasGravity ? FormatingHelper.MassToString(CanCarryKg) : na, fg);
            f.Text(sprites, "carga.estado", !HasGravity ? Loc("NoGravity") : State == STATE_CRIT ? Loc("Crit") : State == STATE_WARN ? Loc("Warn") : Loc("Safe"), stateColor);
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
            var content = new RectangleF(view.X + gap, view.Y + titleOffset + gap, view.Width - gap * 2f, Math.Max(0f, view.Height - titleOffset - gap * 2f));

            Color fg = ForegroundColor;
            Color accent = GetHeaderColor();
            Color stateColor = !HasGravity ? fg : State == STATE_CRIT ? GetErrorColor() : State == STATE_WARN ? GetWarningColor() : accent;
            string na = LocHelper.GetLoc(MOD_PREFIX + "NotAvailable");

            string gravityText = HasGravity ? GravityG.ToString("0.00", FormatingHelper.Culture) + " g" : na;
            string massText = ShipMassKg > 0d ? FormatingHelper.MassToString(ShipMassKg) : na;
            float footerHeight = FOOTER_HEIGHT_BASE * ls;
            RectangleF cardA, cardB, rowsArea, footer;

            if (content.Width / Math.Max(1f, content.Height) >= WIDE_ASPECT)
            {
                float sideWidth = content.Width * SIDE_COLUMN_WIDTH_RATIO;
                float bodyHeight = content.Height - footerHeight - gap;
                float cardHeight = (bodyHeight - gap) * 0.5f;
                cardA = new RectangleF(content.X, content.Y, sideWidth, cardHeight);
                cardB = new RectangleF(content.X, content.Y + cardHeight + gap, sideWidth, cardHeight);
                rowsArea = new RectangleF(content.X + sideWidth + gap, content.Y, content.Width - sideWidth - gap, bodyHeight);
                footer = new RectangleF(content.X, content.Bottom - footerHeight, content.Width, footerHeight);
            }
            else
            {
                float cardHeight = content.Height * TOP_CARDS_HEIGHT_RATIO;
                float cardWidth = (content.Width - gap) * 0.5f;
                cardA = new RectangleF(content.X, content.Y, cardWidth, cardHeight);
                cardB = new RectangleF(content.X + cardWidth + gap, content.Y, cardWidth, cardHeight);
                float rowsTop = content.Y + cardHeight + gap;
                rowsArea = new RectangleF(content.X, rowsTop, content.Width, content.Bottom - footerHeight - gap - rowsTop);
                footer = new RectangleF(content.X, content.Bottom - footerHeight, content.Width, footerHeight);
            }

            DrawTile(sprites, cardA, ICON_GRAVITY, Loc("Gravity"), gravityText, fg, accent, ls);
            DrawTile(sprites, cardB, ICON_MASS, Loc("ShipMass"), massText, fg, accent, ls);

            float rowHeight = (rowsArea.Height - gap * 3f) / 4f;
            float usage = HasGravity ? (float)MathHelper.Clamp(Usage, 0d, 1d) : 0f;
            DrawRow(sprites, RowRect(rowsArea, 0, rowHeight, gap), Loc("LiftThrust"), HasGravity ? FormatingHelper.NewtonForceToString(LiftForceN) : na, -1f, fg, accent, ls);
            DrawRow(sprites, RowRect(rowsArea, 1, rowHeight, gap), Loc("LiftUsage"), HasGravity ? FormatingHelper.PercentageToString((float)Usage) : na, usage, fg, stateColor, ls);
            DrawRow(sprites, RowRect(rowsArea, 2, rowHeight, gap), Loc("MaxMass"), HasGravity ? FormatingHelper.MassToString(MaxMassKg) : na, -1f, fg, accent, ls);
            DrawRow(sprites, RowRect(rowsArea, 3, rowHeight, gap), Loc("SafeMax"), HasGravity ? FormatingHelper.MassToString(SafeMaxKg) : na, -1f, fg, accent, ls);

            DrawFooter(sprites, footer, fg, stateColor, ls);
            return sprites;
        }

        static RectangleF RowRect(RectangleF area, int index, float rowHeight, float gap)
        {
            return new RectangleF(area.X, area.Y + index * (rowHeight + gap), area.Width, rowHeight);
        }

        string Loc(string key)
        {
            string text;
            if (!_loc.TryGetValue(key, out text))
                _loc[key] = text = LocHelper.GetLoc(MOD_PREFIX + "Lift_" + key);
            return text;
        }

        void DrawFrame(List<MySprite> sprites, RectangleF rect, Color fg, Color accent, float ls)
        {
            if (!ThemeChrome.DrawCard(sprites, Theme, rect, BackgroundColor, accent, fg, ls))
                sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, Color.Lerp(BackgroundColor, fg, 0.08f)));
        }

        float DrawIconCell(List<MySprite> sprites, RectangleF rect, string icon, Color fg, Color accent, float ls)
        {
            float cellWidth = Math.Min(rect.Height * 0.8f, rect.Width * 0.3f);
            var cell = new RectangleF(rect.X, rect.Y, cellWidth, rect.Height);
            ThemeChrome.DrawIconCell(sprites, Theme, cell, accent, ls);
            float iconSize = Math.Min(cellWidth, rect.Height) * 0.6f;
            sprites.Add(new MySprite(SpriteType.TEXTURE, icon, cell.Center, new Vector2(iconSize, iconSize), fg, null, TextAlignment.CENTER));
            return cell.Right;
        }

        void DrawTile(List<MySprite> sprites, RectangleF rect, string icon, string title, string value, Color fg, Color accent, float ls)
        {
            DrawFrame(sprites, rect, fg, accent, ls);
            float pad = 4f * ls;
            float left = DrawIconCell(sprites, rect, icon, fg, accent, ls) + pad * 2f;
            float width = Math.Max(1f, rect.Right - pad * 2f - left);

            float labelScale = 0.45f * ls;
            string label = FormatingHelper.FitText(TextSurface, ThemeChrome.LabelText(Theme, title), TextFont, labelScale, width, 0f, 0.3f, out labelScale);
            float labelHeight = MeasureLineHeight(labelScale);
            sprites.Add(new MySprite(SpriteType.TEXT, label, new Vector2(left, rect.Y + pad), null, new Color(fg, 0.7f), TextFont, TextAlignment.LEFT, labelScale));

            float valueHeight = Bare.LineHeight(1f);
            Bare.Text(sprites, value, new Vector2(left, rect.Bottom - pad - valueHeight), TextAlignment.LEFT, 1f, fg, width, Math.Max(1f, rect.Height - labelHeight - pad * 3f));
        }

        void DrawRow(List<MySprite> sprites, RectangleF rect, string label, string value, float fraction, Color fg, Color fill, float ls)
        {
            DrawFrame(sprites, rect, fg, fill, ls);
            float pad = 4f * ls;
            float left = rect.X + pad * 3f;
            float right = rect.Right - pad * 3f;
            float width = Math.Max(1f, right - left);
            float scale = 0.5f * ls;
            bool valueFits = Bare.Fits(value, 1f, width, rect.Height);
            float textHeight = Math.Max(MeasureLineHeight(scale), valueFits ? Bare.LineHeight(1f) : 0f);
            bool hasBar = fraction >= 0f;
            float barHeight = Math.Max(BAR_HEIGHT_BASE * ls, rect.Height - textHeight - pad * 2.5f);
            float textTop = hasBar ? rect.Y + pad : rect.Center.Y - textHeight * 0.5f;

            float valueWidth = valueFits ? MeasureText(value, Host.FontScale).X : 0f;
            string labelText = FormatingHelper.FitText(TextSurface, ThemeChrome.LabelText(Theme, label), TextFont, scale, Math.Max(1f, width - valueWidth - pad), 0f, 0.3f, out scale);
            sprites.Add(new MySprite(SpriteType.TEXT, labelText, new Vector2(left, textTop), null, new Color(fg, 0.7f), TextFont, TextAlignment.LEFT, scale));
            Bare.Text(sprites, value, new Vector2(right, textTop), TextAlignment.RIGHT, 1f, fg, width, rect.Height);

            if (!hasBar) return;
            float barTop = textTop + textHeight + pad * 0.5f;
            if (barTop + barHeight > rect.Bottom - pad * 0.5f) return;
            var bar = new RectangleF(left, barTop, width, barHeight);
            ThemeChrome.DrawBar(sprites, Theme, bar, fraction, fill, fg, ls);
        }

        void DrawFooter(List<MySprite> sprites, RectangleF rect, Color fg, Color stateColor, float ls)
        {
            if (!ThemeChrome.DrawFooterBand(sprites, Theme, rect, BackgroundColor, stateColor, fg, ls))
                sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, Color.Lerp(BackgroundColor, fg, 0.08f)));

            float pad = 8f * ls;
            float scale = 0.55f * ls;
            float textHeight = MeasureLineHeight(scale);
            float textTop = rect.Center.Y - textHeight * 0.5f;

            string state = !HasGravity ? Loc("NoGravity") : State == STATE_CRIT ? Loc("Crit") : State == STATE_WARN ? Loc("Warn") : Loc("Safe");
            string stateFont = ThemeChrome.TitleFont(Theme, TextFont, state);
            sprites.Add(new MySprite(SpriteType.TEXT, state, new Vector2(rect.Right - pad, textTop), null, stateColor, stateFont, TextAlignment.RIGHT, scale));
            if (!HasGravity) return;

            string carry = ThemeChrome.LabelText(Theme, Loc("CanCarry")) + ": " + FormatingHelper.MassToString(CanCarryKg);
            float stateWidth = MeasureText(state, scale).X;
            string carryText = FormatingHelper.FitText(TextSurface, carry, TextFont, scale, Math.Max(1f, rect.Width - pad * 3f - stateWidth), 0f, 0.3f, out scale);
            sprites.Add(new MySprite(SpriteType.TEXT, carryText, new Vector2(rect.X + pad, textTop), null, fg, TextFont, TextAlignment.LEFT, scale));
        }
    }
}
