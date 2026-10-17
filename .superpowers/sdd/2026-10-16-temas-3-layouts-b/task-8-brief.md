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
