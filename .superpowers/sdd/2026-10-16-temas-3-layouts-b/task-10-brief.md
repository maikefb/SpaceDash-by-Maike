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
