Identifier: global.InventoryActionController : MonoBehaviour

Architectural Role: Component Logic / Action Controller

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - TryEquipFromInventorySlot(int slotIndex): Attempts to equip an item from the given slot index in the player's inventory.
  - TryEquipFromInventorySlot(InventorySlot sourceSlot): Validates and transfers an equipable item from the source slot to the appropriate equipment slot. Handles swapping if equipment slot is occupied.
  - TryUnequip(EquipmentSlot equipmentSlotType): Validates and transfers an item from the given equipment slot back to the player's inventory.
  - CanEquip(InventorySlot slot): Returns true if the slot contains an item with an EquipableComponent.
  - CanUse(InventorySlot slot): Returns true if the slot contains an item with a ConsumableComponent.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on InventoryManager (for player and equipment containers), UIInventoryEventsSO (event channel), InventorySlot, ItemInstance, EquipableComponent, ConsumableComponent.
- Downstream: Subscribed to by UIInventoryEventsSO channels. Updates observe events.

Data Schema:
- InventoryManager _playerInventory -> Reference to the player's main inventory container.
- InventoryManager _equipmentInventory -> Reference to the player's equipment container.
- UIInventoryEventsSO _uiInventoryEvents -> Event channel for UI interaction requests (equip, use, unequip).

Side Effects & Lifecycle:
- OnEnable/OnDisable: Subscribes/unsubscribes to global UIEventsSO channels (OnRequestEquip, OnRequestUse, OnRequestUnequip).
- Modifies InventorySlot state (Clear, SetItem) directly.
- Triggers _uiInventoryEvents.OnInventoryUpdated upon successful equip or unequip.
