### Task 3: Cartões de energia (Baterias, H₂, Reatores, Solar, Eólica)

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/EnergyCardApp.cs:35`

**Interfaces:**
- Consumes: `_kind`, `stats = Snapshot.Kinds[(int)_kind]` (`Total`, `Working`, `CurrentOutputMw`, `MaxOutputMw`, `LoadRatio`, `State`), `Snapshot.StoredRatio`/`BatteryStatus`/`BatteryMode` (baterias), nível de tanques de H₂ (ver como `DrawIllustration` :85 obtém o nível para `EnergyKind.Hydrogen`), `KindName`, `StateText`, `BatteryModeText`.

- [ ] **Step 1:** `HeroValue()`: Battery → `PercentageToString(StoredRatio)`; Hydrogen → `%` do nível dos tanques; Reactor/Solar/Wind → `MegaWattsToString(CurrentOutputMw)`. `HeroFraction()`: Battery/Hydrogen → nível; demais → `LoadRatio`.
- [ ] **Step 2:** `DrawMinimal`: ícone do tipo (sprite já usado no cartão cheio: `"SD_Icon_Battery"`, `"IconHydrogen"`, `"SD_Icon_Reactor"`, `"SD_Icon_Solar"`, `"SD_Icon_Wind"`) a `24*Scale`; `Bare.Hero(HeroValue, area superior, 3f para %, 2.5f para MW, EnergyTheme.StateColor)`; `ThemeChrome.DrawBar` com `HeroFraction`; linha `Working + "/" + Total + " · " + (Battery ? BatteryModeText : StateText) + tendência` onde tendência = `"▲"`/`"▼"` por `BatteryStatus` (só baterias; sem glifo nos demais).
- [ ] **Step 3:** `DrawCompact`: tiles 2×2 com cabeçalho `KindName + " " + Working + "/" + Total` (`Bare.Text`, fontMul 1f, acima da grade): Battery → carga %, modo, entrada MW (`BatteryInputMw`), saída MW; Hydrogen → nível %, saída MW, máx MW, estado; Reactor/Solar/Wind → saída MW, máx MW, carga %, estado.
- [ ] **Step 4:** `.\check.ps1`; no jogo os cinco cartões. Commit `feat: cartões de energia em Mínimo e Compacto`.

---
