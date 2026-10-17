# Task 6 — Diferenciação Forma dos temas desenhados + paleta Café — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; `check.ps1` e o teste no jogo — passo 6 — não rodaram.)

## O que mudou

### `Core/Data/Scripts/Core/Client/Gui/Styling/ThemeChrome.cs`

- Novas constantes: `CAFE_BORDER_GAP = 2f`, `CAFE_CORNER_RADIUS = 4f`, `CAFE_CARD_MIX = 0.4f`, `CAFE_SHADOW_ALPHA = 0.2f`, `CAFE_DASH = 6f`, `CAFE_DASH_GAP = 4f`, `FUTURISTIC_FOOTER_ALPHA = 0.25f`; cores `static readonly Color CAFE_INK (62,42,28)`, `CAFE_PAPER (242,232,214)`, `CAFE_BROWN (120,80,50)`.
- Novos públicos: `Foreground(theme, user)` (Café → `CAFE_INK`), `Background(theme, user)` (Café → `CAFE_PAPER`), `TitleColor(theme, accent, background)` (Futuristic → `background`), `TitleAlignment(theme)` (Clean/Default → LEFT; demais → CENTER).
- `TitleFont`: bold em Futuristic, Clean e Café (antes só Futuristic).
- Novos privados: `DrawDashedLine` (segmentos centrados) e `DrawRoundedRect` (cruz de 2 `Rect` + 4 texturas `"Circle"`).
- Por tema:
  - **Futuristic:** `DrawTitleAccent` preenche a barra inteira com `accent`; `DrawFooterBand` só faixa `WithAlpha(accent, 0.25f)` (sem a linha superior e sem `FrameFill`); `DrawFrame`/`DrawCard` chanfrados inalterados.
  - **Clean:** `DrawCard` devolve true sem desenhar (antes desenhava divisor inferior); `DrawFrame` (1 px `DividerColor`), `DrawTitleAccent` (linha 1 px) e `DrawFooterBand` (só linha superior) já eram assim.
  - **Café:** `DrawFrame` mantém o `FrameFill` e troca a borda de 2 px por duas bordas de 1 px em `CAFE_BROWN`, separadas por `2f*scale`; `DrawCard` desenha sombra (`WithAlpha(CAFE_INK, 0.2f)`, deslocada `(1,1)*scale`) e cartão arredondado (`Lerp(CAFE_PAPER, White, 0.4f)`, raio `4f*scale`); `DrawTitleAccent` tracejado em `CAFE_BROWN` centrado sob o título. `DrawFooterBand` do Café ficou como estava (fill + linha; o brief não pede mudança).
  - **Small:** inalterado em `ThemeChrome` (borda 1 px, `DrawCard`/`DrawFooterBand` true sem desenhar, `FontBoost`).

### `Core/Data/Scripts/Core/Client/SurfaceScripts/Abstract/SurfaceScriptBase.cs`

- `public new Color ForegroundColor => ThemeChrome.Foreground(_userTheme, Surface.ScriptForegroundColor);` e `public new Color BackgroundColor => ThemeChrome.Background(_userTheme, Surface.ScriptBackgroundColor);` ao lado de `_userTheme`. `new` esconde as propriedades não virtuais de `MyTSSCommon`; `IAppHost.ForegroundColor/BackgroundColor` (implementação implícita) passam a apontar para estas.
- `Surface.Script*Color` trocado por `ForegroundColor`/`BackgroundColor` em:
  1. `DrawIndexConflict` (texto da mensagem de conflito de índice);
  2. `OnException` (fundo passado a `RenderSpritesToTexture` no BSoD);
  3. `DrawLoadingFrame` × 3 (duas texturas `Screen_LoadingBar` e texto "aguarde");
  4. `RenderSprites` (`backgroundColor` passado a `ShouldRenderTexture`/`RenderSpritesToTexture`).
  Nenhum uso direto restou no arquivo além das duas propriedades novas.
- `DrawTitle`: retorna cedo em `LcdTheme.Small` (após o bare, com `CaretY = position.Y` já atribuído); removidas as variáveis `small`, o fator 0.6 da barra e o ícone de 24 px; `titleFontSize` virou `const 1.3f`. Alinhamento por `ThemeChrome.TitleAlignment(_userTheme)`, cor por `ThemeChrome.TitleColor(_userTheme, accent, BackgroundColor)`, X = centro do `ViewBox` quando CENTER. `DrawTitleAccent` passou a ser chamado **antes** do ícone e do texto (a faixa sólida do Futuristic cobriria o título se ficasse depois, pois `AddHeaderSprite` adiciona na mesma lista). Usa `_userTheme` em vez de `ThemeKind` em todo o método (mesmo campo que o resto do chrome).

## Divergências em relação ao brief

- Ícone do título também usa `TitleColor` (não só o texto): em Futuristic o ícone em `accent` sobre a faixa `accent` seria invisível.
- Ordem: `DrawTitleAccent` movido para antes dos sprites do título (ver acima); sem isso o passo 2 não funciona.
- `DrawFrame` do Café mantém o preenchimento `FrameFill` da faixa da moldura (o brief só fala da borda dupla); sem ele apareceria o fundo em grade a 66 % entre o `ViewBox` e a borda.

## Verificação

- `check.ps1` **não rodou** (sem `dotnet`/DLLs do jogo). Feito em vez disso:
  - Releitura completa dos dois arquivos procurando erro de compilação e recursos fora de C# 6 (só `=>` em getters, `switch` clássico, `new` em propriedade; nenhuma tupla/pattern/`out var`).
  - Assinaturas conferidas por grep/leitura: `LineRenderer.Rect(Vector2, Vector2, Color)`, `DrawRectOutline(List, RectangleF, float, Color)`, `DrawLine(List, Vector2, Vector2, float, Color, float)`; `MySprite(SpriteType, string, Vector2, Vector2, Color)` (mesma forma de `LineRenderer.Rect`); textura `"Circle"` como em `BarPanel`/`LineChartPanel`; `FormatingHelper.BoldFont(string, string)`; `Color.Lerp`/`Color.White`/`TextAlignment` cobertos pelos `using VRageMath` e `VRage.Game.GUI.TextPanel` já presentes.
  - Variáveis locais em `switch` (`gap`, `radius`, `offset`) têm nomes únicos no bloco; nenhuma variável sem uso (CS0219) introduzida; `small` removido junto com todos os usos.
  - Grep: nenhuma subclasse sobrescreve `DrawTitle`, `ForegroundColor` ou `BackgroundColor`; nenhum `Surface.Script*Color` residual em `SurfaceScriptBase`. Usos fora do mod-base (`DonutPanel`, `DonutDualPanel`, `SettingsClipboard`) não tocados (fora do escopo do brief; ver preocupações).
  - Line endings: 0 `\r` nos dois arquivos (LF).
- Não executado: `.\check.ps1`, `.\build.ps1`, teste no jogo.

## Arquivos alterados

- `Core/Data/Scripts/Core/Client/Gui/Styling/ThemeChrome.cs`
- `Core/Data/Scripts/Core/Client/SurfaceScripts/Abstract/SurfaceScriptBase.cs`

Commit: `feat: temas Futuristic, Clean, Café e Small com chrome distinto` (só os 2 arquivos; sem push).

Havia na árvore, antes desta task, uma alteração não commitada em `Info/Data/Scripts/Info/Client/SurfaceScripts/PowerFilled.cs` (remove `override ThemeKind => Default`). Não é minha e não foi adicionada ao commit.

## Preocupações

- **Vão em Small:** apps que reservam altura de título por conta própria (`Host.TitleVisible ? TITLE_HEIGHT_BASE*ls : 0`, ex.: `LiftCapacityApp:137`) deixam um espaço vazio no topo agora que `DrawTitle` não desenha nada em Small. Pendência já conhecida dos Planos 2/3; não corrigida aqui.
- **Detecção de mudança de tema (uma passada extra):** em `LayoutChanged`, `_backgroundColor/_foregroundColor` são gravados antes de `ResolveFrame` atualizar `_userTheme`. Ao entrar/sair do Café, o próximo tick vê `BackgroundColor != _backgroundColor` e chama `LayoutChanged` mais uma vez; converge no segundo. Inofensivo, mas poderia ser evitado movendo as duas atribuições para depois de `ResolveFrame`.
- **Sombra do cartão Café com alpha:** `DrawRoundedRect` sobrepõe os círculos aos retângulos da cruz; com alpha 0.2 os cantos da sombra ficam ligeiramente mais escuros onde há sobreposição. Visível só de perto; aceitável para 1 px de sombra.
- **Texto/ícone do título em Futuristic:** `AddHeaderSprite` adiciona um `Shadow(1f)` do sprite; sobre a faixa de acento a sombra pode aparecer como um halo. Conferir no jogo.
- **`DonutPanel`/`DonutDualPanel`** ainda leem `surface.ScriptForegroundColor` diretamente: no Café as rosquinhas desenhadas por esses painéis saem na cor do usuário, não em `CAFE_INK`. Fora dos dois arquivos do brief; fica para quando esses painéis forem revistos.
- Sem compilação local: risco residual que só `check.ps1` pega.

## Skills

Nenhuma invocada.
