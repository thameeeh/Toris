Identifier: global.InventoryActionControllerDebugger : MonoBehaviour

Architectural Role: Debug Utility

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - TryEquipFromInventorySlot(): ContextMenu action to test equipping from the configured slot index.
  - TryUnequipSlot(): ContextMenu action to test unequipping the configured equipment slot.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on InventoryActionController, EquipmentSlot.
- Downstream: None (Used manually in Editor).

Data Schema:
- InventoryActionController _actions -> Reference to the target controller being tested.
- int _slotIndex -> Configured index of the inventory slot to test equipping.
- EquipmentSlot _unequipSlot -> Configured equipment slot to test unequipping.

Side Effects & Lifecycle:
- None. Manually invoked via Unity ContextMenu for debugging purposes.
