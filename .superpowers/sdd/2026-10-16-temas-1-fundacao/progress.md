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
