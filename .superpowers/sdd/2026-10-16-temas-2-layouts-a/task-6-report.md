# Task 6 — Gás em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; ver "Verificação".)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/GasApp.cs` (+183 linhas, nenhuma removida; layout cheio e moldura intactos).

- Entrada em `GetSprites`: `if (IsMinimal) { DrawMinimal; return } if (IsCompact) { DrawCompact; return }` antes do ramo `Host.Frame != null`. Sem `Host.AddBackground`: o app nunca o chamou e `LiftCapacityApp` (Task 1, mesma árvore) também não; o host já compõe o fundo.
- Constantes/campos novos: `MINIMAL_HERO_FONT_MUL = 3f`, `TRACK_MIX = 0.15f`, `SEPARATOR = " · "`, `H2_COLOR = new Color(64, 156, 255)` (mesmo azul de `FarmPlotControl.WaterBarColor`), `O2_COLOR = Color.White`, `readonly List<BareLayout.KvRow> _kv` (limpa por frame).
- Helpers: `StatusText()` (Critical/Attention/Nominal via `Loc`, mesmas chaves `LcdMod_Gas_*` do layout cheio), `StatusColor(accent)` (error/warn/accent), `LevelText(stat, na)` (`Capacity == 0` → "N/A"), `MinimalLine()`, `FlowText(stat, na)` (mesma regra de sinal do `FillOutput`).
- `DrawMinimal`: trilho `Lerp(bg, fg, 0.15)`. LCD largo (`BareLayout.IsWide`): anéis na metade esquerda, linha à direita centrada verticalmente. Senão anéis em cima e linha (`fontMul 1f`) embaixo. Linha = `Status` + " · " + `Loc("H2Stored"|"O2Stored")` do tipo com menor `Level` (só tipos com tanque; nenhum tanque → só o status); cor = cor do status. Sem dados → linha nula (some).
- `DrawRings`: dois tipos com tanque → duas metades (empilhadas se `BareLayout.IsTall(area)`, lado a lado senão), H₂ azul e O₂ branco, `%` no centro em `fg` com mul 3 (`Ring` omite se não cabe). Só um tipo → anel único na área toda (centrado). Nenhum → anel vazio (fração 0, `fg`, sem texto). Fração = `Level` (0 quando `Capacity == 0`).
- `DrawCompact`: linhas kv, cada uma só quando existe o dado: H₂ (`% · Stored/Capacity`, barra `Level` azul, cor do valor error/warn/fg pelos contadores Low*), O₂ idem branco, Geradores (`ativos/total · produção/s`, warn se nenhum ativo), Vazão (`Loc("Flow")`: `rate H₂ · rate O₂`, só os tipos com tanque, warn se algum drena), Gelo (`Loc("IceReserve")`, `MassToString(_iceOnGrid + _iceInGens)`, warn se há gerador sem gelo). Zero linhas (⇔ `!HasData`) → `Host.DrawMessage(sprites, LcdMod_NotAvailable, "Warning", warn)`.

## Chaves resx novas

Nenhuma. Reutilizadas: `LcdMod_Gas_H2Stored`, `O2Stored`, `Generators`, `Flow`, `IceReserve`, `Critical`, `Attention`, `Nominal`, `LcdMod_NotAvailable`.

## Divergências em relação ao brief

- "Saída" é uma linha única rotulada `Loc("Flow")` ("Net flow") com ícone do primeiro gás presente, em vez de usar `Loc("H2Output")`/`Loc("O2Output")`: o brief pede uma linha "saída (H₂ rate · O₂ rate)" e esses rótulos são por gás. Trocar é uma linha.
- `StatusText()` duplica a expressão de status que `DrawFramed` e `DrawStatusBar` têm inline; não os alterei para não mexer no layout cheio.

## Verificação

- `check.ps1` não rodou (sem `dotnet`/DLLs). Em vez disso: releitura do diff; grep de cada membro usado: `Bare.Ring(s, area, fraction, fill, track, center, centerColor, mul)`, `Bare.Text(s, text, pos, align, mul, color, maxW, maxH)`, `Bare.Kv(s, area, rows, fg, theme)`, `Bare.Margin`, `Bare.LineHeight`, `BareLayout.IsWide/IsTall/KvRow{Label,Icon,Value,ValueColor,Bar,BarColor}`, `IsMinimal/IsCompact/Theme/GetHeaderColor/GetWarningColor/GetErrorColor` em `AppBase`, `Host.DrawMessage(sprites, message, icon, color)`, `FormatingHelper.VolumeToString/MassToString(double)`, `PercentageToString(float)`, `GasStat.Level/Stored/Capacity/RateLps/ProdLps/Tanks/HasRate/LowAttention/LowCritical`.
- C# 6: só `??`, inicializadores de objeto, ternários; sem tuplas, `out var`, pattern matching, LINQ. `Text`/`Ring` com texto nulo não desenham (guardas do helper), então nenhum `null` chega a `MySprite`.
- Line endings: 0 `\r` no arquivo (LF).
- Não executado: build, jogo (nave só com O₂, LCD 1×3, LCD alto).

## Preocupações

- `O2_COLOR = Color.White` fixo (decisão 5): no tema Café (fundo creme) o anel/barra de O₂ terá pouco contraste. Se incomodar, trocar por `Lerp(fg, White, …)` é local.
- Em LCD largo com dois anéis, cada anel fica com 1/4 da largura; em LCDs pequenos o `%` com mul 3 some (regra 2 da spec), fica só o anel.
- Review Focus 1 coberto (`Capacity == 0` → fração 0, "N/A"); Focus 2 (largo) e 4 (só O₂ → anel único centrado) por construção; sem teste no jogo.
