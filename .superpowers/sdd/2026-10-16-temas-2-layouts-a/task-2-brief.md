### Task 2: Painel de Energia

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/EnergyGeneralApp.cs:27`
- resx: reutilizar `Loc("Production")`, `Loc("Consumption")`, `Loc("Balance")`, `Loc("Charge")` de `EnergyAppBase.Loc` (confirmar chaves `LcdMod_Energy_*`; criar as ausentes).

**Interfaces:**
- Consumes: `Snapshot.ProducedMw`, `ConsumedMw`, `NetMw`, `StoredRatio`, `UsedRatio`, `BatteryStatusColor`, `PowerText`.

- [ ] **Step 1:** `DrawMinimal`: anel com `UsedRatio` (consumo ÷ produção, clamp 0–1), cor: `UsedRatio > 0.9` erro, `> 0.75` atenção, senão accent; centro `%`; linha auxiliar `"P " + MegaWattsToString(ProducedMw) + "  C " + … + "  " + NetMw com sinal` (some inteira se não couber).
- [ ] **Step 2:** `DrawCompact`: tiles 2×2: produção (`"IconEnergy"`), consumo (`"IconEnergy"`), carga das baterias (`"SD_Icon_Battery"`, `PercentageToString(StoredRatio)`, `BatteryStatusColor`), saldo (`"IconEnergy"`, `NetMw` com sinal, verde se ≥ 0 senão erro).
- [ ] **Step 3:** `.\check.ps1`; no jogo. Commit `feat: Painel de Energia em Mínimo e Compacto`.

---
