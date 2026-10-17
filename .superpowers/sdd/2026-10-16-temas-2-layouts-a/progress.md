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
