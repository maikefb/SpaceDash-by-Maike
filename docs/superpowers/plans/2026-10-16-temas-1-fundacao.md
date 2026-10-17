# Temas Mínimo/Compacto — Plano 1 de 3: Fundação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar os dois temas novos no enum e no terminal, o chrome "nada em volta", os helpers compartilhados (anel, kv, tiles, herói, regra de encaixe), a barra padrão, a diferenciação Forma dos quatro temas desenhados e as correções de foto que não dependem de layout novo.

**Architecture:** Tudo em `Core`: `LcdTheme` ganha `Minimal`/`Compact`; `SurfaceScriptBase` suprime título, rodapé e moldura nesses temas; `ThemeChrome` ganha os ramos novos e a paleta Café; um helper novo `BareLayout` (instância por app) concentra medição e desenho dos blocos reutilizáveis. Apps só passam a usar `BareLayout` nos planos 2 e 3. Ao fim deste plano, Mínimo e Compacto renderizam o layout cheio de hoje sem chrome (comportamento transitório aceitável).

**Tech Stack:** C# 6 (ModAPI Space Engineers, whitelist), `MySprite`, `DonutPanel`, `LineRenderer`, `FormatingHelper`. Compila com `.\check.ps1`; monta com `.\build.ps1`.

**Spec:** `docs/superpowers/specs/2026-10-16-temas-minimo-compacto-design.md`

## Global Constraints

- Sem `Comparison<T>`, sem LINQ por frame, sem `new List<MySprite>` por frame (reutilizar `readonly List<MySprite>`).
- Texto nunca encolhe para caber: rótulo sem espaço vira ícone; sem ícone, some; valor sem espaço some.
- Texto segue `Host.FontScale` (já inclui `FontBoost`); ícones, margens e espessuras seguem `Host.ConfiguredScale`.
- Cores de estado: ok = `GetHeaderColor()`, atenção = `GetWarningColor()`, erro = `GetErrorColor()`. H₂ = `new Color(80, 160, 255)`, O₂ = `Color.White`.
- Textos visíveis em `Core/Data/Localization/MyTexts.resx` (en) e `MyTexts.pt-BR.resx`. Prefixo das chaves novas compartilhadas: `LcdMod_Bare_`.
- `Generated/*.g.cs` são escritos à mão.
- Não há framework de teste no repositório. "Teste" de cada tarefa = `.\check.ps1` sem erro + checagem no jogo descrita no passo. Commits em pt-BR `tipo: descrição`, sem co-autor, somente se o usuário autorizar.
- Ponytail ultra: nenhum helper além dos listados; nenhuma abstração para "o próximo tema".

## Review Focus

1. Tema salvo como 5 ou 6 numa versão antiga do mod: `GetTheme` tem que devolver `Default`, nunca lançar. (Task 1)
2. `Surface.FontSize` = 0.1 e LCD de cockpit 128×64: `BareLayout.Fits` tem que devolver false e os helpers não podem dividir por zero nem desenhar sprite com largura negativa. (Task 3)
3. Café com cor de fundo do usuário igual à cor de texto: a paleta fixa substitui as duas, então contraste é garantido; verificar que `DrawMessage` e `DrawLoading` também usam a paleta. (Task 6)
4. `DrawBar` com `rect.Height` menor que 2 px (fonte minúscula): sem cantos arredondados negativos; desenha retângulo simples. (Task 5)
5. Molduras digitais em Mínimo/Compacto: continua desenhando título e moldura como Default; não pode ficar sem fundo. (Task 2)

---

### Task 1: Enum, clamp, combo e rótulos dos temas

**Files:**
- Modify: `Core/Data/Scripts/Core/Common/Config/Components/LcdTheme.cs`
- Modify: `Core/Data/Scripts/Core/Common/Config/Components/GeneralConfigComponentExtensions.cs:25`
- Modify: `Core/Data/Scripts/Core/Client/Terminal/Controls/Generic/ComboboxTheme.cs:30-36`
- Modify: `Core/Data/Localization/MyTexts.resx`, `Core/Data/Localization/MyTexts.pt-BR.resx` (após `LcdMod_Theme_Small`)

**Interfaces:**
- Produces: `LcdTheme.Minimal = 5`, `LcdTheme.Compact = 6`; chaves `LcdMod_Theme_Minimal` ("Minimal"/"Mínimo") e `LcdMod_Theme_Compact` ("Compact"/"Compacto").

- [ ] **Step 1:** Adicionar `Minimal = 5, Compact = 6` ao enum.
- [ ] **Step 2:** Clamp em `GetTheme`: `config.ThemeInternal > (int)LcdTheme.Compact` → `Default`.
- [ ] **Step 3:** Dois `items.Add` novos em `ComboboxTheme.Content`, na ordem Minimal, Compact, depois de Small.
- [ ] **Step 4:** Chaves resx en e pt-BR.
- [ ] **Step 5:** `.\check.ps1` → sem erro. No jogo: combo "Tema" lista Mínimo e Compacto; escolher um não quebra a tela (renderiza o layout cheio por enquanto).
- [ ] **Step 6:** Commit `feat: temas Mínimo e Compacto no enum e no terminal`.

---

### Task 2: Chrome "nada em volta" nos temas novos

**Files:**
- Modify: `Core/Data/Scripts/Core/Client/Gui/Styling/ThemeChrome.cs` (`FrameInset`, `DrawFrame`, `DrawTitleAccent`, `DrawCard`, `DrawFooterBand`, `DrawDivider`, `DrawIconCell`)
- Modify: `Core/Data/Scripts/Core/Client/SurfaceScripts/Abstract/SurfaceScriptBase.cs` (`UpdateViewBox` :404, `DrawTitle` :671, `DrawFooter` :723, `AddBackground` :1035)
- Modify: `Core/Data/Scripts/Core/Client/Gui/Frames/FrameLayouts.cs:44` (`Resolve`)
- Modify: `Info/Data/Scripts/Info/Client/SurfaceScripts/DigitalPictureFrames.cs` (override de `ThemeKind`)

**Interfaces:**
- Produces: `public static bool ThemeChrome.IsBare(LcdTheme theme)` (true para Minimal e Compact); `FrameInset(LcdTheme theme, Vector2 surfaceSize, float defaultThickness, float scale)` (parâmetro novo; bare = `new Vector2(BARE_INSET * scale)` com `const float BARE_INSET = 4f`).
- Consumes: `LcdTheme.Minimal/Compact` (Task 1).

- [ ] **Step 1:** `IsBare` e `BARE_INSET`; `FrameInset` ganha `scale` e o ramo bare. Atualizar os dois chamadores em `SurfaceScriptBase` (`UpdateViewBox`, `AddBackground`) passando `ConfiguredScale`.
- [ ] **Step 2:** Em `DrawFrame`, `DrawTitleAccent`, `DrawCard`, `DrawFooterBand`, `DrawDivider`, `DrawIconCell`: `if (IsBare(theme)) return true;` como primeira linha (não desenham e assumem o desenho). Em `DrawCard` o ramo bare desenha fill `Color.Lerp(background, foreground, 0.06f)` sem borda (decisão 14) e devolve true.
- [ ] **Step 3:** `SurfaceScriptBase.DrawTitle`: após `CaretY = position.Y;`, `if (ThemeChrome.IsBare(_userTheme)) return;`. `DrawFooter`: `if (ThemeChrome.IsBare(_userTheme)) return;` antes de `RenderFooterRoot`. Confirmar que `AddBackground` continua desenhando o fundo (tiled) e que `DrawFrame` devolve true no bare (nada em volta).
- [ ] **Step 4:** `FrameLayouts.Resolve`: devolver `null` quando `ThemeChrome.IsBare(theme)` (e confirmar que já devolve null para Small).
- [ ] **Step 5:** `DigitalPictureFramesSurfaceScript`: `public override LcdTheme ThemeKind { get { var t = base.ThemeKind; return ThemeChrome.IsBare(t) ? LcdTheme.Default : t; } }` (decisão 8).
- [ ] **Step 6:** `.\check.ps1`. No jogo, Elevação em Mínimo: sem título, sem rodapé, sem borda, conteúdo a 4 px × escala da borda. Molduras digitais em Mínimo: igual a Default.
- [ ] **Step 7:** Commit `feat: temas Mínimo e Compacto sem título, rodapé e moldura`.

---

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

### Task 4: Pisca e jogador local em `App`

**Files:**
- Modify: `Core/Data/Scripts/Core/Client/Apps/Abstract/AppBase.cs`

**Interfaces:**
- Produces: `protected bool BlinkOn` → `!AnimationsEnabled || (MyAPIGateway.Session.GameplayFrameCounter / 10) % 2 == 0`, onde `AnimationsEnabled` lê o flag público de `Host.Animations` (`AnimationController`; implementador localiza a propriedade de habilitado em `Core/.../Gui/Animations/`). `protected static long LocalIdentityId` → `MyAPIGateway.Session?.Player?.IdentityId ?? 0L`.

- [ ] **Step 1:** Implementar as duas propriedades.
- [ ] **Step 2:** `.\check.ps1`. Commit `feat: BlinkOn e LocalIdentityId na base dos apps`.

---

### Task 5: Barra padrão (`DrawBar` estilo Relógio)

**Files:**
- Modify: `Core/Data/Scripts/Core/Client/Gui/Styling/ThemeChrome.cs:164` (`DrawBar`)
- Modify: `Core/Data/Scripts/Core/Client/Gui/ControlsTemplates/Progress/ProgressBar.cs:46` (chamada)
- Modify: `Info/Data/Scripts/Info/Client/Apps/GasApp.cs:502`, `Info/Data/Scripts/Info/Client/Apps/LiftCapacityApp.cs` (`DrawRow` com barra), `Info/Data/Scripts/Info/Client/Gui/ControlsTemplates/Custom/Energy/EnergyCardRenderer.cs` (barras desenhadas à mão), `Info/Data/Scripts/Info/Client/Apps/EnergyGeneralApp.cs` (`DrawCharge`)

**Interfaces:**
- Produces: `public static bool DrawBar(List<MySprite> s, LcdTheme theme, RectangleF rect, float fraction, Color fill, Color foreground, float scale, bool showPercent = false)`. Desenho único para todos os temas (inclusive Default, devolve true): trilho `WithAlpha(foreground, 0.15f)` com pontas arredondadas (`LineRenderer` círculo de raio `rect.Height/2` nas duas pontas + retângulo), preenchimento igual na cor `fill`, sem borda. `showPercent` desenha o percentual em Monospace à direita como hoje.

- [ ] **Step 1:** Reescrever `DrawBar`; remover o ramo por tema. Se `rect.Height < 2f`, retângulos simples sem círculos.
- [ ] **Step 2:** `grep -rn "DrawBar(" Info Core` → todos os chamadores compilam; os que exibiam percentual passam `showPercent: true`.
- [ ] **Step 3:** Migrar barras desenhadas à mão: `LiftCapacityApp.DrawRow` (quando `fraction >= 0`), `EnergyGeneralApp.DrawCharge`, barras em `EnergyCardRenderer` (grep `SpriteType.TEXTURE` com `"SquareSimple"` em retângulos finos) e `FrameRenderer.Bar` (`Core/.../Gui/Frames/FrameRenderer.cs`) → `ThemeChrome.DrawBar`.
- [ ] **Step 4:** `.\check.ps1`. No jogo: Gás, Elevação, Energia e Relógio com a mesma barra (trilho arredondado escuro, sem borda).
- [ ] **Step 5:** Commit `refactor: barra de progresso única em ThemeChrome.DrawBar`.

---

### Task 6: Diferenciação Forma dos temas desenhados + paleta Café

**Files:**
- Modify: `Core/Data/Scripts/Core/Client/Gui/Styling/ThemeChrome.cs` (`DrawFrame`, `DrawTitleAccent`, `DrawCard`, `DrawFooterBand`, `DrawDivider`, `TitleFont`, novos `Foreground`/`Background`/`TitleColor`)
- Modify: `Core/Data/Scripts/Core/Client/SurfaceScripts/Abstract/SurfaceScriptBase.cs` (propriedades `ForegroundColor`/`BackgroundColor`, `DrawTitle`, ponto onde `Surface.ScriptBackgroundColor` é atribuído em `RenderSprites`)

**Interfaces:**
- Produces: `public static Color ThemeChrome.Foreground(LcdTheme theme, Color user)` (Café → `CAFE_INK = new Color(62, 42, 28)`), `Background(LcdTheme theme, Color user)` (Café → `CAFE_PAPER = new Color(242, 232, 214)`), `TitleColor(LcdTheme theme, Color accent, Color background)` (Futuristic → `background`; demais → `accent`), `TitleAlignment(LcdTheme theme)` (Clean → LEFT; Futuristic, Café, Small → CENTER). `TitleFont`: bold em Futuristic, Clean e Café.
- Consumes: `IsBare` (Task 2).

- [ ] **Step 1:** `SurfaceScriptBase.ForegroundColor`/`BackgroundColor` passam pelas funções novas (grep onde são definidas; `IAppHost.ForegroundColor` já expõe). Onde `Surface.ScriptBackgroundColor` é atribuído, usar `Background(theme, …)`.
- [ ] **Step 2:** Futuristic: `DrawTitleAccent` preenche a barra inteira com `accent` (texto fica `TitleColor` = fundo); `DrawFrame` mantém chanfro; `DrawCard` chanfrado com `FrameFill`; `DrawFooterBand` faixa sólida `WithAlpha(accent, 0.25f)`.
- [ ] **Step 3:** Clean: `DrawFrame` só retângulo 1 px `DividerColor`; `DrawTitleAccent` só linha 1 px sob o título; `DrawCard` devolve true sem desenhar fundo (só divisores via `DrawDivider` entre linhas, já existente); `DrawFooterBand` devolve true sem faixa, só a linha superior.
- [ ] **Step 4:** Café: `DrawFrame` borda dupla (duas linhas 1 px separadas por `2f * scale`) em `CAFE_BROWN = new Color(120, 80, 50)`; `DrawCard` retângulo arredondado (4 `LineRenderer.Rect` + 4 círculos de raio `4f*scale`) com fill `Color.Lerp(CAFE_PAPER, Color.White, 0.4f)` e sombra 1 px `WithAlpha(CAFE_INK, 0.2f)` deslocada `(1,1)*scale`; `DrawTitleAccent` divisor tracejado (segmentos de `6f*scale` com vão `4f*scale`) sob o título centrado.
- [ ] **Step 5:** `DrawTitle` em `SurfaceScriptBase`: `Alignment = TitleAlignment(theme)`, `Color = TitleColor(theme, GetHeaderColor(), BackgroundColor)`, X = centro quando CENTER senão `ViewBox.X`. Small inalterado (borda 1 px, sem título, fonte `FontBoost`): confirmar que `DrawTitle` já retorna cedo em Small; se não, adicionar.
- [ ] **Step 6:** `.\check.ps1`. No jogo, Painel de Energia nos quatro temas: Futuristic com faixa sólida de título; Clean só linhas, título à esquerda; Café fundo creme com texto escuro e borda dupla; Small sem título com borda fina.
- [ ] **Step 7:** Commit `feat: temas Futuristic, Clean, Café e Small com chrome distinto`.

---

### Task 7: Energia Preenchida % vira app comum

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/SurfaceScripts/PowerFilled.cs:28` (remover override `ThemeKind => Default`)
- Modify: `Info/Data/Scripts/Info/Client/Apps/PowerFilledApp.cs` (`GetSprites` :104, `LayoutChanged` :52)

- [ ] **Step 1:** Apagar o override. Em `GetSprites`, antes do conteúdo: `Host.AddBackground(_sprites); Host.DrawTitle(_sprites);` e usar a área abaixo de `CaretY` (grep como `LiftCapacityApp` lê `titleOffset`) como área de conteúdo.
- [ ] **Step 2:** `.\check.ps1`. No jogo: Energia % em Futuristic com moldura, título e rodapé do tema; em Default igual a antes.
- [ ] **Step 3:** Commit `fix: Energia Preenchida respeita o tema do LCD`.

---

### Task 8: Correções das fotos sem layout novo

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Gui/ControlsTemplates/Custom/Energy/EnergyCardRenderer.cs:68` (`DrawTitleBar`)
- Modify: `Info/Data/Scripts/Info/Client/Apps/EnergyChartApp.cs` (legenda e tamanhos fixos)
- Modify: `Info/Data/Scripts/Info/Client/Apps/DockingAlignmentApp.cs` (valores dos cartões em `DrawAlignment` :640)
- Modify: `Info/Data/Scripts/Info/Client/Apps/GasApp.cs` (rótulos de título/cartões)
- Modify: `Info/Data/Scripts/Info/Client/Apps/LiftCapacityApp.cs` (`DrawTile`, `DrawRow`: tamanho do valor)

- [ ] **Step 1:** `DrawTitleBar`: remover o desenho de `shortTitle` à direita e de qualquer texto vindo de `LcdMod_Frame_GridTelemetry`/`LcdMod_Frame_Telemetry` (grep nos `.cs`; se as chaves ficarem sem uso, apagar dos resx).
- [ ] **Step 2:** `EnergyChartApp`: toda `RotationOrScale` literal passa a `n * Host.FontScale`; rótulo de legenda usa `Bare.Label(text, icon, box, …)` (ícone = `"IconEnergy"` para produção/consumo; `"SD_Icon_Battery"` para carga).
- [ ] **Step 3:** `DockingAlignmentApp`: valores de cartão com `TextAlignment.CENTER` em `rect.Center.X` e Y centrado pela `LineHeight`.
- [ ] **Step 4:** `GasApp`: título de cada cartão via `Bare.Label(title, icon, box, fg)` (ícones já existem: `ICON_H2`, `ICON_O2`; geradores e gelo usam `"IconHydrogen"`/`"IconOxygen"` até o atlas trazer `Generator`/`Ice`).
- [ ] **Step 5:** `LiftCapacityApp.DrawTile`/`DrawRow`: valor com `Bare.Text(...)` e `maxHeight = rect.Height`, `fontMul = 1f` (nunca maior que a caixa).
- [ ] **Step 6:** `.\check.ps1`. No jogo, telas de cockpit menores: nenhum texto estoura; Painel de energia sem "GRID"/"Telemetria".
- [ ] **Step 7:** Commit `fix: textos que estouravam e cabeçalho do painel de energia`.

---

## Self-review

- Cobertura: enum/clamp/combo (T1); chrome bare (T2); regra 2, 3, 4, 5, anel único, LCD largo/alto (T3); pisca e jogador local (T4); regra 7 barra (T5); direção Forma + Café invertido + Small (T6); decisão 10 (T7); correções de foto (T8). Regra 6 (grade de ícones por `Escala`) fica para Inventário/Projetor no Plano 3. Molduras digitais (T2 passo 5).
- Tipos: `BareLayout.KvRow`/`Tile` são `struct` públicos aninhados; `Kv` e `Tiles` recebem `List<>` que o app mantém como campo `readonly` e limpa por frame.
- Proporção: 8 tarefas, código só em assinaturas.
