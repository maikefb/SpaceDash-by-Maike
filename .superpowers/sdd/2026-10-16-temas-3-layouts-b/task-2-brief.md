### Task 2: Antena

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/AntennaApp.cs:89`
- resx: nova `LcdMod_Antenna_Active` ("active"/"ativas"), `LcdMod_Antenna_MaxRange` ("Max range"/"Maior alcance").

**Interfaces:**
- Consumes: `_entries` (`AntennaEntry.Name/StatusText/StatusColor/IsFunctional/StatusIcon/UseLaserIconCompensation`), alcance: `_entryModels` guarda o bloco; ler `IMyRadioAntenna.Radius` / `IMyBeacon.Radius` / `IMyLaserAntenna` (sem alcance: estado).

- [ ] **Step 1:** Contagens: `active` = entradas com `IsFunctional` e transmitindo (reusar o critério que define `StatusColor` verde), `total = _entries.Count`, `maxRange` = maior `Radius`.
- [ ] **Step 2:** `DrawMinimal`: ícone `"SD_Icon_BeaconBroadcast"` (fallback até `AntennaRadio` existir) a `min(W,H)*0.5`, cor accent se `active > 0` senão erro; linha 1 `active + " / " + total`; linha 2 `DistanceToString(maxRange)`.
- [ ] **Step 3:** `DrawCompact`: cabeçalho `Bare.Text(total + " " + Loc("Active")…)`; `Bare.Kv` uma linha por entrada: `Icon = StatusIcon`, rótulo nome, valor alcance ou `StatusText`, `ValueColor = StatusColor`.
- [ ] **Step 4:** `.\check.ps1`; jogo. Commit `feat: Antena em Mínimo e Compacto`.

---
