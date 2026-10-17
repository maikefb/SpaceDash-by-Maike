# Temas Mínimo e Compacto, diferenciação dos temas desenhados e correções visuais

Data: 16/10/2026. Decisões tomadas com o usuário em sessão, com mockups em `.superpowers/brainstorm/1122-1792191196/content/*.html`.

## Intenção

Telas de cockpit são pequenas. O layout cheio de cada tela, mesmo com fonte maior, não cabe nelas de forma legível. O usuário quer dois temas novos que trocam o **layout** da tela, não só a moldura: **Mínimo** (um dado-herói, lido em meio segundo) e **Compacto** (os poucos números que importam, sem gráfico). Em paralelo, os quatro temas desenhados por código (Futuristic, Clean, Café, Small) ficaram visualmente iguais e precisam se distinguir, e há correções pontuais vindas das fotos do jogo.

## Objetivos

- Dois temas novos no combo "Tema" do LCD: `Minimal = 5` e `Compact = 6` no enum `LcdTheme`. Rótulos pt-BR "Mínimo" e "Compacto"; en "Minimal" e "Compact".
- Cada tela listada abaixo tem um layout próprio para Mínimo e outro para Compacto.
- Os quatro temas atuais ganham chrome distinto entre si (direção "Forma").
- Correções das fotos: texto que estoura, texto grande demais, barras de progresso padronizadas, centralização, remoção de "GRID / telemetria" no Painel de energia.

## Não objetivos

- Não mexer no tema Default (N/A) nem nas molduras de imagem DDS existentes.
- Não criar opção de terminal para escolher layout separado do tema: o tema é o seletor de layout.
- Não adicionar atlas, recompressão ou qualquer otimização de bytes.
- Molduras digitais ignora Mínimo e Compacto (cai no layout de hoje).

## Antiobjetivos

- Texto estourando qualquer contêiner. Isso é falha, não detalhe.
- Mínimo com título, borda ou rodapé. Mínimo é só o dado.
- Layout que exija que o jogador leia mais de um número para entender o estado no Mínimo.

## Regras transversais

1. **Chrome dos temas novos:** nada em volta. Sem borda, sem título, sem rodapé, sem faixas. O conteúdo usa a área toda do LCD menos uma margem fixa pequena (sugestão do builder: 4 px × escala).
2. **Tamanho de texto:** segue a fonte do LCD (`Surface.FontSize`, já exposto como `IAppHost.FontScale`) multiplicada pela `Escala` quando a tela usa `LayoutScale`. **Nunca encolher para caber.** Se o texto medido (`MeasureStringInPixels`) não cabe no contêiner (anel, tile, linha): um **rótulo** é trocado pelo **ícone** correspondente (todo rótulo de tile, linha kv e cabeçalho de cartão tem um ícone associado); um **valor** (número, unidade, estado) **some**. Rótulo sem ícone definido some como valor. Isso vale para o número dentro do anel, para os tiles, para as linhas chave-valor e para o layout cheio.
3. **Anel:** mesmo componente visual em todas as telas que o usam (Elevação, Energia, Esteiras, Fazenda, Inventário, Projetor, Defesa, Gás). Fatia colorida sobre trilho escuro, número no centro, cor por estado (verde / âmbar / vermelho) ou por tipo (azul H₂, branco O₂). Diâmetro = menor lado da área menos margens. Reutilizar `DonutPanel` se cobrir; senão, um helper único em `ThemeChrome`.
4. **Tabela chave-valor (Compacto):** rótulo à esquerda, valor à direita em negrito com cor de estado, divisor fino entre linhas, até 5 ou 6 linhas. Linhas de dado inexistente na nave somem (ex.: escudo sem mod, tanques sem tanque).
5. **Tiles (Compacto):** grade 2×2 (ou 3×2 quando indicado), ícone pequeno + rótulo curto em cima, valor embaixo, fundo de cartão do tema. Rótulos sempre localizados (resx).
6. **Grades de ícones:** o tamanho do ícone é controlado pela opção `Escala` do LCD (`ConfiguredScale`); o número de colunas deriva do tamanho do ícone e da largura disponível. Quantidade sob o ícone, na fonte do LCD, some se não couber.
7. **Barras de progresso:** todas as telas passam a usar o estilo da barra do Relógio (foto 4): trilho arredondado escuro, preenchimento na cor de estado, sem borda, altura proporcional à fonte. Um único helper (`ThemeChrome.DrawBar` já existe; alinhar Relógio, Gás, Elevação, Energia a ele).
8. **Jogador local:** onde o layout mostra "você", usar `MyAPIGateway.Session.Player.IdentityId`; cada cliente vê o próprio dado no mesmo LCD físico.

## Layouts por tela

Formato: **Mínimo** / **Compacto**. "anel" segue a regra 3; "kv" a regra 4; "tiles" a regra 5.

| Tela | Mínimo | Compacto |
|---|---|---|
| Capacidade de Elevação | anel com uso de elevação % e palavra de estado (SEGURO / ATENÇÃO / CRÍTICO) embaixo | tiles 2×2: uso %, pode carregar, massa da nave, máx. seguro |
| Painel de Energia | anel com consumo ÷ produção %; linha pequena com produção, consumo e saldo | tiles 2×2: produção, consumo, carga das baterias, saldo |
| Radar Tático | só o escopo (vista de cima), círculo ocupando a tela, pontos por relação, hostil pisca; sem texto | lista dos 4 contatos mais próximos: cor da relação, nome, distância, seta de direção; hostis primeiro; sem mapa |
| Antena | ícone de antena grande; "ativas / total"; maior alcance; ícone muda de cor se nenhuma transmite | lista: ícone do tipo (rádio, laser, farol), nome, alcance ou estado; contagem no título da lista (sem título de tela) |
| Mapa de Esteiras | anel com três fatias (íntegros verde, danificados âmbar, destruídos vermelho); centro = % íntegro sobre o total da planta; legenda com os três números; "N blocos · M redes" | kv: blocos na planta; íntegros com %; danificados com o pior (nome · %); destruídos; desligados · gargalos |
| Fazenda | anel com crescimento médio; "N prontos · próximo em T"; aviso "canteiro sem água" só se houver | kv: prontos para colher; crescimento médio; próxima colheita (planta · tempo); água média; sem água (N · nome) |
| Relógio | só a hora, enorme; linha com ícone dia/noite e próximo nascer ou pôr | kv: hora e data na primeira linha; céu e temperatura; nascer · pôr; O₂; tempo de jogo; servidor ligado |
| Inventário | grade de **todos** os itens, só ícone do item com a quantidade embaixo; base 4 colunas, mas o tamanho do ícone segue `Escala` (regra 6); pagina quando não cabe | kv: barra de ocupação em cima; volume usado / total com %; minérios; lingotes; componentes; munição · ferramentas |
| Projetor | anel com % construído; "faltam N de M blocos"; "K construíveis agora" | grade de componentes que faltam: ícone do componente e quanto falta em vermelho; os já disponíveis mostram "ok"; `Escala` manda no ícone |
| Sobreviventes | jogador local: "VIVO HÁ" + tempo da vida atual em grande; recorde e posição no ranking; morto mostra "MORTO" em vermelho com tempo da última vida | pódio: três primeiros com medalha e tempo vivo; demais em cinza; mortos no fim; até 6 linhas |
| Energia Preenchida % | uma bateria grande preenchível (ícone já existente, ampliado) com a média de carga no centro; linha com contagem e carregando/descarregando | kv com barra por tipo: baterias, tanques H₂, tanques O₂ (média e %), saldo, autonomia |
| Molduras digitais | sem layout próprio: ignora o tema | idem |
| Painel de Defesa | anel com % de armas prontas, fatia vermelha para não prontas; linha com "N de M armas" e motivo mais comum; **anel e texto centrados verticalmente** na tela; pisca em vermelho se alguma arma dispara | kv: estado geral; armas prontas com %; uma linha por tipo com estado curto; munição; escudo |
| Alinhamento de acoplagem | retícula ocupando a tela + distância embaixo; anel do alvo fica verde quando centrado | só números: distância grande; inclinação, guinada e rolagem com seta e cor por tolerância; palavra de estado e nome do conector |
| Gás | dois anéis lado a lado: H₂ (azul) e O₂ (branco); linha com estado do sistema e o tanque pior | kv: H₂ armazenado (% · nível / capacidade); O₂ idem; geradores (ativos · produção); saída H₂ · O₂; gelo |
| Gráfico de energia | saldo atual grande com cor; curva reduzida embaixo; linha com produção, consumo e pico | curva na metade esquerda; kv na direita: produção, consumo, saldo, pico, armazenado, autonomia |
| Cartões de energia (Baterias, Hidrogênio, Reatores, Solar, Eólica) | ícone do tipo; valor-herói enorme (carga nas baterias, nível nos tanques de H₂, saída nos reatores, solar e eólica); barra; linha com contagem, modo e tendência | tiles 2×2 com os quatro valores do tipo; contagem no cabeçalho da grade |

### Esteiras: danificados e destruídos

- **Danificado:** bloco existente com integridade abaixo de 100% (`IMySlimBlock.BuildLevelRatio` ou `IsFullIntegrity`). Checagem nova; hoje só existe "alerta" (desligado ou sem funcionar) e "gargalo".
- **Destruído:** bloco de esteira presente na planta do projetor e ausente na grid real. Fonte: `IMyProjector.ProjectedGrid` (ModAPI), percorrendo blocos projetados do tipo esteira e verificando a posição correspondente na grid real. Só com projetor de reparo carregado e alinhado; sem projetor a fatia e a linha somem e o total passa a ser o da grid real.
- "Desligados" substitui o antigo "alertas" nos textos; "gargalos" continua.

## Diferenciação dos temas desenhados (direção "Forma")

Aplica-se ao layout cheio e ao chrome em `ThemeChrome`:

- **Futuristic:** cantos chanfrados na borda externa; faixa sólida de título na cor de acento com texto na cor de fundo; cartões chanfrados com fundo; rodapé em faixa.
- **Clean:** só linhas de 1 px; título à esquerda em peso 600 com divisor embaixo; sem cartão com fundo, só divisores entre linhas; sem rodapé em faixa.
- **Café:** **fundo claro invertido** (papel creme) com texto escuro, borda dupla marrom, cartões arredondados com fundo mais claro e sombra de 1 px, título centrado em itálico com divisor tracejado. As cores de fundo/texto do usuário são substituídas por uma paleta fixa creme/marrom nesse tema; cor de acento do usuário continua valendo para estados.
- **Small:** borda fina de 1 px, sem título, sem rodapé, fonte maior (`FontBoost` já existente).

## Correções das fotos (layout cheio)

- Painel de energia: remover "GRID" e "Telemetria da grid" do cabeçalho.
- Gráficos de energia: no primeiro, rótulo da legenda que corta vira só o ícone; no segundo, o tamanho do texto segue a fonte do LCD. Nenhum texto fixo em px.
- Acoplagem (foto 2): valores centrados nos espaços dos cartões.
- Gás (foto 3): em tela pequena, título e cartões trocam rótulo por ícone quando o rótulo não cabe (regra 2: some; o ícone fica).
- Barras de progresso: regra 7, todas.
- Elevação (foto 5): textos grandes demais; valor dos cartões segue a fonte do LCD, nunca maior que a altura da caixa.
- Energia Preenchida %: passa a respeitar os temas (hoje ignora).
- Em todas as telas, não só nas citadas nas fotos.

## Restrições

- C# do ModAPI do Space Engineers (whitelist): sem `Comparison<T>`, sem LINQ pesado por frame, sem alocação por frame em listas de sprites (reutilizar `readonly List<MySprite>`).
- Compilar com `.\check.ps1`; montar com `.\build.ps1`.
- Textos visíveis em `MyTexts.resx` e `MyTexts.pt-BR.resx` (en + pt-BR no mínimo).
- `AppSchemaRegistry.g.cs` e demais `Generated/*.g.cs` são escritos à mão.
- Clamp do tema em `GeneralConfigComponentExtensions` sobe para `(int)LcdTheme.Compact`.

## Decisões após a revisão do builder

Do usuário:

- **Projetor das Esteiras:** automático. Primeiro `IMyProjector` da grid com `IsProjecting`, `ProjectedGrid.GridSizeEnum` igual ao da grid real e maioria (≥ 50 %) das posições projetadas coincidindo com blocos reais existentes. Sem opção nova no terminal. Sem projetor que passe no critério, fatia e linha de destruídos somem.

Minhas, a revisar na leitura:

1. **Valor-herói do Mínimo:** fonte do LCD × multiplicador fixo por layout (hora 4×, distância e percentuais 3×, demais 2,5×). Nunca fit-to-box. Se não cabe, some (regra 2).
2. **Opções existentes em Mínimo/Compacto:** `ShowPercent`, `Use24HourClock`, filtros de item e ordenação continuam valendo. `ItemDisplayMode` do Inventário é ignorado nos dois temas novos (a grade é fixa). Controles de terminal não são escondidos.
3. **Escala vs fonte:** texto e altura de barras usam só `FontScale`; ícones, margens e espessura do anel usam `ConfiguredScale`. Anel: diâmetro pela área, não pela escala.
4. **Danificado:** `!IMySlimBlock.IsFullIntegrity`, inclui bloco em construção.
5. **Cores:** ok = `HeaderColor` (acento), âmbar/vermelho = `GetWarningColor()`/`GetErrorColor()` da config de cores. H₂ azul e O₂ branco fixos.
6. **Café invertido:** paleta creme/marrom substitui fundo e texto dentro do `ViewBox` e o fundo do LCD passado à renderização. Ilustrações de `EnergyCardRenderer` e ícones de item ficam como são.
7. **Helpers de `ThemeChrome` com `Minimal`/`Compact`:** `DrawFrame`, `DrawCard`, `DrawFooterBand`, `DrawTitleAccent` devolvem false e não desenham; `FrameInset` devolve a margem fixa; `DrawBar` desenha a barra padrão. Telas fora da tabela (RenderProxy, mensagens) aparecem sem chrome.
8. **Molduras digitais** em `Minimal`/`Compact` renderiza como `Default`.
9. **Barra padrão:** `DrawBar` passa a imitar a barra do Relógio (trilho arredondado, sem borda) e ganha parâmetro para o texto de percentual, desligado por padrão. O control `ProgressBar` do Relógio fica; os apps que desenham barra à mão migram para `DrawBar`.
10. **Energia Preenchida %:** vira app comum: deixa de forçar `ThemeKind = Default`, ganha moldura, título e rodapé do tema no layout cheio.
11. **Nada cabe:** Mínimo mostra só o ícone/anel sem texto; Compacto kv com zero linhas mostra `DrawMessage` com "sem dados" (chave de resx já existente quando houver).
12. **Inventário Mínimo:** mesma fonte de itens de `ItemsAppBase` (filtros, agrupamento por definição, ordenação atual).
13. **Sem jogador local** (dedicado, espectador): Sobreviventes Mínimo mostra o primeiro do ranking. Compacto e layout cheio não destacam o jogador local.
14. **Tile do Compacto:** fundo `Lerp(BackgroundColor, ForegroundColor, 0.06)`, sem borda; `DrawCard` ganha esse caso para os dois temas novos.
15. **LCD largo** (largura > 1,5 × altura): texto auxiliar do Mínimo vai à direita do anel; em LCD alto, os dois anéis do Gás empilham.
16. **Pisca:** alterna a cada `Update10` (cerca de 6 vezes por segundo) só em Radar e Defesa, que já rodam em `Update10`. Respeita `Host.Animations` desligado (fica aceso fixo).
17. **Fontes:** sem itálico nem semibold no jogo; Clean e Café usam o bold de `FormatingHelper.BoldFont` no título.
18. **Idiomas:** chaves novas em `MyTexts.resx` (en) e `MyTexts.pt-BR.resx`; demais idiomas usam o fallback do jogo.

## O que fica para o builder

- Margem interna exata dos temas novos; espessura do anel; altura das barras.
- Onde cortar a lista de contatos/antenas/canteiros quando a tela é muito baixa (regra: nunca meia linha).
- Intervalo de paginação da grade de inventário.
- Como animar o "pisca" do hostil no Radar e da arma disparando na Defesa com o mínimo de custo por frame.
- Tolerâncias de cor dos ângulos na Acoplagem (reaproveitar as do layout cheio).

## Ícones novos para o atlas

Lista a entregar ao usuário no fim da implementação, com os que não existem hoje em `Core/Textures/Sprites`. Candidatos conhecidos pelos mockups: antena rádio, antena laser, farol, medalha ouro/prata/bronze, bloco danificado, bloco destruído, canteiro/planta genérico, gota d'água, bateria grande preenchível (já existe `FillableTexture`), tanque H₂, tanque O₂, torreta, míssil, railgun, escudo, munição, projetor, ícone de rede de esteira, sol/lua (verificar `InGameClockDashboard`).
