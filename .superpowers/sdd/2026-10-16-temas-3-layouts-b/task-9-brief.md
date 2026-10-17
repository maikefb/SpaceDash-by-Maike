### Task 9: Fazenda

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/FarmApp.cs:212`
- resx: `LcdMod_Farm_Ready` ("{0} ready"/"{0} prontos"), `LcdMod_Farm_Next` ("next in {0}"/"próximo em {0}"), `LcdMod_Farm_NoWater` ("plot without water"/"canteiro sem água"), `LcdMod_Farm_AvgGrowth` ("Avg growth"/"Crescimento médio"), `LcdMod_Farm_AvgWater` ("Avg water"/"Água média").

**Interfaces:**
- Consumes: `FarmEntry` (`Ratio`, `RemainingText`, `WaterRatio`, `OutputSprite`, `OutputName`, `StatusColor`, `Plot`), lista de entradas do app, critério de "pronto" (`Ratio >= 1f` ou flag em `FarmPlotEntry`), tempo restante numérico (se só houver `RemainingText`, usar o menor pelo texto não é confiável: expor `RemainingSeconds` em `FarmEntry.Update`).

- [ ] **Step 1:** Agregados: média de `Ratio`, `ready`, próxima (menor `RemainingSeconds` entre não prontos), média de `WaterRatio`, `noWater` (contagem e primeiro `OutputName` com `WaterRatio <= 0`).
- [ ] **Step 2:** `DrawMinimal`: `Bare.Ring(média, accent, "%")`; linha `Loc("Ready", ready) + " · " + Loc("Next", RemainingText)`; linha de aviso `Loc("NoWater")` em atenção só se `noWater > 0`.
- [ ] **Step 3:** `DrawCompact`: `Bare.Kv`: prontos, crescimento médio (`Bar`), próxima colheita (`OutputName · RemainingText`, `Icon = OutputSprite`), água média (`Bar`, azul H₂), sem água (`N · nome`, erro; some se 0).
- [ ] **Step 4:** `.\check.ps1`; jogo. Commit `feat: Fazenda em Mínimo e Compacto`.

---
