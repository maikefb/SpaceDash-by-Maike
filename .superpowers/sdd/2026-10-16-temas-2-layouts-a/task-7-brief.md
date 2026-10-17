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
