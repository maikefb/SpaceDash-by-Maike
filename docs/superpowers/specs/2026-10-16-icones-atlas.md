# Ícones para o atlas dos LCDs

Formato: cada ícone branco puro sobre fundo transparente, um só tom (o mod tinge pela cor do tema), 256×256 px, traço de 16 a 20 px, margem interna de 24 px. Nome do sprite no jogo: `SD_Icon_<Nome>`. Já existem no mod: Battery, BeaconBroadcast, Docking, Engine, FarmPlot, GridBroadcastingOn/Off, JumpDrive, Moon, OusideTemp, Reactor, ShieldIcon, SignalMode, Solar, Sun, SunRise, SunSet, Warning, Wind e os climas. O jogo já tem IconEnergy, IconHydrogen, IconOxygen, IconTemperature, Danger, Cross, Arrow, Circle, Triangle, SquareSimple. Ícones de item e de bloco vêm das definições do jogo; não entram no atlas.

## Genéricos (estado e tendência)

| Nome | Desenho | Onde |
|---|---|---|
| Ok | círculo com check | kv, tiles, estado geral |
| Error | círculo com X | kv, tiles, estado geral |
| Info | círculo com "i" | mensagens |
| TrendUp | seta diagonal para cima | tendência de carga, saldo |
| TrendDown | seta diagonal para baixo | idem |
| Balance | balança de dois pratos | saldo de energia, massa |
| Gauge | medidor de ponteiro em meia-lua | uso de elevação, % genérico |
| Hourglass | ampulheta | autonomia, tempo restante, tempo de jogo |
| Timer | cronômetro | próxima colheita, recarga |
| Percent | símbolo % | opção "mostrar porcentagem" |
| Power | símbolo liga/desliga | blocos desligados |
| Link | dois elos de corrente | redes de esteira, conexão |

## Elevação

| Nome | Desenho |
|---|---|
| Gravity | planeta com anel |
| Mass | peso de academia (kettlebell) |
| ThrustUp | seta grossa para cima com chama embaixo |
| MassMax | pilha de três placas |
| SafeMass | escudo com check |
| Carry | caixa com seta para dentro |

## Energia (painel, gráfico, cartões, preenchida)

| Nome | Desenho |
|---|---|
| Production | raio com seta para cima |
| Consumption | raio com seta para baixo |
| Peak | linha de gráfico com pico |
| Storage | cilindro (barril de dados) |
| BatteryLarge | bateria vertical grande, contorno só (preenchimento é feito por código) |
| TankH2 | tanque esférico com "H₂" |
| TankO2 | tanque esférico com "O₂" |
| HydrogenEngine | motor com gota |
| Charging | bateria com raio |
| Discharging | bateria com seta para fora |

## Gás

| Nome | Desenho |
|---|---|
| Generator | gerador (caixa com engrenagem e gota) |
| Ice | cristal de gelo (hexágono facetado) |
| FlowOut | tubo com seta saindo |
| FlowIn | tubo com seta entrando |
| Refuel | bomba de combustível |

## Esteiras

| Nome | Desenho |
|---|---|
| ConveyorNetwork | três quadrados ligados por tubos |
| BlockIntact | cubo inteiro |
| BlockDamaged | cubo com rachadura |
| BlockDestroyed | cubo tracejado com X |
| Bottleneck | funil |
| Sorter | funil com setas (classificador) |

## Fazenda

| Nome | Desenho |
|---|---|
| Sprout | broto com duas folhas |
| Harvest | cesto com planta |
| WaterDrop | gota |
| NoWater | gota cortada por barra |
| GrowthStage | planta em três alturas (pequena, média, cheia) |

## Relógio

| Nome | Desenho |
|---|---|
| Calendar | folha de calendário |
| Server | rack de servidor |
| GameTime | controle/joystick com relógio |
| Day | sol com horizonte |
| Night | lua com horizonte |

## Inventário (categorias)

| Nome | Desenho |
|---|---|
| Ore | pedra bruta |
| Ingot | lingote |
| Component | engrenagem com parafuso |
| Ammo | cartucho |
| Tool | chave inglesa |
| Container | contêiner de carga |
| Volume | cubo com régua |

## Projetor

| Nome | Desenho |
|---|---|
| Projector | projetor com feixe |
| Blueprint | planta (folha com grade) |
| Weld | maçarico com faísca |
| Armor | placa de blindagem |
| BlocksRemaining | cubo tracejado com número vazio |

## Sobreviventes

| Nome | Desenho |
|---|---|
| MedalGold | medalha com "1" |
| MedalSilver | medalha com "2" |
| MedalBronze | medalha com "3" |
| Alive | coração com pulso |
| Dead | caveira |
| Trophy | troféu (recorde) |
| You | silhueta de engenheiro com seta |

## Defesa

| Nome | Desenho |
|---|---|
| Turret | torreta gatling |
| Missile | míssil |
| Railgun | canhão longo com bobinas |
| Artillery | canhão curto |
| Autocannon | canhão duplo |
| AmmoBox | caixa de munição |
| Firing | boca de arma com clarão |
| Reloading | cartucho com seta circular |
| Crosshair | retícula |

## Acoplagem

| Nome | Desenho |
|---|---|
| Connector | conector (cilindro com flange) |
| Distance | régua com duas setas |
| Pitch | avião de perfil com seta vertical |
| Yaw | avião de cima com seta horizontal |
| Roll | avião de frente com seta circular |
| Aligned | retícula com check |

## Antena

| Nome | Desenho |
|---|---|
| AntennaRadio | antena com ondas |
| AntennaLaser | parábola com feixe reto |
| Beacon | farol com luz |
| Range | círculos concêntricos com seta |

## Radar

| Nome | Desenho |
|---|---|
| ContactShip | triângulo (nave) |
| ContactStation | quadrado com antena |
| ContactCharacter | silhueta pequena |
| Lock | cadeado com retícula |

Total: 100 ícones.

## Pendentes

Constantes de `Core/Data/Scripts/Core/Client/Gui/Styling/BareIcons.cs` (temas Mínimo e Compacto) que ainda apontam para um fallback em vez do sprite final do atlas. Gerado por `python -I tools/BareIcons/pending.py Core/Data/Scripts/Core/Client/Gui/Styling/BareIcons.cs`; as demais constantes (`Warning`, `Battery`, `Hydrogen`, `Oxygen`, `Sunrise`, `Sunset`) já usam o ícone definitivo do mod ou do jogo.

| Constante | Fallback atual | Nome final |
|---|---|---|
| `BareIcons.Balance` | `IconEnergy` | `SD_Icon_Balance` |
| `BareIcons.Gauge` | `IconTemperature` | `SD_Icon_Gauge` |
| `BareIcons.Hourglass` | `IconEnergy` | `SD_Icon_Hourglass` |
| `BareIcons.Mass` | `IconEnergy` | `SD_Icon_Mass` |
| `BareIcons.SafeMass` | `ShieldIcon` | `SD_Icon_SafeMass` |
| `BareIcons.Production` | `IconEnergy` | `SD_Icon_Production` |
| `BareIcons.Consumption` | `IconEnergy` | `SD_Icon_Consumption` |
| `BareIcons.Peak` | `IconEnergy` | `SD_Icon_Peak` |
| `BareIcons.Storage` | `Battery` | `SD_Icon_Storage` |
| `BareIcons.TankH2` | `IconHydrogen` | `SD_Icon_TankH2` |
| `BareIcons.TankO2` | `IconOxygen` | `SD_Icon_TankO2` |
| `BareIcons.HydrogenEngine` | `IconHydrogen` | `SD_Icon_HydrogenEngine` |
| `BareIcons.Charging` | `Battery` | `SD_Icon_Charging` |
| `BareIcons.Discharging` | `Battery` | `SD_Icon_Discharging` |
| `BareIcons.Generator` | `IconEnergy` | `SD_Icon_Generator` |
| `BareIcons.Ice` | `MyObjectBuilder_Ore/Ice` | `SD_Icon_Ice` |
| `BareIcons.Sprout` | `IconEnergy` | `SD_Icon_Sprout` |
| `BareIcons.WaterDrop` | `MyObjectBuilder_Ore/Ice` | `SD_Icon_WaterDrop` |
| `BareIcons.NoWater` | `MyObjectBuilder_Ore/Ice` | `SD_Icon_NoWater` |
| `BareIcons.Server` | `BeaconBroadcast` | `SD_Icon_Server` |
| `BareIcons.Ore` | `MyObjectBuilder_Ore/Iron` | `SD_Icon_Ore` |
| `BareIcons.Ingot` | `MyObjectBuilder_Ingot/Iron` | `SD_Icon_Ingot` |
| `BareIcons.Component` | `MyObjectBuilder_Component/SteelPlate` | `SD_Icon_Component` |
| `BareIcons.Ammo` | `MyObjectBuilder_AmmoMagazine/NATO_25x184mm` | `SD_Icon_Ammo` |
| `BareIcons.Volume` | `MyObjectBuilder_CargoContainer/LargeBlockLargeContainer` | `SD_Icon_Volume` |
| `BareIcons.AntennaRadio` | `RadioAntenna` | `SD_Icon_AntennaRadio` |

Total: 26 pendentes de 32 constantes.
