### Task 4: Pisca e jogador local em `App`

**Files:**
- Modify: `Core/Data/Scripts/Core/Client/Apps/Abstract/AppBase.cs`

**Interfaces:**
- Produces: `protected bool BlinkOn` → `!AnimationsEnabled || (MyAPIGateway.Session.GameplayFrameCounter / 10) % 2 == 0`, onde `AnimationsEnabled` lê o flag público de `Host.Animations` (`AnimationController`; implementador localiza a propriedade de habilitado em `Core/.../Gui/Animations/`). `protected static long LocalIdentityId` → `MyAPIGateway.Session?.Player?.IdentityId ?? 0L`.

- [ ] **Step 1:** Implementar as duas propriedades.
- [ ] **Step 2:** `.\check.ps1`. Commit `feat: BlinkOn e LocalIdentityId na base dos apps`.

---

