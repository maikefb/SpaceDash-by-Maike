# Task 5 — Relógio em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; ver "Verificação". Sem commit: o orquestrador commita. Sem .resx editado: chaves novas abaixo.)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/InGameClockDashboardApp.cs`:

- Constantes `MINIMAL_HERO_FONT_MUL = 4f`, `MINIMAL_HERO_RATIO = 0.7f`, `ICON_BASE = 16f`, `SEPARATOR = " · "`, `ICON_SUNRISE/ICON_SUNSET/ICON_OXYGEN/ICON_UPTIME`; campos `readonly string _timerIcon = ResolveTimerIcon()` (reaproveitado pelo `MetricValueControl` do layout cheio, que antes chamava `ResolveTimerIcon()` inline) e `readonly List<BareLayout.KvRow> _kv`.
- `GetSprites`: logo após `sprites.Clear()`, `if (IsMinimal) { EnsureSnapshot(); DrawMinimal; return } if (IsCompact) { EnsureSnapshot(); DrawCompact; return }`, antes do `GetContentBounds`. Sem `AddBackground` no app: `InGameClockDashboard.cs` (surface script) já chama `AddBackground` e `DrawTitle` (que retorna cedo em temas bare). A leitura preguiçosa do snapshot que existia inline virou `EnsureSnapshot()` (mesmas 4 linhas), chamada também pelo layout cheio; nada mais do layout cheio mudou.
- `HasSolarEvents()`: `ClockMode == LocalSolar && HasLocalSolarTime && (HasTerrainSunrise || HasTerrainSunset)` (mesma condição do layout cheio, mais a exigência de pelo menos um evento).
- `NextSolarEvent(out icon)`: com eventos solares, calcula o delta modular (`ClockDashboardSolarTime.PositiveModulo(evento - LocalSolarHour, 24)`) para nascer e pôr e escolhe o mais próximo; devolve `FormatSolarEventTime(hora, config)` e o ícone `WeatherSunRise`/`WeatherSunSet`. Sem eventos: devolve `FormatDayMoment(DayMoment)` e ícone nulo (espaço profundo, polar, sem ciclo).
- `DrawMinimal`: área = `Host.ViewBox`, `fg = GetForegroundColor()`. Herói = `FormatCompactTime(DisplayDateTime, ClockDashboardComponent)` (24 h "HH:mm"; 12 h "hh:mmtt" minúsculo, com am/pm; `Bare.Hero` desenha o texto inteiro ou nada, então o sufixo nunca é cortado). Em `BareLayout.IsWide(view)`: herói na metade esquerda, linha à direita (LEFT, centrada verticalmente); senão herói nos 70 % superiores e linha centrada no 30 % restante. Snapshot nulo: herói "N/A" (`LcdMod_NotAvailable`).
- `DrawMinimalLine`: ícone dia/noite (`ClockDashboardSpriteMap.ResolveDayMomentIcon`) + ícone do evento (se houver) + texto; lado do ícone = `min(altura da linha, 16 × ConfiguredScale)`, gap = `Bare.Margin`. Largura do texto medida com `MeasureText(text, Bare.FontScale)`; se não cabe, o texto some e ficam os ícones (regra 2); se nem os ícones cabem, nada. Texto via `Bare.Text(fontMul 1f)`.
- `DrawCompact`: `Bare.Kv(sprites, Host.ViewBox, _kv, fg, Theme)` com, nesta ordem:
  1. hora (`Label`) · ícone dia/noite · data (`Value = FormatCompactDate`);
  2. só com `HasPlanet`: `Label = WeatherDisplayName` (fallback `Unavailable`), `Icon = ResolveWeatherIcon(WeatherSubtype)`, `Value` = temperatura ambiente (`FormatingHelper.TemperatureToString` com o modo configurado, ou `Unavailable`), `ValueColor = TemperatureColor(nível)`;
  3. só com `HasSolarEvents()`: `Label = LcdMod_ClockDashboard_SunEvents`, `Icon = WeatherSunRise`, `Value = nascer · pôr` (só o disponível quando falta um);
  4. O₂: `Label = LcdMod_ClockDashboard_Oxygen`, `Icon = IconOxygen`, `Value = FormatOxygen`, `ValueColor`/`BarColor = OxygenColor(ratio)`, `Bar = ratio` (clamp 0..1);
  5. tempo de jogo: `Label = ClockDashboardLocalization.PlayTime`, `Icon = _timerIcon`, `Value = FormatDuration(ElapsedPlayTime)`;
  6. servidor ligado: `Label = ServerUptime`, `Icon = BeaconBroadcast`, `Value = FormatDuration(WorldElapsedSeconds(GameDateTime))`.
  Snapshot nulo: `Host.DrawMessage(sprites, ClockDashboardLocalization.Unavailable, "Warning", GetWarningColor())`. Sem sessão: durações 0 (igual ao layout cheio).

## Divergências em relação ao brief

- Ícones `SD_Icon_Sun*` do brief não existem no atlas; usei os que o layout cheio já usa: `WeatherSun`, `WeatherMoon`, `WeatherSunRise`, `WeatherSunSet` (em `Core/Data/Textures.sbc`), `IconOxygen` e `BeaconBroadcast` (vanilla), ícone de clima via `ClockDashboardSpriteMap.ResolveWeatherIcon`, timer via `ResolveTimerIcon()`.
- Linha "hora e data": `Value` = só a data (`yyyy/MM/dd`), sem o dia da semana do layout cheio, para a linha caber em LCD pequeno.
- Sem planeta a linha de clima/temperatura some (brief); em planeta sem clima o rótulo vira `Unavailable` com ícone `WeatherSun`.
- Sem evento solar o Mínimo mostra o momento do dia (`FormatDayMoment`) no lugar da hora do próximo evento.

## Chaves resx novas (não adicionadas; a incluir em `MyTexts.resx` e `MyTexts.pt-BR.resx`)

| Chave | en | pt-BR |
|---|---|---|
| `LcdMod_ClockDashboard_SunEvents` | `Sunrise · Sunset` | `Nascer · Pôr` |
| `LcdMod_ClockDashboard_Oxygen` | `Oxygen` | `Oxigênio` |

Chaves existentes usadas: `LcdMod_NotAvailable`, `LcdMod_Common_Value_Unavailable`, `LcdMod_ClockDashboard_PlayTime`, `LcdMod_ClockDashboard_ServerUptime`, `LcdMod_ClockDashboard_DayMoment_*`.

## Texturas

Nenhuma nova. Usadas: `WeatherSun`, `WeatherMoon`, `WeatherSunRise`, `WeatherSunSet`, `Weather*` (clima), `IconOxygen`, `BeaconBroadcast`, `Warning`, textura do bloco Timer via `TextureHelper.GetOrAddTextureForBlock` (fallback `Circle`).

## Verificação

- `check.ps1` **não rodou** (sem `dotnet`/DLLs aqui). Em vez disso:
  - Grep/leitura de cada membro usado: `App.IsMinimal/IsCompact/Bare/Theme/GetForegroundColor/GetWarningColor/MeasureText(string, float)` (`AppBase.cs`); `BareLayout.Hero/Text/Kv/KvRow/Margin/Scale/FontScale/LineHeight/IsWide`; `IAppHost.ViewBox/DrawMessage(sprites, message, icon, color, scale = 1f)`; `ClockDashboardFormatter.FormatCompactTime/FormatCompactDate/FormatSolarEventTime/FormatDayMoment/FormatOxygen/FormatDuration/WorldElapsedSeconds`; `ClockDashboardSpriteMap.ResolveDayMomentIcon/ResolveWeatherIcon`; `ClockDashboardSolarTime.PositiveModulo` (public static); `ClockDashboardLocalization.Unavailable/PlayTime/ServerUptime`; campos do `ClockDashboardSnapshot` (`ClockDashboardTypes.cs`); `ClockEnvironmentReader.Read` nunca devolve nulo (`CreateDefaultSnapshot`), mas o nulo é tratado mesmo assim; `Constants.MOD_PREFIX` (`LcdMod.Common.Helpers.Constants`, já usado no arquivo via `Constants.MIN_SCREEN_HEIGHT_TO_WIDTH_RATIO`; `Generated` não está importado, sem ambiguidade); `MathHelper.Clamp` (VRageMath); `FormatingHelper.TemperatureToString` (mesma chamada do layout cheio).
  - Precedência de `sunrise != null && sunset != null ? a : sunrise ?? sunset`: `??` liga antes de `?:`, resultado é o pretendido.
  - Inicializador de campo `_timerIcon = ResolveTimerIcon()`: método estático, roda antes do corpo do construtor; `_squarePlayTimeValue` recebe o mesmo valor de antes.
  - C# 6: sem tuplas, `is` pattern, `out var`, `default` literal; `out string eventIcon;` declarado antes. Sem LINQ. Sem alocação nova por frame além das structs na lista reutilizada e das strings de formatação (iguais às do layout cheio).
  - Releitura do `git diff` do arquivo; LF (0 `\r`); sem comentários, sem TODO; layout cheio intocado além da extração de `EnsureSnapshot` e do campo `_timerIcon`.
- Não executado: `check.ps1`, `build.ps1`, jogo (12 h e 24 h).

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/InGameClockDashboardApp.cs` (+225/-12)

## Preocupações

- Review Focus 5: em 12 h a hora é `"hh:mmtt"` minúsculo ("06:30pm"), 7 caracteres a 4× a fonte; em LCD 1×1 com fonte 1 deve caber; em fonte maior some inteira (regra 2), nunca cortada. Validar no jogo.
- O `Bare.Text` do Mínimo compara a altura medida com `box.Height = LineHeight(1f)`; é o mesmo que `Bare.Hero` faz. Se a medição de uma string sem descendentes vier maior que a sonda "Ag" (não deveria), o texto some e ficam só os ícones.
- Em `IsWide`, a linha fica à direita do herói com LEFT, como no Gás; no 1×3 a hora a 4× pode não caber na metade esquerda com fonte > 1 e sumir.
- `IconOxygen` e `BeaconBroadcast` não estão em `Textures.sbc`; são texturas vanilla do jogo já usadas pelo layout cheio.
- Duas chaves resx novas pendentes (tabela acima): até serem adicionadas, `LocHelper.GetLoc` devolve a própria chave nos rótulos de nascer/pôr e O₂.
