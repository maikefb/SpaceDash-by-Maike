# Task 5 — Barra padrão (`ThemeChrome.DrawBar` estilo Relógio) — Relatório

## Status: DONE_WITH_CONCERNS

(Concerns só por não ter compilador na máquina; ver "Verificação".)

## O que mudou por arquivo

- `Core/Data/Scripts/Core/Client/Gui/Styling/ThemeChrome.cs`
  - `DrawBar` ganha `bool showPercent = false` e perde o `switch (theme)`: desenho único para todos os temas. Com `bar.Height >= 2f` usa `BarPanel.CreateSprites(sprites, bar.Position, bar.Size, fill, track, fraction, cornerRadius: bar.Height * 0.5f)` — é exatamente o que o Default (e portanto o `ProgressBar` do Relógio) já desenhava: trilho em pílula (`"Circle"` nas pontas + `"SquareSimple"` no meio, com bleed anti-costura) e preenchimento igual, sem borda. Abaixo de 2 px: dois `LineRenderer.Rect` (trilho + fill). Trilho = `WithAlpha(foreground, CLEAN_LINE_ALPHA)` (0.15). Decisão: reaproveitar `BarPanel` em vez de desenhar à mão, porque já produz o visual pedido e é o visual de referência (Relógio); desenhar à mão duplicaria `AddPill`.
  - Percentual: só com `showPercent` **e** `rect.Height >= PERCENT_MIN_HEIGHT * scale`; mesmo cálculo de `textScale`/`textWidth` de antes (a folga `thickness * 2f` virou `Math.Max(1f, scale) * 2f` inline, já que `thickness` só servia à borda do Futuristic).
  - `///` de `DrawBar` ajustado. Nenhuma constante ficou órfã (`OUTLINE_ALPHA` segue usada em `DrawFrame`/`DrawCard`/`DrawIconCell`; `PERCENT_*` e `CLEAN_LINE_ALPHA` seguem usadas).
- `Info/Data/Scripts/Info/Client/Gui/ControlsTemplates/Custom/Energy/EnergyCardRenderer.cs`
  - As duas barras da tabela rótulo | valor (:211 empilhada, :223 lado a lado) passam a chamar `ThemeChrome.DrawBar(..., palette.Label, metrics.S, showPercent: true)` diretamente (eram `DrawBar(..., bipolar: false, ...)` local, que já delegava a `ThemeChrome`).
  - `DrawBar` local (:505) mantido: ramo não bipolar delega a `ThemeChrome.DrawBar` sem percentual (usado só por `DrawValueBox`, cujo valor-herói já é o número); ramo bipolar continua à mão.
- `Core/Data/Scripts/Core/Client/Gui/Frames/FrameRenderer.cs`
  - `Bar(sprites, field, ratio, color)` deixa de delegar a `Bar(from, to)` e chama `ThemeChrome.DrawBar(sprites, LcdTheme.Default, ToSurface(slot.Box), ratio, color, color, _scale.Y)`. `Bar(from, to)` fica como está e segue servindo `BipolarBar`. Novos `using LcdMod.Client.Gui.Styling;` e `using LcdMod.Common.Config.Components;`.
  - `FrameRenderer` não conhece tema nem primeiro plano: o tema é ignorado pelo novo `DrawBar` (passei `Default`); como "foreground" passei a própria cor do preenchimento, então o trilho é a cor da barra a 15 % por cima do trilho que a textura da moldura já traz. `scale` só influencia o percentual (desligado).
- Sem mudança (já chamavam `ThemeChrome.DrawBar`): `LiftCapacityApp.DrawRow` (:257), `EnergyGeneralApp.DrawCharge` (via `DrawValueBox` → `EnergyCardRenderer.DrawBar` → `ThemeChrome.DrawBar`), `GasApp` (:502), `BareLayout.Kv` (:163), `FarmPlotControl` (:149/:150), `ProgressBar` (:46). Não há mais nenhum `"SquareSimple"` desenhado à mão como barra em `EnergyCardRenderer`.

## `showPercent: true` — onde e por quê

- `EnergyCardRenderer` :211 e :223 (linhas da tabela com `BarFraction >= 0`): quando a linha tem barra o layout **não** escreve `row.Value` (o `if (hasBar) DrawBar(...) else DrawText(value)`), logo o percentual da barra era a única leitura numérica da linha (Carga, Disponibilidade). Sem `true` o número sumiria.
- Não passei (e registro) em:
  - `ProgressBar.cs:46` (Relógio): brief manda não mudar → percentual deixa de aparecer nas barras altas do Relógio (regra 9: "desligado por padrão").
  - `GasApp`, `LiftCapacityApp`, `BareLayout.Kv`: o valor já está em texto na linha.
  - `EnergyCardRenderer.DrawBar` local / `DrawValueBox`: valor-herói já mostra o número.
  - `FarmPlotControl`: barras de crescimento/água sob o ícone, sem texto numérico ao lado; em dúvida, não passei (antes o percentual aparecia se a barra tivesse ≥ 10 px × escala; agora não).
  - `FrameRenderer.Bar`: moldura tem campos de valor próprios.

## O que ficou à mão

- Barra bipolar de `EnergyCardRenderer.DrawBar` (:516-524): trilho via `BarPanel.CreateBackgroundSprites`, fill a partir do centro e marcador central. `ThemeChrome.DrawBar` único é 0..1 da esquerda; não cobre barra bipolar. Mantida.
- `FrameRenderer.Bar(from, to)` + `BipolarBar`: idem (saldo da moldura Futuristic).

## Verificação

- `check.ps1` não rodou (sem `dotnet`/DLLs). Em vez disso:
  - `grep -rn "DrawBar(" Core Info` — 9 chamadores de `ThemeChrome.DrawBar`, todos com 7 posicionais (+ `showPercent: true` nos 2 do Energy). Nenhum usa a assinatura antiga de forma incompatível.
  - `grep -n "switch (theme)" ThemeChrome.cs` — nenhum dentro de `DrawBar` (o de :221 é `DrawIconCell`).
  - Assinaturas conferidas por leitura: `BarPanel.CreateSprites(List<MySprite>, Vector2, Vector2, Color, Color, float, Color? = null, float cornerRadius = -1f, ProgressBarStyle = PillBleed)`; `LineRenderer.Rect(Vector2, Vector2, Color)`; `LcdTheme` em `LcdMod.Common.Config.Components`; `FrameSlot.Kind == 'b'` e `ToSurface` já existentes em `FrameRenderer`.
  - C# 6: só argumento nomeado (`cornerRadius:`, `showPercent:`), `out` com declaração prévia. Sem LINQ.
  - Line endings: `git ls-files --eol` → `i/lf w/lf` nos 3 arquivos; 0 `\r`.
- Não executado: `check.ps1`, build, teste no jogo.

## Arquivos alterados

- `Core/Data/Scripts/Core/Client/Gui/Styling/ThemeChrome.cs`
- `Core/Data/Scripts/Core/Client/Gui/Frames/FrameRenderer.cs`
- `Info/Data/Scripts/Info/Client/Gui/ControlsTemplates/Custom/Energy/EnergyCardRenderer.cs`

## Preocupações

- Sem compilação local; risco residual só `check.ps1` pega.
- Mudança visual nos temas Futuristic/Clean/Cafe/Small: as barras passam de retângulo com/sem borda para pílula. É o que a regra 7 pede, mas afeta todos os temas, não só Mínimo/Compacto.
- `FrameRenderer.Bar(ratio)`: trilho tintado (cor do fill a 15 %) sobreposto ao trilho da textura da moldura, e fill arredondado dentro de um trilho que pode ser retangular em alguma textura. Se ficar ruim no jogo, a alternativa é reverter só esse método para `Bar(0, ratio)`.
- Percentual sumiu onde antes aparecia por altura (Relógio, Farm, Gás/Elevação em barras altas). Intencional pela decisão 9, mas vale checar no jogo que nenhuma tela perdeu um número que só a barra mostrava.
- `ProgressBar` tem `CornerRadius`/`ProgressBarStyle` que já não chegam ao desenho (já era assim antes desta task); fora do escopo.
