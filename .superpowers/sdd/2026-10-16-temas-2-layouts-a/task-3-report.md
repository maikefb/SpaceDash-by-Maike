# Task 3 — Cartões de energia em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; ver "Verificação" e "Preocupações".)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/EnergyCardApp.cs` (+166 linhas, nenhuma removida; layout cheio e moldura intocados).

- `GetSprites`: após `PrepareFrame()` / `_sprites.Clear()` e antes do ramo `Host.Frame != null`: `if (IsMinimal) { DrawMinimal(...); return; } if (IsCompact) { DrawCompact(...); return; }`. Não chamo `Host.AddBackground`: este app nunca chamou (o host adiciona o fundo em `SurfaceScriptBase`), e `LiftCapacityApp` (Task 1) segue o mesmo fluxo.
- `HeroValue(s, stats)`: `Total == 0` → `"0"`; Battery → `PercentageToString(StoredRatio)`; Hydrogen → `PercentageToString(stats.LoadRatio)`; Reactor/Solar/Wind → `MegaWattsToString(CurrentOutputMw)`.
- `HeroFraction`: `Total == 0` → 0; Battery → `StoredRatio`; demais → `LoadRatio`.
- `KindIcon`: Battery → `"Battery"`; Hydrogen → `"IconHydrogen"`; demais → `TextureHelper.GetOrAddTextureForBlock(stats.RepresentativeDefinition, "IconEnergy")` (o mesmo fallback que `DrawIllustration` já usa).
- `DrawMinimal`: ícone a `24 * Bare.Scale` em `fg`; `Bare.Hero` com 3f (Battery/Hydrogen, percentual) ou 2.5f (MW) na cor `EnergyTheme.StateColor(ref Palette, stats.State)`; `ThemeChrome.DrawBar(sprites, Theme, rect, fraction, stateColor, fg, Bare.Scale)` com altura `max(2, LineHeight(1) * 0.5)`; linha auxiliar `Bare.Text(fontMul 1f, fg)` = `WorkingText + " · " + (Battery ? BatteryModeText : StateText)` + tendência só nas baterias. Vertical: ícone no topo, linha no rodapé, barra acima dela, herói no que sobra. `BareLayout.IsWide(view)`: ícone + herói na metade esquerda, barra + linha centradas verticalmente na direita.
  - `stats.Total == 0` (Review Focus 5): herói `"0"`, cor `fg`, fração 0, linha `"0/0 · <estado NoUnits>"`; nada divide por zero (`LoadRatio` já é seguro).
  - `HasData == false`: só ícone + barra vazia em `fg`, sem texto.
- `DrawCompact`: `!HasData` → `Host.DrawMessage(sprites, NotAvailable, "Warning", GetWarningColor())`. Senão cabeçalho `Bare.Text(KindName + " " + WorkingText, fontMul 1f, cor de estado)` no topo e `Bare.Tiles(grid, _tiles, 2, 2, fg, GetHeaderColor(), Theme)` abaixo; `_tiles` é `readonly List<BareLayout.Tile>(4)` limpa por frame.
  - Battery: Carga % (`ChargeColor`), Modo, Entrada MW (`BatteryInputMw`), Saída MW.
  - Hydrogen: Carga % (`LoadRatio`; `Working == 0` → N/A em `LabelMuted`), Saída, Máx, Estado.
  - Reactor/Solar/Wind: Saída, Máx, Carga % (Solar/Wind usam `Efficiency`/`AvailabilityRatio`, como o cartão cheio), Estado.

## Divergências em relação ao brief

- **Nível de H₂:** `EnergyKind.Hydrogen` são motores a hidrogênio; o `EnergySnapshot` não tem nível de tanque, e `DrawIllustration` usa `stats.LoadRatio` para o anel desse tipo. Como o plano proíbe coletar dado novo, o herói/fração do H₂ é a utilização (`LoadRatio`) em %, não o nível dos tanques da spec. Se o nível dos tanques for obrigatório, precisa de dado novo no snapshot (fora do escopo deste plano).
- **Tendência das baterias:** `+` (Charging) / `-` (Discharging) em vez de `▲`/`▼`; nenhum arquivo do repositório usa esses glifos e não há como confirmar que a fonte do jogo os tem. Trocar é uma linha em `MinimalLine`.
- **Texturas:** `SD_Icon_Battery`, `SD_Icon_Reactor`, `SD_Icon_Solar`, `SD_Icon_Wind` **não existem** em `Core/Data/Textures.sbc` (só `Battery` e as `SD_Illus*`). Usei `Battery`, `IconHydrogen` (vanilla, já usado em `GasApp`) e o ícone do bloco representativo com fallback `IconEnergy`.
- Rótulo "Estado" dos tiles: `MyTexts.GetString("TerminalStatus")` (string do jogo, a mesma que o cartão cheio usa na linha de status das baterias), para não criar chave nova.

## Chaves resx novas

Nenhuma. Todas já existem: `LcdMod_Energy_Charge`, `_Mode`, `_Input`, `_Output`, `_MaxOutput`, `_Load`, `_Efficiency`, `_Working`, `LcdMod_NotAvailable`, mais `DisplayName_BlockGroup_*`, `BlockPropertyTitle_*` e `TerminalStatus` do jogo.

## Texturas usadas

`Battery` (Textures.sbc), `IconHydrogen`, `IconEnergy` (vanilla), ícone do bloco via `TextureHelper.GetOrAddTextureForBlock`.

## Verificação

- `check.ps1` não rodou (sem `dotnet`/DLLs do jogo). Em vez disso: grep de cada membro usado — `BareLayout.Hero/Text/Tiles/Tile/IsWide/Margin/Scale/LineHeight`; `ThemeChrome.DrawBar(List, LcdTheme, RectangleF, float, Color, Color, float, bool = false)`; `IAppHost.ViewBox/ForegroundColor/DrawMessage(sprites, message, icon, color, scale = 1f)`; `App.IsMinimal/IsCompact/Bare/Theme/GetHeaderColor/GetWarningColor`; `EnergyAppBase.HasData/Palette/NotAvailable/Loc/KindName/StateText/BatteryModeText/WorkingText`; `EnergyTheme.StateColor/ChargeColor/LoadColor(ref EnergyPalette, ...)`; `EnergySnapshot.StoredRatio/BatteryStatus/BatteryMode/BatteryInputMw`; `EnergyKindStats.Total/Working/CurrentOutputMw/MaxOutputMw/LoadRatio/AvailabilityRatio/State/RepresentativeDefinition`; `TextureHelper.GetOrAddTextureForBlock(MyCubeBlockDefinition, string)`; `FormatingHelper.PercentageToString/MegaWattsToString`; construtor `MySprite` de 7 argumentos (mesma forma de `BareLayout`).
- `using LcdMod.Client.Gui.Styling` adicionado (BareLayout/ThemeChrome).
- C# 6: só `=>` em getter, switch clássico, sem LINQ, sem tuplas/`out var`/pattern matching. Sem comentários novos, sem TODO.
- LF: `git ls-files --eol` → `i/lf w/lf`; 0 bytes `\r` no arquivo.
- Diff relido ao final; nenhum git de escrita executado.

## Preocupações

- **Paleta contrastada contra o painel, não contra o fundo:** `PrepareFrame` → `BuildPalette` usa `paintPanel = !clean && OwnsTitle && !transparente`, ou seja, em Mínimo/Compacto as cores de estado passam por `EnsureMinimalContrast(panel)` com o `SurfaceColor` do tema, mas o painel não é desenhado nesses temas. Geralmente invisível; se incomodar, mudança proposta (BLOCKED, arquivo não meu) em `EnergyAppBase.BuildPalette`: `bool paintPanel = !clean && !IsBare && OwnsTitle && ...`.
- Sem compilação local: risco residual que só `check.ps1` pega.
- LCD muito baixo: herói some (regra 2) antes da barra/linha; ícone fica sempre, podendo sobrepor a barra se `view.Height < 24*Scale + barra + linha`. Aceitável para o tema; não há fallback de ícone menor.
- Linha auxiliar do Compacto usa `WorkingText` ("{0}/{1}"), igual ao Mínimo.

## Skills

Nenhuma invocada.
