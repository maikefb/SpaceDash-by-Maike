### Task 3: Esteiras — danificados e destruídos (dados)

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs` (`Rebuild` :155, contadores :92, `EvaluateAlerts`)
- resx: `LcdMod_ConveyorMap_Alerts` → texto "Off"/"Desligados"; novas `LcdMod_ConveyorMap_Damaged` ("Damaged"/"Danificados"), `LcdMod_ConveyorMap_Destroyed` ("Destroyed"/"Destruídos"), `LcdMod_ConveyorMap_Intact` ("Intact"/"Íntegros"), `LcdMod_ConveyorMap_Blueprint` ("Blueprint"/"Planta").

**Interfaces:**
- Produces: `public int Damaged { get; }`, `public int Destroyed { get; }`, `public int BlueprintTotal { get; }` (= `_blocks.Count + Destroyed`, ou `_blocks.Count` sem projetor), `public string WorstDamagedName { get; }`, `public float WorstDamagedRatio { get; }`, `public bool HasBlueprint { get; }`.
- Consumes: `_blocks[b].SlimBlock` (`IMySlimBlock.IsFullIntegrity`, `BuildLevelRatio`), `gridLogic.Blocks` para achar `IMyProjector`.

- [ ] **Step 1:** Em `Rebuild`, após montar `_blocks`: `Damaged` = quantidade com `!SlimBlock.IsFullIntegrity`; `WorstDamaged*` = menor `BuildLevelRatio` (nome = `CustomName` se `IMyTerminalBlock`, senão `BlockDefinition.SubtypeName`).
- [ ] **Step 2:** `FindRepairProjector(IMyCubeGrid grid)`: primeiro `IMyProjector` da grid com `IsProjecting && ProjectedGrid != null && ProjectedGrid.GridSizeEnum == grid.GridSizeEnum` e, amostrando os blocos de `ProjectedGrid.GetBlocks(_projected)` (lista reutilizada), `matches / count >= 0.5f` onde `match` = `grid.CubeExists(grid.WorldToGridInteger(ProjectedGrid.GridIntegerToWorld(slim.Position)))`. `HasBlueprint = projector != null`.
- [ ] **Step 3:** `Destroyed` = blocos projetados cujo `BlockDefinition` passa no mesmo predicado de esteira usado para montar `_blocks` e que não têm bloco real na posição correspondente. Cap: parar de contar em `MAX_NODES` e marcar `_tooLarge`.
- [ ] **Step 4:** Layout cheio: rodapé troca "alertas" por `Loc("Alerts")` com o texto novo "Desligados"; acrescentar `Damaged`/`Destroyed` ao rodapé quando > 0 (uma string, `Bare.Text` some se não couber).
- [ ] **Step 5:** `.\check.ps1`; jogo: nave com projetor de reparo e um tubo removido → destruídos = 1; projetor desligado → linha some.
- [ ] **Step 6:** Commit `feat: Esteiras contam danificados e destruídos pela planta do projetor`.

---
