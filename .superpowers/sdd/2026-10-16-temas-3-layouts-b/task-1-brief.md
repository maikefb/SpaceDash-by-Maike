### Task 1: Radar Tático

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/RadarApp.cs:510` (`GetSprites`), reutilizar `_sortedContacts`, cores por relação (`TryBuildTargetInfo` :1173 recebe `errColor`, `warnColor`, `allyColor`), `ContactRecord.Relationship/Distance/WorldPosition/Name`.

- [ ] **Step 1:** `RelationColor(MyRelationsBetweenPlayerAndBlock)` → erro para `Enemies`, atenção para `Neutral`/`NoOwnership`, accent para `Owner`/`FactionShare`/`Friends` (mapa igual ao do layout cheio).
- [ ] **Step 2:** `DrawMinimal`: círculo de raio `min(W,H)/2 - Margin` com anel `WithAlpha(fg, 0.3)` e cruz central; para cada contato em `_sortedContacts` dentro do alcance: posição = projeção top-down do offset relativo ao bloco (reutilizar a matriz `gridMatrix` e o cálculo de `TryBuildTargetInfo`, ignorando o eixo vertical), ponto `LineRenderer` círculo de raio `3f*Scale`; hostil usa `BlinkOn ? erro : WithAlpha(erro, 0.3)`. Sem texto.
- [ ] **Step 3:** `DrawCompact`: ordenar cópia local `_compactList` (`readonly List<ContactRecord>`) com `Sort` por comparador estático sem `Comparison<T>` (classe `IComparer<ContactRecord>` estática): hostis primeiro, depois distância. `Bare.Kv` com até 4 linhas: rótulo = nome (`Icon` = `"Triangle"` tingido pela relação não é possível no `Label`; usar `Label(nome, null, …)` e um quadrado colorido `6f*Scale` antes), valor = `DistanceToString(Distance) + " " + seta` onde seta = direção no plano (8 setores: `↑↗→↘↓↙←↖`; fallback ASCII `N NE E …`).
- [ ] **Step 4:** `.\check.ps1`; jogo. Commit `feat: Radar em Mínimo e Compacto`.

---
