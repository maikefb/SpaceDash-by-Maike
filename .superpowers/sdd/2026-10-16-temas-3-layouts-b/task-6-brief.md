### Task 6: Inventário

**Files:**
- Modify: `Info/Data/Scripts/Info/Client/Apps/InventoryApp.cs`, `Info/Data/Scripts/Info/Client/Apps/Abstract/ItemsAppBase.cs:292` (`GetSprites`: ramo bare antes do `switch (PresentationMode)`)
- resx: `LcdMod_Inventory_Ores/Ingots/Components/Ammo/Tools/Volume` ("Ores"/"Minérios" etc.) se não existirem.

**Interfaces:**
- Produces em `ItemsApp`: `protected virtual void DrawMinimal(List<MySprite>)`, `protected virtual void DrawCompact(List<MySprite>)` (default: grade e kv descritos abaixo); `protected void DrawIconGrid(List<MySprite> s, RectangleF area, ObservableList<ItemEntry> items, int page)` (regra 6; usa `ResolveSprite(itemType)` e `FormatItemQty`).
- Consumes: `_displayItems`, `ItemEntry` (tipo e quantidade; campos em `ViewModel/ItemEntry`), volume usado/total (ler de `InventoryViewModel`; se não exposto, somar `IMyInventory.CurrentVolume/MaxVolume` dos blocos filtrados no `Update`), categoria por `MyItemType.TypeId` (`MyObjectBuilder_Ore`, `_Ingot`, `_Component`, `_AmmoMagazine`, `_PhysicalGunObject`/`_Tool`).

- [ ] **Step 1:** `DrawIconGrid` com paginação pelas constantes globais; `DrawMinimal` padrão = grade com `_displayItems`.
- [ ] **Step 2:** `InventoryApp.DrawCompact`: `ThemeChrome.DrawBar` ocupação no topo; `Bare.Kv`: volume (`VolumeToString(used) + " / " + VolumeToString(total)` · `%`), minérios, lingotes, componentes, munição · ferramentas (quantidades somadas por categoria, `FormatItemQty`).
- [ ] **Step 3:** `.\check.ps1`; jogo com `Escala` 0,5 / 1 / 2: ícones mudam de tamanho e colunas recalculam. Commit `feat: Inventário em Mínimo e Compacto`.

---
