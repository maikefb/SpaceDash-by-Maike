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

