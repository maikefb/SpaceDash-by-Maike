### Task 3: Helper `BareLayout` (medição, anel, kv, tiles, herói)

**Files:**
- Create: `Core/Data/Scripts/Core/Client/Gui/Styling/BareLayout.cs`
- Modify: `Core/Data/Scripts/Core/Client/Apps/Abstract/AppBase.cs` (propriedades `IsMinimal`, `IsCompact`, `IsBare`, `Bare`)

**Interfaces:**
- Produces (namespace `LcdMod.Client.Gui.Styling`):

```csharp
public sealed class BareLayout
{
    public BareLayout(IAppHost host, ITextStyleProvider style);   // App implementa ITextStyleProvider
    public float FontScale { get; }         // Host.FontScale
    public float Scale { get; }             // Host.ConfiguredScale
    public float Margin { get; }            // 4f * Scale
    public float LineHeight(float fontMul); // FormatingHelper.LineHeight(FontScale * fontMul, style, surface)
    public bool Fits(string text, float fontMul, float maxWidth, float maxHeight);
    public bool Fits(string text, float fontMul, float maxWidth);

    // Texto que some em vez de encolher. Devolve false se não desenhou.
    public bool Text(List<MySprite> s, string text, Vector2 pos, TextAlignment align, float fontMul, Color color, float maxWidth, float maxHeight, bool bold = false);

    // Rótulo com fallback: texto → ícone (sprite) → nada. Devolve a largura usada.
    public float Label(List<MySprite> s, string text, string icon, RectangleF box, Color color, float fontMul = 1f);

    // Anel: fatia sobre trilho, texto central opcional. diameter = min(area.W, area.H) - 2*Margin.
    public void Ring(List<MySprite> s, RectangleF area, float fraction, Color fill, Color track, string center, Color centerColor, float centerFontMul);
    public void Ring(List<MySprite> s, RectangleF area, IReadOnlyList<DonutSegment> segments, Color track, string center, Color centerColor, float centerFontMul);
    public static float RingThickness(float diameter);   // Math.Max(4f, diameter * 0.12f)

    public struct KvRow { public string Label, Icon, Value; public Color ValueColor; public float Bar; public Color BarColor; }  // Bar < 0 = sem barra
    // Linhas de altura LineHeight(1f)*1.5; divisor fino WithAlpha(fg, 0.15) entre linhas; corta na última linha inteira que cabe. Devolve linhas desenhadas.
    public int Kv(List<MySprite> s, RectangleF area, List<KvRow> rows, Color fg, LcdTheme theme);

    public struct Tile { public string Icon, Label, Value; public Color ValueColor; }
    // Grade cols×rows; cada tile: DrawCard(theme) no fundo, ícone 16*Scale + rótulo (Label fallback) em cima, valor (Text, some se não cabe) embaixo.
    public void Tiles(List<MySprite> s, RectangleF area, List<Tile> tiles, int cols, int rows, Color fg, Color accent, LcdTheme theme);

    // Valor-herói: fontMul fixo (4 hora, 3 % e distância, 2.5 demais). Some se não cabe. Devolve a altura ocupada (0 se não desenhou).
    public float Hero(List<MySprite> s, string text, RectangleF area, float fontMul, Color color);

    public static bool IsWide(RectangleF r);   // r.Width > 1.5f * r.Height
    public static bool IsTall(RectangleF r);   // r.Height > 1.5f * r.Width
}
```

- Em `App`: `protected bool IsMinimal => Theme == LcdTheme.Minimal; protected bool IsCompact => Theme == LcdTheme.Compact; protected bool IsBare => ThemeChrome.IsBare(Theme); protected BareLayout Bare { get; }` (instanciado no construtor de `App` com `this`).

- [ ] **Step 1:** Criar a classe com as assinaturas acima. Medição via `FormatingHelper.GetSizeInPixel(text, style, FontScale * fontMul, …)` (assinatura em `FormatingHelper.cs:84`). `Ring` usa `DonutPanel.DrawDonut`/`DrawSegmentedDonut` com `innerRadius = outer - RingThickness(d)`; texto central só se `Fits(center, mul, inner*1.6f, inner*1.0f)`. Trilho = `track` (chamador passa `Color.Lerp(bg, fg, 0.15f)`).
- [ ] **Step 2:** Propriedades em `App`.
- [ ] **Step 3:** Guardas: todo método devolve sem desenhar quando `area.Width <= 0 || area.Height <= 0` ou `FontScale <= 0`.
- [ ] **Step 4:** `.\check.ps1`.
- [ ] **Step 5:** Commit `feat: helper BareLayout para anel, kv, tiles e texto que some`.

---

