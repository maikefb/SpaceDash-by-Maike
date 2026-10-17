# Task 3 — Esteiras: danificados e destruídos (dados) — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; `.resx` não editado por regra de concorrência — chaves listadas abaixo para o orquestrador.)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs`:

- Campos reutilizados: `readonly List<IMySlimBlock> _projected`, `readonly Dictionary<string, string> _loc`.
- `CollectDamage(IMyCubeGrid grid, IList<IMyCubeBlock> all)`, chamado em `Rebuild` logo após `FilterToNetwork`/`BuildNodes` e antes do cálculo de `_tooLarge`:
  - `Damaged` = blocos de `_blocks` com `SlimBlock != null && !IsFullIntegrity`; `WorstDamagedRatio` = menor `BuildLevelRatio` (1f quando nada danificado), `WorstDamagedName` = `CustomName` se `IMyTerminalBlock`, senão `SlimBlock.BlockDefinition.Id.SubtypeName` (`IMySlimBlock.BlockDefinition` é `MyDefinitionBase`, o subtipo fica em `.Id`); `null` quando nada danificado.
  - `Destroyed` = blocos de `ProjectedGrid` (lista `_projected` preenchida por `FindRepairProjector`) cujo `FatBlock.Model` tem porta de esteira (`GetPorts(model).Count > 0`, o mesmo cache/predicado de `CollectPorts`) e sem cubo real em `grid.WorldToGridInteger(projected.GridIntegerToWorld(slim.Position))`. Para de contar em `MAX_NODES`; `Rebuild` faz `_tooLarge = _nodes.Count > MAX_NODES || Destroyed >= MAX_NODES`.
  - `HasBlueprint = projector != null`; `BlueprintTotal = _blocks.Count + Destroyed` (sem projetor `Destroyed` é 0, logo = `_blocks.Count`).
- `FindRepairProjector(IMyCubeGrid grid, IList<IMyCubeBlock> all)`: primeiro `IMyProjector` em `all` (= `gridLogic.Blocks.All`) com `!Closed && IsProjecting && ProjectedGrid != null && ProjectedGrid.GridSizeEnum == grid.GridSizeEnum`; `ProjectedGrid.GetBlocks(_projected, null)`; `matches / count >= 0.5f` com `match = grid.CubeExists(grid.WorldToGridInteger(projected.GridIntegerToWorld(slim.Position)))`. Devolve o projetor deixando `_projected` carregada (reaproveitada por `CollectDamage`, que a limpa ao fim). Recebe `all` além de `grid` (assinatura do brief era só `grid`) para não chamar `grid.GetBlocks` de novo.
- Rodapé do layout cheio (`DrawFooter`): o segmento de `LcdMod_ConveyorMap_Footer` que contém `{4}` é ignorado; depois dos segmentos restantes o código acrescenta `| {_alerts} {Loc("Alerts")}` (vermelho se > 0). Em seguida, uma única string `| N DANIFICADOS | M DESTRUÍDOS` (só as partes > 0) via `Bare.Text(..., fontMul 0.46f * Scale, maxWidth = ViewBox.Right - x, maxHeight = rowHeight)`: some inteira se não couber; cor erro se há destruídos, senão aviso. `SummaryColor` perdeu o ramo `{4}`.
- `Loc(key)`: cache em `_loc` de `LocHelper.GetLoc(MOD_PREFIX + "ConveyorMap_" + key).ToUpperInvariant()` (o rodapé é todo em caixa alta).

## Assinaturas públicas produzidas (para a Task 4)

```csharp
public int Damaged { get; private set; }
public int Destroyed { get; private set; }
public int BlueprintTotal { get; private set; }
public string WorstDamagedName { get; private set; }   // null quando Damaged == 0
public float WorstDamagedRatio { get; private set; }   // 1f quando Damaged == 0
public bool HasBlueprint { get; private set; }
string Loc(string key)                                  // privado; devolve o texto em CAIXA ALTA
```

Todas recalculadas em `Rebuild` (throttle de `REBUILD_TICKS` / mudança de contagem de blocos). Observação para a Task 4: `Loc` devolve maiúsculas; se o layout Mínimo/Compacto quiser caixa normal, usar `LocHelper.GetLoc(MOD_PREFIX + "ConveyorMap_Intact")` etc. diretamente ou trocar o `ToUpperInvariant()` para o rodapé.

## Chaves resx (não editadas aqui — a aplicar em `Core/Data/Localization/MyTexts.resx` e `MyTexts.pt-BR.resx`)

| Chave | en | pt-BR | Situação |
|---|---|---|---|
| `LcdMod_ConveyorMap_Alerts` | `Off` | `Desligados` | **não existe hoje** em nenhum dos dois resx (o brief dizia "muda de texto"); criar. Usada pelo rodapé. |
| `LcdMod_ConveyorMap_Damaged` | `Damaged` | `Danificados` | nova; usada pelo rodapé |
| `LcdMod_ConveyorMap_Destroyed` | `Destroyed` | `Destruídos` | nova; usada pelo rodapé |
| `LcdMod_ConveyorMap_Intact` | `Intact` | `Íntegros` | nova; reservada para a Task 4 (não usada nesta task) |
| `LcdMod_ConveyorMap_Blueprint` | `Blueprint` | `Planta` | nova; reservada para a Task 4 (não usada nesta task) |
| `LcdMod_ConveyorMap_Footer` | `{0} NETWORKS \| {1} BLOCKS \| {2} OPEN ENDS \| {3} BOTTLENECKS` | `{0} REDES \| {1} BLOCOS \| {2} PONTAS VAZIAS \| {3} GARGALOS` | **recomendado** remover o segmento `\| {4} ALERTS` / `\| {4} ALERTAS`. O código já ignora qualquer segmento com `{4}`, então funciona com ou sem essa edição. |

Sem a chave `_Alerts` no resx, `LocHelper.GetLoc` devolve o que devolve hoje para chave ausente (provavelmente a própria chave) — o rodapé mostraria a chave em maiúsculas até o resx ser atualizado.

## Verificação

- `check.ps1` não rodou (sem `dotnet`/DLLs do jogo). Feito em vez disso:
  - Releitura do diff completo: C# 6 apenas (`out` com declaração prévia, `as` + null check, sem tuplas/pattern matching/`out var`); nenhuma variável sem uso; nenhuma lambda em `GetBlocks`; nenhum LINQ; sem alocação nova por `Rebuild` além de preencher `_projected` (lista reutilizada) e a string de `WorstDamagedName` (referência a `CustomName`/`SubtypeName`, sem concatenação).
  - Membros conferidos por uso existente no repo: `IMyProjector` e `.ProjectedGrid`, `.Closed` (`ProjectorViewModel.cs`), `IMyCubeGrid.GetBlocks(List<IMySlimBlock>)` (vários), `IMyCubeGrid.GridSizeEnum` (`ConveyorMapApp.cs:160`), `IMySlimBlock.FatBlock/Position/BlockDefinition` (`ProjectorViewModel.cs`, `ConveyorMapApp.cs:405`), `Id.SubtypeName` (`GasApp.cs:233`), `IMyTerminalBlock.CustomName`, `Bare.Text(List<MySprite>, string, Vector2, TextAlignment, float fontMul, Color, float maxWidth, float maxHeight, bool bold = false)` (`BareLayout.cs:66`; mede com `host.FontScale * fontMul`, por isso `fontMul = 0.46f * Scale` ≡ `textScale` do rodapé), `GetErrorColor/GetWarningColor/MeasureText` (já usados no arquivo), padrão `Loc`/`_loc` de `LiftCapacityApp.cs:265`.
  - **Sem uso prévio no repo** (confiados à documentação do ModAPI): `IMyProjector.IsProjecting` (herdado de `Sandbox.ModAPI.Ingame.IMyProjector`), `IMyCubeGrid.CubeExists(Vector3I)`, `IMyCubeGrid.WorldToGridInteger(Vector3D)`, `IMyCubeGrid.GridIntegerToWorld(Vector3I)`, `IMySlimBlock.IsFullIntegrity`, `IMySlimBlock.BuildLevelRatio`, e o segundo parâmetro `GetBlocks(list, null)` (`Func<IMySlimBlock,bool> collect = null`). Todos existem em `VRage.Game.ModAPI`/`Sandbox.ModAPI` e estão na whitelist, mas é o primeiro uso aqui.
  - Line endings: arquivo continua LF (0 bytes `\r`; `git ls-files --eol` = `i/lf w/lf`).
- Não executado: `.\check.ps1`, `.\build.ps1`, teste no jogo (Step 5: tubo removido → destruídos = 1; projetor desligado → linha some; Review Focus 2: projetor de outra nave → `HasBlueprint` false, `Destroyed` 0, `BlueprintTotal = _blocks.Count`).

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs` (único; sem git).

## Preocupações

- Chave `LcdMod_ConveyorMap_Alerts` ausente nos resx atuais: até o orquestrador adicioná-la, o rodapé mostra texto de fallback.
- `Damaged` conta todos os blocos de `_blocks`, inclusive os filtrados pela âncora de rede (`_filteredOut`) e os contraídos (`_removed`), porque são blocos reais; o rodapé mostra `_blocks.Count - _filteredOut` em "BLOCOS". Se quiser coerência estrita com o filtro de rede, basta pular `_removed[b]` em `CollectDamage` (mas aí pass-throughs contraídos também sairiam).
- `Destroyed` usa `slim.Position` (célula central) para blocos multi-célula da planta; um bloco grande parcialmente sobreposto por outro bloco real na célula central não conta como destruído. Conforme brief.
- Custo: o critério de 50 % percorre todos os blocos projetados do primeiro projetor que passa nos filtros de `IsProjecting`/tamanho; só em `Rebuild` (throttled), mas plantas enormes (milhares de blocos) custam um `CubeExists` por bloco a cada rebuild.
- `MeasureText(" ", textScale)` é o espaçamento entre o segmento de alertas e o texto de dano; aloca o sprite de texto só dentro de `Bare.Text` se couber.

## Skills

Nenhuma invocada.
