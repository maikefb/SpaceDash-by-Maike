# Task 10 — `BareIcons`, lista de ícones pendentes do atlas e verificação — Relatório

## Status: DONE_WITH_CONCERNS

(Concerns por não ter compilador na máquina e pelo passo 3 — `check.ps1`/`build.ps1`/jogo — ser do usuário; ver "Verificação".)

## O que foi implementado

- `Core/Data/Scripts/Core/Client/Gui/Styling/BareIcons.cs` (novo, `public static class BareIcons`, namespace `LcdMod.Client.Gui.Styling`): 32 `public const string`, uma por conceito realmente usado nos ramos Mínimo/Compacto, valor = fallback que o código já usava, e um `///` de uma linha com o nome final `SD_Icon_<Nome>` da spec ou "Já definitivo".
- Nos apps, só dentro de `DrawMinimal`/`DrawCompact` e dos helpers criados pelos Planos 2/3 (`KindIcon`, `NextSolarEvent`, `StoredRow`), cada literal/constante local de ícone de conceito passou a `BareIcons.X`. Constantes locais que só os ramos novos usavam foram removidas; as que o layout cheio também usa (`ICON_GRAVITY`/`ICON_MASS` do Lift, `ICON_POWER`/`ICON_BATTERY` do EnergyChart, `ICON_H2`/`ICON_O2`/`ICON_ICE`/`ICON_FALLBACK`/`LABEL_ICONS` do Gas, `ICON_UPTIME` do Relógio) ficaram intactas.
- Formas geométricas (`Circle`, `SquareSimple`, `CircleHollow`, `Triangle`, `AH_VelocityVector`, `MEDAL_SPRITE = "Circle"`) não foram tocadas.
- `docs/superpowers/specs/2026-10-16-icones-atlas.md`: seção `## Pendentes` no fim (tabela constante | fallback atual | nome final), gerada pelo script.

### Constantes (fallback → nome final)

| Constante | Fallback | Nome final | Usada em |
|---|---|---|---|
| `Warning` | `Warning` | já definitivo (atlas do mod) | `DrawCompact` de 14 apps (`Host.DrawMessage`) |
| `Balance` | `IconEnergy` | `SD_Icon_Balance` | EnergyGeneral tile, EnergyChart kv |
| `Gauge` | `IconTemperature` | `SD_Icon_Gauge` | LiftCapacity tile "LiftUsage" |
| `Hourglass` | `IconEnergy` | `SD_Icon_Hourglass` | PowerFilled kv "Autonomy" |
| `Mass` | `IconEnergy` | `SD_Icon_Mass` | LiftCapacity tiles "CanCarry"/"ShipMass" |
| `SafeMass` | `ShieldIcon` | `SD_Icon_SafeMass` | LiftCapacity tile "SafeMax" |
| `Production` | `IconEnergy` | `SD_Icon_Production` | EnergyGeneral, EnergyChart, EnergyCard (Output/MaxOutput e fallback do `KindIcon`) |
| `Consumption` | `IconEnergy` | `SD_Icon_Consumption` | EnergyGeneral, EnergyChart, EnergyCard "Input" |
| `Peak` | `IconEnergy` | `SD_Icon_Peak` | EnergyChart kv "Peak1h" |
| `Storage` | `Battery` | `SD_Icon_Storage` | EnergyChart kv "Stored" |
| `Battery` | `Battery` | já definitivo (atlas do mod) | EnergyGeneral "Charge", EnergyCard `KindIcon` |
| `TankH2` / `TankO2` | `IconHydrogen` / `IconOxygen` | `SD_Icon_TankH2` / `SD_Icon_TankO2` | Gas `StoredRow` |
| `HydrogenEngine` | `IconHydrogen` | `SD_Icon_HydrogenEngine` | EnergyCard `KindIcon` (Hydrogen) |
| `Charging` / `Discharging` | `Battery` | `SD_Icon_Charging` / `SD_Icon_Discharging` | EnergyChart kv "FullIn"/"EmptyIn" |
| `Hydrogen` / `Oxygen` | `IconHydrogen` / `IconOxygen` | já definitivo (jogo) | Gas kv "Flow", Relógio kv "Oxygen" |
| `Generator` | `IconEnergy` | `SD_Icon_Generator` | Gas kv "Generators" (fallback de `_genIcon`) |
| `Ice` | `MyObjectBuilder_Ore/Ice` | `SD_Icon_Ice` | Gas kv "IceReserve" |
| `Sprout` | `IconEnergy` | `SD_Icon_Sprout` | Farm (fallback de `_plantIcon`) |
| `WaterDrop` / `NoWater` | `MyObjectBuilder_Ore/Ice` | `SD_Icon_WaterDrop` / `SD_Icon_NoWater` | Farm kv "AvgWater" / "NoWater" |
| `Sunrise` / `Sunset` | `WeatherSunRise` / `WeatherSunSet` | já definitivo (atlas do mod) | Relógio `NextSolarEvent`, kv "SunEvents" |
| `Server` | `BeaconBroadcast` | `SD_Icon_Server` | Relógio kv "ServerUptime" |
| `Ore` / `Ingot` / `Component` / `Ammo` / `Volume` | ícones de item/bloco do jogo | `SD_Icon_Ore` … `SD_Icon_Volume` | Inventory `DrawCompact` |
| `AntennaRadio` | `RadioAntenna` | `SD_Icon_AntennaRadio` | Antenna `DrawMinimal` |

Total: 32 constantes, 26 pendentes, 6 definitivas.

## Divergências em relação ao brief

- Nomes do brief sem uso nos ramos novos **não** foram criados (`AntennaLaser`, `Beacon`, `Medal*`, `BlockDamaged/Destroyed`, `Turret`, `Projector`, `ConveyorNetwork`, `Ok`, `Error`, `Day`, `Night`): Conveyor/Defesa/Projetor/Sobreviventes não usam ícone de conceito nos ramos novos (só texto, ícone do bloco/arma ou `Circle`), e o ícone dia/noite do Relógio vem de `ClockDashboardSpriteMap.ResolveDayMomentIcon`, compartilhado com o layout cheio (fora do escopo).
- `Shield` do exemplo virou `SafeMass` (conceito da spec para o tile "SafeMax"; `ShieldIcon` segue como fallback).
- `Sunrise`/`Sunset` e `Hydrogen`/`Oxygen` marcados como definitivos: a spec não lista nascer/pôr do sol (o atlas do mod já tem `WeatherSunRise`/`WeatherSunSet`) e aceita `IconHydrogen`/`IconOxygen` do jogo.
- `EnergyChartApp.DrawCompact`: introduzi `bool charging` para escolher rótulo e ícone (`Charging`/`Discharging`) sem repetir a comparação.
- `EnergyCardApp`: "Input" → `Consumption`, "Output"/"MaxOutput" → `Production`; fallback do bloco representativo também `Production`.
- `GasApp` kv "Flow" ficou com `Hydrogen`/`Oxygen` (mesma seleção de antes); não troquei para `FlowIn`/`FlowOut` porque mudaria a lógica de escolha do ícone, não só o literal.

## Verificação

- Sem compilador (`dotnet`/DLLs do jogo ausentes). Em vez disso:
  - Grep de cada constante removida (`ICON_SAFE`, `ICON_SUNRISE`, `ICON_SUNSET`, `ICON_OXYGEN`, `MINIMAL_ICON`, `ICON_WATER`, `ICON_PLANT`, `ICON_ORE/INGOT/COMPONENT/AMMO/VOLUME`, `ICON_HYDROGEN`, `ICON_POWER`/`ICON_BATTERY` em EnergyGeneral/EnergyCard) → 0 referências órfãs.
  - Grep de cada constante de `BareIcons` em `Info/.../Apps` → todas com ≥ 1 uso.
  - Grep dos literais de conceito (`Warning`, `IconEnergy`, `IconTemperature`, `IconHydrogen`, `IconOxygen`, `Battery`, `ShieldIcon`, `RadioAntenna`, `BeaconBroadcast`, `WeatherSun*`, `WeatherMoon`, `MyObjectBuilder_*`) em `Apps/`: só restam fora dos ramos novos — declarações de constantes locais do layout cheio, controles do layout quadrado do Relógio (linhas 128-145, 695), `ResolveForecastIcon` (879-900), `EnergyCardApp` 253 (cartão cheio), `DockingAlignmentApp` 541/736 (mensagem fora de `DrawMinimal`/`DrawCompact`), `RadarApp` 1638 e `Loc("Warning")` do `EnergyAppBase`.
  - `using LcdMod.Client.Gui.Styling;` já existia em todos os apps editados (só `ConveyorLayout`, `DigitalPictureFramesApp` e `LiftPhysics` não têm, e não foram tocados).
  - C# 6: `BareIcons.cs` só tem `const string` e comentários `///`; nos apps, apenas uma `bool` local nova. Nenhuma interpolação, `out var`, pattern matching ou `default` literal.
  - Line endings: `BareIcons.cs` e os apps editados em LF, UTF-8 sem BOM (`grep -c '\r'` = 0); `git ls-files --eol` mostra `i/lf w/lf` para os apps.
  - Script `python3 -I -X utf8 /tmp/claude-0/-home-user-SpaceDash-by-Maike/907ce8e1-8b0f-5e9b-a7bd-4e24f639ee0e/scratchpad/bare_icons_pending.py Core/Data/Scripts/Core/Client/Gui/Styling/BareIcons.cs` → 26 pendentes de 32; saída colada na spec.
- Não executado: `.\check.ps1`, `.\build.ps1`, teste no jogo (passo 3, do usuário).

## Arquivos alterados

- `Core/Data/Scripts/Core/Client/Gui/Styling/BareIcons.cs` (novo)
- `Info/Data/Scripts/Info/Client/Apps/AntennaApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/DefenseDashboardApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/EnergyCardApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/EnergyChartApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/EnergyGeneralApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/FarmApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/GasApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/InGameClockDashboardApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/InventoryApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/LiftCapacityApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/PowerFilledApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/ProjectorApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/RadarApp.cs`
- `Info/Data/Scripts/Info/Client/Apps/SurvivorsApp.cs`
- `docs/superpowers/specs/2026-10-16-icones-atlas.md` (seção "Pendentes")

Não tocados: `ItemsAppBase.cs`/`EnergyAppBase.cs` (nenhum ícone de conceito nos ramos novos), `DockingAlignmentApp.cs` (só geometria nos ramos novos), `.resx`, `.sbc`, `ThemeChrome`, `BareLayout`, `FrameLayouts`. Sem git (o orquestrador commita).

## Self-review

- Comportamento visual idêntico: todo `BareIcons.X` tem o mesmo valor que o literal/constante substituído.
- Nenhuma constante sem uso em `BareIcons`; nenhum comentário além do `///` de uma linha por constante e do da classe.
- `RadioAntenna` não está em `Core/Data/Textures*.sbc` (só `RadioAntennaDisabled`); já era assim antes desta task — é sprite do jogo.

## Preocupações

- Sem compilação local: risco residual só de typo; recomendo `check.ps1` antes do commit.
- Vários conceitos distintos compartilham o mesmo fallback (`IconEnergy` em 8 constantes, `Battery` em 4, `Ore/Ice` em 3): até o atlas chegar, tiles vizinhos do EnergyGeneral/EnergyChart/Lift mostram o mesmo ícone — mesmo visual de antes.
- Quando os `SD_Icon_*` entrarem no atlas, basta trocar o valor em `BareIcons.cs`; o layout cheio continua com os seus literais/constantes locais e precisará de passagem própria.

## Skills

Nenhuma invocada.
