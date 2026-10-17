# Temas Mínimo/Compacto — Plano 2 de 3: Layouts A (energia, gás, elevação, defesa, acoplagem)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Layout Mínimo e Compacto para Capacidade de Elevação, Painel de Energia, Cartões de energia, Gráfico de energia, Energia Preenchida %, Gás, Painel de Defesa e Alinhamento de acoplagem.

**Architecture:** Cada app ganha dois métodos privados `DrawMinimal(List<MySprite>)` e `DrawCompact(List<MySprite>)` chamados no início de `GetSprites` (antes do ramo `Host.Frame != null`), desenhando só com `Bare` (Plano 1). Nenhum dado novo é coletado neste plano; só o que o app já calcula. Área útil = `Host.ViewBox` (o chrome bare já aplica a margem).

**Tech Stack:** idem Plano 1. **Spec:** `docs/superpowers/specs/2026-10-16-temas-minimo-compacto-design.md`. **Depende de:** Plano 1 completo (`BareLayout`, `IsMinimal`, `IsCompact`, `BlinkOn`).

## Global Constraints

Os do Plano 1, mais:
- Padrão de entrada em todo `GetSprites`:

```csharp
_sprites.Clear();
Host.AddBackground(_sprites);
if (IsMinimal) { DrawMinimal(_sprites); return _sprites; }
if (IsCompact) { DrawCompact(_sprites); return _sprites; }
```
- Sem dados (`HasData == false`): Mínimo desenha só o anel/ícone vazio (fração 0, cor `fg`); Compacto chama `Host.DrawMessage(_sprites, LocHelper.GetLoc("LcdMod_NotAvailable"), "Warning", GetWarningColor())`.
- Linha auxiliar do Mínimo: `Bare.Text(..., fontMul 1f)`, abaixo do anel; em LCD largo (`BareLayout.IsWide(ViewBox)`), à direita do anel (anel ocupa a metade esquerda).
- Rótulos de tile/kv: chaves resx já existentes da tela quando houver; novas com prefixo da tela.
- Multiplicadores do herói: hora 4; percentual e distância 3; demais 2,5.

## Review Focus

1. Divisão por zero quando `MaxMassKg`, `ProducedMw`, `Capacity` ou `weapon.Total` é 0: fração = 0, texto "N/A". (Tasks 1, 2, 6, 7)
2. LCD 1×3 (muito largo) em Mínimo: anel na metade esquerda, texto à direita; nada sobrepõe. (todas)
3. Defesa com `Host.Animations` desligado: nunca pisca, fica vermelho fixo. (Task 7)
4. Gás só com O₂ (sem tanque de H₂): o anel de H₂ some e o de O₂ centraliza. (Task 6)
5. Cartão de energia sem unidades (`stats.Total == 0`): herói "0" com `fg` e contagem "0"; nada quebra. (Task 3)

---

### Task 1: Capacidade de Elevação

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/LiftCapacityApp.cs`
- Modify: resx (chaves `LcdMod_Lift_Safe/Warn/Crit/NoGravity` já existem; nova `LcdMod_Lift_CanCarry` "Can carry"/"Pode carregar" se não existir)

**Interfaces:**
- Consumes: `Usage`, `State`, `HasGravity`, `CanCarryKg`, `ShipMassKg`, `SafeMaxKg`, `Loc(key)`.

- [ ] **Step 1:** `DrawMinimal`: `Bare.Ring(area, usage, stateColor, track, PercentageToString(Usage), stateColor, 3f)`; abaixo, `Bare.Text(stateWord, …, fontMul 1.2f, stateColor)` onde `stateWord` = `Loc("Safe"|"Warn"|"Crit"|"NoGravity")` em maiúsculas via `ThemeChrome.LabelText(Futuristic, …)` (reuso do upper).
- [ ] **Step 2:** `DrawCompact`: `Bare.Tiles(area, _tiles, 2, 2, fg, accent, Theme)` com tiles (ícone, rótulo, valor): (`"IconTemperature"`→ trocar por `"SD_Icon_Gauge"` quando existir, `Loc("LiftUsage")`, `%`, stateColor), (`"IconEnergy"`, `Loc("CanCarry")`, `MassToString(CanCarryKg)`), (`"IconEnergy"`, `Loc("ShipMass")`, massa), (`"SD_Icon_ShieldIcon"`, `Loc("SafeMax")`, `MassToString(SafeMaxKg)`). `_tiles` é `readonly List<BareLayout.Tile>` limpa por frame.
- [ ] **Step 3:** `.\check.ps1`; no jogo LCD de cockpit 1×1: anel com % e SEGURO; Compacto 4 tiles legíveis; fonte 0,5 → textos somem antes de estourar.
- [ ] **Step 4:** Commit `feat: Elevação em Mínimo e Compacto`.

---

### Task 2: Painel de Energia

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/EnergyGeneralApp.cs:27`
- resx: reutilizar `Loc("Production")`, `Loc("Consumption")`, `Loc("Balance")`, `Loc("Charge")` de `EnergyAppBase.Loc` (confirmar chaves `LcdMod_Energy_*`; criar as ausentes).

**Interfaces:**
- Consumes: `Snapshot.ProducedMw`, `ConsumedMw`, `NetMw`, `StoredRatio`, `UsedRatio`, `BatteryStatusColor`, `PowerText`.

- [ ] **Step 1:** `DrawMinimal`: anel com `UsedRatio` (consumo ÷ produção, clamp 0–1), cor: `UsedRatio > 0.9` erro, `> 0.75` atenção, senão accent; centro `%`; linha auxiliar `"P " + MegaWattsToString(ProducedMw) + "  C " + … + "  " + NetMw com sinal` (some inteira se não couber).
- [ ] **Step 2:** `DrawCompact`: tiles 2×2: produção (`"IconEnergy"`), consumo (`"IconEnergy"`), carga das baterias (`"SD_Icon_Battery"`, `PercentageToString(StoredRatio)`, `BatteryStatusColor`), saldo (`"IconEnergy"`, `NetMw` com sinal, verde se ≥ 0 senão erro).
- [ ] **Step 3:** `.\check.ps1`; no jogo. Commit `feat: Painel de Energia em Mínimo e Compacto`.

---

### Task 3: Cartões de energia (Baterias, H₂, Reatores, Solar, Eólica)

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/EnergyCardApp.cs:35`

**Interfaces:**
- Consumes: `_kind`, `stats = Snapshot.Kinds[(int)_kind]` (`Total`, `Working`, `CurrentOutputMw`, `MaxOutputMw`, `LoadRatio`, `State`), `Snapshot.StoredRatio`/`BatteryStatus`/`BatteryMode` (baterias), nível de tanques de H₂ (ver como `DrawIllustration` :85 obtém o nível para `EnergyKind.Hydrogen`), `KindName`, `StateText`, `BatteryModeText`.

- [ ] **Step 1:** `HeroValue()`: Battery → `PercentageToString(StoredRatio)`; Hydrogen → `%` do nível dos tanques; Reactor/Solar/Wind → `MegaWattsToString(CurrentOutputMw)`. `HeroFraction()`: Battery/Hydrogen → nível; demais → `LoadRatio`.
- [ ] **Step 2:** `DrawMinimal`: ícone do tipo (sprite já usado no cartão cheio: `"SD_Icon_Battery"`, `"IconHydrogen"`, `"SD_Icon_Reactor"`, `"SD_Icon_Solar"`, `"SD_Icon_Wind"`) a `24*Scale`; `Bare.Hero(HeroValue, area superior, 3f para %, 2.5f para MW, EnergyTheme.StateColor)`; `ThemeChrome.DrawBar` com `HeroFraction`; linha `Working + "/" + Total + " · " + (Battery ? BatteryModeText : StateText) + tendência` onde tendência = `"▲"`/`"▼"` por `BatteryStatus` (só baterias; sem glifo nos demais).
- [ ] **Step 3:** `DrawCompact`: tiles 2×2 com cabeçalho `KindName + " " + Working + "/" + Total` (`Bare.Text`, fontMul 1f, acima da grade): Battery → carga %, modo, entrada MW (`BatteryInputMw`), saída MW; Hydrogen → nível %, saída MW, máx MW, estado; Reactor/Solar/Wind → saída MW, máx MW, carga %, estado.
- [ ] **Step 4:** `.\check.ps1`; no jogo os cinco cartões. Commit `feat: cartões de energia em Mínimo e Compacto`.

---

### Task 4: Gráfico de energia

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/EnergyChartApp.cs:74`

**Interfaces:**
- Consumes: a rotina que desenha a curva no layout cheio (extrair para `DrawCurve(List<MySprite>, RectangleF area, EnergySnapshot s)` se ainda não for separada), `NetMw`, pico = máximo de `ProducedHistory`/`UsedHistory`, `StoredMwh`, `EtaSeconds`.

- [ ] **Step 1:** Extrair `DrawCurve(area)` reutilizável sem legenda.
- [ ] **Step 2:** `DrawMinimal`: `Bare.Hero(NetMw com sinal, metade superior, 2.5f, NetMw >= 0 ? accent : erro)`; `DrawCurve` no terço inferior; linha com produção, consumo e pico.
- [ ] **Step 3:** `DrawCompact`: `DrawCurve` na metade esquerda; `Bare.Kv` à direita: produção, consumo, saldo, pico, armazenado (`MegaWattHoursToString`), autonomia (`FormatTimeHours(EtaSeconds/3600)` ou N/A quando `NO_ETA`).
- [ ] **Step 4:** `.\check.ps1`; jogo. Commit `feat: Gráfico de energia em Mínimo e Compacto`.

---

### Task 5: Energia Preenchida %

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/PowerFilledApp.cs`

**Interfaces:**
- Consumes: `_collectors[i]` (`AverageCharge`, `FillableTexture`, `StatusColor`, contagem, `ChargingLabel`), `DrawFillableTexture` :363; tipos: baterias, tanques H₂, tanques O₂ (identificar pelo índice/kind do coletor).

- [ ] **Step 1:** `DrawMinimal`: `DrawFillableTexture` do coletor de baterias com `iconSize = min(W, H) - 2*Margin`, `%` médio no centro via `Bare.Text` (fontMul 3f); linha com `count + " · " + (carregando/descarregando)`. Sem baterias: usa o primeiro coletor com itens.
- [ ] **Step 2:** `DrawCompact`: `Bare.Kv` com uma linha por coletor com itens (baterias, tanques H₂, tanques O₂): rótulo, valor `%` médio, `Bar = AverageCharge`, `BarColor = StatusColor`; mais saldo (`Snapshot` não existe aqui: usar o que o app já lê; se não houver, omitir a linha) e autonomia idem.
- [ ] **Step 3:** `.\check.ps1`; jogo. Commit `feat: Energia Preenchida em Mínimo e Compacto`.

---

### Task 6: Gás

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/GasApp.cs:317`
- resx: reutilizar `Loc("H2Stored")`, `Loc("O2Stored")`, `Loc("Generators")`, `Loc("H2Output")`, `Loc("O2Output")`, `Loc("IceRe…")` (gelo).

**Interfaces:**
- Consumes: `_h2`/`_o2` (`Level`, `Stored`, `Capacity`, `RateLps`, `Tanks`, `LowCritical`, `LowAttention`), `_gens`, `_gensActive`, `_h2.ProdLps + _o2.ProdLps`, gelo em kg (campo que `ReadInventories` acumula), `Status` (`STATUS_*`).

- [ ] **Step 1:** `DrawMinimal`: se os dois têm tanque: duas áreas lado a lado (ou empilhadas quando `IsTall`), `Bare.Ring` H₂ azul e O₂ branco com `%` no centro; se só um, anel único centrado. Linha: palavra do `Status` (reusar texto do layout cheio) + `" · "` + tanque pior (`Loc("H2Stored")`/`Loc("O2Stored")` do menor `Level`).
- [ ] **Step 2:** `DrawCompact`: `Bare.Kv`: H₂ (`%` · `VolumeToString(Stored)/VolumeToString(Capacity)`, `Bar = Level`, azul), O₂ idem branco, geradores (`_gensActive + "/" + _gens + " · " + VolumeToString(prod) + "/s"`), saída (`H₂ rate · O₂ rate`), gelo (`MassToString`). Linhas de tipo sem tanque somem.
- [ ] **Step 3:** `.\check.ps1`; jogo nave só com O₂. Commit `feat: Gás em Mínimo e Compacto`.

---

### Task 7: Painel de Defesa

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/DefenseDashboardApp.cs:84`
- resx: novas `LcdMod_DefenseDashboard_Weapons` ("weapons"/"armas"), `LcdMod_DefenseDashboard_Ready` ("Ready"/"Prontas"), `LcdMod_DefenseDashboard_Ammo` ("Ammo"/"Munição"), `LcdMod_DefenseDashboard_Shield` ("Shield"/"Escudo"); motivos: reutilizar textos de `WeaponState` já localizados no layout cheio.

**Interfaces:**
- Consumes: `_sortedWeapons` (`WeaponAggregate.Total/Ready/Shooting/Warning/Unavailable/DisplayName/SpriteName`), `_hasShields`, `shield` primário (`ShieldInfo`: percentual, ler como `ShieldStatus` faz), `ShowPercent`, `CountText`.

- [ ] **Step 1:** Agregados: `totalReady = Σ Ready`, `total = Σ Total`, `anyShooting = Σ Shooting > 0`, motivo mais comum = maior entre `Σ Warning` e `Σ Unavailable` (texto do estado correspondente).
- [ ] **Step 2:** `DrawMinimal`: bloco (anel + duas linhas) centrado verticalmente em `ViewBox`; anel com `totalReady/total`, fatia restante em erro (usar `Ring` segmentado: verde `ready`, vermelho `1-ready`), centro `%`; linha 1 `CountText(totalReady, total) + " " + Loc("Weapons")`; linha 2 motivo. Cor do anel = erro quando `anyShooting && BlinkOn`.
- [ ] **Step 3:** `DrawCompact`: `Bare.Kv`: estado geral (palavra: `Shooting`/`Warning`/`Ready` pelo agregado), armas prontas (`ShowPercent ? % : CountText`), uma linha por `_sortedWeapons[i]` (ícone `SpriteName`, nome, estado curto com cor), munição (se o app expõe contagem; senão omitir), escudo (`%` se `_hasShields`). `Kv` corta na última linha inteira.
- [ ] **Step 4:** `.\check.ps1`; jogo com uma torreta disparando: pisca; `Animations` off: fixo. Commit `feat: Painel de Defesa em Mínimo e Compacto`.

---

### Task 8: Alinhamento de acoplagem

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/DockingAlignmentApp.cs:640` (`DrawAlignment` recebe os offsets; os ramos novos ficam dentro dela)
- resx: reutilizar rótulos de inclinação/guinada/rolagem e palavras de estado já usadas no layout cheio.

**Interfaces:**
- Consumes: `positionOffset`, `rotationOffset` (X = pitch, Y = yaw, Z = roll em graus, conferir em `RemapOffsetsToDisplayReference`), `dockingAxisDistance`, tolerâncias de cor do layout cheio (função que escolhe cor por ângulo; reutilizar), `_targetBlock.CustomName`, `_referenceBlock.CustomName`, `DrawAlignmentTicks` :902.

- [ ] **Step 1:** `DrawMinimal`: quadrado = menor lado de `ViewBox`; retícula (duas linhas cruzadas `WithAlpha(fg, 0.4)` + `DrawAlignmentTicks`); marcador do alvo em `center + positionOffset.XY * escala do quadrado` (reutilizar o mapeamento do layout cheio) com anel `LineRenderer` círculo verde quando `|X|,|Y|` dentro da tolerância senão `fg`; distância `Bare.Hero(DistanceToString(dockingAxisDistance), faixa inferior, 3f)`. Em `IsWide`: quadrado à esquerda, distância à direita.
- [ ] **Step 2:** `DrawCompact`: `Bare.Hero(distância, 3f)` no topo; três linhas `Bare.Kv`: inclinação, guinada, rolagem com valor `"▲ 2,1°"`/`"▼"`/`"◀"`/`"▶"`/`"↻"`/`"↺"` (seta por sinal) e `ValueColor` pela tolerância; linha final palavra de estado + nome do conector alvo.
- [ ] **Step 3:** `.\check.ps1`; jogo. Commit `feat: Acoplagem em Mínimo e Compacto`.

---

## Self-review

- Cobertura da tabela da spec: Elevação (T1), Painel (T2), Cartões (T3), Gráfico (T4), Energia % (T5), Gás (T6), Defesa (T7), Acoplagem (T8). Correção "Acoplagem valores centrados" e "Elevação textos grandes" já no Plano 1 T8.
- Consistência: todos usam `Bare.Ring/Kv/Tiles/Hero/Text/Label` com as assinaturas do Plano 1 T3; `BlinkOn` do Plano 1 T4.
- Glifos de seta (`▲▼◀▶↻↺`): a fonte do jogo pode não ter todos; o implementador testa no jogo e, se um glifo renderizar como caixa, troca pelas setas ASCII `^ v < >` e `CW`/`CCW`.
