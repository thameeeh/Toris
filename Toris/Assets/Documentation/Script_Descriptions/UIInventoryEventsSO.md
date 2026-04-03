Identifier: OutlandHaven.Inventory.UIInventoryEventsSO : ScriptableObject

Architectural Role: Decoupled Event / Event Channel ScriptableObject

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None.
- Public API: Exposes public `UnityAction` delegates for global UI and inventory events.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on core data types (`ItemInstance`, `InventorySlot`, `InventoryManager`, `SalvageType`, `EquipmentSlot`).
- Downstream: Invoked by UI presentation components (e.g., `InventorySlotView`). Subscribed to by system logic controllers (e.g., `InventoryTransferManagerSO`, `PlayerEquipmentController`, `ShopManagerSO`) acting as decoupled event listeners.

Data Schema:
- UnityAction OnInventoryUpdated -> Generic inventory state refresh.
- UnityAction OnShopInventoryUpdated -> Shop inventory state refresh.
- UnityAction<ItemInstance, int> OnRequestBuy -> Triggers purchase transaction (item, quantity).
- UnityAction<ItemInstance, int> OnRequestSell -> Triggers sale transaction (item, quantity).
- UnityAction<int> OnCurrencyChanged -> Broadcaster for currency changes.
- UnityAction<InventorySlot> OnItemClicked -> Broadcasts standard slot selection.
- UnityAction<InventorySlot> OnItemRightClicked -> Broadcasts context actions (e.g., auto-fill, use).
- UnityAction<InventorySlot, SalvageType> OnRequestSalvage -> Triggers item salvage logic.
- UnityAction<InventorySlot, InventorySlot> OnRequestForge -> Triggers item forge logic.
- UnityAction<InventorySlot> OnRequestEquip -> Triggers item equipment logic.
- UnityAction<InventorySlot> OnRequestUse -> Triggers item usage logic.
- UnityAction<EquipmentSlot> OnRequestUnequip -> Triggers item unequip logic.
- UnityAction<InventoryManager, InventorySlot, InventoryManager, InventorySlot> OnRequestMoveItem -> Triggers cross-container transaction evaluation.
- UnityAction<InventorySlot, string> OnRequestSelectForProcessing -> Triggers assignment of item to proxy slot.

Side Effects & Lifecycle:
- Asset-based lifecycle.
- Pure message bus; contains no execution logic.
- Memory leak risk if subscribers do not explicitly unsubscribe via `-=` in their `OnDisable()` or `Dispose()` methods.