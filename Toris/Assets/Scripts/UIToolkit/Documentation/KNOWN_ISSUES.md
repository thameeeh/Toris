# Known Issues & Future Fixes

## Inventory & UI
* **[RESOLVED] Registration Race Condition:** Fixed by implementing a "Scene Handshake" in `UIBootstraper.cs`. The bootstrapper now explicitly resolves and binds inventory managers to the `GlobalSession` at `Start()`, guaranteeing that UI components have valid references before they attempt to render content.
* **PlayerEquipmentController Reference Loss:** Upon scene transitions, `PlayerEquipmentController` retains a serialized reference to an `InventoryManager` from the previous scene. Since this object is destroyed, the controller fails to refresh equipment state, breaking UI stat updates. Currently lacks a mechanism to dynamically re-bind the equipment manager instance after scene load.
