### Task 5: Energia Preenchida %

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/PowerFilledApp.cs`

**Interfaces:**
- Consumes: `_collectors[i]` (`AverageCharge`, `FillableTexture`, `StatusColor`, contagem, `ChargingLabel`), `DrawFillableTexture` :363; tipos: baterias, tanques H₂, tanques O₂ (identificar pelo índice/kind do coletor).

- [ ] **Step 1:** `DrawMinimal`: `DrawFillableTexture` do coletor de baterias com `iconSize = min(W, H) - 2*Margin`, `%` médio no centro via `Bare.Text` (fontMul 3f); linha com `count + " · " + (carregando/descarregando)`. Sem baterias: usa o primeiro coletor com itens.
- [ ] **Step 2:** `DrawCompact`: `Bare.Kv` com uma linha por coletor com itens (baterias, tanques H₂, tanques O₂): rótulo, valor `%` médio, `Bar = AverageCharge`, `BarColor = StatusColor`; mais saldo (`Snapshot` não existe aqui: usar o que o app já lê; se não houver, omitir a linha) e autonomia idem.
- [ ] **Step 3:** `.\check.ps1`; jogo. Commit `feat: Energia Preenchida em Mínimo e Compacto`.

---
