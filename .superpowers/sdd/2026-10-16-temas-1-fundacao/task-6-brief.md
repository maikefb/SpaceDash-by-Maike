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

