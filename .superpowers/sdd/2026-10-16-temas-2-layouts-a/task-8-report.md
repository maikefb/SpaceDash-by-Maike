# Task 8 — Alinhamento de acoplagem em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador nesta máquina; 4 chaves resx novas a adicionar pelo orquestrador; ver "Preocupações".)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/DockingAlignmentApp.cs`:

- `DrawAlignment` ganha os ramos `if (IsMinimal) { DrawMinimal(...); return; } if (IsCompact) { DrawCompact(...); return; }` no início, antes do layout cheio. O fundo já está em `_sprites` nesse ponto (`EnsureStaticSprites` chama `Host.AddBackground` + `Host.DrawTitle`; `Update` copia os estáticos antes de `DrawAlignment`).
- `EnsureStaticSprites`: `if (IsBare) return;` logo após `_layout = CalculateLayoutMetrics();`, para que os sprites estáticos do layout cheio (ilustração, ticks, arco de rolagem, rótulos, faixa de rodapé) não entrem em `_sprites` nos dois temas novos. Nada do layout cheio muda de comportamento.
- `DrawNoTarget(message)`: substitui as duas chamadas a `Host.DrawMessage` em `Update` (sem referência/sem alvo e alvo fora do cone). Mínimo → retícula vazia (`DrawMinimal(Vector3D.Zero, 0d, false)`); demais temas → `Host.DrawMessage` com a mesma mensagem de antes (`_selectReferenceMessage` / `_noTargetMessage`, chaves já existentes). Compacto, portanto, mostra a mensagem de "sem alvo" que o app já usava.
- `DrawMinimal(positionOffset, dockingAxisDistance, hasTarget)`:
  - `IsWide(ViewBox)`: quadrado de lado `min(W/2, H)` à esquerda, centrado na vertical; distância à direita. Senão: quadrado de lado `min(W, H - LineHeight(3) - Margin)` no topo, centrado; distância na faixa inferior.
  - Retícula (`DrawMinimalReticle`): duas linhas cruzando o quadrado inteiro via `LineRenderer.DrawLine` com `ThemeChrome.WithAlpha(fg, 0.4f)` e espessura `max(1, 1.5 * Scale)`; `DrawAlignmentTicks(_sprites, square, line)` reutilizado (assinatura `(List<MySprite>, RectangleF, Color)`); marcador de referência `CircleHollow` no centro como no layout cheio.
  - Marcador do alvo: `AH_VelocityVector` em `PositionToSquare(square, positionOffset)` (mapeamento extraído do layout cheio: clamp de `-X*0.025` e `Y*0.025` em ±0,36 → `NormalizedToSquare`), tamanho `square.Width * 0.1 * GetVectorTextureScale(Z)`; anel `CircleHollow` de 1,6× o marcador. Cor do marcador e do anel = `GetHeaderColor()` (acento) quando `|X|,|Y| <= 0,25 m`, senão `fg`.
  - Distância: `Bare.Hero(DistanceText(d), heroArea, 3f, fg)` com `FormatingHelper.DistanceToString((float)d)` em cache por centímetro.
- `DrawCompact(positionOffset, rotationOffset, dockingAxisDistance)`: `Bare.Hero(distância, 3f)` na faixa superior de altura `LineHeight(3)`; `Bare.Kv` em `_kvRows` (campo `readonly List<BareLayout.KvRow>`, `Clear()` por frame) com:
  - inclinação (`_pitchLabel`, `^`/`v`), guinada (`_yawLabel`, `>`/`<`), rolagem (`_rollLabel`, `CW`/`CCW`), valor `"seta graus°"` (`{0} {1:0.0}°`, módulo do ângulo) em cache por décimo de grau (`_angleTexts`/`_angleQuantized`), `ValueColor` por tolerância (`ToleranceColor`: ≤ 2° acento; ≤ 6° `GetWarningColor()`; senão `GetErrorColor()`);
  - linha final: rótulo = palavra de estado (`StateWord`: conector `Connected` → "Conectado"; `Connectable` → "Pronto"; senão posição XY ≤ 0,25 m e três ângulos ≤ 2° → "Alinhado"; senão "Ajustar"), valor = `_targetBlock.CustomName`.
  - Sentido das setas segue o layout cheio: inclinação positiva move a guia para cima (`^`), guinada positiva para a direita (`>`), rolagem positiva gira horário (`CW`).
- Constantes novas: `ALIGN_TOLERANCE_DEGREES = 2f`, `ALIGN_TOLERANCE_METERS = 0.25f`, `TOLERANCE_WARNING_FACTOR = 3f`, `BARE_HERO_FONT_MUL = 3f`, `BARE_RETICLE_ALPHA = 0.4f`, `BARE_RETICLE_THICKNESS = 1.5f`, `BARE_TARGET_RING_SCALE = 1.6f`.
- `using LcdMod.Client.Gui.ControlsTemplates.Panels;` para `LineRenderer`.

## Divergências em relação ao brief

- O app não tinha "função de cor por tolerância" (a cor do layout cheio vem do status do conector, `GetStatusColor`). Criei `ToleranceColor` com limiares fixos (2° / 0,25 m, âmbar até 3×).
- Anel do alvo: textura `CircleHollow` (já usada pelo app para o marcador de referência) em vez de dois círculos `Circle` ou `LineRenderer`.
- Setas em ASCII (`^ v < > CW CCW`) conforme o Self-review do plano, para evitar glifo-caixa.
- No Mínimo, o marcador do alvo usa a mesma cor do anel (acento/fg) em vez do vermelho do layout cheio, para não haver vermelho dentro do anel verde.
- Mínimo sem referência configurada também mostra só a retícula vazia (sem a mensagem "selecione um conector"); Compacto e layout cheio continuam com a mensagem.

## Chaves resx novas (não editei .resx)

| Chave | en (`MyTexts.resx`) | pt-BR (`MyTexts.pt-BR.resx`) |
|---|---|---|
| `LcdMod_DockingAlignment_State_Connected` | Connected | Conectado |
| `LcdMod_DockingAlignment_State_Ready` | Ready | Pronto |
| `LcdMod_DockingAlignment_State_Aligned` | Aligned | Alinhado |
| `LcdMod_DockingAlignment_State_Adjust` | Adjust | Ajustar |

Rótulos de inclinação/guinada/rolagem reutilizam `BlockPropertyTitle_ProjectionRotationX/Y/Z` (já cacheados em `_pitchLabel`/`_yawLabel`/`_rollLabel`). Mensagens de sem alvo reutilizam `LcdMod_DockingAlignment_NoTarget` / `LcdMod_DockingAlignment_SelectReference`.

## Verificação

- `check.ps1` não rodou (sem `dotnet`/DLLs do jogo). Em vez disso:
  - Releitura do diff completo; C# 6 apenas (sem tuplas, `out var`, pattern matching, LINQ).
  - Grep/leitura de cada membro usado: `BareLayout.Hero/Kv/LineHeight/Margin/IsWide/KvRow` (campos `Label, Icon, Value, ValueColor, Bar, BarColor`); `LineRenderer.DrawLine(sprites, p1, p2, width, color, extend = 0)` em `LcdMod.Client.Gui.ControlsTemplates.Panels`; `ThemeChrome.WithAlpha(Color, float)`; `FormatingHelper.DistanceToString(float, string = "0.##")`; `App.IsMinimal/IsCompact/IsBare/Bare/Theme/GetHeaderColor/GetWarningColor/GetErrorColor`; `Host.DrawMessage(sprites, message, icon, color, scale)`; `DrawAlignmentTicks(List<MySprite>, RectangleF, Color)`; `NormalizedToSquare` estático; `Vector3D.Zero`; `MyShipConnectorStatus.Connected/Connectable` (alias já no arquivo).
  - `Host.DrawTitle` devolve cedo em temas bare (`ThemeChrome.IsBare(_userTheme)`), então os sprites estáticos nos dois temas são só o fundo.
  - Line endings: diff sem `\r` (LF).
- Não executado: `check.ps1`, `build.ps1`, teste no jogo.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/DockingAlignmentApp.cs`

Sem commit (orquestrador commita).

## Self-review

- Sem comentários novos, sem TODO, sem alocação por frame além de `KvRow` (struct) e strings em cache quantizado.
- Layout cheio: só a extração de `PositionToSquare` (mesmo cálculo, mesma chamada) e o `return` precoce em `EnsureStaticSprites` para temas bare.
- Variáveis não usadas: nenhuma (`heroHeight` só no ramo alto, usado; `margin` usado nos dois ramos).

## Preocupações

- As 4 chaves resx precisam existir, senão `MyTexts.Get` devolve o nome da chave na tela.
- Limiares de tolerância (2°, 0,25 m) são escolha minha; ajustar no jogo se o conector travar com folgas maiores/menores.
- `TryGetDisplayReferenceRotation` (modo controlador sem cockpit) já desenhava mensagem e seguia desenhando no layout cheio; comportamento mantido também nos temas novos.
- LCD muito baixo em Mínimo (altura < `LineHeight(3) + Margin`): o lado do quadrado fica ≤ 0 e nada é desenhado (regra "some"); em LCD largo isso não ocorre porque o quadrado usa a altura toda.
- Sem compilação local: risco residual de erro que só `check.ps1` pega.

## Skills

Nenhuma invocada.
