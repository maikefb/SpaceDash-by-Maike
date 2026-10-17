# Task 8 — Sobreviventes em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador; resx não editado por regra de concorrência; ver "Preocupações".)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/SurvivorsApp.cs` (+118 linhas, sem remoções; layout cheio intocado).

- `using LcdMod.Client.Gui.Styling;` (para `BareLayout.KvRow` e `ThemeChrome.WithAlpha`).
- `SurvivorsLocalization`: propriedades `AliveFor` e `Rank` (chaves `LcdMod_Survivors_AliveFor` / `LcdMod_Survivors_Rank`).
- Constantes: `MINIMAL_HERO_FONT_MUL = 2.5f`, `COMPACT_DIM_ALPHA = 0.6f`, `KV_ROW_RATIO = 1.5f`, `COMPACT_ROWS = 6`, `MEDAL_SPRITE = "Circle"` (mesmo sprite do marcador de `SurvivorRowControl`).
- `GetSprites`: entrada `if (IsMinimal) { DrawMinimal; return } if (IsCompact) { DrawCompact; return }` antes do layout cheio. O fundo não é adicionado no app: `SurvivorsSurfaceScript.GetSprites` já chama `AddBackground` + `DrawTitle` e só chama `_app.GetSprites()` quando `HasData` e `HasVisibleItems()` (mesmo arranjo de `DefenseDashboardApp`).
- `Duration(ticks)`: atalho para `ClockDashboardFormatter.FormatDuration(WorldClock.ToSeconds(ticks))`, a formatação já usada nas linhas.
- `ResolveLocalIndex()`: índice em `_sorted` da entrada com `IdentityId == LocalIdentityId`; 0 (primeiro do ranking) se não há (dedicado/espectador, `LocalIdentityId == 0`) — decisão 13 / Review Focus 4.
- `DrawMinimal`: área `Host.ViewBox`; `me = _sorted[ResolveLocalIndex()]`. Vivo: `Bare.Text(AliveFor, 1f, accent)` centrado + `Bare.Hero(duração da vida atual, 2.5f, accent)` + `Bare.Text(string.Format(Rank, posição, recorde), 1f, fg)`. Morto: `Hero(Dead, GetErrorColor())` + linha com a duração (`BestTicks`). As três linhas empilham centradas verticalmente; se a pilha (`LineHeight(2.5) + 2×LineHeight(1)`) não cabe na altura, só o herói na área toda (que some sozinho se não couber — regra 2). Lista vazia → nada além do que o surface script já desenhou.
- `DrawCompact`: `_kvRows` (campo reutilizado, sem alocação por frame) com até 6 entradas de `_sorted` na ordem existente (`CompareEntries` já põe vivos primeiro, maior tempo vivo no topo, mortos no fim — nenhuma reordenação/lambda). Rótulo = nome, valor = tempo vivo; linhas 4+ com `ValueColor = WithAlpha(fg, 0.6f)`; mortos com valor `Dead` em `GetErrorColor()`. `Icon = "Circle"` para quem tem medalha (`ResolveMedal` após `ComputeMedals(now)`), como fallback do rótulo. Além disso a área do `Kv` é recuada à esquerda em `markerSide + Margin` (`markerSide = min(rowHeight/2, 16×Scale)`) e, para cada linha desenhada com medalha, um sprite `Circle` tingido com a cor da medalha é desenhado nesse recuo — é o que torna a medalha visível (o `Icon` do `KvRow` só aparece quando o texto do rótulo não cabe, e é desenhado em `fg`, sem tinta). Lista vazia → `Host.DrawMessage(sprites, LocHelper.GetLoc(MOD_PREFIX + "NotAvailable"), "Warning", GetWarningColor())`.

## Chaves resx novas (NÃO adicionadas — regra de concorrência; o orquestrador adiciona)

| Chave | `MyTexts.resx` (en) | `MyTexts.pt-BR.resx` |
|---|---|---|
| `LcdMod_Survivors_AliveFor` | `ALIVE FOR` | `VIVO HÁ` |
| `LcdMod_Survivors_Rank` | `#{0} · record {1}` | `#{0} · recorde {1}` |

`LcdMod_Survivors_Dead` já existe (`Dead` / `Morto`, conferido em ambos os arquivos). `LcdMod_NotAvailable` já existe (`N/A` / `N/D`).

## Verificação

- `check.ps1` / jogo: **não rodaram** (sem dotnet nem DLLs nesta máquina).
- Grep de cada membro usado: `App.IsMinimal/IsCompact/Bare/Theme/LocalIdentityId/GetHeaderColor/GetForegroundColor/GetErrorColor/GetWarningColor` (AppBase.cs 227–258); `BareLayout.Text/Hero/Kv/LineHeight/Margin/Scale/KvRow{Label,Icon,Value,ValueColor,Bar}`; `ThemeChrome.WithAlpha(Color, float)`; `IAppHost.DrawMessage(sprites, message, icon, color, scale = 1f)` e `ViewBox`; `SurvivorEntry.IdentityId/Name/Alive/CurrentLifeTicks/BestTicks`; `WorldClock.ElapsedTicks/ToSeconds`; `ClockDashboardFormatter.FormatDuration(double)`; construtor `MySprite(SpriteType, string, Vector2, Vector2?, Color, string, TextAlignment)` na mesma forma de `BareLayout.Label`.
- C# 6 / whitelist: sem LINQ, sem lambda/`Comparison<T>`, sem `out var`, tuplas ou pattern matching; `string.Format`, `Math.Min`, `CultureInfo.InvariantCulture` já usados no projeto. `Color?` com `.HasValue/.Value`.
- Diff relido inteiro; 0 `\r` no diff (LF), sem comentários, sem TODO.

## Divergências em relação ao brief

- Medalha no Compacto: além de `Icon` no `KvRow` (como pedido), desenho o marcador tingido à esquerda do `Kv`, porque só o `Icon` nunca apareceria com a cor da medalha (ver acima). Para isso repliquei o fator 1,5 da altura de linha do `Kv` (`KV_ROW_RATIO`, privado em `BareLayout`).
- "Duração da última vida" no Mínimo (morto): `SurvivorEntry` só guarda `RecordTicks`; a linha mostra o recorde (`BestTicks`), que para um morto é o próprio `RecordTicks`.
- Cinza das linhas 4+: só o valor fica em `WithAlpha(fg, 0.6)`; o `Kv` desenha o rótulo sempre em `fg` (sem cor de rótulo por linha).

## Preocupações

- Sem compilação local: rodar `.\check.ps1` antes do commit.
- Se `BareLayout.KV_ROW_RATIO` mudar, o marcador de medalha do Compacto desalinha da linha; alternativa futura: expor a altura de linha do `Kv` no helper (fora do escopo desta task).
- Com `ViewBox` muito estreito (`Width <= markerSide + Margin`), a área do `Kv` fica com largura ≤ 0 e o `Kv` devolve 0 sem desenhar — só os marcadores não aparecem porque `drawn == 0`; sem exceção.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/SurvivorsApp.cs`

Nenhum comando git executado (orquestrador commita: `feat: Sobreviventes em Mínimo e Compacto`).
