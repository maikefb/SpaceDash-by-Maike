using System;
using System.Collections.Generic;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Helpers;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace LcdMod.Client.Gui.Frames
{
    /// <summary>
    ///     Desenha uma moldura: a textura esticada por eixo à superfície, os rótulos traduzidos e os valores nos campos. As
    ///     coordenadas da tabela estão em pixels da textura de origem e são escaladas por eixo para a SurfaceSize.
    /// </summary>
    public sealed class FrameRenderer
    {
        // ponytail: fator fixo px CSS → altura de linha do jogo e posição da linha de base; ajustar no jogo se o texto sair do trilho.
        const float LINE_PER_PX = 1.2f;
        const float BASELINE = 0.78f;
        const float MIN_SCALE_RATIO = 0.5f;
        // ponytail: valores ocupam 1,3× a altura da caixa; rótulos sem largura máxima usam a distância à borda (ou 60 % da textura quando centrados).
        const float VALUE_FILL = 1.3f;
        const float LABEL_MARGIN = 16f;
        const float CENTER_MAX_FRACTION = 0.6f;

        public readonly FrameLayout Layout;
        readonly IMyTextSurface _surface;
        readonly string _font;
        readonly Vector2 _scale;
        readonly Vector2 _origin;
        readonly float _unitLine;
        readonly List<MySprite> _static = new List<MySprite>();
        Color _staticColor;

        public FrameRenderer(FrameLayout layout, IMyTextSurface surface, string font)
        {
            Layout = layout;
            _surface = surface;
            _font = font;
            _scale = surface.SurfaceSize / layout.Size;
            _origin = (surface.TextureSize - surface.SurfaceSize) * 0.5f;
            _unitLine = FormatingHelper.LineHeight(1f, surface, font);
        }

        public bool Has(string field)
        {
            return Layout.Slots.ContainsKey(field);
        }

        /// <summary>Caixa do campo em pixels da superfície; vazia quando a moldura não tem o campo.</summary>
        public RectangleF Rect(string field)
        {
            FrameSlot slot;
            return Layout.Slots.TryGetValue(field, out slot) ? ToSurface(slot.Box) : new RectangleF();
        }

        /// <summary>Fundo e rótulos traduzidos, montados uma vez por layout/idioma/cor e reaproveitados a cada Run.</summary>
        public void Static(List<MySprite> sprites, Color labelColor)
        {
            if (_static.Count == 0 || _staticColor != labelColor)
            {
                _static.Clear();
                _staticColor = labelColor;
                _static.Add(new MySprite(SpriteType.TEXTURE, Layout.Texture, _surface.TextureSize * 0.5f, _surface.SurfaceSize, Color.White));
                Labels(_static, labelColor);
            }

            sprites.AddRange(_static);
        }

        void Labels(List<MySprite> sprites, Color color)
        {
            for (int i = 0; i < Layout.Labels.Count; i++)
            {
                FrameLabel label = Layout.Labels[i];
                string text = label.Key[0] == '=' ? label.Key.Substring(1) : Localize(label.Key);
                if (string.IsNullOrEmpty(text))
                    continue;
                string font = label.Bold ? FormatingHelper.BoldFont(_font, text) : _font;
                float requested = Scale(label.FontPx);
                float maxWidth = (label.MaxWidth > 0f ? label.MaxWidth : DefaultMaxWidth(label)) * _scale.X;
                float scale;
                text = FormatingHelper.FitText(_surface, text, font, requested, maxWidth, 0f, requested * MIN_SCALE_RATIO, out scale);
                var anchor = _origin + label.Anchor * _scale;
                float top = anchor.Y - _unitLine * scale * BASELINE;
                sprites.Add(new MySprite(SpriteType.TEXT, text, new Vector2(anchor.X, top), null, color, font, label.Align, scale));
            }
        }

        float DefaultMaxWidth(FrameLabel label)
        {
            float width = Layout.Size.X;
            switch (label.Align)
            {
                case TextAlignment.CENTER:
                    return Math.Min(width * CENTER_MAX_FRACTION, 2f * Math.Min(label.Anchor.X, width - label.Anchor.X) - 2f * LABEL_MARGIN);
                case TextAlignment.RIGHT:
                    return label.Anchor.X - LABEL_MARGIN;
                default:
                    return width - label.Anchor.X - LABEL_MARGIN;
            }
        }

        /// <summary>Campos comuns a todas as molduras: nome da grid no cabeçalho e tipo de vínculo no rodapé.</summary>
        public void Header(List<MySprite> sprites, IMyCubeBlock block, int linkType, Color color)
        {
            Text(sprites, "cabecalho.nome_grid", block.CubeGrid.CustomName, color);
            Text(sprites, "rodape.estado_link", LinkTypeText((GridLinkTypeEnum)linkType), color, false);
        }

        static string LinkTypeText(GridLinkTypeEnum linkType)
        {
            switch (linkType)
            {
                case GridLinkTypeEnum.Mechanical: return Localize("LcdMod_MechanicalConnection");
                case GridLinkTypeEnum.Physical: return Localize("LcdMod_PhysicalConnection");
                case GridLinkTypeEnum.Electrical: return Localize("LcdMod_ElectricalConnection");
                default: return Localize("LcdMod_LocalGrid");
            }
        }

        /// <summary>Texto do tamanho da caixa, centrado nela (cabeçalho e rodapé seguem a tabela); encolhe até 0,5× e corta quando não cabe.</summary>
        public void Text(List<MySprite> sprites, string field, string text, Color color, bool bold = true)
        {
            FrameSlot slot;
            if (string.IsNullOrEmpty(text) || !Layout.Slots.TryGetValue(field, out slot) || slot.Kind != 't')
                return;
            string font = bold ? FormatingHelper.BoldFont(_font, text) : _font;
            RectangleF box = ToSurface(slot.Box);
            float requested = Scale(Math.Max(slot.FontPx, slot.Box.Height * VALUE_FILL));
            float scale;
            text = FormatingHelper.FitText(_surface, text, font, requested, box.Width, 0f, requested * MIN_SCALE_RATIO, out scale);
            TextAlignment align = field.StartsWith("cabecalho.") || field.StartsWith("rodape.") ? slot.Align : TextAlignment.CENTER;
            float x = align == TextAlignment.RIGHT ? box.Right : align == TextAlignment.CENTER ? box.Center.X : box.X;
            float top = box.Center.Y - _unitLine * scale * 0.5f;
            sprites.Add(new MySprite(SpriteType.TEXT, text, new Vector2(x, top), null, color, font, align, scale));
        }

        /// <summary>Preenchimento da barra entre duas frações (0..1) da largura; o trilho já está na textura.</summary>
        public void Bar(List<MySprite> sprites, string field, float from, float to, Color color)
        {
            FrameSlot slot;
            if (!Layout.Slots.TryGetValue(field, out slot) || slot.Kind != 'b')
                return;
            from = MathHelper.Clamp(from, 0f, 1f);
            to = MathHelper.Clamp(to, 0f, 1f);
            if (to <= from)
                return;
            RectangleF box = ToSurface(slot.Box);
            float left = box.X + box.Width * from;
            float width = box.Width * (to - from);
            sprites.Add(LineRenderer.Rect(new Vector2(left + width * 0.5f, box.Center.Y), new Vector2(width, box.Height), color));
        }

        public void Bar(List<MySprite> sprites, string field, float ratio, Color color)
        {
            Bar(sprites, field, 0f, ratio, color);
        }

        /// <summary>Saldo: metade esquerda para negativo, metade direita para positivo.</summary>
        public void BipolarBar(List<MySprite> sprites, string field, float fraction, Color color)
        {
            fraction = MathHelper.Clamp(fraction, -1f, 1f);
            if (fraction >= 0f)
                Bar(sprites, field, 0.5f, 0.5f + fraction * 0.5f, color);
            else
                Bar(sprites, field, 0.5f + fraction * 0.5f, 0.5f, color);
        }

        RectangleF ToSurface(RectangleF box)
        {
            return new RectangleF(_origin.X + box.X * _scale.X, _origin.Y + box.Y * _scale.Y, box.Width * _scale.X, box.Height * _scale.Y);
        }

        float Scale(float fontPx)
        {
            return fontPx * _scale.Y * LINE_PER_PX / _unitLine;
        }

        static string Localize(string key)
        {
            string text = MyTexts.GetString(MyStringId.GetOrCompute(key));
            return text.StartsWith("[SD] ") ? text.Substring(5) : text;
        }
    }
}
