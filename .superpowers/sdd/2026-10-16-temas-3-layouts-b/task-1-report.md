# Task 1 — Radar Tático em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Concerns por não ter compilador na máquina e por uma constante duplicada do `BareLayout`; ver abaixo.)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/RadarApp.cs`:

- `GetSprites`: após `_backgroundSprites.Clear()` e antes do layout cheio: `if (IsMinimal) { DrawMinimal(...); return; } if (IsCompact) { DrawCompact(...); return; }`. O fundo já é desenhado por `RadarSurfaceScript.GetSprites` (`AddBackground`), então o app não o repete. Layout cheio intocado.
- `RelationColor(MyRelationsBetweenPlayerAndBlock, err, warn, ally)` (estático): `Enemies` → erro; `Owner`/`FactionShare` → acento; demais (`Neutral`, `NoOwnership`, `Friends`...) → atenção. `ContactColor` passou a delegar para ele (mesmo mapa de antes, sem mudança de comportamento).
- `DrawMinimal`: área `Host.ViewBox`; raio `min(W,H)/2 - Bare.Margin`; anel `Circle` em `WithAlpha(fg, 0.3)` com miolo em `BackgroundColor` (mesma técnica do layout cheio), espessura `2*Scale`; cruz central com `LineRenderer.Rect` na mesma cor. Pontos: projeção top-down pela matriz de referência (`TryGetReferenceWorldMatrix` → fallback `Block.WorldMatrix`), `TransformNormal(offset, Transpose(gridMatrix))`, tela = `(X, Z)` (mesmos eixos de `TryBuildTargetInfo`, ignorando `Y`), escala `raio / GetWorldRange()`; ponto `Circle` de diâmetro `6*Scale` (raio `3*Scale`); limite `MAX_CONTACTS`. Hostil: `BlinkOn ? erro : WithAlpha(erro, 0.3)`. Sem texto. Sem contatos: só o escopo vazio.
- `DrawCompact`: sem contatos → `Host.DrawMessage(sprites, LocHelper.GetLoc("LcdMod_NotAvailable"), "Warning", GetWarningColor())`. Com contatos: `_compactList` (`readonly List<ContactRecord>`) recebe `_sortedContacts` e ordena com `Sort(CompactContactOrder.Instance)` — classe aninhada `sealed class CompactContactOrder : IComparer<ContactRecord>` (hostis primeiro, depois `Distance`); nenhum `Comparison<T>`/lambda novo. Até 4 linhas em `Bare.Kv` numa área recuada à esquerda em `6*Scale + Margin`; rótulo = nome (`"Unknown"` se vazio, como o rodapé do layout cheio), valor = `DistanceToString(Distance) + " " + setor` com 8 setores ASCII `N NE E SE S SW W NW` (`atan2(X, -Z)` do mesmo offset planar). Quadrado `SquareSimple` de lado `6*Scale` na cor da relação, centrado verticalmente em cada linha desenhada (`Kv` devolve a contagem).
- Constantes novas: `COMPACT_MAX_ROWS`, `COMPACT_ROW_RATIO`, `COMPACT_SWATCH_PX`, `MINIMAL_DOT_RADIUS_PX`, `MINIMAL_RING_WIDTH_PX`, `SCOPE_RING_ALPHA`, `HOSTILE_DIM_ALPHA`, `_sectorNames`.

## Chaves resx novas

Nenhuma. A mensagem de "sem contatos" reutiliza `LcdMod_NotAvailable` (já existe em en e pt-BR); o Radar não tinha chave própria de "sem contatos" (só `Radar_DetectedEntities` e `Radar_AntennaRangeWarning`).

## Texturas usadas

`Circle`, `SquareSimple` (via `LineRenderer.Rect`), `Warning` (ícone do `DrawMessage`). Nenhuma nova para o atlas.

## Verificação

- `check.ps1` **não rodou** (sem `dotnet`/DLLs do jogo nesta máquina).
- Grep/leitura de cada membro usado: `IsMinimal`, `IsCompact`, `BlinkOn`, `Theme`, `Bare`, `Host`, `GetForegroundColor/GetHeaderColor/GetWarningColor/GetErrorColor` (`AppBase.cs`); `BareLayout.Margin/Scale/LineHeight/Kv/KvRow` (`BareLayout.cs`); `ThemeChrome.WithAlpha(Color, float)` (estático, namespace `LcdMod.Client.Gui.Styling`, já importado); `LineRenderer.Rect(center, size, color)`; `IAppHost.DrawMessage(sprites, message, icon, color, scale = 1f)`; `FormatingHelper.DistanceToString(float)`; `ContactRecord.Relationship/Distance/WorldPosition/Name`; `TryGetReferenceWorldMatrix`, `GetWorldRange`, `AddTexture`, `MAX_CONTACTS` (já no arquivo).
- C# 6 / whitelist: sem LINQ, sem `Comparison<T>`, sem tuplas/`out var`/pattern matching; `IComparer<T>` já é o padrão pedido (commit `0a06111` removeu o `Comparison<T>` estático).
- Releitura completa do diff: sem variáveis sem uso, sem comentários, sem TODO; LF confirmado (`git diff` sem `\r`; `git ls-files --eol` → `i/lf w/lf`).
- Review Focus 1 (sem hostil): `hostileColor` só se aplica a `Enemies`, logo nenhum ponto pisca; `Kv` corta em linhas inteiras e `_compactRows` só tem contatos reais, sem buraco. Não há ramo de `Animations` (ruling).
- Não executado: `check.ps1`, `build.ps1`, jogo.

## Preocupações

- `COMPACT_ROW_RATIO = 1.5f` duplica a constante privada `KV_ROW_RATIO` de `BareLayout`, necessária para alinhar o quadrado colorido com cada linha do `Kv`. Se `BareLayout` mudar a razão, os quadrados desalinham. Proposta (arquivo fora do meu escopo, `Core/.../Styling/BareLayout.cs`): expor `public float KvRowHeight => LineHeight(1f) * KV_ROW_RATIO;` e usar aqui `Bare.KvRowHeight`.
- Mínimo: o filtro `planar.LengthSquared() > worldRange²` é redundante (`_sortedContacts` já filtra por distância 3D ≤ `_maxRange`), mantido como guarda barata.
- Sem compilação local: risco residual só o `check.ps1` pega.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/RadarApp.cs`

Sem git (orquestrador commita).
