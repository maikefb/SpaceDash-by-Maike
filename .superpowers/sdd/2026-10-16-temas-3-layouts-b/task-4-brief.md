### Task 4: Esteiras — layouts

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs:543`

**Interfaces:**
- Consumes: Task 3 + `_alerts` (desligados), `_bottlenecks`, número de redes (contar `_rootSize[i] > 0`), `_blocks.Count`.

- [ ] **Step 1:** `DrawMinimal`: `_segments` (`readonly List<DonutSegment>`): íntegros `(intact/BlueprintTotal, accent)`, danificados `(Damaged/BlueprintTotal, warn)`, destruídos `(Destroyed/BlueprintTotal, erro)`; centro `%` íntegro; legenda uma linha `intact · damaged · destroyed` com cores; linha 2 `_blocks.Count + " blocos · " + redes + " redes"` (chaves resx `LcdMod_ConveyorMap_BlocksNetworks` com `{0}`/`{1}`).
- [ ] **Step 2:** `DrawCompact`: `Bare.Kv`: planta (`BlueprintTotal`, só se `HasBlueprint`), íntegros (`intact + " · " + %`), danificados (`Damaged + " · " + TrimName(WorstDamagedName) + " " + %`; some se 0), destruídos (some se 0 ou sem planta), desligados · gargalos.
- [ ] **Step 3:** `.\check.ps1`; jogo. Commit `feat: Esteiras em Mínimo e Compacto`.

---
