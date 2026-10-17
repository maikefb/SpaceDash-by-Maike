# Task 7 — Projetor em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; `IconGridPageCount` da base tem setter privado — ver "Preocupações".)

## O que foi implementado

### `Info/Data/Scripts/Info/Client/Apps/ProjectorApp.cs` (único arquivo alterado)

- `protected override void DrawMinimal(List<MySprite> s)`:
  - `hasBlueprint = Projector != null && Projector.IsProjecting`. Sem planta: `total = remaining = 0`, `fraction = 0` (anel vazio, só trilho), centro `LcdMod_NotAvailable`, nenhuma linha de texto.
  - `fraction = total > 0 ? 1 - remaining / total : 0`; `Bare.Ring(area, fraction, accent = GetHeaderColor(), track = Lerp(BackgroundColor, fg, 0.15), center = PercentageToString(fraction), fg, 3f)`.
  - Linhas (`fontMul 1`, `Bare.Text`, somem se não cabem): `string.Format(Loc("Remaining"), remaining, total)` e `string.Format(Loc("Buildable"), Projector.BuildableBlocksCount)`.
  - `BareLayout.IsWide(ViewBox)`: anel na metade esquerda, as duas linhas à direita centradas verticalmente; senão anel em cima (altura = `ViewBox.Height − 2·LineHeight(1) − Margin`) e as duas linhas centradas embaixo.
- `protected override void DrawCompact(List<MySprite> s)`:
  - Sem planta ou `DisplayItems.Count == 0` → `Host.DrawMessage(s, LocHelper.Empty, "Warning", GetWarningColor())` (mesma chave `LcdMod_Empty` que o script `Projector.cs` usa em `AddEmptySprites`).
  - Grade com a **mesma fórmula** de `ItemsApp.DrawIconGrid` (`icon = 32·Bare.Scale`, `gap = 4·Bare.Scale`, `cols = max(1, floor(W/(icon+gap)))`, `rows = max(1, floor((H+gap)/(icon+LineHeight(1)+gap)))`, centrada horizontalmente, página = `(GameplayFrameCounter/300) % pages`), desenhada no app porque a quantidade precisa de cor por célula: ícone via `DrawItemIcon(ResolveSprite(item.ItemType))`; sob ele `FormatItemQty(missing)` em `GetErrorColor()` quando `missing > 0`, ou `Loc("Ok")` em accent quando `missing == 0`.
  - Fonte: `DisplayItems` (a projeção ordenada/filtrada de `ProjectorViewModel.Items`, onde `ItemEntry.Amount` = quantidade **faltante** por material, 0 quando disponível). Em vez de `ComponentMissing` (`IReadOnlyDictionary<MyItemType,double>`): mesma informação para componentes, respeita ordenação/filtros do LCD e o toggle de lingotes existente, e evita alocar uma lista por frame para indexar o dicionário.
  - Paginação própria: `_compactPages` (recalculado em cada `DrawCompact`), `CompactPage`, `_lastCompactPage`; `Update()` chama `InvalidateContentSprites()` quando `IsCompact && _compactPages > 1` e a página mudou (espelha o que `ItemsApp.Update` faz com `IconGridPageCount`).
- `Loc(key)` com cache em `Dictionary<string,string>` (prefixo `LcdMod_Projector_`), limpo em `LayoutChanged` (padrão do `LiftCapacityApp`).
- Layout cheio (`DrawFooter`, modos de apresentação, construtor) intacto.

## Chaves resx novas (não editei .resx)

| Chave | en | pt-BR |
|---|---|---|
| `LcdMod_Projector_Remaining` | `{0} of {1} blocks left` | `faltam {0} de {1} blocos` |
| `LcdMod_Projector_Buildable` | `{0} buildable now` | `{0} construíveis agora` |
| `LcdMod_Projector_Ok` | `ok` | `ok` |

Reutilizadas (já existem em `MyTexts.resx`): `LcdMod_NotAvailable` (centro do anel sem planta), `LcdMod_Empty` (mensagem do Compacto sem planta/itens).

## Verificação

- `check.ps1`/build **não rodaram** (sem `dotnet`/DLLs do jogo).
- Releitura do diff (+119 linhas): C# 6 apenas (`=>` em getters, `out` com declaração prévia, sem tuplas/`out var`/pattern matching/`$"` novo); sem LINQ; sem comentários/TODO; nenhuma variável/constante sem uso; `override` bate com `protected virtual void DrawMinimal(List<MySprite> s)` / `DrawCompact(List<MySprite> s)` da base.
- Grep de cada membro usado: `ItemsApp.DisplayItems/ResolveSprite/DrawItemIcon/InvalidateContentSprites/ViewBox/ForegroundColor/BackgroundColor`; `App.Bare/IsCompact/Host/GetHeaderColor/GetWarningColor/GetErrorColor`; `BareLayout.Scale/Margin/LineHeight/Text/Ring(area, fraction, fill, track, center, centerColor, fontMul)/IsWide`; `IAppHost.DrawMessage(sprites, message, icon, color, scale = 1f)`; `LocHelper.GetLoc/Empty`; `FormatingHelper.PercentageToString(float)/FormatItemQty(double)`; `ProjectorViewModel.TotalBlocks/RemainingBlocks/Projector`; `ItemEntry.ItemType/Amount` (cast `(double)` já usado em `CompareDisplayItems`); `IMyProjector.IsProjecting` (uso em `ConveyorMapApp.cs:244`); `MyAPIGateway.Session.GameplayFrameCounter` (uso em `ItemsAppBase.cs:162`); `Color.Lerp` (uso em `LiftCapacityApp.cs:139`).
- `IMyProjector.BuildableBlocksCount`: membro de `Sandbox.ModAPI.Ingame.IMyProjector` (herdado por `Sandbox.ModAPI.IMyProjector`), sem uso prévio no repo — confirmar no `check.ps1`; se falhar, remover a linha `buildableText` (2 linhas em `DrawMinimal`).
- Usings necessários já existiam no arquivo (`VRageMath`, `VRage.Game.GUI.TextPanel`, `LcdMod.Client.Gui.Styling`, `LcdMod.Client.Helpers`, `Sandbox.ModAPI`, `System.Collections.Generic`).
- Line endings: `i/lf w/lf`, 0 bytes `\r`.
- Não executado: jogo (LCD largo/alto, `Escala` 0,5/1/2, planta com 0 faltantes, sem projetor).

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/ProjectorApp.cs`

Sem git (o orquestrador commita).

## Preocupações

1. **`IconGridPageCount` tem `private set`** em `ItemsAppBase.cs` — a subclasse não consegue reutilizar a paginação da base ao desenhar a própria grade. Por isso o Projetor tem `_compactPages`/`CompactPage`/`_lastCompactPage` próprios e a invalidação em `Update`. Se o orquestrador preferir uma só fonte, mudar o setter para `protected set` (1 token em `ItemsAppBase.cs:157`) e trocar `_compactPages` por `IconGridPageCount` aqui — fora do escopo desta task.
2. **Mesmo bloqueio parcial da Task 6:** `Info/Data/Scripts/Info/Client/SurfaceScripts/Projector.cs:83` retorna `AddEmptySprites` quando `!_app.HasItems` antes de chamar `_app.GetSprites()`. O Mínimo com projetor sem planta (anel vazio) e o `DrawMessage` do Compacto só aparecem se esse guard for relaxado nos temas bare, como sugerido no relatório da Task 6. Na prática, com planta carregada `Items` nunca fica vazio (todo material necessário gera uma entrada, mesmo com falta 0), então a grade do Compacto e o anel do Mínimo chegam à tela nos casos normais.
3. `ProjectorViewModel` força `TotalBlocks >= 1` e `RemainingBlocks = 0` sem projetor; por isso o "sem planta" é decidido por `Projector.IsProjecting`, não por `TotalBlocks == 0`. Projetor presente mas sem planta carregada → anel vazio + `N/A`, conforme o brief.
4. `string.Format` das duas linhas do Mínimo aloca a cada `GetSprites`; só roda quando o app está sujo (mudança de contagem do projetor), não por frame.
5. `BuildableBlocksCount` sem try/catch (o ViewModel protege `TotalBlocks/RemainingBlocks` com try/catch por histórico de exceções do ModAPI); se o `check.ps1` compilar e o jogo lançar, embrulhar a leitura como o ViewModel faz.

## Skills

Nenhuma invocada.
