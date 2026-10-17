# Task 2 — Chrome "nada em volta" nos temas novos — Relatório

## Status: DONE

## O que foi implementado

- `ThemeChrome.cs`
  - `const float BARE_INSET = 4f`, `const float BARE_CARD_MIX = 0.06f`.
  - `public static bool IsBare(LcdTheme theme)` → true para `Minimal` e `Compact`.
  - `FrameInset(theme, surfaceSize, defaultThickness, float scale)`: ramo bare devolve `new Vector2(BARE_INSET * scale)` antes do `switch`.
  - `DrawFrame`, `DrawTitleAccent`, `DrawFooterBand`, `DrawDivider`, `DrawIconCell`: `if (IsBare(theme)) return true;` como primeira linha.
  - `DrawCard`: ramo bare desenha fill `Color.Lerp(background, foreground, 0.06f)` sem borda e devolve true (decisão 14).
- `SurfaceScriptBase.cs`
  - `UpdateViewBox` (:412) e `AddBackground` (:1061): passam `ConfiguredScale` a `FrameInset`.
  - `DrawTitle`: após `CaretY = position.Y;`, `if (ThemeChrome.IsBare(_userTheme)) return;` (CaretY continua definido).
  - `DrawFooter`: `if (ThemeChrome.IsBare(_userTheme)) return;` antes de `RenderFooterRoot`.
  - `AddBackground` continua desenhando o fundo tiled; `DrawFrame` devolve true no bare, então `DrawChrome` (9-slice) não roda.
- `FrameLayouts.cs`: `Resolve` devolve `null` quando `ThemeChrome.IsBare(theme)` (`using LcdMod.Client.Gui.Styling` adicionado). Small já devolvia null por ausência de chave na tabela (não há `Screen(LcdTheme.Small, ...)` gerado), sem ramo explícito.
- `DigitalPictureFrames.cs`: `public override LcdTheme ThemeKind` que mapeia bare → `LcdTheme.Default` (decisão 8); `using LcdMod.Client.Gui.Styling` adicionado.

## Verificação

Comando: `powershell -NoProfile -ExecutionPolicy Bypass -File .\check.ps1`

```
== compilando Core + Info ==
OK: Info
exit=0
```

(`check.ps1` compila Core + Info num único `dotnet build`; nenhum erro ou warning CS.)

`FrameInset(` tem exatamente 2 chamadores, ambos atualizados. Line endings: os 4 arquivos continuam 100% CRLF (contagem `\r$` == total de linhas).

Não executado: `.\build.ps1` (proibido pelo brief) e teste no jogo (Step 6 visual).

## Arquivos alterados

- `Core/Data/Scripts/Core/Client/Gui/Styling/ThemeChrome.cs`
- `Core/Data/Scripts/Core/Client/SurfaceScripts/Abstract/SurfaceScriptBase.cs`
- `Core/Data/Scripts/Core/Client/Gui/Frames/FrameLayouts.cs`
- `Info/Data/Scripts/Info/Client/SurfaceScripts/DigitalPictureFrames.cs`

Commit: `feat: temas Mínimo e Compacto sem título, rodapé e moldura` (só os 4 arquivos; `.superpowers/`, `docs/`, `IMAGENS/` não adicionados).

## Self-review

- Completude: 5 passos de código cobertos, inclusive `FrameLayouts.Resolve` e `ThemeKind` em `DigitalPictureFramesSurfaceScript`.
- Comentários: só um `///` de uma linha em `IsBare` (método público novo) e atualização do `///` existente de `FrameInset` para citar o novo ramo. Nenhum comentário inline.
- `DrawCard` bare usa `LineRenderer.Rect` já existente no arquivo; `BARE_CARD_MIX` segue o padrão de constantes do arquivo (`FRAME_FILL_MIX`, `CAFE_FILL_MIX`).
- Em `DrawTitle`, o retorno bare fica antes de `Frame != null || !TitleVisible`; equivalente em efeito, mas garante que `CaretY` já está definido.

## Preocupações

- `DrawTitle` no bare não soma `titleBarHeight` a `CaretY`: conteúdo começa no topo do ViewBox (4 px × escala). É o comportamento pedido ("sem título"), mas apps que assumem espaço de título podem colar no topo — intenção do tema.
- `FrameLayouts.Resolve` retorna null para Small apenas porque não existe layout gerado para Small; não há guarda explícita. Não alterado (fora do pedido).
- Branch `main` está 8 commits à frente de `origin/main` (já estava antes desta tarefa); nenhum push feito.

## Skills

Invocadas: `ponytail:ponytail` (ultra), `ponytail:ponytail-audit`, `audit-augmentation`, `audit-context-building`. Todas existiam no catálogo; nenhum setup executado (valem como diretrizes). `audit-augmentation`/`audit-context-building` não se aplicam a uma tarefa de implementação pontual; não foram usados além de leitura.
