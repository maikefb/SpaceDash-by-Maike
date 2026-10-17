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
