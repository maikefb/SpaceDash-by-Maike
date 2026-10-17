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
