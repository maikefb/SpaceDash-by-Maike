### Task 8: Correções das fotos sem layout novo

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Gui/ControlsTemplates/Custom/Energy/EnergyCardRenderer.cs:68` (`DrawTitleBar`)
- Modify: `Info/Data/Scripts/Info/Client/Apps/EnergyChartApp.cs` (legenda e tamanhos fixos)
- Modify: `Info/Data/Scripts/Info/Client/Apps/DockingAlignmentApp.cs` (valores dos cartões em `DrawAlignment` :640)
- Modify: `Info/Data/Scripts/Info/Client/Apps/GasApp.cs` (rótulos de título/cartões)
- Modify: `Info/Data/Scripts/Info/Client/Apps/LiftCapacityApp.cs` (`DrawTile`, `DrawRow`: tamanho do valor)

- [ ] **Step 1:** `DrawTitleBar`: remover o desenho de `shortTitle` à direita e de qualquer texto vindo de `LcdMod_Frame_GridTelemetry`/`LcdMod_Frame_Telemetry` (grep nos `.cs`; se as chaves ficarem sem uso, apagar dos resx).
- [ ] **Step 2:** `EnergyChartApp`: toda `RotationOrScale` literal passa a `n * Host.FontScale`; rótulo de legenda usa `Bare.Label(text, icon, box, …)` (ícone = `"IconEnergy"` para produção/consumo; `"SD_Icon_Battery"` para carga).
- [ ] **Step 3:** `DockingAlignmentApp`: valores de cartão com `TextAlignment.CENTER` em `rect.Center.X` e Y centrado pela `LineHeight`.
- [ ] **Step 4:** `GasApp`: título de cada cartão via `Bare.Label(title, icon, box, fg)` (ícones já existem: `ICON_H2`, `ICON_O2`; geradores e gelo usam `"IconHydrogen"`/`"IconOxygen"` até o atlas trazer `Generator`/`Ice`).
- [ ] **Step 5:** `LiftCapacityApp.DrawTile`/`DrawRow`: valor com `Bare.Text(...)` e `maxHeight = rect.Height`, `fontMul = 1f` (nunca maior que a caixa).
- [ ] **Step 6:** `.\check.ps1`. No jogo, telas de cockpit menores: nenhum texto estoura; Painel de energia sem "GRID"/"Telemetria".
- [ ] **Step 7:** Commit `fix: textos que estouravam e cabeçalho do painel de energia`.

---

## Self-review

- Cobertura: enum/clamp/combo (T1); chrome bare (T2); regra 2, 3, 4, 5, anel único, LCD largo/alto (T3); pisca e jogador local (T4); regra 7 barra (T5); direção Forma + Café invertido + Small (T6); decisão 10 (T7); correções de foto (T8). Regra 6 (grade de ícones por `Escala`) fica para Inventário/Projetor no Plano 3. Molduras digitais (T2 passo 5).
- Tipos: `BareLayout.KvRow`/`Tile` são `struct` públicos aninhados; `Kv` e `Tiles` recebem `List<>` que o app mantém como campo `readonly` e limpa por frame.
- Proporção: 8 tarefas, código só em assinaturas.
