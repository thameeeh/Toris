# Known Issues & Future Fixes

## Inventory & UI
* **[RESOLVED] Registration Race Condition:** Fixed by implementing a "Scene Handshake" in `UIBootstraper.cs`. The bootstrapper now explicitly resolves and binds inventory managers to the `GlobalSession` at `Start()`, guaranteeing that UI components have valid references before they attempt to render content.
* **PlayerEquipmentController Reference Loss:** Upon scene transitions, `PlayerEquipmentController` retains a serialized reference to an `InventoryManager` from the previous scene. Since this object is destroyed, the controller fails to refresh equipment state, breaking UI stat updates. Currently lacks a mechanism to dynamically re-bind the equipment manager instance after scene load.
* **Shop Context Ownership Cleanup:** `ShopManagerSO` and vendor screen controllers can both respond to shop-opening context. Prefer one owner, likely the presenter/controller layer, so the shop manager only handles authoritative buy/sell transactions.
* **Smith Sub-View Lifecycle Cleanup:** `ForgeSubView` and `SalvageSubView` still assemble proxy slots inside `SetVisualElements()`. They currently instantiate once, so this is not urgent, but it should move to `Setup()` in a future UI lifecycle cleanup.
