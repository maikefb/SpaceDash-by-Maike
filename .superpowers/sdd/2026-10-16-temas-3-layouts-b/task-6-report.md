# Task 6 — Inventário em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; uma dependência fora dos arquivos permitidos — ver "Preocupações".)

## O que foi implementado

### `Info/Data/Scripts/Info/Client/Apps/Abstract/ItemsAppBase.cs` (`ItemsApp`, base de Inventário e Projetor)

- Ramo bare em `GetSprites` **antes** de todo o pipeline de cache/controles: `if (IsBare) { ClearInteractiveTree(); _contentSpritesDirty = _footerSpritesDirty = true; IsMinimal ? DrawMinimal : DrawCompact; ClearDirtyAfterRender(); return; }`. Os controles interativos (lista, scroll, cabeçalhos de ordenação) ficam ocultos; as flags de cache ficam sujas para que a volta a um tema normal reconstrua tudo. Nada no `switch (PresentationMode)` foi alterado (decisão 2 da spec: `ItemDisplayMode` ignorado nos temas novos).
- `protected virtual void DrawMinimal(List<MySprite> s)` — default: `DrawIconGrid(s, ViewBox, _displayItems, IconGridPage)` (`Host.ViewBox` inteiro, sem `FooterHeight`).
- `protected virtual void DrawCompact(List<MySprite> s)` — **decisão:** default também = grade de ícones. O kv de categorias só faz sentido com volume de inventário (Inventário); o Projetor (Task 7) sobrescreve os dois.
- `protected void DrawIconGrid(List<MySprite> s, RectangleF area, ObservableList<ItemEntry> items, int page)` (regra 6 / Global Constraints):
  - `icon = 32f * Bare.Scale`, `gap = 4f * Bare.Scale`, `cols = max(1, floor(area.Width / (icon + gap)))`, `rows = max(1, floor((area.Height + gap) / (icon + LineHeight(1) + gap)))` (última linha não precisa do gap inferior).
  - Célula: ícone via `DrawItemIcon(ResolveSprite(item.ItemType))` + quantidade `FormatItemQty((double)Amount)` centrada sob o ícone com `Bare.Text(fontMul 1f, maxWidth = icon + gap, maxHeight = LineHeight(1))` — some se não cabe. Grade centrada horizontalmente na área.
  - Paginação: `pages = ceil(count / (cols*rows))`, guardado em `IconGridPageCount`; `page` normalizado por módulo. `0` itens → retorna sem desenhar e sem exceção (Review Focus 3).
- `protected int IconGridPageCount { get; private set; }` (1 por default; atualizado por `DrawIconGrid`) e `protected int IconGridPage => (GameplayFrameCounter / 300) % max(1, IconGridPageCount)` — só varia quando há mais itens que células.
- `Update()`: em tema bare, quando `IconGridPageCount > 1` e a página mudou, `InvalidateContentSprites()` (o script `Inventory.cs` só chama `RenderSprites` com `_app.IsDirty`; sem isso a página nunca trocaria).
- `protected ObservableList<ItemEntry> DisplayItems => _displayItems` — expõe à subclasse a mesma fonte de itens (filtros, agrupamento, ordenação atual; decisão 12).

### `Info/Data/Scripts/Info/Client/Apps/InventoryApp.cs`

- `Update()` override: `base.Update()`; só em `IsCompact`, `ReadVolume()` (dentro de try/catch com `ErrorHandlerHelper`).
- `ReadVolume()`: soma `IMyInventory.CurrentVolume/MaxVolume` (m³ → litros ×1000) dos blocos com inventário do escopo configurado:
  - com `SelectedBlocks`/`SelectedGroups`: blocos por `MyAPIGateway.Entities.GetEntityById` e `GridLogic.GetTerminalGroupBlocks` (lista reutilizada), deduplicados por `HashSet<long>` reutilizado;
  - sem whitelist: `LinkedTypedBlockSourceSet<IMyTerminalBlock>` (`TerminalBlocks`) com o `GridLinkTypeInternal` do app, mesmo padrão do `GasApp`.
  - Sem alocação por frame (listas/sets são campos). Só `InvalidateContentSprites()` quando usado/total mudam. Guarda o ícone do primeiro bloco com inventário (`BlockDefinition.ToString()`) para a linha de volume.
- `DrawCompact` override: `ThemeChrome.DrawBar` de ocupação no topo (`6f * Scale` de altura; cor: header < 80 % ≤ warning < 95 % ≤ error); abaixo, `Bare.Kv` com:
  1. Volume: `VolumeToString(used) + " / " + VolumeToString(total) + " · " + PercentageToString(fração)` (fração 0 e "0 L / 0 L · 0%" quando `max == 0`);
  2. Minérios, 3. Lingotes, 4. Componentes, 5. Munição · Ferramentas — somas por `ItemEntry.TypeId` (`MyObjectBuilder_Ore`, `_Ingot`, `_Component`, `_AmmoMagazine`, `_PhysicalGunObject`) com `FormatItemQty`; rótulos via `ItemCategoryHelper.GetGroupDisplayName("Ore"/"Ingot"/"Component"/"AmmoMagazine"/"PhysicalGun")` (chaves do jogo que o app já usa no título); ícones de item fixos como fallback do `Label`.
  - Com **0 itens** as linhas de categoria não entram: kv só com a barra e a linha de volume (Review Focus 3).
- `Close()`: `_blocks.Dispose()` + `base.Close()`.
- Modos de apresentação existentes, construtor e migração de `ItemDisplayMode` intactos.

## Assinaturas produzidas (a Task 7 consome)

```csharp
// ItemsApp (Info/.../Apps/Abstract/ItemsAppBase.cs)
protected ObservableList<ItemEntry> DisplayItems { get; }
protected int IconGridPageCount { get; private set; }   // 1 por default; DrawIconGrid atualiza
protected int IconGridPage { get; }                     // (GameplayFrameCounter / 300) % max(1, IconGridPageCount)
protected virtual void DrawMinimal(List<MySprite> s);   // default: DrawIconGrid(s, ViewBox, DisplayItems, IconGridPage)
protected virtual void DrawCompact(List<MySprite> s);   // default: idem grade; InventoryApp sobrescreve com kv
protected void DrawIconGrid(List<MySprite> s, RectangleF area, ObservableList<ItemEntry> items, int page);
```

O ramo bare de `GetSprites` já está na base; o Projetor só precisa sobrescrever `DrawMinimal`/`DrawCompact`. A invalidação por troca de página também já está em `ItemsApp.Update` (depende de `IconGridPageCount`, que `DrawIconGrid` mantém).

## Chaves resx novas (não editei .resx)

| Chave | en | pt-BR |
|---|---|---|
| `LcdMod_Inventory_Volume` | `Volume` | `Volume` |

`LcdMod_Inventory_Ores/Ingots/Components/Ammo/Tools` **não** foram criadas: os rótulos reutilizam as chaves do jogo que `ItemCategoryHelper.GetGroupDisplayName` já usa (`RadialMenuGroupTitle_VoxelOres`, `Description_BlueprintClass_Ingots`, `DisplayName_BlueprintClass_Components`, `DisplayName_BlueprintClass_Ammo`, `DisplayName_BlueprintClass_Tools`). Grep confirmou que nenhuma chave `LcdMod_Inventory_*` ou `*Volume*` existe em `MyTexts.resx`.

## Verificação

- `check.ps1`/build **não rodaram** (sem `dotnet`/DLLs do jogo nesta máquina).
- Releitura do diff: C# 6 apenas (inicializador de auto-propriedade, `nameof` já existente, `delegate` anônimo, sem tuplas/`out var`/pattern matching); sem LINQ novo; sem variáveis sem uso; sem comentários/TODO.
- Grep de cada membro usado: `BareLayout.Scale/Margin/LineHeight/Text/Kv/KvRow`; `ThemeChrome.DrawBar(List, LcdTheme, RectangleF, float, Color, Color, float, bool = false)`; `App.IsBare/IsMinimal/IsCompact/Bare/Theme/GetHeaderColor/GetWarningColor/GetErrorColor/ClearDirtyAfterRender`; `ItemsApp.ResolveSprite/DrawItemIcon/ForegroundColor/BackgroundColor/ViewBox/GridLogic/BlockSelectionComponent/InvalidateContentSprites/ClearInteractiveTree`; `ItemEntry.ItemType/TypeId/Amount` (gerado, `MyFixedPoint`, cast explícito para double já usado em `CompareDisplayItems`); `ObservableList<T>.Count/this[int]`; `IObservableList<T>.Count/this[int]` (padrão do `GasApp`); `LinkedTypedBlockSourceSet<T>.Bind/Unbind/Sources/Dispose`; `TypedBlockCollection.TerminalBlocks`; `GridLogic.GetTerminalGroupBlocks(IEnumerable<string>, List<IMyTerminalBlock>)` (limpa a lista); `BlockSelectionConfigComponent.SelectedBlocks (long[]) / SelectedGroups (string[]) / GridLinkTypeInternal (int)`; `FormatingHelper.FormatItemQty(double)/VolumeToString(double)/PercentageToString(float)`; `LocHelper.GetLoc`; `Constants.MOD_PREFIX`; `ErrorHandlerHelper.LogError(e, this)`; `MyAPIGateway.Session.GameplayFrameCounter`, `MyAPIGateway.Entities.GetEntityById`.
- Herança: `grep ": ItemsApp"` → só `InventoryApp` e `ProjectorApp`; nenhum deles definia `DrawMinimal`/`DrawCompact`/`DrawIconGrid`/`DisplayItems`/`IconGridPage*`; `ProjectorApp.Update` já é `override` chamando `base.Update()`, então herda a invalidação de página.
- `SurfaceScriptBase.DrawTitle` retorna cedo em tema bare, logo o `ViewBox` inteiro é do app.
- Line endings: ambos os arquivos `i/lf w/lf`, 0 bytes `\r`.
- Não executado: jogo com `Escala` 0,5 / 1 / 2.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/Abstract/ItemsAppBase.cs`
- `Info/Data/Scripts/Info/Client/Apps/InventoryApp.cs`

Sem git (o orquestrador commita).

## Preocupações

1. **BLOCKED parcial — `Info/Data/Scripts/Info/Client/SurfaceScripts/Inventory.cs:76`** (fora dos arquivos permitidos): `GetSprites` do script faz `if (_app == null || !_app.HasItems) { AddEmptySprites...; return; }` antes de chamar `_app.GetSprites()`. Com 0 itens o app nunca desenha, então o Review Focus 3 ("Mínimo grade vazia; Compacto kv só com a barra de volume") é atendido no app mas **não chega à tela**: o usuário vê a mensagem de vazio do script. Correção de uma linha para quem puder tocar o arquivo: `if (_app == null || (!_app.HasItems && !ThemeChrome.IsBare(ThemeKind)))` (ou expor `IsBare` do app). O mesmo vale para `Projector.cs:83` (Task 7).
2. Sem compilação local: risco residual de assinatura. Pontos a conferir no `check.ps1`: cast `(double)inventory.CurrentVolume` (`MyFixedPoint` → double, igual ao `GasApp`), `switch` sobre `item.TypeId` com `const string`.
3. Volume do escopo por whitelist de blocos não aplica o filtro de link-scope que `ItemsAppViewModel` aplica (`IsBlockInConfiguredLinkScope`); diferença só aparece se um bloco selecionado estiver numa grid fora do vínculo configurado. Expor usado/total pelo `ItemsAppViewModel` seria mais fiel, mas o arquivo não estava no escopo da task.
4. `ThemeChrome.DrawBar` com `showPercent` default false: a barra de ocupação não escreve percentual; o percentual vai na linha de volume do kv.
5. Spec decisão 11 diz "kv com zero linhas → `DrawMessage`"; aqui o kv do Compacto nunca tem zero linhas (volume sempre entra), coerente com o Review Focus 3 que pede só barra/volume.

## Skills

Nenhuma invocada.
