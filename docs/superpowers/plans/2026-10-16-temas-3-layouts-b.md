# Temas Mínimo/Compacto — Plano 3 de 3: Layouts B (radar, antena, esteiras, fazenda, relógio, inventário, projetor, sobreviventes) e entrega

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Layouts Mínimo e Compacto das oito telas restantes, os dados novos das Esteiras (danificados e destruídos via projetor), a grade de ícones por `Escala`, e a lista final de ícones para o atlas.

**Architecture:** Mesmo padrão do Plano 2 (`DrawMinimal`/`DrawCompact` por app, só `Bare`). Esteiras ganha coleta nova dentro de `Rebuild` (já throttled). Inventário e Projetor desenham a grade fora do `ItemsApp` genérico (modo de apresentação ignorado nos temas novos).

**Tech Stack:** idem. **Spec:** `docs/superpowers/specs/2026-10-16-temas-minimo-compacto-design.md`. **Depende de:** Planos 1 e 2 (o padrão de `GetSprites` e os helpers).

## Global Constraints

Os dos Planos 1 e 2, mais:
- Listas (contatos, antenas, pódio, canteiros): nunca meia linha; `Bare.Kv` já corta.
- Grade de ícones (regra 6): `icon = 32f * Scale` (`ConfiguredScale`), `cols = max(1, floor(area.Width / (icon + gap)))`, `gap = 4f * Scale`; quantidade sob o ícone com `Bare.Text(fontMul 1f)`, some se não cabe. Paginação: troca de página a cada 300 frames (`GameplayFrameCounter / 300`), só quando há mais itens que células.
- Ícones ainda inexistentes no atlas: usar o fallback indicado na tarefa e registrar o nome definitivo em `BareIcons` (Task 9).

## Review Focus

1. Radar sem contato hostil e `Animations` off: nenhum ponto pisca, lista mostra até 4 contatos sem buraco. (Task 1)
2. Esteiras com projetor projetando outra nave (critério ≥ 50 % falha): fatia de destruídos some e total volta ao da grid real. (Task 3)
3. Inventário com 0 itens em Mínimo: grade vazia sem exceção; em Compacto, kv só com a barra de volume. (Task 6)
4. Sobreviventes em servidor dedicado (sem `Session.Player`): Mínimo mostra o primeiro do ranking. (Task 8)
5. Relógio em 12 h (`Use24HourClock` false): a hora enorme inclui AM/PM e some inteira se não couber, nunca corta o sufixo. (Task 5)

---

### Task 1: Radar Tático

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/RadarApp.cs:510` (`GetSprites`), reutilizar `_sortedContacts`, cores por relação (`TryBuildTargetInfo` :1173 recebe `errColor`, `warnColor`, `allyColor`), `ContactRecord.Relationship/Distance/WorldPosition/Name`.

- [ ] **Step 1:** `RelationColor(MyRelationsBetweenPlayerAndBlock)` → erro para `Enemies`, atenção para `Neutral`/`NoOwnership`, accent para `Owner`/`FactionShare`/`Friends` (mapa igual ao do layout cheio).
- [ ] **Step 2:** `DrawMinimal`: círculo de raio `min(W,H)/2 - Margin` com anel `WithAlpha(fg, 0.3)` e cruz central; para cada contato em `_sortedContacts` dentro do alcance: posição = projeção top-down do offset relativo ao bloco (reutilizar a matriz `gridMatrix` e o cálculo de `TryBuildTargetInfo`, ignorando o eixo vertical), ponto `LineRenderer` círculo de raio `3f*Scale`; hostil usa `BlinkOn ? erro : WithAlpha(erro, 0.3)`. Sem texto.
- [ ] **Step 3:** `DrawCompact`: ordenar cópia local `_compactList` (`readonly List<ContactRecord>`) com `Sort` por comparador estático sem `Comparison<T>` (classe `IComparer<ContactRecord>` estática): hostis primeiro, depois distância. `Bare.Kv` com até 4 linhas: rótulo = nome (`Icon` = `"Triangle"` tingido pela relação não é possível no `Label`; usar `Label(nome, null, …)` e um quadrado colorido `6f*Scale` antes), valor = `DistanceToString(Distance) + " " + seta` onde seta = direção no plano (8 setores: `↑↗→↘↓↙←↖`; fallback ASCII `N NE E …`).
- [ ] **Step 4:** `.\check.ps1`; jogo. Commit `feat: Radar em Mínimo e Compacto`.

---

### Task 2: Antena

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/AntennaApp.cs:89`
- resx: nova `LcdMod_Antenna_Active` ("active"/"ativas"), `LcdMod_Antenna_MaxRange` ("Max range"/"Maior alcance").

**Interfaces:**
- Consumes: `_entries` (`AntennaEntry.Name/StatusText/StatusColor/IsFunctional/StatusIcon/UseLaserIconCompensation`), alcance: `_entryModels` guarda o bloco; ler `IMyRadioAntenna.Radius` / `IMyBeacon.Radius` / `IMyLaserAntenna` (sem alcance: estado).

- [ ] **Step 1:** Contagens: `active` = entradas com `IsFunctional` e transmitindo (reusar o critério que define `StatusColor` verde), `total = _entries.Count`, `maxRange` = maior `Radius`.
- [ ] **Step 2:** `DrawMinimal`: ícone `"SD_Icon_BeaconBroadcast"` (fallback até `AntennaRadio` existir) a `min(W,H)*0.5`, cor accent se `active > 0` senão erro; linha 1 `active + " / " + total`; linha 2 `DistanceToString(maxRange)`.
- [ ] **Step 3:** `DrawCompact`: cabeçalho `Bare.Text(total + " " + Loc("Active")…)`; `Bare.Kv` uma linha por entrada: `Icon = StatusIcon`, rótulo nome, valor alcance ou `StatusText`, `ValueColor = StatusColor`.
- [ ] **Step 4:** `.\check.ps1`; jogo. Commit `feat: Antena em Mínimo e Compacto`.

---

### Task 3: Esteiras — danificados e destruídos (dados)

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs` (`Rebuild` :155, contadores :92, `EvaluateAlerts`)
- resx: `LcdMod_ConveyorMap_Alerts` → texto "Off"/"Desligados"; novas `LcdMod_ConveyorMap_Damaged` ("Damaged"/"Danificados"), `LcdMod_ConveyorMap_Destroyed` ("Destroyed"/"Destruídos"), `LcdMod_ConveyorMap_Intact` ("Intact"/"Íntegros"), `LcdMod_ConveyorMap_Blueprint` ("Blueprint"/"Planta").

**Interfaces:**
- Produces: `public int Damaged { get; }`, `public int Destroyed { get; }`, `public int BlueprintTotal { get; }` (= `_blocks.Count + Destroyed`, ou `_blocks.Count` sem projetor), `public string WorstDamagedName { get; }`, `public float WorstDamagedRatio { get; }`, `public bool HasBlueprint { get; }`.
- Consumes: `_blocks[b].SlimBlock` (`IMySlimBlock.IsFullIntegrity`, `BuildLevelRatio`), `gridLogic.Blocks` para achar `IMyProjector`.

- [ ] **Step 1:** Em `Rebuild`, após montar `_blocks`: `Damaged` = quantidade com `!SlimBlock.IsFullIntegrity`; `WorstDamaged*` = menor `BuildLevelRatio` (nome = `CustomName` se `IMyTerminalBlock`, senão `BlockDefinition.SubtypeName`).
- [ ] **Step 2:** `FindRepairProjector(IMyCubeGrid grid)`: primeiro `IMyProjector` da grid com `IsProjecting && ProjectedGrid != null && ProjectedGrid.GridSizeEnum == grid.GridSizeEnum` e, amostrando os blocos de `ProjectedGrid.GetBlocks(_projected)` (lista reutilizada), `matches / count >= 0.5f` onde `match` = `grid.CubeExists(grid.WorldToGridInteger(ProjectedGrid.GridIntegerToWorld(slim.Position)))`. `HasBlueprint = projector != null`.
- [ ] **Step 3:** `Destroyed` = blocos projetados cujo `BlockDefinition` passa no mesmo predicado de esteira usado para montar `_blocks` e que não têm bloco real na posição correspondente. Cap: parar de contar em `MAX_NODES` e marcar `_tooLarge`.
- [ ] **Step 4:** Layout cheio: rodapé troca "alertas" por `Loc("Alerts")` com o texto novo "Desligados"; acrescentar `Damaged`/`Destroyed` ao rodapé quando > 0 (uma string, `Bare.Text` some se não couber).
- [ ] **Step 5:** `.\check.ps1`; jogo: nave com projetor de reparo e um tubo removido → destruídos = 1; projetor desligado → linha some.
- [ ] **Step 6:** Commit `feat: Esteiras contam danificados e destruídos pela planta do projetor`.

---

### Task 4: Esteiras — layouts

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs:543`

**Interfaces:**
- Consumes: Task 3 + `_alerts` (desligados), `_bottlenecks`, número de redes (contar `_rootSize[i] > 0`), `_blocks.Count`.

- [ ] **Step 1:** `DrawMinimal`: `_segments` (`readonly List<DonutSegment>`): íntegros `(intact/BlueprintTotal, accent)`, danificados `(Damaged/BlueprintTotal, warn)`, destruídos `(Destroyed/BlueprintTotal, erro)`; centro `%` íntegro; legenda uma linha `intact · damaged · destroyed` com cores; linha 2 `_blocks.Count + " blocos · " + redes + " redes"` (chaves resx `LcdMod_ConveyorMap_BlocksNetworks` com `{0}`/`{1}`).
- [ ] **Step 2:** `DrawCompact`: `Bare.Kv`: planta (`BlueprintTotal`, só se `HasBlueprint`), íntegros (`intact + " · " + %`), danificados (`Damaged + " · " + TrimName(WorstDamagedName) + " " + %`; some se 0), destruídos (some se 0 ou sem planta), desligados · gargalos.
- [ ] **Step 3:** `.\check.ps1`; jogo. Commit `feat: Esteiras em Mínimo e Compacto`.

---

### Task 5: Relógio

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/InGameClockDashboardApp.cs:199`

**Interfaces:**
- Consumes: `_snapshot` (ler os campos em `ClockDashboard/` — hora `DisplayDateTime`, dia/noite, próximo nascer/pôr via `ClockDashboardFormatter.BuildIncomingArrivalDateTime`, clima, temperatura, O₂, tempo de jogo, uptime), `ClockDashboardFormatter.FormatCompactTime/FormatCompactDate/FormatDuration`, `ClockDashboardLocalization.*`, ícones `"SD_Icon_Sun"`, `"SD_Icon_Moon"`, `"SD_Icon_SunRise"`, `"SD_Icon_SunSet"`, climas.

- [ ] **Step 1:** `DrawMinimal`: `Bare.Hero(FormatCompactTime(...), área superior 70 %, 4f, fg)`; linha: ícone dia/noite + próximo evento (`SunRise`/`SunSet` + hora).
- [ ] **Step 2:** `DrawCompact`: `Bare.Kv`: hora e data (`Label = hora`, `Value = data`), céu e temperatura (`Icon` clima, valor temperatura), nascer · pôr, O₂ (`%` com `Bar`), tempo de jogo (`FormatDuration`), servidor ligado (uptime). Linhas sem dado (sem planeta: nascer/pôr, clima) somem.
- [ ] **Step 3:** `.\check.ps1`; jogo em 12 h e 24 h. Commit `feat: Relógio em Mínimo e Compacto`.

---

### Task 6: Inventário

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/InventoryApp.cs`, `Info/Data/Scripts/Info/Client/Apps/Abstract/ItemsAppBase.cs:292` (`GetSprites`: ramo bare antes do `switch (PresentationMode)`)
- resx: `LcdMod_Inventory_Ores/Ingots/Components/Ammo/Tools/Volume` ("Ores"/"Minérios" etc.) se não existirem.

**Interfaces:**
- Produces em `ItemsApp`: `protected virtual void DrawMinimal(List<MySprite>)`, `protected virtual void DrawCompact(List<MySprite>)` (default: grade e kv descritos abaixo); `protected void DrawIconGrid(List<MySprite> s, RectangleF area, ObservableList<ItemEntry> items, int page)` (regra 6; usa `ResolveSprite(itemType)` e `FormatItemQty`).
- Consumes: `_displayItems`, `ItemEntry` (tipo e quantidade; campos em `ViewModel/ItemEntry`), volume usado/total (ler de `InventoryViewModel`; se não exposto, somar `IMyInventory.CurrentVolume/MaxVolume` dos blocos filtrados no `Update`), categoria por `MyItemType.TypeId` (`MyObjectBuilder_Ore`, `_Ingot`, `_Component`, `_AmmoMagazine`, `_PhysicalGunObject`/`_Tool`).

- [ ] **Step 1:** `DrawIconGrid` com paginação pelas constantes globais; `DrawMinimal` padrão = grade com `_displayItems`.
- [ ] **Step 2:** `InventoryApp.DrawCompact`: `ThemeChrome.DrawBar` ocupação no topo; `Bare.Kv`: volume (`VolumeToString(used) + " / " + VolumeToString(total)` · `%`), minérios, lingotes, componentes, munição · ferramentas (quantidades somadas por categoria, `FormatItemQty`).
- [ ] **Step 3:** `.\check.ps1`; jogo com `Escala` 0,5 / 1 / 2: ícones mudam de tamanho e colunas recalculam. Commit `feat: Inventário em Mínimo e Compacto`.

---

### Task 7: Projetor

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/ProjectorApp.cs`
- resx: `LcdMod_Projector_Remaining` ("{0} of {1} blocks left"/"faltam {0} de {1} blocos"), `LcdMod_Projector_Buildable` ("{0} buildable now"/"{0} construíveis agora"), `LcdMod_Projector_Ok` ("ok").

**Interfaces:**
- Consumes: `ProjectorViewModel.TotalBlocks/RemainingBlocks/ComponentMissing/Items`, `IMyProjector.BuildableBlocksCount` (ModAPI; se não estiver na whitelist, omitir a linha "construíveis").

- [ ] **Step 1:** `DrawMinimal` (override): `Bare.Ring(fraction = 1 - Remaining/Total, accent)` com `%` construído; linha `Loc("Remaining", Remaining, Total)`; linha `Loc("Buildable", BuildableBlocksCount)`.
- [ ] **Step 2:** `DrawCompact` (override): grade de componentes (regra 6) a partir de `ComponentMissing`: quantidade em erro quando falta > 0; `"ok"` em accent quando a entrada está em `Items` com falta 0 (disponível).
- [ ] **Step 3:** `.\check.ps1`; jogo. Commit `feat: Projetor em Mínimo e Compacto`.

---

### Task 8: Sobreviventes

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/SurvivorsApp.cs:93`
- resx: `LcdMod_Survivors_AliveFor` ("ALIVE FOR"/"VIVO HÁ"), `LcdMod_Survivors_Rank` ("#{0} · record {1}"/"#{0} · recorde {1}"); `Dead` já existe.

**Interfaces:**
- Consumes: `_sorted` (ranking), `SurvivorEntry.IdentityId/Name/Alive/CurrentLifeTicks(now)/BestTicks(now)`, `ResolveMedal(identityId)`, formatação de duração já usada nas linhas, `LocalIdentityId` (Plano 1 T4), `now` = ticks que o app já usa.

- [ ] **Step 1:** `DrawMinimal`: `me` = entrada com `IdentityId == LocalIdentityId`, senão `_sorted[0]`; vivo: `Bare.Text(Loc("AliveFor"), 1f, accent)` + `Bare.Hero(duração da vida atual, 2.5f, accent)` + linha `Loc("Rank", posição, duração do recorde)`; morto: `Hero(Dead, erro)` + linha com duração da última vida.
- [ ] **Step 2:** `DrawCompact`: `Bare.Kv` até 6 linhas: ranking com `Icon` = sprite da medalha para 1–3 (ícones atuais de `ResolveMedal`), rótulo nome, valor tempo vivo; 4+ em `WithAlpha(fg, 0.6)`; mortos no fim em erro.
- [ ] **Step 3:** `.\check.ps1`; jogo. Commit `feat: Sobreviventes em Mínimo e Compacto`.

---

### Task 9: Fazenda

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/FarmApp.cs:212`
- resx: `LcdMod_Farm_Ready` ("{0} ready"/"{0} prontos"), `LcdMod_Farm_Next` ("next in {0}"/"próximo em {0}"), `LcdMod_Farm_NoWater` ("plot without water"/"canteiro sem água"), `LcdMod_Farm_AvgGrowth` ("Avg growth"/"Crescimento médio"), `LcdMod_Farm_AvgWater` ("Avg water"/"Água média").

**Interfaces:**
- Consumes: `FarmEntry` (`Ratio`, `RemainingText`, `WaterRatio`, `OutputSprite`, `OutputName`, `StatusColor`, `Plot`), lista de entradas do app, critério de "pronto" (`Ratio >= 1f` ou flag em `FarmPlotEntry`), tempo restante numérico (se só houver `RemainingText`, usar o menor pelo texto não é confiável: expor `RemainingSeconds` em `FarmEntry.Update`).

- [ ] **Step 1:** Agregados: média de `Ratio`, `ready`, próxima (menor `RemainingSeconds` entre não prontos), média de `WaterRatio`, `noWater` (contagem e primeiro `OutputName` com `WaterRatio <= 0`).
- [ ] **Step 2:** `DrawMinimal`: `Bare.Ring(média, accent, "%")`; linha `Loc("Ready", ready) + " · " + Loc("Next", RemainingText)`; linha de aviso `Loc("NoWater")` em atenção só se `noWater > 0`.
- [ ] **Step 3:** `DrawCompact`: `Bare.Kv`: prontos, crescimento médio (`Bar`), próxima colheita (`OutputName · RemainingText`, `Icon = OutputSprite`), água média (`Bar`, azul H₂), sem água (`N · nome`, erro; some se 0).
- [ ] **Step 4:** `.\check.ps1`; jogo. Commit `feat: Fazenda em Mínimo e Compacto`.

---

### Task 10: `BareIcons`, lista de ícones do atlas e verificação final

**Files:**
- Create: `Core/Data/Scripts/Core/Client/Gui/Styling/BareIcons.cs` (`static class BareIcons` com `const string` por conceito: `Gauge`, `Mass`, `Production`, `Consumption`, `Storage`, `Generator`, `Ice`, `AntennaRadio`, `AntennaLaser`, `Beacon`, `MedalGold/Silver/Bronze`, `BlockDamaged`, `BlockDestroyed`, `Sprout`, `WaterDrop`, `TankH2`, `TankO2`, `Turret`, `Ammo`, `Projector`, `ConveyorNetwork`, `Ok`, `Error`, `Day`, `Night` …) apontando hoje para o fallback existente e, em comentário de uma linha por constante, o nome final `SD_Icon_<Nome>` da spec de ícones.
- Modify: todos os apps dos Planos 2 e 3 trocam literais de ícone por `BareIcons.*`.
- Modify: `docs/superpowers/specs/2026-10-16-icones-atlas.md` (marcar quais já têm fallback e quais faltam).

- [ ] **Step 1:** Criar `BareIcons` e substituir literais (grep `"SD_Icon_\|"Icon` nos `DrawMinimal`/`DrawCompact`).
- [ ] **Step 2:** `python -I -X utf8 -c` que lista as constantes de `BareIcons.cs` cujo valor não é `SD_Icon_<Nome>` esperado → é a lista a entregar; colar no final de `2026-10-16-icones-atlas.md` como "Pendentes".
- [ ] **Step 3:** `.\check.ps1`; `.\build.ps1`. No jogo, para cada tela da tabela da spec, nos 7 temas, em LCD 1×1, Wide e cockpit pequeno com fonte 0,5 / 1 / 2 e `Escala` 0,5 / 1 / 2: nenhum texto estoura; Mínimo sem título/borda/rodapé; Compacto cabe ou corta linha inteira; Default inalterado; Molduras digitais ignora os temas novos.
- [ ] **Step 4:** Commit `chore: BareIcons e lista de ícones pendentes do atlas`.

---

## Self-review

- Cobertura: Radar (T1), Antena (T2), Esteiras dados+layouts (T3, T4; decisão do usuário sobre projetor automático em T3 passo 2), Relógio (T5), Inventário com regra 6 e decisão 12 (T6), Projetor (T7), Sobreviventes com decisão 13 (T8), Fazenda (T9), ícones (T10). Energia Preenchida e Defesa no Plano 2.
- Nomes: `Damaged`, `Destroyed`, `BlueprintTotal`, `HasBlueprint`, `WorstDamagedName/Ratio` definidos em T3 e consumidos em T4; `DrawIconGrid` definido em T6 e reutilizado em T7; `LocalIdentityId` e `BlinkOn` do Plano 1.
- Riscos: `IMyProjector.BuildableBlocksCount`, `ProjectedGrid.GetBlocks` e `GridIntegerToWorld` precisam estar na whitelist do ModAPI; o implementador confirma no `check.ps1` e, se um faltar, omite a linha correspondente e registra no relatório da tarefa.
