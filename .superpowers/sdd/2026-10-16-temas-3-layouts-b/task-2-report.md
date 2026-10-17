# Task 2 — Antena em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; resx não editado por regra de concorrência.)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/AntennaApp.cs`:

- `GetSprites`: após `ClearInteractiveTree()` e `sprites.Clear()`, ramos `IsMinimal` → `DrawMinimal` e `IsCompact` → `DrawCompact` (com `ClearDirtyAfterRender()`), antes do `switch` do layout cheio. O fundo já é desenhado pelo surface script (`Antenna.cs:72 AddBackground`), e título/rodapé já são suprimidos nos temas bare por `SurfaceScriptBase.DrawTitle/DrawFooter` (`ThemeChrome.IsBare`). Layout cheio intocado.
- `Loc(suffix)` → `LocHelper.GetLoc(MOD_PREFIX + "Antenna_" + suffix)` (novo; o app não tinha `Loc`). `using static LcdMod.Common.Helpers.Constants` adicionado.
- `IsTransmitting(entry, out range)`: resolve o bloco por `MyAPIGateway.Entities.GetEntityById(entry.EntryId)` (EntryId == EntityId, ver collectors) e aplica o critério de "verde" dos collectors: rádio `Enabled && IsFunctional && IsBroadcasting`; farol `Enabled && IsFunctional`; laser `Enabled && IsFunctional && Status == Connected`. `range` = `Radius` do rádio/farol só quando ligado e funcional (0 para laser, desligado ou danificado).
- `CountEntries(out active, out maxRange)`.
- `DrawMinimal`: ícone `"RadioAntenna"` com lado `min(W,H) * 0.5`, cor `GetHeaderColor()` se `active > 0` senão `GetErrorColor()`; linha 1 `active + " / " + total`; linha 2 `Loc("MaxRange") + " " + DistanceToString(maxRange)` e, se não couber, só a distância (regra 2: rótulo some, valor fica); nada se `maxRange == 0`. `IsWide`: ícone centrado na metade esquerda, linhas à esquerda da metade direita. Caso contrário bloco (ícone + 2 linhas) centrado verticalmente; se as linhas não cabem, só o ícone centrado.
- `DrawCompact`: cabeçalho `Bare.Text(active + " / " + total + " " + Loc("Active"))` no topo; `Bare.Kv` abaixo com uma `KvRow` por entrada: `Label = Name`, `Icon = StatusIcon`, `Value` = alcance (`DistanceToString`) ou primeira linha de `StatusText` (laser: estado; desligado/danificado: texto do estado), `ValueColor = StatusColor`, `Bar = -1`. Lista `_rows` reutilizada (`readonly`). Sem entradas → `Host.DrawMessage(LocHelper.Empty, "Warning", GetWarningColor())` (mesma chave `LcdMod_Empty` que o surface script usa em `AddEmptySprites`; na prática o surface script já intercepta `!HasEntries` antes do app).

## Divergências em relação ao brief

- `_entryModels` guarda `AntennaEntry`, não o bloco (brief errado). O bloco é obtido por `MyAPIGateway.Entities.GetEntityById(EntryId)` (padrão já usado em `ProjectorViewModel`, `DockingAlignmentApp`, `ItemsAppViewModel`). Alternativa descartada: percorrer `GridLogic.Blocks.*Antennas`, que ignora o tipo de link entre grids.
- Cabeçalho do Compacto: `active + " / " + total + " " + Loc("Active")` em vez de `total + " " + Loc("Active")` do plano — "3 ativas" com o total seria mentira; "2 / 3 ativas" é o mesmo número do Mínimo.
- `LcdMod_Antenna_MaxRange` entra como rótulo da linha 2 do Mínimo (com fallback para só a distância); o plano listava a chave mas não dizia onde usá-la.
- `active` conta pelo bloco (não por `StatusColor`), porque `StatusColor` é `ForegroundColor` tanto para "transmitindo" quanto para "desligado" nos collectors.

## Chaves resx novas (NÃO adicionadas; orquestrador aplica em `MyTexts.resx` e `MyTexts.pt-BR.resx`)

| Chave | en | pt-BR |
|---|---|---|
| `LcdMod_Antenna_Active` | `active` | `ativas` |
| `LcdMod_Antenna_MaxRange` | `Max range` | `Maior alcance` |

Conferido por grep: nenhuma das duas existe hoje (`LcdMod_Antenna` só como título `[SD] Antenna`).

## Texturas

- `"SD_Icon_BeaconBroadcast"` **não existe** em `Core/Data/Textures.sbc` / `Texture-Remaps.sbc`. Usado `"RadioAntenna"` (remap de `Textures/Sprites/Hud/SignalMode.dds`, `Texture-Remaps.sbc:20`), o mesmo ícone que o app já desenha para rádio transmitindo. Candidato a `BareIcons.AntennaRadio` na Task 10. Existem também `"BeaconBroadcast"` (custom) e `"RadioAntennaDisabled"`.
- Linhas do Compacto usam `entry.StatusIcon` (`RadioAntenna`, `RadioAntennaDisabled`, `BeaconBroadcast`, `BroadcastingOn/Off`, `RotationPlane`, `Search`, `Disconnected`, `GridPower`, `Warning`), todos já registrados.

## Verificação

- `check.ps1` não rodou (sem dotnet/DLLs). Em vez disso:
  - Grep de cada membro usado: `AntennaEntry.EntryId/Name/StatusIcon/StatusText/StatusColor` (`AntennaEntry.cs`); `IMyRadioAntenna.Radius/IsBroadcasting`, `IMyBeacon.Radius`, `IMyLaserAntenna.Status` com `MyLaserAntennaStatus.Connected` (usados nos collectors com `Sandbox.ModAPI` / `Sandbox.ModAPI.Ingame`); `MyAPIGateway.Entities.GetEntityById` (uso existente em Info); `Bare.Text/Kv/Margin/LineHeight`, `BareLayout.IsWide/KvRow` (assinaturas de `BareLayout.cs`); `IsMinimal/IsCompact/Theme/GetHeaderColor/GetErrorColor/GetForegroundColor/GetWarningColor` (`AppBase.cs`); `Host.ViewBox/DrawMessage` (`IAppHost`); `LocHelper.Empty/GetLoc`; `FormatingHelper.DistanceToString(float, string = "0.##")`; construtor `MySprite` de 7 argumentos igual ao de `BareLayout.Label`.
  - Aliases `using` para `IMyBeacon`/`IMyRadioAntenna`/`IMyLaserAntenna`/`MyAPIGateway`/`MyLaserAntennaStatus` para não importar `Sandbox.ModAPI` inteiro (evita ambiguidade com `Sandbox.ModAPI.Ingame`); nenhum nome colide com os `using` já existentes no arquivo.
  - C# 6: `out` declarado antes, sem tuplas/pattern/`out var`/LINQ. Sem comentários, sem TODO.
  - Line endings: 0 `\r` no arquivo (LF). Diff relido inteiro.
- Não executado: `.\check.ps1`, `.\build.ps1`, jogo.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/AntennaApp.cs` (sem git).

## Preocupações

- `GetEntityById` por entrada por frame (lookup em dicionário): barato, mas é uma chamada ao jogo a mais por antena no Mínimo/Compacto. Se quiser zero chamadas, o caminho é `AntennaEntry` expor `Range`/`IsTransmitting` preenchidos pelos collectors (arquivos fora do escopo desta task → não feito).
- `FirstLine` faz `Substring` por frame só para entradas sem alcance (laser, desligado, danificado). Mesma ordem de alocação que as concatenações de string do cabeçalho (padrão igual ao `DefenseDashboardApp`).
- Rádio desligado mostra o texto de estado em vez do alcance (escolha deliberada: `range` zerado quando `!Enabled || !IsFunctional`), e o alcance máximo do Mínimo só considera antenas ligadas e funcionais.
- O cabeçalho do Compacto ocupa `LineHeight + Margin`; em LCD muito baixo o `Kv` pode ficar com 0 linhas (só cabeçalho). Aceitável pela regra "nunca meia linha".
