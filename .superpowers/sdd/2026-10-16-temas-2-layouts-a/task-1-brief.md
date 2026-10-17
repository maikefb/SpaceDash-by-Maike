### Task 1: Capacidade de Elevação

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/LiftCapacityApp.cs`
- Modify: resx (chaves `LcdMod_Lift_Safe/Warn/Crit/NoGravity` já existem; nova `LcdMod_Lift_CanCarry` "Can carry"/"Pode carregar" se não existir)

**Interfaces:**
- Consumes: `Usage`, `State`, `HasGravity`, `CanCarryKg`, `ShipMassKg`, `SafeMaxKg`, `Loc(key)`.

- [ ] **Step 1:** `DrawMinimal`: `Bare.Ring(area, usage, stateColor, track, PercentageToString(Usage), stateColor, 3f)`; abaixo, `Bare.Text(stateWord, …, fontMul 1.2f, stateColor)` onde `stateWord` = `Loc("Safe"|"Warn"|"Crit"|"NoGravity")` em maiúsculas via `ThemeChrome.LabelText(Futuristic, …)` (reuso do upper).
- [ ] **Step 2:** `DrawCompact`: `Bare.Tiles(area, _tiles, 2, 2, fg, accent, Theme)` com tiles (ícone, rótulo, valor): (`"IconTemperature"`→ trocar por `"SD_Icon_Gauge"` quando existir, `Loc("LiftUsage")`, `%`, stateColor), (`"IconEnergy"`, `Loc("CanCarry")`, `MassToString(CanCarryKg)`), (`"IconEnergy"`, `Loc("ShipMass")`, massa), (`"SD_Icon_ShieldIcon"`, `Loc("SafeMax")`, `MassToString(SafeMaxKg)`). `_tiles` é `readonly List<BareLayout.Tile>` limpa por frame.
- [ ] **Step 3:** `.\check.ps1`; no jogo LCD de cockpit 1×1: anel com % e SEGURO; Compacto 4 tiles legíveis; fonte 0,5 → textos somem antes de estourar.
- [ ] **Step 4:** Commit `feat: Elevação em Mínimo e Compacto`.

---
