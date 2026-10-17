### Task 7: Projetor

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/ProjectorApp.cs`
- resx: `LcdMod_Projector_Remaining` ("{0} of {1} blocks left"/"faltam {0} de {1} blocos"), `LcdMod_Projector_Buildable` ("{0} buildable now"/"{0} construíveis agora"), `LcdMod_Projector_Ok` ("ok").

**Interfaces:**
- Consumes: `ProjectorViewModel.TotalBlocks/RemainingBlocks/ComponentMissing/Items`, `IMyProjector.BuildableBlocksCount` (ModAPI; se não estiver na whitelist, omitir a linha "construíveis").

- [ ] **Step 1:** `DrawMinimal` (override): `Bare.Ring(fraction = 1 - Remaining/Total, accent)` com `%` construído; linha `Loc("Remaining", Remaining, Total)`; linha `Loc("Buildable", BuildableBlocksCount)`.
- [ ] **Step 2:** `DrawCompact` (override): grade de componentes (regra 6) a partir de `ComponentMissing`: quantidade em erro quando falta > 0; `"ok"` em accent quando a entrada está em `Items` com falta 0 (disponível).
- [ ] **Step 3:** `.\check.ps1`; jogo. Commit `feat: Projetor em Mínimo e Compacto`.

---
