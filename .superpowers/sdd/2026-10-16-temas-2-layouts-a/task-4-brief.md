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
