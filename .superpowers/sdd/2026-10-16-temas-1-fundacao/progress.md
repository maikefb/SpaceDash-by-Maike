# SDD ledger — plan: docs/superpowers/plans/2026-10-16-temas-1-fundacao.md

Spec: docs/superpowers/specs/2026-10-16-temas-minimo-compacto-design.md (lida). Base do plano: 8c849e3 (main, ahead 6 de origin/main).

Ruling: trabalhar direto em `main`, sem worktree/branch — o projeto commita em main (histórico) e as regras do usuário proíbem criar branch sem pedido — custo se errado: commits em main que o usuário teria de mover para branch.
Ruling: os passos "Commit" do plano estão autorizados pela aprovação "executar o plano"; sem push — custo se errado: commits locais a desfazer com reset soft.
Ruling: `.superpowers/` não está no .gitignore; implementadores fazem `git add` só dos arquivos da tarefa — custo se errado: lixo no commit.

## Preflight scan

| Par | Produz / Consome | Achado |
|---|---|---|
| T1→T2 | `LcdTheme.Minimal/Compact` / `IsBare` | ok |
| T2↔T6 | ambos editam `ThemeChrome.DrawFrame/DrawTitleAccent/DrawCard/DrawFooterBand` e `SurfaceScriptBase.DrawTitle` | sequencial; T2 só early-return bare, T6 ramos por tema; sem conflito |
| T2 | `FrameInset` ganha `scale`; 2 chamadores (SurfaceScriptBase:412, :1055) | ok |
| T2→T3 | `DrawCard` bare (fill 6 %) / `BareLayout.Tiles` | ok |
| T3→T8 | `Bare.Label/Text` / EnergyChart, Gas, Lift | ok |
| T5 | `DrawBar` param opcional; `ProgressBar.cs:46` compila sem mudança | ok |
| T6→T3 | paleta Café via `Host.ForegroundColor` / `BareLayout` lê do host | ok |
| T2→T7 | `DrawTitle` bare / PowerFilled chama `Host.DrawTitle` | ok |
| T4 | nome da propriedade de habilitado em `AnimationController` não confirmado | implementador localiza |
| cada tarefa | texto consistente consigo (arquivos criados vs tocados) | ok |

Sem conflitos a decidir.

## Progresso
Task 1: BASE 8c849e3; implementer aa1bea1022ee4499b; DONE_WITH_CONCERNS dba805c.
Ruling: `LcdMod_Theme_Small` pt-BR era "Compacto" e colidiria com o tema Compact — Small passa a "Pequeno" em pt-BR (spec fixa só Mínimo/Compacto) — custo se errado: um rótulo de combo a renomear.
Task 1: fix round 1/5 em andamento (rótulo pt-BR).
Task 1: fix round 1/5 (1 addressed, 0 open; commits dba805c..2a041f1)
Task 1: complete (commits 8c849e3..2a041f1, review clean). Verificação no jogo pendente para o fim do plano.
Task 2: BASE 2a041f1
Task 2: implementer aaa8885343c0c0136; DONE 4fa9e9b; review clean.
Task 2: minor (deferred): apps reservam FooterHeight (ProjectorApp:139, DockingAlignmentApp:622, ItemsAppBase:383, ConveyorMapApp:101) mesmo no bare — tratar nos layouts (Planos 2/3).
Task 2: complete (commits 2a041f1..4fa9e9b, review clean)
Ruling: sessão movida para a nuvem a pedido do usuário; docs/superpowers e .superpowers/sdd commitados para a nuvem ter plano e ledger; IMAGENS/ e .superpowers/brainstorm/ entram no .gitignore — custo se errado: um commit de docs a reverter.
Ruling: .superpowers/ está no .gitignore, então o add anterior não levou o ledger; workspace deste plano entra com git add -f para a nuvem retomar da Task 3 — custo se errado: um commit de docs a reverter.
Ruling: a nuvem não tem `dotnet` nem as DLLs do Space Engineers (CompileCheck referencia `SEBin`); `check.ps1` não roda aqui. Tasks 3..8 seguem com revisão manual de compilação (C# 6, whitelist do ModAPI) pelo revisor; `check.ps1` + jogo ficam para o usuário no fim — custo se errado: erros de compilação a corrigir localmente num passe só.
Task 3: BASE 481f104
Task 3: implementer a962786bd00b857fe; DONE_WITH_CONCERNS 48df5bd (sem compilador; LF é o eol do checkout, .gitattributes eol=lf).
Ruling: nesta sessão na nuvem o hook de parada do usuário exige commit+push e a instrução da sessão fixa a branch `main-gv0qq9`; a partir daqui cada task e cada atualização do ledger são enviadas para `origin/main-gv0qq9` (nunca para `main`) — custo se errado: uma branch remota a apagar.
Task 3: reviewer a2caf2dc69ad9e005; CLEAN (minors não aplicados: divisor do Kv 1 px fixo, medições repetidas em RingCenter; % da barra no Kv fica para a Task 5).
Task 3: complete (commits 481f104..48df5bd, review clean; check.ps1 pendente para o usuário)
Ruling: não existe flag de animações habilitadas em `AnimationController` nem na config (grep em Core/Info); `BlinkOn` da Task 4 fica só `(GameplayFrameCounter / 10) % 2 == 0`, sem `AnimationsEnabled` — custo se errado: uma condição a acrescentar quando a flag existir.
Task 4: BASE 48df5bd
Task 4: implementer ac6c572a04f5d8179; DONE db59acf (2 linhas; `MyAPIGateway` qualificado como o resto do arquivo). Revisão feita pelo orquestrador (diff de 2 linhas): CLEAN.
Task 4: complete (commits 48df5bd..db59acf)
Task 5: BASE db59acf
Task 5: implementer aa87dc3b439a8d015; DONE_WITH_CONCERNS fa5687c (BarPanel.CreateSprites em pílula; bipolar fica à mão; showPercent só na tabela do EnergyCardRenderer).
Task 5: reviewer aaf6fbd767251d3de; CLEAN (minors: trilho de DrawBar sobre o trilho da moldura DDS em FrameRenderer.Bar(ratio) — conferir no jogo, reverter só ali se feio; `theme` ignorado em DrawBar; percentual automático sumiu em barras altas — decisão 9).
Task 5: complete (commits 8cea855..fa5687c, review clean)
Task 6: BASE fa5687c
Ruling: Task 7 executada pelo orquestrador fora de ordem (arquivos disjuntos da Task 6): o surface script de Energia Preenchida já chamava AddBackground/DrawTitle e o app já posiciona o conteúdo em TITLE_BAR_HEIGHT_BASE (40 px × escala × fonte); sem layout DDS gerado para "BatteryGraph", então Frame fica null e o título sai por código. Mudança = remover `ThemeKind => Default` — custo se errado: uma linha a restaurar.
Task 7: complete (commit 5bdc970, 1 linha; revisão pelo orquestrador). Vão de título em Small/bare fica para os Planos 2/3 como os demais apps.
Task 6: implementer a1bf6a83068da97e0; DONE_WITH_CONCERNS 1b2c889 (divergências: DrawTitleAccent antes do texto; ícone do título em TitleColor; Café mantém FrameFill).
Task 8: BASE 44c0b55 (arquivos disjuntos da Task 6; roda em paralelo à revisão da Task 6)
Task 6: reviewer a22c669c75b922e39; CLEAN. Pendência para a revisão final: usos residuais de `Surface.ScriptForegroundColor` fora do brief ficam ilegíveis no Café (DonutPanel:303/306, DonutDualPanel:24/27/60, AntennaApp:458/466, ProjectorApp:180/203/230, PowerFilledApp:254/349/357, DockingAlignmentApp:643/944) → trocar por `Host.ForegroundColor`/`host.ForegroundColor`. Minors não aplicados: LayoutChanged extra ao trocar para/do Café; halo do título sobre a faixa Futuristic (conferir no jogo).
Task 6: complete (commits 7bcefa8..1b2c889, review clean)
Task 8: implementer a14398787ca7f8fc3; DONE_WITH_CONCERNS 0536643 (GRID/Telemetria vem de FrameLayouts.g.cs, não do EnergyCardRenderer — follow-up do orquestrador).
Task 8: follow-up do orquestrador 5a4b8f6: 76 rótulos (=GRID, GridTelemetry, Telemetry, OfGrid, RealTime) removidos de FrameLayouts.g.cs, 4 chaves dos resx en/pt-BR, SKIP_LABELS em gen.py.
Task 8: reviewer a9af122e912d89534; CLEAN (minors: valor das linhas da Elevação a 1×FontScale pode sumir em linhas baixas — alternativa 0.5f*Scale se feio no jogo; LINQ pré-existente em EnergyChartApp:103/176; negrito perdido nos títulos via Bare.Label).
Task 8: complete (commits 1a0f388..0536643 + follow-up 5a4b8f6, review clean)
Plano 1: todas as 8 tasks completas. Revisão final da branch em andamento: (a) trocar usos residuais de Surface.ScriptForegroundColor por Host.ForegroundColor (pendência da Task 6); (b) revisor de branch sobre 8c849e3..HEAD.
Revisão final da branch: reviewer a0ae47b7e6c49d97c; CLEAN (minors). Aplicados pelo orquestrador: 02470d6 (apps leem Host.ForegroundColor) e 4014e3b (DonutDualPanel recebe foreground/background do Projetor; DonutPanel.CreateSprites legado removido). Não aplicados: BRACKET_ARM órfã; `theme` ignorado em DrawBar; rótulos com FitText em EnergyChart/Gas/Lift ficam para os Planos 2/3. Checklist para o usuário no jogo: ver relatório do revisor (itens 1–7) — principal: `new ForegroundColor/BackgroundColor` em SurfaceScriptBase no check.ps1; trilho duplo em FrameRenderer.Bar nas molduras DDS; % automático das barras sumiu (decisão 9).
Plano 1: COMPLETO em 4014e3b.
