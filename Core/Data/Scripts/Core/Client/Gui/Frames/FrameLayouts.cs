using System.Collections.Generic;
using LcdMod.Client.Gui.Styling;
using LcdMod.Common.Config.Components;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.Frames
{
    /// <summary>Campo de dado de uma moldura, em pixels da textura: 't' texto alinhado na caixa, 'b' barra preenchida da esquerda.</summary>
    public struct FrameSlot
    {
        public RectangleF Box;
        public char Kind;
        public float FontPx;
        public TextAlignment Align;
    }

    /// <summary>Rótulo estático traduzido em tempo de execução; <see cref="Key"/> com prefixo '=' é texto literal.</summary>
    public struct FrameLabel
    {
        public string Key;
        public Vector2 Anchor;
        public TextAlignment Align;
        public float FontPx;
        public bool Bold;
        public float MaxWidth;
    }

    public sealed class FrameLayout
    {
        public string Texture;
        public Vector2 Size;
        public readonly Dictionary<string, FrameSlot> Slots = new Dictionary<string, FrameSlot>();
        public readonly List<FrameLabel> Labels = new List<FrameLabel>();
    }

    /// <summary>Tabela gerada por tools/FrameLayouts/gen.py (FrameLayouts.g.cs): uma moldura por tema × tela × tiny.</summary>
    static partial class FrameLayouts
    {
        static readonly Dictionary<string, FrameLayout> Layouts = new Dictionary<string, FrameLayout>();
        static FrameLayout _current;
        static bool _built;

        /// <summary>Moldura do tema para a tela, preferindo a Tiny quando pedida; null quando o tema não tem moldura para a tela.</summary>
        public static FrameLayout Resolve(LcdTheme theme, string screenId, bool tiny)
        {
            if (theme == LcdTheme.Default || ThemeChrome.IsBare(theme) || string.IsNullOrEmpty(screenId))
                return null;
            screenId = screenId.Substring(screenId.IndexOf('_') + 1);
            if (!_built)
            {
                _built = true;
                Build();
            }

            FrameLayout layout;
            if (tiny && Layouts.TryGetValue(Key(theme, screenId, true), out layout))
                return layout;
            return Layouts.TryGetValue(Key(theme, screenId, false), out layout) ? layout : null;
        }

        static string Key(LcdTheme theme, string screenId, bool tiny)
        {
            return (int)theme + "|" + screenId + (tiny ? "|t" : "");
        }

        static void Screen(LcdTheme theme, string screenId, bool tiny, string texture, float width, float height)
        {
            _current = new FrameLayout { Texture = texture, Size = new Vector2(width, height) };
            Layouts[Key(theme, screenId, tiny)] = _current;
        }

        static void Slot(string field, char kind, float x, float y, float width, float height, float fontPx, char align)
        {
            _current.Slots[field] = new FrameSlot
            {
                Box = new RectangleF(x, y, width, height), Kind = kind, FontPx = fontPx, Align = Alignment(align)
            };
        }

        static void Label(string key, float x, float y, char align, float fontPx, bool bold, float maxWidth)
        {
            _current.Labels.Add(new FrameLabel
            {
                Key = key, Anchor = new Vector2(x, y), Align = Alignment(align), FontPx = fontPx, Bold = bold, MaxWidth = maxWidth
            });
        }

        static TextAlignment Alignment(char align)
        {
            return align == 'R' ? TextAlignment.RIGHT : align == 'C' ? TextAlignment.CENTER : TextAlignment.LEFT;
        }
    }
}
