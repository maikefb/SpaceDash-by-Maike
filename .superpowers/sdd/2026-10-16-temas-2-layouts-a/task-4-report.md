# Task 4 — Gráfico de energia em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; ver "Verificação".)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/EnergyChartApp.cs`:

- `GetSprites`: entrada `if (IsMinimal) { DrawMinimal(_sprites); return _sprites; } if (IsCompact) { DrawCompact(_sprites); return _sprites; }` logo após `_sprites.Clear()` e antes do ramo `Host.Frame != null`. O app não chamava `Host.AddBackground` antes e continua não chamando (adicionar mudaria o layout cheio; nenhum app de energia o chama).
- Step 1 — `DrawPlotWithoutAxis(plot, history, head, axisMax, color)` virou `DrawCurve(List<MySprite> sprites, RectangleF plot, float[] history, int head, float axisMax, Color seriesColor)` (mesmo corpo, lista por parâmetro). Os dois chamadores do layout cheio (`DrawTiny`, `DrawChartBlock`) passam `_sprites`: única mudança no layout cheio. Overload novo `DrawCurve(List<MySprite>, RectangleF, EnergySnapshot)`: quadro + série produzida (`Palette.ValueNeutral`) + série usada (`Palette.ChartUsed`) no mesmo plot, escala `AxisMax(s)`, sem eixo nem legenda.
- `PeakMw(s)`: máximo de `ProducedHistory` e `UsedHistory` por dois `for` (sem LINQ novo; os dois `.Max()` pré-existentes ficam).
- `BalanceColor(s)`: `NetMw >= 0 ? GetHeaderColor() : GetErrorColor()` (decisão 5).
- `DrawMinimal`: sem dados → `Bare.Hero(NotAvailable, ViewBox, 2.5f, fg)` e nada mais. Com dados: herói `PowerText(NetMw, signed: true)` a 2,5× na metade superior com cor de saldo; linha auxiliar `Bare.Text(1f)` "Produzida X  Usada Y  Pico 1 h Z" centrada entre o herói e a curva; `DrawCurve(s)` no terço inferior. LCD largo (`BareLayout.IsWide`): herói na metade esquerda; à direita a linha no topo e a curva embaixo (decisão 15).
- `DrawCompact`: sem dados → `Host.DrawMessage(sprites, EmptyMessage, "Warning", GetWarningColor())` (`EmptyMessage` = `LcdMod_Energy_NoUnits`, a mensagem de "sem dados" que o app já usa). Com dados: `DrawCurve(s)` na metade esquerda; `Bare.Kv` na direita com `readonly List<BareLayout.KvRow> _kv` limpa por frame, `Bar = -1f` em todas: produção, consumo, saldo (cor de saldo), pico (`LcdMod_Frame_Peak1h`), e, só com baterias (`HasBatteries`, regra 4), armazenado (`MegaWattHoursToString(StoredMwh)`) e autonomia (rótulo `FullIn`/`EmptyIn` conforme `BatteryStatus`, como `EtaRow`; valor `EtaText(EtaSeconds)` ou `N/A` quando `NO_ETA`).
- Constantes: `MINIMAL_HERO_FONT_MUL = 2.5f`, `MINIMAL_CURVE_SHARE = 1f/3f`, `PEAK_KEY = "LcdMod_Frame_Peak1h"`. `using LcdMod.Client.Gui.Styling` adicionado (tipo `BareLayout.KvRow`).

## Chaves resx novas

Nenhuma. Reutilizadas: `LcdMod_Energy_Produced/Used/Balance/Stored/FullIn/EmptyIn/NoUnits`, `LcdMod_Frame_Peak1h` ("Peak 1 h" / "Pico 1 h"), `LcdMod_NotAvailable`.

## Verificação

- `check.ps1`/jogo não rodaram (sem `dotnet`/DLLs). Em vez disso:
  - Grep de cada membro usado: `BareLayout.Hero/Text/Kv/Margin/LineHeight/IsWide`, `BareLayout.KvRow` (campos `Label, Icon, Value, ValueColor, Bar`), `App.Bare/IsMinimal/IsCompact/Theme/GetHeaderColor/GetErrorColor/GetWarningColor`, `IAppHost.ViewBox/ForegroundColor/DrawMessage(sprites, message, icon, color, scale = 1f)`, `EnergySnapshot.NetMw/ProducedMw/ConsumedMw/StoredMwh/EtaSeconds/NO_ETA/HasData/HasBatteries/BatteryStatus/ProducedHistory/UsedHistory/HistoryHead`, `EnergyBatteryStatus.Charging`, `EnergyAppBase.PowerText/EtaText/Loc/NotAvailable/EmptyMessage/LineThickness/AxisMax`, `FormatingHelper.MegaWattHoursToString`, `LineChartPanel.DrawFrame/DrawSeries`, `LocHelper.GetLoc`. Texturas `IconEnergy`, `Battery`, `Warning` existem em `Core/Data/Textures.sbc`.
  - Releitura do diff: C# 6 só (sem `out var`, tuplas, pattern matching); sem variáveis sem uso; `DrawPlotWithoutAxis` não sobrou em lugar nenhum; nenhum `.Max()` novo.
  - LF preservado (`grep -c '\r'` = 0; `git ls-files --eol` = `i/lf w/lf`).
- Não executado: `.\check.ps1`, `.\build.ps1`, teste no jogo.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/EnergyChartApp.cs`

Sem git (ruling do orquestrador).

## Preocupações

- Sem compilação local: risco residual de erro só detectável por `check.ps1`.
- Mínimo com `IsWide`: o herói ocupa a metade esquerda em altura total e a curva fica à direita sob a linha; em LCD 1×3 com fonte grande a linha pode não caber (some, regra 2) e a curva ocupa quase tudo à direita.
- `PowerText(..., float.MaxValue)` dá sempre duas casas; o herói a 2,5× some inteiro se não couber na largura (regra 2), sem fallback para menos casas.
- A linha auxiliar do Mínimo concatena strings por frame (três `PowerText` + rótulos); mesmo padrão de `DrawTiny`.
- Linhas de bateria do Compacto somem sem baterias (regra 4) em vez de mostrar `N/A`; se o revisor preferir `N/A` fixo (brief), é trocar o `if (s.HasBatteries)` por valores `NotAvailable`.
- Observação fora do escopo: o diff em andamento de `LiftCapacityApp.cs` (Task 1) usa `ViewBox`, `ForegroundColor`, `BackgroundColor` sem `Host.`; não encontrei esses membros em `App`/`AppBase`/`ControlBase` por grep — vale o revisor conferir.
