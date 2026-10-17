# Task 7 — Painel de Defesa em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Concerns por não ter compilador na máquina e por uma chave resx nova que não pude adicionar; ver abaixo.)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/DefenseDashboardApp.cs`:

- `GetSprites`: entrada `if (IsMinimal) { DrawMinimal; return } if (IsCompact) { DrawCompact; return }` logo após `sprites.Clear()`, antes do ramo `Host.Frame != null`. **Sem** `Host.AddBackground(_sprites)`: o host (`Info/.../SurfaceScripts/DefenseDashboard.cs:58`) já chama `AddBackground` antes dos sprites do app, e os apps irmãos deste plano (`LiftCapacityApp`, `EnergyGeneralApp`) também não o repetem; chamar de novo desenharia o fundo duas vezes.
- Campos: `const MINIMAL_HERO_FONT_MUL = 3f`, `RING_TRACK_MIX = 0.15f`; `readonly List<BareLayout.KvRow> _rows` (limpa por frame); `readonly DonutSegment[] _ringSegments = new DonutSegment[2]` (array reutilizado, passado como `IReadOnlyList<DonutSegment>` à sobrecarga segmentada de `Bare.Ring`; sem alocação por frame).
- `Loc(suffix)` → `LocHelper.GetLoc(MOD_PREFIX + "DefenseDashboard_" + suffix)` (via `using static LcdMod.Common.Helpers.Constants`).
- `SumWeapons(out total, ready, shooting, warning, unavailable)`: Σ sobre `_sortedWeapons` (Step 1).
- `StateWord(shooting, warning, unavailable, accent, out color)`: `Firing`/accent se dispara; senão motivo mais comum: `Disabled`/erro se `unavailable >= warning` (e > 0), `NotReady`/aviso se `warning > 0`; senão `Ready`/accent. Reutiliza as chaves já localizadas que `WeaponStatus` usa (`DefenseDashboardLocalization.Firing/Disabled/NotReady/Ready`), sem editar o helper.
- `DrawMinimal` (Step 2): anel segmentado `[ready/total → readyColor, 1-ready/total → GetErrorColor()]` sobre trilho `Lerp(BackgroundColor, fg, 0.15)`; `readyColor = erro quando anyShooting && BlinkOn, senão accent` (ruling: sem ramo de animações); centro `%` com fontMul 3; `total == 0` → fração 0 nos dois segmentos (só trilho), centro `LcdMod_NotAvailable` ("N/A") se `HasData`, senão sem texto e cor `fg` (Review Focus 1 / Global Constraint "sem dados"). Linha 1 `CountText(ready, total) + " " + Loc("Weapons")`; linha 2 = `StateWord` com a cor do estado; ambas só quando `total > 0`. Bloco anel + 2 linhas **centrado verticalmente** em `Host.ViewBox` (`top = Y + (H - ringSide - textBlock)/2`). Se a altura não deixa um anel ao menos do tamanho do bloco de texto, só o anel na área toda (decisão 11). `BareLayout.IsWide(view)`: anel na metade esquerda, as duas linhas à direita centradas verticalmente (decisão 15).
- `DrawCompact` (Step 3): `_rows` com `Bar = -1f` em todas (sem barra): (1) `Weapons` → estado geral (`StateWord` agregado); (2) `Ready` → `CountText(ready, total)` (já respeita `ShowPercent`), accent se todas prontas, senão aviso; (3) uma linha por `_sortedWeapons[i]` com `Label = DisplayName`, `Icon = SpriteName`, valor = `StateWord` do agregado com cor; (4) escudo só se `_hasShields` e `Latest.Primary.HasCapacity`: rótulo `ProviderName` (fallback `ShieldFallback`), valor `%` de `ChargeRatio` em accent, ou `Offline` em erro se `!IsWorking`. Linhas de armas só quando `total > 0`. **Munição omitida**: o app não expõe contagem de munição (o `GetAmmunitionAmount()` é consultado por bloco dentro de `GetWeaponState` e só vira `WeaponState.Warning`); nada novo coletado, conforme o plano. Zero linhas → `Host.DrawMessage(sprites, Loc "LcdMod_NotAvailable", "Warning", GetWarningColor())`. Depois `Bare.Kv(sprites, Host.ViewBox, _rows, fg, Theme)` (corta na última linha inteira).
- Layout cheio e `DrawFramed` intocados.

## Chaves resx novas (NÃO adicionadas — .resx fora do meu escopo)

Só uma das quatro previstas é realmente nova (grep em `Core/Data/Localization/MyTexts.resx`):

| Chave | en | pt-BR |
|---|---|---|
| `LcdMod_DefenseDashboard_Weapons` | `weapons` | `armas` |

Já existentes e reutilizadas: `LcdMod_DefenseDashboard_Ready` ("Ready"/"Pronto"), `LcdMod_DefenseDashboard_ShieldFallback` ("Shield" — usada no lugar de uma nova `_Shield`), `_Firing`, `_NotReady`, `_Disabled`, `_Offline`, `LcdMod_NotAvailable`. `LcdMod_DefenseDashboard_Ammo` não é necessária (linha de munição omitida).

Até a chave entrar nos dois .resx, `MyTexts.Get` devolve o nome da chave na linha 1 do Mínimo e no rótulo da 1ª linha do Compacto.

## Verificação

- `check.ps1`/build **não rodaram** (sem `dotnet`/DLLs do jogo nesta máquina).
- Releitura do diff completo procurando C# > 6 e whitelist: só `out` com declaração prévia, `nameof` nenhum, sem LINQ, sem lambdas/`Comparison<T>`, sem tuplas/`out var`/pattern matching/`default` literal. Array → `IReadOnlyList<T>` é conversão de linguagem (.NET 4.5+).
- Grep de cada membro usado: `App.IsMinimal/IsCompact/Bare/BlinkOn/Theme/GetForegroundColor/GetHeaderColor/GetWarningColor/GetErrorColor` (`AppBase.cs:227-256`); `IAppHost.ViewBox/BackgroundColor/DrawMessage(sprites, message, icon, color, scale=1f)`; `BareLayout.Ring(s, area, IReadOnlyList<DonutSegment>, track, center, centerColor, mul)`, `Text(s, text, pos, align, mul, color, maxW, maxH)`, `Kv(s, area, List<KvRow>, fg, theme)`, `KvRow{Label, Icon, Value, ValueColor, Bar, BarColor}`, `Margin`, `LineHeight`, `IsWide`; `DonutSegment(float, Color)` em `LcdMod.Client.Gui.ControlsTemplates.Progress`; `LocHelper.GetLoc(string)`; `Constants.MOD_PREFIX = "LcdMod_"`; `FormatingHelper.PercentageToString(float)`; `ShieldInfo.HasCapacity/IsWorking/ProviderName/ChargeRatio` e `DefenseSnapshot.Primary` (já usados neste app e em `DefenseDashboardControls.cs`); `Color.Lerp`.
- Novos `using`: `LcdMod.Client.Gui.ControlsTemplates.Progress`, `LcdMod.Client.Gui.Styling`, `using static LcdMod.Common.Helpers.Constants` (mesmo padrão de `LiftCapacityApp`). Sem conflito de nomes com `Loc`/`StateWord`/`SumWeapons` na classe parcial (grep).
- Line endings: arquivo em LF (`git ls-files --eol`: `i/lf w/lf`; 0 bytes `\r`).
- Sem comentários, sem TODO, sem git write.

## Preocupações

- Sem compilação local: risco residual só pegável por `check.ps1`.
- Review Focus 3 ("Animations desligado → vermelho fixo"): não implementado, conforme ruling do orquestrador (não há flag de animações; `BlinkOn` sempre alterna). Se o Review Focus for mantido literalmente, falhará por decisão de design, não por omissão.
- `Bare.Text` com `maxHeight = lineHeight`: a altura medida do texto deve ser ≤ `LineHeight(1f)`; é o mesmo uso de `EnergyGeneralApp.DrawMinimal`, logo consistente com os irmãos.
- Mínimo com só escudos (sem armas) mostra anel vazio com "N/A" e sem linhas — o escudo não aparece no Mínimo (o brief só prevê armas no anel).
- Compacto: a 1ª linha e o rótulo "weapons" dependem da chave resx nova acima.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/DefenseDashboardApp.cs`

Sem commit (orquestrador commita).
