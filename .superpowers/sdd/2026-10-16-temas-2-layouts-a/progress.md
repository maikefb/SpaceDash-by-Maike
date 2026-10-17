# SDD ledger — plan: docs/superpowers/plans/2026-10-16-temas-2-layouts-a.md

Spec: docs/superpowers/specs/2026-10-16-temas-minimo-compacto-design.md. Depende do Plano 1 (completo em 02470d6). Branch: main-gv0qq9 (nuvem), push após cada commit.

Ruling (herdados do Plano 1): sem compilador na nuvem → revisor atua como compilador humano; `check.ps1` + jogo ficam para o usuário no fim. Não existe flag de animações: `BlinkOn` pisca sempre (Review Focus 3 do plano vira "pisca a cada 10 frames"; sem ramo `Animations off`).
Ruling: as 8 tasks editam apps disjuntos → implementadores rodam em paralelo (ondas), SEM `git add`/`commit` (o orquestrador commita por task após a revisão) — custo se errado: dois agentes no mesmo índice do git; mitigado proibindo git nos implementadores.
Ruling: resx é arquivo compartilhado → implementadores NÃO editam resx; usam a chave no código e listam as chaves novas (en/pt-BR) no relatório; o orquestrador insere num commit único por onda — custo se errado: chave exibida crua até o commit de resx.
Ruling: `SD_Icon_Battery` não existe nas texturas (achado da T8 do Plano 1); o nome certo é `Battery`. Implementadores confirmam cada textura em `Core/Data/Textures.sbc` ou em uso no código antes de usar.

## Preflight scan

| Par | Produz / Consome | Achado |
|---|---|---|
| P1-T3→todas | `Bare.Ring/Kv/Tiles/Hero/Text/Label`, `IsMinimal/IsCompact` | assinaturas finais em BareLayout.cs (HEAD) |
| P1-T4→T7 | `BlinkOn` | existe; sem gate de animações (ruling) |
| T1..T8 | arquivos | disjuntos (LiftCapacityApp, EnergyGeneralApp, EnergyCardApp, EnergyChartApp, PowerFilledApp, GasApp, DefenseDashboardApp, DockingAlignmentApp) |
| T1, T2, T7 | resx | centralizado no orquestrador (ruling) |
| T5 | PowerFilledApp desenha rodapé próprio | ramos bare retornam antes; sem conflito |

## Progresso
Onda 1: BASE 50490af; Tasks 1..8 em paralelo (implementadores sem git/resx). Commits por task pelo orquestrador após revisão.
Task 1: implementer aa959abe391bdd1a3; DONE_WITH_CONCERNS (árvore; sem chaves resx novas).
Task 2: implementer a60ba2f94194f48f9; DONE_WITH_CONCERNS (árvore; sem chaves resx; achado: EnergyAppBase.DrawChrome desenha chrome no bare — orquestrador trata).
Ruling: EnergyAppBase.DrawChrome devolve após PrepareFrame() quando IsBare (Palette/Metrics continuam preparados para os ramos bare dos apps de energia) — commit 9d96773; vale para T2, T3 e T4.
Task 3: implementer a56ef37aa66df7c05; DONE_WITH_CONCERNS (árvore; sem resx; H₂ herói = LoadRatio; +/- em vez de ▲▼; BLOCKED opcional: BuildPalette.paintPanel no bare).
Task 5: implementer ab0e94537658f2936; DONE_WITH_CONCERNS (árvore; sem resx; sem linhas H₂/O₂/saldo — app não tem esses dados).
Task 6: implementer a5fc698d5b3a93abc; DONE_WITH_CONCERNS (árvore; sem resx; O₂ branco fixo no Café).
Task 1: reviewer a1aa34dfe11143487; CLEAN (minors: centro do anel vazio em vez de N/A quando MaxMassKg==0 com gravidade; 1.2f do brief mantido). Task 1: complete (commit 403483d).
Ruling: BuildPalette.paintPanel exclui IsBare (cores contrastam com o fundo, não com um painel que não é desenhado) — commit 0d17cc1.
Task 2: reviewer a224e849b775d39a9; CLEAN. Task 2: complete (commit 4c15e3e).
Task 4: implementer ac4587b2bdfeca520; DONE_WITH_CONCERNS (árvore; sem resx; DrawPlotWithoutAxis → DrawCurve; linhas de bateria somem sem baterias).
Task 7: implementer ac37a16cdf95ca01d; DONE_WITH_CONCERNS (árvore; resx nova: LcdMod_DefenseDashboard_Weapons = weapons/armas; munição omitida — app não expõe).
Task 8: implementer a881f1becfaefeba2; DONE_WITH_CONCERNS (árvore; resx novas: LcdMod_DockingAlignment_State_Connected/Ready/Aligned/Adjust; tolerâncias 2°/0,25 m escolhidas pelo implementador).
Task 5: reviewer ad4afcdb4882a6ae1; CLEAN (minors: ramos 'sem itens' inalcançáveis porque o surface script intercepta antes). Task 5: complete (commit 4626e30).
Task 3: reviewer af1a299167927db50; CLEAN (minor: cabeçalho do Compacto via Text em vez de Label). Task 3: complete (commit 61ba310).
Task 6: reviewer a13af478bc31f815e; CLEAN (minors: GetLoc por anel por frame; O₂ branco no Café a validar). Task 6: complete (commit ef71796).
Task 4: reviewer aa4169c80961fad1f; CLEAN. Task 4: complete (commit 7b86b88).
resx da onda 1: 5 chaves (DefenseDashboard_Weapons; DockingAlignment_State_Connected/Ready/Aligned/Adjust) em en e pt-BR — commit 6fe29ef.
Task 7: reviewer a9c456064214d3846; CLEAN (minors: linha 2 mostra Firing em vez do motivo mais comum enquanto há tiro; rótulo 'armas' minúsculo no kv). Task 7: complete (commit b9c253a).
Task 8: reviewer a3bd373bc58789a8a; CLEAN (minors: herói some em LCD muito baixo antes de tentar; caches não zerados em ResetTextCaches). Task 8: complete (commit 60e896c).
Plano 2: COMPLETO em 60e896c (8 tasks + 2 fixes transversais em EnergyAppBase + resx). check.ps1 e jogo pendentes para o usuário.
Revisão final Planos 2+3: reviewer aedd48380f053b1da; CLEAN (minors 2 e 8 aplicados em b7e3cc6: segmento {4} removido do ConveyorMap_Footer en/pt-BR; BareIcons.Warning na Acoplagem). Não aplicados: caches Loc/Label duplicados no ConveyorMap; KV_ROW_RATIO duplicado em Radar/Sobreviventes; paginação duplicada no Projetor; trilho Lerp repetido; GetLoc(NotAvailable) por frame. Checklist de teste no relatório do revisor e no resumo ao usuário.
PLANO COMPLETO em b7e3cc6. check.ps1, build.ps1 e jogo: usuário.
