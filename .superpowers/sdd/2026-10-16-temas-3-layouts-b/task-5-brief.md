### Task 5: Relógio

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/InGameClockDashboardApp.cs:199`

**Interfaces:**
- Consumes: `_snapshot` (ler os campos em `ClockDashboard/` — hora `DisplayDateTime`, dia/noite, próximo nascer/pôr via `ClockDashboardFormatter.BuildIncomingArrivalDateTime`, clima, temperatura, O₂, tempo de jogo, uptime), `ClockDashboardFormatter.FormatCompactTime/FormatCompactDate/FormatDuration`, `ClockDashboardLocalization.*`, ícones `"SD_Icon_Sun"`, `"SD_Icon_Moon"`, `"SD_Icon_SunRise"`, `"SD_Icon_SunSet"`, climas.

- [ ] **Step 1:** `DrawMinimal`: `Bare.Hero(FormatCompactTime(...), área superior 70 %, 4f, fg)`; linha: ícone dia/noite + próximo evento (`SunRise`/`SunSet` + hora).
- [ ] **Step 2:** `DrawCompact`: `Bare.Kv`: hora e data (`Label = hora`, `Value = data`), céu e temperatura (`Icon` clima, valor temperatura), nascer · pôr, O₂ (`%` com `Bar`), tempo de jogo (`FormatDuration`), servidor ligado (uptime). Linhas sem dado (sem planeta: nascer/pôr, clima) somem.
- [ ] **Step 3:** `.\check.ps1`; jogo em 12 h e 24 h. Commit `feat: Relógio em Mínimo e Compacto`.

---
