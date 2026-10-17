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

