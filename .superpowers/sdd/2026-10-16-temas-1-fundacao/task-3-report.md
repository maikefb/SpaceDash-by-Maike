# Task 3 — Helper `BareLayout` (medição, anel, kv, tiles, herói) — Relatório

## Status: DONE_WITH_CONCERNS

(Concerns por não ter compilador na máquina; ver "Verificação".)

## O que foi implementado

- `Core/Data/Scripts/Core/Client/Gui/Styling/BareLayout.cs` (novo, `public sealed class BareLayout`, namespace `LcdMod.Client.Gui.Styling`), com todas as assinaturas do brief:
  - `BareLayout(IAppHost host, ITextStyleProvider style)`; `FontScale` (`Host.FontScale`), `Scale` (`Host.ConfiguredScale`), `Margin` (`4f * Scale`).
  - `LineHeight(fontMul)` → `FormatingHelper.LineHeight(FontScale * fontMul, style, surface)`.
  - `Fits(text, fontMul, maxWidth, maxHeight)` / `Fits(text, fontMul, maxWidth)`; texto vazio, `FontScale <= 0` ou limites `<= 0` → false.
  - `Text(...)`: mede com a fonte resolvida (bold via `FormatingHelper.BoldFont`), não desenha se não cabe, devolve false; nunca encolhe.
  - `Label(...)`: texto (LEFT, centrado verticalmente na caixa) se cabe; senão ícone TEXTURE quadrado de lado `min(box.Height, 16*Scale)`; senão nada. Devolve a largura usada.
  - `Ring(...)` ×2: `d = min(W,H) - 2*Margin`, `outer = d/2`, `inner = outer - RingThickness(d)`; `DonutPanel.DrawDonut(s, center, inner, outer, fraction, fill, track)` / `DrawSegmentedDonut(s, center, inner, outer, segments, track)` (ambos estáticos; parâmetros opcionais no default). Texto central só se `Fits(center, mul, inner*1.6f, inner)`.
  - `RingThickness(d) = Math.Max(4f, d*0.12f)`.
  - `KvRow` / `Kv(...)`: altura de linha `LineHeight(1f)*1.5f`; corta em `floor(area.Height / rowHeight)` linhas; valor à direita em bold com `ValueColor` (some se não cabe na largura da área); rótulo via `Label` na largura restante (menos `Margin`); divisor 1 px `WithAlpha(fg, 0.15f)` no topo das linhas 2..n; barra opcional (`Bar >= 0`) via `ThemeChrome.DrawBar(s, theme, rect, Bar, BarColor, fg, Scale)` (assinatura atual) na metade inferior da folga da linha (`(rowHeight - lineHeight) * 0.5f` de altura). Com barra o texto fica no topo da linha; sem barra fica centrado. Devolve linhas desenhadas.
  - `Tile` / `Tiles(...)`: grade cols×rows com gap `Margin`; `ThemeChrome.DrawCard(s, theme, rect, Host.BackgroundColor, accent, fg, Scale)` no fundo; interior com inset `Margin`; metade superior: ícone `min(16*Scale, metade)` em `fg` + rótulo via `Label(text, icon: null, ...)`; metade inferior: valor via `Text` com `fontMul 1.4f`, centrado, `maxHeight` = metade inferior.
  - `Hero(...)`: `Text` centrado na área com `fontMul`; devolve `LineHeight(fontMul)` ou 0.
  - `IsWide` / `IsTall` com fator 1.5.
  - Guardas: `Text`, `Label`, `Ring`, `Kv`, `Tiles`, `Hero` devolvem sem desenhar quando lista nula, área `<= 0` ou `FontScale <= 0`.
- `AppBase.cs`: `protected bool IsMinimal`, `IsCompact`, `IsBare` (via `ThemeChrome.IsBare(Theme)`), `protected BareLayout Bare { get; }` instanciado no construtor após a verificação de `host` nulo (`new BareLayout(host, this)`). Usa a propriedade `Theme` já existente (`Host.ThemeKind`); nenhuma duplicata. `using LcdMod.Client.Gui.Styling` já existia.

## Divergências em relação ao brief

- Medição: em vez do overload `GetSizeInPixel(text, ITextStyleProvider, ...)` uso o overload por nome de fonte (`GetSizeInPixel(text, font, size, surface)`) com `font = style.ResolvedTextFont` (fallback "White") e `BoldFont(font, text)` quando bold — é o que o overload por `ITextStyleProvider` faz internamente, e permite medir o bold com a mesma fonte usada no sprite. `LineHeight` usa o overload por `ITextStyleProvider`.
- `Tiles`: o ícone do tile é desenhado sempre (quando `Icon` não vazio) e o rótulo é passado a `Label` com `icon = null`, para não duplicar o ícone quando o texto não cabe. Efeito visual = rótulo → ícone → nada.
- Sem alocação por frame: `Kv`/`Tiles` iteram as listas do chamador; nenhuma lista nova.

## Verificação

- `check.ps1` **não rodou**: não há `dotnet` nem DLLs do jogo nesta máquina. Em vez disso:
  - Releitura completa de `BareLayout.cs` procurando erro de compilação e recursos fora de C# 6 (sem tuplas, `is` pattern, `out var`, `default` literal; só `=>` em getters, `nameof`, `out` com declaração prévia).
  - Grep/leitura de cada assinatura usada: `IAppHost.FontScale/ConfiguredScale/Surface/BackgroundColor`; `ITextStyleProvider.ResolvedTextFont`; `FormatingHelper.GetSizeInPixel(string, string, float, IMyTextSurface)`, `LineHeight(float, ITextStyleProvider, IMyTextSurface, string)`, `BoldFont(string, string)`; `DonutPanel.DrawDonut` / `DrawSegmentedDonut` (estáticos, posicionais até `backgroundColor`); `DonutSegment`; `ThemeChrome.DrawCard(sprites, theme, rect, background, accent, foreground, scale)`, `DrawBar(sprites, theme, rect, fraction, fill, foreground, scale)`, `WithAlpha`, `IsBare`; `LineRenderer.Rect(center, size, color)`; construtores de `MySprite` com 7 e 8 argumentos (mesma forma de `LiftCapacityApp.cs`); `LcdTheme.Minimal/Compact`.
  - Conflitos de nome: grep de `IsMinimal|IsCompact|IsBare|Bare|BareLayout` em `Core`/`Info` — nenhum membro com esses nomes em subclasses de `App` ou em `Control`.
  - Line endings: **neste checkout os .cs estão em LF**, não CRLF: `.gitattributes` tem `* text=auto eol=lf` e `git ls-files --eol` mostra `i/lf w/lf` para `AppBase.cs` e `ThemeChrome.cs` (0 linhas com `\r`). O arquivo novo segue o mesmo (LF, UTF-8 sem BOM, como os existentes). O relatório da Task 2 fala em CRLF porque foi feito num checkout Windows; o blob no git é LF nos dois casos.
- Não executado: `.\check.ps1`, `.\build.ps1`, teste no jogo.

## Arquivos alterados

- `Core/Data/Scripts/Core/Client/Gui/Styling/BareLayout.cs` (novo)
- `Core/Data/Scripts/Core/Client/Apps/Abstract/AppBase.cs`

Commit: `48df5bd` — `feat: helper BareLayout para anel, kv, tiles e texto que some` (só os 2 arquivos; `.superpowers/`, `docs/`, `IMAGENS/` não adicionados; sem push).

## Self-review

- Completude: todas as assinaturas do brief presentes com os nomes e tipos pedidos; propriedades em `App` conforme o brief.
- Comentários: só `///` de uma linha na classe, nos métodos públicos cujo nome não basta e no campo `Bar`. Nenhum `TODO`, nenhum método ou parâmetro extra.
- Constantes nomeadas no padrão de `ThemeChrome` (`MARGIN_BASE`, `ICON_BASE`, `RING_THICKNESS_MIN` ...).
- `Kv`: a largura do valor reservada para o rótulo inclui `Margin` de folga; se o valor não cabe (some), o rótulo usa a largura toda.

## Preocupações

- Sem compilação local: risco residual de erro de sintaxe/assinatura que só `check.ps1` pega. Recomendo rodar `check.ps1` na máquina Windows antes da Task 4.
- `ThemeChrome.DrawBar` atual escreve o percentual em Monospace quando `rect.Height >= 10*scale`; a barra do `Kv` tem ~0,25 × linha de altura (com fonte 1 ≈ 7 px), então normalmente não aparece, mas com fonte grande pode aparecer. Task 5 muda `DrawBar`; nada feito aqui.
- `Ring` com `d < 8 px`: `inner` fica negativo e `DonutPanel` o fixa em 0 (disco cheio); texto central não cabe e some. Aceitável para o tema.
- Line endings LF vs CRLF do brief: ver "Verificação"; se o repositório for aberto no Windows com `autocrlf`, o git converte na saída.

## Skills

Nenhuma invocada (ponytail/audit do catálogo não disponíveis nesta sessão; diretrizes seguidas: YAGNI, sem comentários supérfluos).
