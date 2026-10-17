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

