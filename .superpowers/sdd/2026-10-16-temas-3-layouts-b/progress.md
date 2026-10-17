# SDD ledger — plan: docs/superpowers/plans/2026-10-16-temas-3-layouts-b.md

Spec: docs/superpowers/specs/2026-10-16-temas-minimo-compacto-design.md. Depende dos Planos 1 e 2 (completos em d2dd522). Branch: main-gv0qq9 (nuvem), push após cada commit.

Rulings herdados: sem compilador na nuvem (revisor = compilador humano; check.ps1 + jogo para o usuário); sem flag de animações (BlinkOn pisca sempre; Review Focus 1 do Radar perde o ramo "Animations off"); implementadores em paralelo SEM git e SEM resx (orquestrador commita por task e insere chaves por onda); texturas confirmadas em Core/Data/Textures*.sbc ou por uso no código; `SD_Icon_*` dos briefs em geral não existem — usar fallback e registrar.
Ruling: Task 10 (BareIcons + lista de ícones) fica por último, feita pelo orquestrador ou por um implementador único após as ondas; o passo 3 (build + jogo) é do usuário.

## Preflight scan

| Par | Produz / Consome | Achado |
|---|---|---|
| T3→T4 | `Damaged/Destroyed/BlueprintTotal/HasBlueprint/WorstDamaged*` em ConveyorMapApp | mesmo arquivo → sequencial (onda B) |
| T6→T7 | `ItemsApp.DrawIconGrid/DrawMinimal/DrawCompact` virtuais | Projector herda de ItemsApp → sequencial (onda B) |
| T1,T2,T3,T5,T6,T8,T9 | arquivos | disjuntos → onda A em paralelo (T6 toca InventoryApp + ItemsAppBase; ninguém mais toca ItemsAppBase na onda A) |
| T3 | ModAPI: `IMyProjector.ProjectedGrid/IsProjecting`, `IMyCubeGrid.GetBlocks/CubeExists/WorldToGridInteger/GridIntegerToWorld`, `IMySlimBlock.IsFullIntegrity/BuildLevelRatio` | whitelist conhecida; confirmar por uso no repo |
| T7 | `IMyProjector.BuildableBlocksCount` | existe no Ingame.IMyProjector; omitir se não achar uso/compilar |
| T2,T3,T6,T7,T8,T9 | resx | centralizado (orquestrador) |

## Progresso
Onda A: BASE d2dd522; Tasks 1, 2, 3, 5, 6, 8, 9 em paralelo.
Task 1: implementer a41be4beb7fa8d63c; DONE_WITH_CONCERNS (árvore; sem resx; COMPACT_ROW_RATIO duplica KV_ROW_RATIO de BareLayout).
Task 2: implementer aacbf432412a34bb1; DONE_WITH_CONCERNS (árvore; resx novas: LcdMod_Antenna_Active = active/ativas, LcdMod_Antenna_MaxRange = Max range/Maior alcance; bloco via GetEntityById; ícone RadioAntenna).
Task 3: implementer a7a0f8d105588c9a5; DONE_WITH_CONCERNS (árvore; resx novas: ConveyorMap_Alerts=Off/Desligados, _Damaged, _Destroyed, _Intact, _Blueprint; recomenda tirar '| {4} ALERTS' do ConveyorMap_Footer; Loc do app devolve maiúsculas).
Task 8: implementer a0c089e1138132dda; DONE_WITH_CONCERNS (árvore; resx novas: Survivors_AliveFor = ALIVE FOR/VIVO HÁ, Survivors_Rank = '#{0} · record {1}'/'#{0} · recorde {1}'; fator 1,5 da linha do Kv duplicado — 2ª ocorrência, candidato a BareLayout.KvRowHeight na Task 10).
Task 9: implementer a1c8df6a6c7a3f38f; DONE_WITH_CONCERNS (árvore; resx novas: Farm_Ready, _Next, _NoWater, _AvgGrowth, _AvgWater, _ReadyToHarvest; FarmEntry ganhou IsReady/RemainingSeconds no mesmo arquivo).
Task 1: reviewer a8cb238b481911bdf; CLEAN (minors: COMPACT_ROW_RATIO duplicado; setores ASCII). Task 1: complete (commit 479bb22).
Nota T9: Farm.cs usa DrawTitle da base (retorna cedo no bare) — preocupação do implementador não se confirma.
Task 5: implementer a0c91654ffc1670de; DONE_WITH_CONCERNS (árvore; resx novas: ClockDashboard_SunEvents = 'Sunrise · Sunset'/'Nascer · Pôr', ClockDashboard_Oxygen = Oxygen/Oxigênio; EnsureSnapshot extraído, usado também pelo layout cheio).
Task 2: reviewer a3740f041bfdcb1c7; CLEAN (minors: 2 GetEntityById por antena por frame; Substring por frame). Task 2: complete (commit e52d0b3).
Task 3: reviewer a7e187e5a70897377; CLEAN (minor 1 aplicado pelo orquestrador: espaço no segmento de alertas do rodapé). Task 3: complete (commit 974ba95).
Task 8: reviewer ac7cc4eeb9b5e5b62; CLEAN (minor 1 aplicado: Icon do KvRow removido para não duplicar o círculo da medalha; cinza por índice e última vida = recorde mantidos). Task 8: complete (commit 839edd4).
Onda B: Task 4 (Esteiras layouts) lançada sobre 974ba95.
Task 9: reviewer acdb6b86db93a8041; CLEAN (minors: anel some antes das linhas em LCD muito baixo; OutputName como rótulo da linha de próxima colheita). Task 9: complete (commit e5e2f5d).
Task 6: implementer a6941b81accb12110; DONE_WITH_CONCERNS (árvore; resx nova: Inventory_Volume = Volume/Volume; Inventory.cs intercepta 0 itens antes do app — mantido: equivale à decisão 11, como nos demais surface scripts).
Task 5: reviewer a7641ed54a289da04; CLEAN (minors: Label=hora some antes da data; delta 0 no evento solar). Task 5: complete (commit 5d273ac).
Task 4: implementer adf3a808585538434; DONE_WITH_CONCERNS (árvore; resx novas: ConveyorMap_BlocksNetworks = '{0} blocks · {1} networks'/'{0} blocos · {1} redes', ConveyorMap_OffBottlenecks = 'Off · Bottlenecks'/'Desligados · Gargalos'; NetworkCount por _rootSize inclui nós isolados).
Task 6: reviewer a6240afdc42955a74; FIX → (1) resx Inventory_Volume: coberto pelo passe de resx; (2) GetTerminalGroupBlocks alocava por Update: orquestrador cacheia _groupBlocks e só refaz quando SelectedGroups muda de referência ou a cada 300 frames. Task 6: complete (commit 27c1347).
Onda B: Task 7 (Projetor) lançada sobre 27c1347.
