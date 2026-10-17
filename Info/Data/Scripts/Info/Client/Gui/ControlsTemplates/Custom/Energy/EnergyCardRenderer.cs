using System;
using System.Collections.Generic;
using LcdMod.Client.Extensions;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace LcdMod.Client.Gui.ControlsTemplates.Custom.Energy
{
    public struct EnergyRow
    {
        public string Label;
        public string Value;
        public Color ValueColor;
        public float BarFraction;

        /// <summary>Texto cuja largura o valor reserva no mínimo, para a tabela não mudar de forma a cada amostra.</summary>
        public string WidthTemplate;

        public EnergyRow(string label, string value, Color valueColor, float barFraction = -1f, string widthTemplate = null)
        {
            Label = label;
            Value = value;
            ValueColor = valueColor;
            BarFraction = barFraction;
            WidthTemplate = widthTemplate;
        }
    }

    /// <summary>Moldura com cantoneiras, barra de título, tabela rótulo | valor, caixa de valor e ilustração dos painéis de energia.</summary>
    public static class EnergyCardRenderer
    {
        const float BAR_ROW_RATIO = 0.45f;
        /// <summary>Quanto rótulos e valores podem crescer além do tamanho natural quando a linha é alta.</summary>
        public const float MAX_GROWTH = 1.6f;

        /// <summary>Empilhar rótulo sobre valor quando isso deixa o valor ao menos 15 % maior que lado a lado.</summary>
        const float STACK_GAIN = 1.15f;

        /// <summary>Altura mínima, em U, que a ilustração reserva para o ícone antes de trocar a pílula por chip e tirar o selo.</summary>
        const float MIN_ICON_UNITS = 3f;

        public static void DrawPanel(List<MySprite> sprites, RectangleF rect, ref EnergyPalette palette, ref EnergyMetrics metrics)
        {
            if (metrics.Theme == LcdTheme.Clean)
                return;
            if (metrics.Theme == LcdTheme.Futuristic)
            {
                ThemeChrome.DrawChamferedBox(sprites, rect, palette.Panel, palette.Frame, palette.ValueAttention, metrics.S);
                return;
            }
            if (palette.Panel.A > 0)
                sprites.Add(LineRenderer.Rect(rect.Center, rect.Size, palette.Panel));
            LineRenderer.DrawRectOutline(sprites, rect, metrics.FrameLine, palette.Frame);
            LineRenderer.DrawCornerBrackets(sprites, rect, metrics.BracketArm, metrics.BracketThickness, palette.Bracket);
        }

        /// <summary>Título em caixa-alta e negrito entre dois blocos, ajustado ao tamanho dos rótulos.</summary>
        public static void DrawTitleBar(List<MySprite> sprites, IMyTextSurface surface, string font, RectangleF bar, string title,
            ref EnergyPalette palette, ref EnergyMetrics metrics)
        {
            bool clean = metrics.Theme == LcdTheme.Clean;
            bool futuristic = metrics.Theme == LcdTheme.Futuristic;
            bool plain = clean || futuristic;
            float blockWidth = plain ? 0f : metrics.U * 0.6f;
            float blockHeight = bar.Height * 0.4f;
            float left = bar.X + metrics.Padding;
            float right = bar.Right - metrics.Padding;
            if (!plain)
            {
                sprites.Add(LineRenderer.Rect(new Vector2(left + blockWidth * 0.5f, bar.Center.Y), new Vector2(blockWidth, blockHeight), palette.Frame));
                sprites.Add(LineRenderer.Rect(new Vector2(right - blockWidth * 0.5f, bar.Center.Y), new Vector2(blockWidth, blockHeight), palette.Frame));
            }

            float textLeft = plain ? left : left + blockWidth + metrics.Padding * 0.6f;
            float lineEnd = plain ? right : right - blockWidth - metrics.Padding * 0.6f;
            float maxWidth = lineEnd - textLeft - metrics.U;
            float scale;
            string upper = clean ? title ?? string.Empty : (title ?? string.Empty).ToUpperInvariant();
            string titleFont = clean ? font : FormatingHelper.BoldFont(font, upper);
            string text = FormatingHelper.FitText(surface, upper, titleFont, metrics.Text(EnergyTheme.TITLE), maxWidth, bar.Height * 0.9f,
                metrics.Text(EnergyTheme.LABEL), out scale);

            if (maxWidth > 0f)
            {
                Vector2 size = FormatingHelper.GetSizeInPixel(text, titleFont, scale, surface);
                if (futuristic)
                    sprites.Add(Text(text, new Vector2(bar.Center.X, bar.Center.Y - size.Y * 0.5f), scale, palette.Label, TextAlignment.CENTER, titleFont));
                else
                    sprites.Add(Text(text, new Vector2(textLeft, bar.Center.Y - size.Y * 0.5f), scale, palette.Label, TextAlignment.LEFT, titleFont));

                float lineStart = textLeft + size.X + metrics.Padding;
                if (!plain && lineEnd - lineStart >= metrics.U)
                    sprites.Add(LineRenderer.Rect(new Vector2((lineStart + lineEnd) * 0.5f, bar.Center.Y), new Vector2(lineEnd - lineStart, metrics.DividerThickness), palette.Divider));
            }

            sprites.Add(LineRenderer.Rect(new Vector2(bar.Center.X, bar.Bottom - metrics.DividerThickness * 0.5f),
                new Vector2(bar.Width - metrics.Padding * 2f, metrics.DividerThickness), futuristic ? palette.Frame : palette.Divider));
        }

        /// <summary>Medidas de uma tabela para as primeiras <see cref="Measured"/> linhas.</summary>
        struct TablePlan
        {
            public int Measured;
            public int Rows;
            public int Capacity;
            public bool Stacked;
            public float LabelWidth;
            public float ValueWidth;
        }

        /// <summary>
        ///     Tabela rótulo | valor (valor em negrito). Rótulos e valores usam a mesma razão de ajuste (o valor nunca fica
        ///     menor que o rótulo); o divisor fica na largura do maior rótulo. Quando nem na escala mínima os dois cabem lado a
        ///     lado, cada linha empilha o rótulo sobre o valor. Linhas que não cabem na altura saem do fim; as larguras são
        ///     medidas só nas linhas desenhadas.
        /// </summary>
        public static void DrawValueTable(List<MySprite> sprites, IMyTextSurface surface, string font, RectangleF area, List<EnergyRow> rows,
            ref EnergyPalette palette, ref EnergyMetrics metrics, float maxGrowth = MAX_GROWTH)
        {
            if (rows.Count == 0 || area.Height <= 0f || area.Width <= 0f)
                return;

            float pad = metrics.Padding;
            float unitLine = FormatingHelper.LineHeight(1f, surface, font);
            float labelNatural = metrics.Text(EnergyTheme.LABEL);
            float valueNatural = Math.Max(metrics.Text(EnergyTheme.VALUE), labelNatural * (EnergyTheme.VALUE / EnergyTheme.LABEL));

            var plan = Plan(surface, font, area, rows, rows.Count, labelNatural, valueNatural, unitLine, pad, metrics.U, maxGrowth);
            while (plan.Rows < plan.Measured)
                plan = Plan(surface, font, area, rows, plan.Rows, labelNatural, valueNatural, unitLine, pad, metrics.U, maxGrowth);
            if (plan.Capacity > plan.Measured && plan.Measured < rows.Count)
            {
                int grown = Math.Min(rows.Count, plan.Capacity);
                var candidate = Plan(surface, font, area, rows, grown, labelNatural, valueNatural, unitLine, pad, metrics.U, maxGrowth);
                if (candidate.Rows == grown)
                    plan = candidate;
            }

            int rowCount = plan.Rows;
            bool anyBar = AnyBar(rows, rowCount);
            float rowHeight = area.Height / rowCount;
            bool futuristic = metrics.Theme == LcdTheme.Futuristic;
            float barHeight = anyBar ? Math.Max(2f, rowHeight * BAR_ROW_RATIO) : 0f;
            float textArea = rowHeight - (anyBar ? barHeight + pad * 0.9f : 0f);
            float stackFit = Math.Min((area.Width - pad) / Math.Max(1f, Math.Max(plan.LabelWidth, plan.ValueWidth)),
                textArea / (unitLine * (labelNatural + valueNatural)));
            float common = Math.Min(1f, stackFit);
            float width = area.Width - pad * 4f;
            float heightCap = textArea * 0.85f / unitLine;
            float ratio = Math.Min(maxGrowth, width / Math.Max(1f, plan.LabelWidth + plan.ValueWidth));
            float labelScale = Math.Min(labelNatural * ratio, heightCap);
            float valueScale = Math.Min(valueNatural * ratio, heightCap);
            if (labelScale < EnergyMetrics.MIN_TEXT)
            {
                labelScale = EnergyMetrics.MIN_TEXT;
                float rest = width - plan.LabelWidth * (EnergyMetrics.MIN_TEXT / labelNatural);
                valueScale = Math.Min(valueScale, valueNatural * rest / Math.Max(1f, plan.ValueWidth));
            }

            valueScale = Math.Max(EnergyMetrics.MIN_TEXT, valueScale);
            bool stacked = plan.Stacked || valueNatural * common >= valueScale * STACK_GAIN;

            if (stacked)
            {
                float grow = Math.Min(maxGrowth, stackFit);
                labelScale = Math.Max(EnergyMetrics.MIN_TEXT, labelNatural * grow);
                valueScale = Math.Max(EnergyMetrics.MIN_TEXT, valueNatural * grow);
            }

            float labelLeft = area.X + (stacked ? pad * 0.5f : pad);
            float dividerX = labelLeft + plan.LabelWidth * (labelScale / labelNatural) + pad;
            float rowGap = futuristic ? Math.Max(2f, pad * 0.4f) : 0f;
            if (!stacked && !futuristic)
                sprites.Add(LineRenderer.Rect(new Vector2(dividerX, area.Center.Y), new Vector2(metrics.DividerThickness, area.Height), palette.BoxFrame));

            for (int i = 0; i < rowCount; i++)
            {
                EnergyRow row = rows[i];
                float top = area.Y + rowHeight * i;
                if (futuristic)
                    DrawRowCells(sprites, new RectangleF(area.X, top + rowGap * 0.5f, area.Width, rowHeight - rowGap), stacked ? -1f : dividerX - pad * 0.5f, rowGap,
                        ref palette, ref metrics);
                else if (i > 0)
                    sprites.Add(LineRenderer.Rect(new Vector2(area.Center.X, top), new Vector2(area.Width, metrics.DividerThickness), palette.Divider));

                bool hasBar = row.BarFraction >= 0f;
                if (stacked)
                {
                    float labelLine = unitLine * labelScale;
                    float valueLine = unitLine * valueScale;
                    DrawText(sprites, surface, font, row.Label, labelLeft, top + labelLine * 0.5f, area.Width - pad, labelLine, labelScale,
                        palette.Label, TextAlignment.LEFT);
                    if (hasBar)
                        ThemeChrome.DrawBar(sprites, metrics.Theme, new RectangleF(labelLeft, top + labelLine + pad * 0.5f, area.Width - pad, rowHeight - labelLine - pad),
                            row.BarFraction, row.ValueColor, palette.Label, metrics.S, showPercent: true);
                    else
                        DrawText(sprites, surface, FormatingHelper.BoldFont(font, row.Value), row.Value, area.Right - pad * 0.5f, top + labelLine + valueLine * 0.5f,
                            area.Width - pad, valueLine, valueScale, row.ValueColor, TextAlignment.RIGHT);
                }
                else
                {
                    float textCenterY = top + rowHeight * 0.5f;
                    DrawText(sprites, surface, font, row.Label, labelLeft, textCenterY, dividerX - pad - labelLeft, textArea, labelScale,
                        palette.Label, TextAlignment.LEFT);
                    if (hasBar)
                        ThemeChrome.DrawBar(sprites, metrics.Theme, new RectangleF(dividerX + pad, top + pad, area.Right - dividerX - pad * 2f, rowHeight - pad * 2f),
                            row.BarFraction, row.ValueColor, palette.Label, metrics.S, showPercent: true);
                    else
                        DrawText(sprites, surface, FormatingHelper.BoldFont(font, row.Value), row.Value, area.Right - pad, textCenterY, area.Right - dividerX - pad * 2f, textArea,
                            valueScale, row.ValueColor, TextAlignment.RIGHT);
                }
            }
        }

        /// <summary>Células chanfradas de uma linha do Futuristic: rótulo e valor separados em <paramref name="splitX"/>, ou uma só quando empilhados.</summary>
        static void DrawRowCells(List<MySprite> sprites, RectangleF row, float splitX, float gap, ref EnergyPalette palette, ref EnergyMetrics metrics)
        {
            Color fill = CellFill(ref palette);
            Color line = ThemeChrome.WithAlpha(palette.Frame, 0.6f);
            if (splitX < 0f)
            {
                ThemeChrome.DrawChamferedBox(sprites, row, fill, line, palette.ValueAttention, metrics.S);
                return;
            }

            ThemeChrome.DrawChamferedBox(sprites, new RectangleF(row.X, row.Y, splitX - gap * 0.5f - row.X, row.Height), fill, line, palette.ValueAttention, metrics.S);
            ThemeChrome.DrawChamferedBox(sprites, new RectangleF(splitX + gap * 0.5f, row.Y, row.Right - splitX - gap * 0.5f, row.Height), fill, line,
                new Color(0, 0, 0, 0), metrics.S);
        }

        static Color CellFill(ref EnergyPalette palette)
        {
            return new Color(Color.Lerp(palette.Backdrop, palette.Frame, 0.08f), 1f);
        }

        static TablePlan Plan(IMyTextSurface surface, string font, RectangleF area, List<EnergyRow> rows, int measured,
            float labelNatural, float valueNatural, float unitLine, float pad, float u, float maxGrowth)
        {
            var plan = new TablePlan { Measured = measured };
            for (int i = 0; i < measured; i++)
            {
                plan.LabelWidth = Math.Max(plan.LabelWidth, FormatingHelper.GetSizeInPixel(rows[i].Label ?? string.Empty, font, labelNatural, surface).X);
                plan.ValueWidth = Math.Max(plan.ValueWidth, ValueWidth(surface, font, rows[i], valueNatural));
            }

            float width = area.Width - pad * 4f;
            plan.Stacked = plan.LabelWidth * (EnergyMetrics.MIN_TEXT / labelNatural) + plan.ValueWidth * (EnergyMetrics.MIN_TEXT / valueNatural) > width;
            float ratio = plan.Stacked ? 1f : Math.Min(maxGrowth, width / Math.Max(1f, plan.LabelWidth + plan.ValueWidth));
            float floorRow = FloorRow(plan.Stacked, unitLine, u);
            float minRow = plan.Stacked
                ? floorRow + (AnyBar(rows, measured) ? pad : 0f)
                : Math.Max(2.4f * u, EnergyTheme.MIN_ROW_LINES * unitLine * Math.Max(EnergyMetrics.MIN_TEXT, valueNatural * Math.Min(1f, ratio)));
            plan.Capacity = (int)Math.Floor(area.Height / minRow);
            int count = Math.Min(measured, plan.Capacity);
            if (count < 2 && measured >= 2 && area.Height >= 2f * floorRow)
                count = 2;
            plan.Rows = Math.Max(1, count);
            return plan;
        }

        /// <summary>Altura mínima de uma linha na escala mínima.</summary>
        static float FloorRow(bool stacked, float unitLine, float u)
        {
            return stacked
                ? 2.3f * unitLine * EnergyMetrics.MIN_TEXT
                : Math.Max(2.4f * u, EnergyTheme.MIN_ROW_LINES * unitLine * EnergyMetrics.MIN_TEXT);
        }

        /// <summary>
        ///     Menores áreas (X = largura, Y = altura) em que <see cref="DrawValueTable"/> mostra até duas linhas inteiras na escala
        ///     mínima, empilhadas e lado a lado.
        /// </summary>
        public static void MinTableSizes(IMyTextSurface surface, string font, List<EnergyRow> rows, ref EnergyMetrics metrics, out Vector2 stacked,
            out Vector2 sideBySide)
        {
            float labelWidth = 0f;
            float valueWidth = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                labelWidth = Math.Max(labelWidth, FormatingHelper.GetSizeInPixel(rows[i].Label ?? string.Empty, font, EnergyMetrics.MIN_TEXT, surface).X);
                valueWidth = Math.Max(valueWidth, ValueWidth(surface, font, rows[i], EnergyMetrics.MIN_TEXT));
            }

            float unitLine = FormatingHelper.LineHeight(1f, surface, font);
            int lines = Math.Min(2, rows.Count);
            stacked = new Vector2(Math.Max(labelWidth, valueWidth) + metrics.Padding + 1f, FloorRow(true, unitLine, metrics.U) * lines);
            sideBySide = new Vector2(labelWidth + valueWidth + metrics.Padding * 4f + 1f, FloorRow(false, unitLine, metrics.U) * lines);
        }

        static float ValueWidth(IMyTextSurface surface, string font, EnergyRow row, float scale)
        {
            string value = row.Value ?? string.Empty;
            float width = FormatingHelper.GetSizeInPixel(value, FormatingHelper.BoldFont(font, value), scale, surface).X;
            return row.WidthTemplate == null
                ? width
                : Math.Max(width, FormatingHelper.GetSizeInPixel(row.WidthTemplate, FormatingHelper.BoldFont(font, row.WidthTemplate), scale, surface).X);
        }

        static bool AnyBar(List<EnergyRow> rows, int count)
        {
            for (int i = 0; i < count; i++)
                if (rows[i].BarFraction >= 0f)
                    return true;
            return false;
        }

        /// <summary>
        ///     Caixa com rótulo em cima, valor grande (negrito) no meio, texto secundário e barra embaixo. Rótulo e secundário
        ///     ficam com no máximo um quarto da altura cada e nunca maiores que o valor; faltando altura saem nesta ordem:
        ///     secundário, barra, rótulo.
        /// </summary>
        public static void DrawValueBox(List<MySprite> sprites, IMyTextSurface surface, string font, RectangleF box, string label, string value,
            Color valueColor, string subText, float barFraction, bool bipolarBar, float valueType, ref EnergyPalette palette, ref EnergyMetrics metrics,
            string widthTemplate = null)
        {
            if (metrics.Theme == LcdTheme.Futuristic)
                ThemeChrome.DrawChamferedBox(sprites, box, CellFill(ref palette), ThemeChrome.WithAlpha(palette.Frame, 0.6f), palette.ValueAttention, metrics.S);
            else if (metrics.Theme != LcdTheme.Clean)
                LineRenderer.DrawRectOutline(sprites, box, metrics.DividerThickness, palette.BoxFrame);
            float pad, inner, labelHeight, subHeight, barHeight;
            LayoutValueBox(surface, font, box.Height, !string.IsNullOrEmpty(label), !string.IsNullOrEmpty(subText), barFraction >= 0f || bipolarBar,
                ref metrics, out pad, out inner, out labelHeight, out subHeight, out barHeight);
            float innerWidth = box.Width - pad * 2f;
            float innerTop = box.Y + pad * 0.6f;
            if (innerWidth <= 0f || inner <= 0f)
                return;

            float unitLine = FormatingHelper.LineHeight(1f, surface, font);
            float bottom = innerTop + inner - barHeight - subHeight;
            float valueTop = innerTop + labelHeight;
            float valueHeight = bottom - valueTop;
            float requested = Math.Min(Math.Max(metrics.Text(valueType), valueHeight / unitLine * 0.9f), metrics.Text(valueType) * 2f);
            if (widthTemplate != null)
                FormatingHelper.FitText(surface, widthTemplate, FormatingHelper.BoldFont(font, widthTemplate), requested, innerWidth, valueHeight,
                    EnergyMetrics.MIN_TEXT, out requested);
            float valueScale = DrawText(sprites, surface, FormatingHelper.BoldFont(font, value), value, box.Center.X, valueTop + valueHeight * 0.5f, innerWidth,
                valueHeight, requested, valueColor, TextAlignment.CENTER);

            if (labelHeight > 0f)
                DrawText(sprites, surface, font, label.ToUpperInvariant(), box.X + pad, innerTop + labelHeight * 0.5f, innerWidth, labelHeight,
                    Math.Min(labelHeight / unitLine, valueScale), palette.LabelMuted, TextAlignment.LEFT);

            if (barHeight > 0f)
            {
                float height = barHeight - pad * 0.4f;
                DrawBar(sprites, new RectangleF(box.X + pad, innerTop + inner - height, innerWidth, height), barFraction, bipolarBar, valueColor, ref palette, ref metrics);
            }

            DrawText(sprites, surface, font, subText, box.X + pad, bottom + subHeight * 0.5f, innerWidth, subHeight, Math.Min(subHeight / unitLine, valueScale),
                palette.LabelMuted, TextAlignment.LEFT);
        }

        /// <summary>Altura que sobra para o valor numa caixa com essa altura e esses elementos.</summary>
        public static float ValueBoxSlice(IMyTextSurface surface, string font, float boxHeight, bool hasLabel, bool hasSub, bool hasBar, ref EnergyMetrics metrics)
        {
            float pad, inner, labelHeight, subHeight, barHeight;
            LayoutValueBox(surface, font, boxHeight, hasLabel, hasSub, hasBar, ref metrics, out pad, out inner, out labelHeight, out subHeight, out barHeight);
            return inner - labelHeight - subHeight - barHeight;
        }

        /// <summary>Largura útil de <see cref="DrawValueBox"/>: o recuo lateral encolhe em caixas baixas.</summary>
        public static float ValueBoxInnerWidth(RectangleF box, ref EnergyMetrics metrics)
        {
            return box.Width - ValueBoxPad(box.Height, ref metrics) * 2f;
        }

        static float ValueBoxPad(float boxHeight, ref EnergyMetrics metrics)
        {
            return Math.Min(metrics.Padding, boxHeight * 0.08f);
        }

        static void LayoutValueBox(IMyTextSurface surface, string font, float boxHeight, bool hasLabel, bool hasSub, bool hasBar, ref EnergyMetrics metrics,
            out float pad, out float inner, out float labelHeight, out float subHeight, out float barHeight)
        {
            pad = ValueBoxPad(boxHeight, ref metrics);
            inner = boxHeight - pad * 1.2f;
            float unitLine = FormatingHelper.LineHeight(1f, surface, font);
            float minLine = unitLine * EnergyMetrics.MIN_TEXT;
            barHeight = hasBar ? Math.Max(3f, metrics.U * 0.5f) + pad * 0.4f : 0f;
            float natural = Math.Max(minLine, Math.Min(unitLine * Math.Max(EnergyMetrics.MIN_TEXT, metrics.Text(EnergyTheme.LABEL) * 0.9f),
                (inner - barHeight) * 0.25f));
            float remaining = inner - minLine - barHeight;
            if (hasLabel && remaining < minLine && barHeight > 0f)
            {
                barHeight = 0f;
                remaining = inner - minLine;
            }

            labelHeight = hasLabel && remaining >= minLine ? natural : 0f;
            subHeight = hasSub && remaining >= (hasLabel ? 2f : 1f) * minLine ? natural : 0f;
        }

        /// <summary>
        ///     Ícone do tipo com selo acima e pílula de estado abaixo. Quando o texto do estado não cabe inteiro, fica só uma
        ///     pílula na cor do estado. Sem altura para o ícone (<see cref="MIN_ICON_UNITS"/>), a pílula vira chip e depois o selo
        ///     sai. Com <paramref name="loadRatio"/> ≥ 0, um anel de utilização com brilho envolve o ícone.
        /// </summary>
        public static void DrawIllustration(List<MySprite> sprites, IMyTextSurface surface, string font, RectangleF area, string icon, Color iconColor, float iconAspect,
            string badge, string stateText, Color stateColor, float loadRatio, ref EnergyPalette palette, ref EnergyMetrics metrics)
        {
            float pad = metrics.Padding;
            float top = area.Y + pad * 0.6f;
            float bottom = area.Bottom - pad * 0.6f;
            float innerWidth = area.Width - pad * 2f;
            if (innerWidth <= 0f || bottom <= top)
                return;

            string badgeText = null;
            float badgeScale = 0f;
            float badgeHeight = 0f;
            if (!string.IsNullOrEmpty(badge))
            {
                badgeText = FormatingHelper.FitText(surface, badge.ToUpperInvariant(), font, metrics.Text(EnergyTheme.LABEL) * 0.9f, innerWidth,
                    area.Height * 0.2f, EnergyMetrics.MIN_TEXT, out badgeScale);
                badgeHeight = FormatingHelper.GetSizeInPixel(badgeText, font, badgeScale, surface).Y + pad * 0.4f;
            }

            float stateScale = EnergyMetrics.MIN_TEXT;
            string state = stateText.ToUpperInvariant();
            float stateWidth = innerWidth - pad * 0.8f;
            bool textFits = stateWidth > 0f && FormatingHelper.FitText(surface, state, font, metrics.Text(EnergyTheme.LABEL), stateWidth, area.Height * 0.18f,
                EnergyMetrics.MIN_TEXT, out stateScale).Length == state.Length;
            Vector2 size = FormatingHelper.GetSizeInPixel(state, font, stateScale, surface);
            float pillHeight = size.Y + pad * 0.5f;
            float chipHeight = Math.Max(3f, metrics.U * 0.6f);
            float spare = bottom - top - pad * 0.5f - metrics.U * MIN_ICON_UNITS;
            bool showPill = textFits && spare - badgeHeight - pillHeight >= 0f;
            bool showBadge = badgeText != null && spare - badgeHeight - (showPill ? pillHeight : chipHeight) >= 0f;
            if (!showBadge)
                showPill = textFits && spare - pillHeight >= 0f;

            if (showBadge)
            {
                sprites.Add(Text(badgeText, new Vector2(area.Center.X, top), badgeScale, palette.LabelMuted, TextAlignment.CENTER, font));
                top += badgeHeight;
            }

            if (showPill)
            {
                var fill = new Color(Color.Lerp(palette.Backdrop, stateColor, 0.15f), 1f);
                float pillWidth = Math.Min(innerWidth, size.X + pad * 1.6f);
                var pill = new RectangleF(area.Center.X - pillWidth * 0.5f, bottom - pillHeight, pillWidth, pillHeight);
                BorderRenderer.CreateSpritesFromRect(pill, sprites, fill, pillHeight * 0.5f, 1f, new Color(stateColor, 1f), Math.Max(1f, metrics.S));
                sprites.Add(Text(state, new Vector2(area.Center.X, pill.Center.Y - size.Y * 0.5f), stateScale, stateColor.EnsureMinimalContrast(fill),
                    TextAlignment.CENTER, font));
                bottom -= pillHeight + pad * 0.5f;
            }
            else
            {
                float chipWidth = Math.Min(innerWidth, metrics.U * 3f);
                BorderRenderer.CreateSpritesFromRect(new RectangleF(area.Center.X - chipWidth * 0.5f, bottom - chipHeight, chipWidth, chipHeight), sprites,
                    new Color(stateColor, 1f), chipHeight * 0.5f);
                bottom -= chipHeight + pad * 0.5f;
            }

            float iconSize = Math.Min(innerWidth, bottom - top) * 0.85f;
            if (iconSize <= 0f)
                return;

            var center = new Vector2(area.Center.X, (top + bottom) * 0.5f);
            if (loadRatio >= 0f)
            {
                float radius = iconSize * 0.5f;
                Color ringColor = EnergyTheme.LoadColor(ref palette, loadRatio);
                sprites.Add(new MySprite(SpriteType.TEXTURE, "Circle", center, new Vector2(Math.Min(radius * 2.5f, Math.Min(innerWidth, bottom - top))),
                    new Color(ringColor, 0.08f + 0.12f * loadRatio)));
                DonutPanel.DrawDonut(sprites, center, radius * (1f - EnergyTheme.RING_THICKNESS), radius, loadRatio, ringColor, palette.GaugeTrack,
                    EnergyTheme.DONUT_STEPS, 0f, 2f * metrics.S);
                iconSize = radius * (1f - EnergyTheme.RING_THICKNESS) * 2f * 0.72f;
            }

            var iconBox = new Vector2(iconSize);
            if (iconAspect > 1f)
            {
                float width = Math.Min(innerWidth, (bottom - top) * iconAspect);
                iconBox = new Vector2(width, width / iconAspect);
            }
            sprites.Add(new MySprite(SpriteType.TEXTURE, icon, center, iconBox, iconColor));
            if (metrics.Theme == LcdTheme.Futuristic && iconAspect > 1f)
            {
                float margin = pad * 0.4f;
                ThemeChrome.DrawChamferedBox(sprites, new RectangleF(center.X - iconBox.X * 0.5f - margin, center.Y - iconBox.Y * 0.5f - margin,
                    iconBox.X + margin * 2f, iconBox.Y + margin * 2f), new Color(0, 0, 0, 0), ThemeChrome.WithAlpha(palette.Frame, 0.6f), palette.ValueAttention, metrics.S);
            }
        }

        /// <summary>Barra em pílula (0..1) ou bipolar (-1..1 a partir do centro), sem clip.</summary>
        public static void DrawBar(List<MySprite> sprites, RectangleF rect, float fraction, bool bipolar, Color fillColor, ref EnergyPalette palette, ref EnergyMetrics metrics)
        {
            if (rect.Width <= 0f || rect.Height <= 0f)
                return;

            if (!bipolar)
            {
                ThemeChrome.DrawBar(sprites, metrics.Theme, rect, fraction, fillColor, palette.Label, metrics.S);
                return;
            }

            BarPanel.CreateBackgroundSprites(sprites, rect.Position, rect.Size, palette.GaugeTrack, 0f);
            float half = (rect.Width - rect.Height) * 0.5f * MathHelper.Clamp(Math.Abs(fraction), 0f, 1f);
            if (half > 0.5f)
            {
                float direction = fraction < 0f ? -1f : 1f;
                sprites.Add(LineRenderer.Rect(new Vector2(rect.Center.X + direction * half * 0.5f, rect.Center.Y), new Vector2(half, rect.Height), fillColor));
            }

            sprites.Add(LineRenderer.Rect(rect.Center, new Vector2(Math.Max(1f, rect.Height * 0.25f), rect.Height * 1.6f), palette.Label));
        }

        /// <summary>
        ///     Texto ajustado à caixa (encolhe e, se preciso, corta com reticências); a posição vertical é o centro da linha. Devolve
        ///     a escala usada.
        /// </summary>
        public static float DrawText(List<MySprite> sprites, IMyTextSurface surface, string font, string text, float x, float centerY,
            float maxWidth, float maxHeight, float requestedScale, Color color, TextAlignment alignment)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f || maxHeight <= 0f)
                return requestedScale;

            float scale;
            string fitted = FormatingHelper.FitText(surface, text, font, requestedScale, maxWidth, maxHeight, EnergyMetrics.MIN_TEXT, out scale);
            if (fitted.Length != 0)
            {
                float height = FormatingHelper.GetSizeInPixel(fitted, font, scale, surface).Y;
                sprites.Add(Text(fitted, new Vector2(x, centerY - height * 0.5f), scale, color, alignment, font));
            }

            return scale;
        }

        public static MySprite Text(string text, Vector2 position, float scale, Color color, TextAlignment alignment, string font)
        {
            return new MySprite(SpriteType.TEXT, text, position, null, color, font, alignment, scale);
        }
    }
}
