### Task 7: Energia Preenchida % vira app comum

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/SurfaceScripts/PowerFilled.cs:28` (remover override `ThemeKind => Default`)
- Modify: `Info/Data/Scripts/Info/Client/Apps/PowerFilledApp.cs` (`GetSprites` :104, `LayoutChanged` :52)

- [ ] **Step 1:** Apagar o override. Em `GetSprites`, antes do conteúdo: `Host.AddBackground(_sprites); Host.DrawTitle(_sprites);` e usar a área abaixo de `CaretY` (grep como `LiftCapacityApp` lê `titleOffset`) como área de conteúdo.
- [ ] **Step 2:** `.\check.ps1`. No jogo: Energia % em Futuristic com moldura, título e rodapé do tema; em Default igual a antes.
- [ ] **Step 3:** Commit `fix: Energia Preenchida respeita o tema do LCD`.

---

