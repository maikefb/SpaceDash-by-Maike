# SpaceDash by Maike - código fonte

[![Licença: GPL v3](https://img.shields.io/badge/licen%C3%A7a-GPL%20v3-blue.svg)](LICENSE) **Licença: GNU GPL v3** — mods derivados devem permanecer abertos e gratuitos. Veja [COPYRIGHT.md](COPYRIGHT.md).

Um mod de LCD para Space Engineers, `SpaceDash` (antes `LCD_INFO_MAIKE`), derivado do "Arthur's LCD Mod" 0.2.6, sem a interação por clique no LCD. Até 27/09/2026 eram dois mods (`LCD_INFO_MAIKE` e `LCD_Maike`); as telas do segundo foram incorporadas ao primeiro.

| Pasta | Vai para | Conteúdo |
|---|---|---|
| `Core/` | o mod | scripts compartilhados, fontes, sprites, localização |
| `Info/` | `Mods/SpaceDash` | telas, `Storage.sbc`, `metadata.mod`, `Constants.Mod.cs` |

## Telas

| Mod | Tela (aparece com o prefixo `[SD]` na lista de scripts) | ID do script | Controles próprios no menu K |
|---|---|---|---|
| SpaceDash | Gás Preenchido % | `LcdInfo_GasGraph` | modo de exibição, tipo de vínculo, passo da rolagem |
| SpaceDash | Painel de Defesa | `LcdInfo_DefenseDashboard` | só os comuns |
| SpaceDash | Energia % | `LcdInfo_BatteryGraph` | tipo de vínculo, passo da rolagem |
| SpaceDash | Gráfico de energia | `LcdInfo_EnergyChart` | tipo de vínculo |
| SpaceDash | Painel de energia | `LcdInfo_EnergyGeneral` | tipo de vínculo |
| SpaceDash | Energia: baterias / painéis solares / turbinas eólicas / motores a hidrogênio / reatores | `LcdInfo_EnergyBatteries`, `LcdInfo_EnergySolar`, `LcdInfo_EnergyWind`, `LcdInfo_EnergyHydrogen`, `LcdInfo_EnergyReactors` | tipo de vínculo |
| SpaceDash | Fazenda | `LcdInfo_Farm` | passo da rolagem |
| SpaceDash | Antena | `LcdInfo_AntennaGraph` | modo de exibição, linhas, tipo de vínculo, filtro de blocos, passo da rolagem |
| SpaceDash | Inventário | `LcdInfo_InventoryCharts` | exibição de itens, ocultar vazios, ordenação, filtros de blocos e itens, tipo de vínculo, passo da rolagem |
| SpaceDash | Projetor | `LcdInfo_ProjectorCharts` | exibição de itens, projetor de referência, **Fabricar componentes faltantes**, filtro de blocos, tipo de vínculo |
| SpaceDash | Relógio do jogo | `LcdInfo_InGameClockDashboard` | formato 24 horas, unidade de temperatura |
| SpaceDash | Propulsão | `LcdInfo_Thrust` | só os comuns |
| SpaceDash | Alinhamento de acoplagem | `LcdInfo_DockingAlignment` | modo de exibição, bloco de referência |
| SpaceDash | Radar | `LcdInfo_Radar` | magnificação, referência |
| SpaceDash | Sobreviventes | `LcdInfo_Survivors` | passo da rolagem |
| SpaceDash | Moldura digital | `LcdInfo_DigitalPictureFrames` | imagens, intervalo de troca, modo de exibição |

Comuns a todas: brilho, escala, título visível, cores, copiar e colar configurações. O switch "Sincronizar cores" (ligado por padrão, antes de "Cores personalizadas", visível em bloco de facção e independente delas) guarda cabeçalho, alerta, erro, fonte e fundo na variável de mundo `LcdMod.colors.<factionId>`; todo LCD da facção com o switch ligado segue essa paleta e qualquer alteração nele a atualiza. Desligado, a tela mantém as cores atuais e edições ficam só nela.

O Inventário escreve uma linha de diagnóstico no painel de informações do bloco (menu K): tipo de vínculo, inventários rastreados, tipos de item no grid, itens da tela, itens exibidos e fila de varredura. Serve para localizar onde a cadeia para quando a tela fica vazia.

As sete telas de energia leem um serviço compartilhado por grid e tipo de vínculo (`EnergyDataModule`): uma amostra a cada 50 frames, 50 amostras de histórico, compartilhadas por todas as telas do mesmo grid e vínculo e preservadas por 10 s quando a última tela sai. O Gráfico de energia é o port do "Power Chart" (PowerGraph) com os erros corrigidos: eixo compartilhado e redondo, "Usada %" é o consumo sobre a capacidade disponível (máximo dos geradores e das baterias que estão funcionando), porque no jogo a produção acompanha o consumo e a razão do original ficava sempre em 100 %, previsão "N/D" quando não há fluxo, status, saldo e previsão das baterias pela variação da energia armazenada, com a regra do jogo (o excedente da entrada sobre a saída entra com a eficiência de recarga de cada bateria; o déficit sai sem perda) e com histerese, inclusive no "Completo", que só vale quando nenhuma bateria que pode carregar tem espaço. As sete telas começam com o vínculo **Conexão elétrica**, também quando se troca o "Energia %" por uma delas. No modo criativo, e nas baterias de NPC em grids gerados por NPC, o jogo não descarrega baterias e enche as que podem carregar num ritmo fixo (1/8 da capacidade por segundo vezes a eficiência de recarga: 10 %/s nas baterias comuns e 11,25 %/s nas Prototech, sem depender da entrada); status, saldo e previsão seguem essa regra. Enquanto alguma bateria que pode carregar tem espaço, a carga aparece no máximo como 99 %, e a energia armazenada, no máximo como 99,4 % da capacidade. Painéis solares e turbinas em operação sem sol ou vento ficam em "Atenção", como o emissivo do jogo. A previsão "Vazia em"/"Cheia em" só conta baterias que podem fluir (ligadas, funcionais e fora de Recarregar ou Descarregar, conforme o sentido). Unidades ligadas que não funcionam (bateria vazia, reator sem combustível) aparecem como "Inoperante", não como "Desligado". Os cartões e o painel geral usam moldura, cantoneiras e barra de título desenhadas pelo mod, sem texturas externas. O combo de vínculo ganhou a opção **Conexão elétrica** (grids na mesma rede elétrica: juntas mecânicas e conectores acoplados), disponível também para Inventário, Projetor, Gás, Antena e Energia %. Nas telas de energia o vínculo Física é tratado como Elétrica: trem de pouso e conectores em modo comércio não transportam energia. "Grade local" (valor fora do enum) deixou de consultar os grupos do jogo, o que lançava exceção, e vale só a própria grade.

O botão **Fabricar componentes faltantes** enfileira nas montadoras do grid (vínculo físico, em modo de montagem) os componentes que faltam para a projeção, dividindo por igual; montadoras básicas só entram quando não há outra que use a blueprint.

O Relógio do jogo ganhou duas linhas: tempo de jogo da sessão atual (`ElapsedPlayTime`) e há quanto tempo o servidor está ligado. O segundo é o `ElapsedGameTime` do mundo (`GameDateTime` menos 2081-01-01): começa em zero na criação do mundo, acumula entre reinícios e o próprio jogo sincroniza nos clientes, sem pacote de rede do mod. Períodos com o servidor desligado não contam, porque o jogo não guarda a data de criação.

A tela Sobreviventes é um ranking por tempo vivo: cada linha traz posição, nome, tempo da vida atual e recorde pessoal; os três maiores recordes ganham um marcador dourado, prata e bronze. Mortos ficam no fim, esmaecidos, só com o recorde. O servidor marca spawn e morte pelos eventos `PlayerSpawned` e `PlayerDied`, no mesmo tempo do mundo do Relógio, e guarda a lista em `Storage/LCD_INFO_MAIKE_LCD_INFO_MAIKE/LcdMod.survivors.xml` dentro do save. Clientes recebem a lista pelo pacote de rede 2, pedida ao entrar e reenviada a cada mudança. Quando há mais jogadores do que linhas, a tela pagina no ritmo do passo da rolagem; com o passo em zero mostra só a primeira página. Jogadores vivos antes da instalação do mod começam a contar quando o mod os vê pela primeira vez.

## O que saiu em relação ao mod original

- Interação no LCD: cliques, arraste, cursor, tooltips, diálogos e menus.
- Botões desenhados na tela do Projetor: o "Craft all" virou o botão do menu K e a ação de barra "Fabricar componentes faltantes" (arraste o LCD para a barra); a alternância componentes/lingotes não voltou, a tela mostra componentes.
- Roda do mouse no Radar: o alcance é o slider de magnificação.
- Telas que ficaram no mod original: Cargo, Integridade, Markdown, Media Player, Mercado NPC, Mapa planetário, Mapa estelar, Jogos, Botões, Render Proxy.
- Moldura digital veio sem o seletor na própria tela: as imagens são escolhidas no terminal. Imagens do PC são DDS na pasta `Storage/<mod>` do jogo (o mod grava lá um `png-to-dds.bat` e um leia-me); `import.txt` ou `/lcd import <arquivo>` registra. Ao colocar a imagem num LCD, o cliente a envia ao servidor (pacote 5), que guarda em `Storage/<mod>/<steamId>-<nome>.dds` com um índice em `LcdMod.textures.xml` no world storage e distribui a todos. Quem entra depois pede ao servidor (pacote 4) e recebe mesmo com o dono offline. Teto de 5 MB e 2048x2048 por imagem; 32 imagens por jogador no servidor (a mais antiga é substituída); um envio por jogador e imagem a cada 30 s.

## Build

Copia `Core` + `Info` para `Mods/SpaceDash`. Não compila nada: o jogo compila os scripts ao carregar.

Os scripts vão todos para `Data/Scripts/LCD_INFO_MAIKE/` (subpastas `Core` e `Info`), porque o jogo compila cada pasta de primeiro nível de `Data/Scripts` como um assembly separado.

```powershell
.\build.ps1
```

O build apaga e recria a pasta de destino. Ele só apaga pastas que ele mesmo gerou (marcador `.maike-lcd-build`).

## Verificação de compilação

Compila `Core` + `Info` contra as DLLs do jogo em `Bin64`, sem gerar o mod, para pegar erros antes de abrir o jogo. Exige dotnet SDK e o Space Engineers instalado no caminho padrão da Steam.

```powershell
.\check.ps1
```

O projeto usado fica em `tools/CompileCheck`. O caminho do jogo pode ser sobrescrito com `-p:SEBin=...` no `dotnet build`. A checagem não aplica a whitelist do ModAPI; isso só o jogo confirma.

## Identidade do mod

Definida em `Info/Data/Scripts/Info/Constants.Mod.cs` e `Info/Data/Storage.sbc`:

| Mod | Prefixo de IDs | Porta de rede | GUID de storage |
|---|---|---|---|
| SpaceDash | `LcdInfo_` | 46542 | `a5b1a096-656a-400a-b512-8d37953649e7` |

O mod extinto `LCD_Maike` usava o prefixo `LcdMk_`, a porta 46543 e o GUID `fdb7cec0-fd4e-4ba2-b41a-322b45ad6600`. LCDs configurados com `LcdMk_Thrust`, `LcdMk_DockingAlignment` ou `LcdMk_Radar` precisam ter a tela escolhida de novo, e as configurações salvas sob o GUID antigo não são lidas.

Chaves de localização continuam `LcdMod_*`.

## Como o mod estende o núcleo

- `Constants.Mod.cs`: prefixo de IDs, porta e GUIDs.
- `TerminalManager.Info.cs`: registra os controles do menu K (`AddModRegistrations`).
- `TerminalVisibility.Info.cs`: diz em quais scripts cada controle aparece (`VisibleForScript`).
- `ClientComponent.Mod.cs`: módulo de dados de defesa e limpeza de caches entre mundos.
- `ServerComponent.Info.cs`: rastreador de sobreviventes no servidor (spawn, morte, recorde e persistência).
- `Generated/`: fonte estática do que o gerador do mod original produzia (tipo do app por tela e componentes de configuração).

## Pendências conhecidas

- O framework de GUI ainda carrega a infraestrutura de interação sem chamador (tooltips, barra do ScrollPanel, arraste do ListBoxItem, ComboBox).
- Os remaps de ícones `.png` da HUD geram avisos "extension not supported" no log, como no mod original.
- Teste em servidor dedicado com dois clientes ainda não foi feito.

## Direitos autorais

Veja [COPYRIGHT.md](COPYRIGHT.md): copyright de Maike Bressan, projeto em colaboração com Arthur, criador do Arthur's LCD Mod, e licença GNU GPL v3 ([LICENSE](LICENSE)): mods derivados devem permanecer abertos e gratuitos sob a mesma licença.
