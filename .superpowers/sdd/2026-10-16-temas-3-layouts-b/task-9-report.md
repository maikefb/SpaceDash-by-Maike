# Task 9 — Fazenda em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; `.resx` não editados por regra de concorrência; ver "Preocupações".)

## O que foi implementado

- `Info/Data/Scripts/Info/Client/Apps/FarmApp.cs` (único arquivo tocado; `FarmEntry` é classe aninhada dele):
  - `FarmEntry` ganha `IsReady` (bool) e `RemainingSeconds` (double, `-1` = desconhecido/não plantado/morto; `0` = pronto). `Update` recebe os dois e os inclui no `presentationChanged`. Único chamador (`GetOrUpdateEntry`) atualizado; `FarmPlotControl` só usa `Bind`.
  - Critério de "pronto" extraído em `IsPlotReady(plot, ratio)`: plantado, vivo e (`IsPlantFullyGrown` ou `IsHarvestable` ou `ratio >= 0.999f`) — o mesmo que `GetRemainingText` já usava. `GetRemainingSeconds` devolve o número; `GetRemainingText` passa a só formatar (mesmo texto de antes em todos os ramos).
  - `GetSprites`: após `BeginFarmEntryControlFrame`/`Clear` (o fundo e o título são adicionados por `FarmSurfaceScript.GetSprites`, não pelo app), `if (IsMinimal || IsCompact) { Aggregate(); DrawMinimal/DrawCompact; ClearDirtyAfterRender(); return; }`; layout cheio intocado.
  - `Aggregate()` (sem alocação, campos do app): média de `Ratio` sobre todos os canteiros; `_ready`; `_next` = não pronto com menor `RemainingSeconds >= 0`; `_watered`/`_avgWater` só sobre canteiros com `Plot.StorageComponent != null`; `_noWater` e `_noWaterName` (primeiro `OutputName` com `WaterRatio <= 0`); `_plantIcon` = primeiro `OutputSprite` não vazio.
  - `DrawMinimal`: `Bare.Ring(média, accent, trilho Lerp(bg, fg, 0.15), "NN %", fg, 3f)`; linha `Loc("Ready", ready)` + `" · "` + `Loc("Next", RemainingText)` (a parte "próximo" some quando não há canteiro não pronto); linha `Loc("NoWater")` em `GetWarningColor()` só se `noWater > 0`. `IsWide`: anel na metade esquerda, linhas à direita centradas verticalmente; senão anel em cima e linhas embaixo. Sem canteiros: anel vazio (fração 0) sem texto.
  - `DrawCompact`: `Bare.Kv` com prontos (`Farm_ReadyToHarvest`, valor `N/total`, acento se `N > 0`), crescimento médio (`Farm_AvgGrowth`, barra acento), próxima colheita (rótulo `OutputName`, ícone `OutputSprite`, valor `Loc("Next", RemainingText)`; some sem candidato), água média (`Farm_AvgWater`, barra azul `new Color(64,156,255)` como o H₂ do Gás; some se nenhum canteiro tem armazenamento), sem água (`Farm_NoWater`, valor `N · nome`, cor de erro; some se 0). Sem canteiros: `Host.DrawMessage(sprites, LocHelper.Empty, "Warning", warn)` (`LcdMod_Empty`, a chave que o surface script já usa).
  - `Loc(key)` com cache (`_loc`, limpo em `LayoutChanged`) e `Loc(key, arg)` com `FormatingHelper.Culture`, no padrão de `GasApp`.
  - Ícones: planta = `OutputSprite` do primeiro canteiro com item (fallback `"IconEnergy"`); água = `"MyObjectBuilder_Ore/Ice"` (fallback, não há sprite de água no atlas).

## Chaves resx novas (NÃO adicionadas aos .resx — para o orquestrador)

| Chave | en (`MyTexts.resx`) | pt-BR (`MyTexts.pt-BR.resx`) |
|---|---|---|
| `LcdMod_Farm_Ready` | `{0} ready` | `{0} prontos` |
| `LcdMod_Farm_Next` | `next in {0}` | `próximo em {0}` |
| `LcdMod_Farm_NoWater` | `plot without water` | `canteiro sem água` |
| `LcdMod_Farm_AvgGrowth` | `Avg growth` | `Crescimento médio` |
| `LcdMod_Farm_AvgWater` | `Avg water` | `Água média` |
| `LcdMod_Farm_ReadyToHarvest` | `Ready to harvest` | `Prontos para colher` |

Nenhuma delas existe hoje (só `Farm_Water` e `Farm_OutputAmount`). `Farm_ReadyToHarvest` é extra em relação ao brief: `Farm_Ready` é uma frase formatada ("3 prontos") e não serve como rótulo da linha kv; a spec (tabela, linha Fazenda) pede "prontos para colher" no Compacto.

## Verificação

- `check.ps1`/jogo não executados (sem `dotnet` nem DLLs aqui).
- Releitura completa do diff: C# 6 apenas (inicializador de auto-propriedade, `?.`, `out` declarado antes, `=>` em getters existentes). Sem LINQ, sem tuplas, sem `out var`.
- Grep/leitura de cada membro usado: `AppBase.IsMinimal/IsCompact/Theme/Bare/Host/GetHeaderColor/GetWarningColor/GetErrorColor`; `BareLayout.Ring/Kv/Text/Margin/LineHeight/IsWide`, `BareLayout.KvRow` (campos `Label, Icon, Value, ValueColor, Bar, BarColor`); `IAppHost.ViewBox/ForegroundColor/BackgroundColor/DrawMessage(sprites, message, icon, color, scale = 1f)`; `LocHelper.GetLoc/Empty`; `FormatingHelper.PercentageToString(float)/Culture`; `FarmPlotEntry.StorageComponent`; `Color.Lerp`; `RectangleF.Right/Bottom/Center`. `using LcdMod.Client.Gui.Styling` adicionado.
- `FarmEntry.Update`: único chamador atualizado (grep `.Update(` em `FarmPlotControl.cs` → nenhum).
- LF: 0 bytes `\r` no arquivo. Sem comentários novos, sem TODO.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/FarmApp.cs`

Sem comandos git de escrita.

## Preocupações

- **Surface script intercepta o caso vazio:** `FarmSurfaceScript.GetSprites` (`Info/Data/Scripts/Info/Client/SurfaceScripts/Farm.cs`) desenha `AddBackground`, **`DrawTitle`** e, quando `!HasVisibleItems()`, `DrawMessage(LocHelper.Empty)` sem chamar o app. Logo (a) os ramos "sem canteiros" de `DrawMinimal`/`DrawCompact` só rodam se esse script mudar, e (b) o título é desenhado também em Mínimo/Compacto, contra a regra 1. Não toquei em `Farm.cs` (fora do escopo); fica para quem cuidar do padrão de entrada dos surface scripts.
- `Loc(key, arg)` aloca string por frame (como `GasApp`); os agregados não alocam.
- Ícone de água é o do gelo; registrar nome definitivo na lista do atlas quando existir.
- Sem compilação local: risco residual de erro de sintaxe/assinatura que só `check.ps1` pega.
