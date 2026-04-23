Identifier: OutlandHaven.Inventory.InventoryManager : MonoBehaviour

Architectural Role: Component Logic / Data Container

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - bool AddItem(ItemInstance itemInstance, int quantity): Adds quantity of items to available stacks or empty slots. Returns true if fully added, false if insufficient space.
  - bool RemoveItem(ItemInstance itemInstance, int quantity): Removes quantity of items from matching stacks. Returns true if fully removed, false if insufficient quantity.

Dependency Graph (Crucial for Scaling):
- Upstream:
  - Depends on `InventoryContainerSO` (rules/blueprint).
  - Depends on `UIInventoryEventsSO` (event channel).
  - Depends on `GameSessionSO` (global state).
  - Depends on `ItemInstance` and `InventorySlot` (data classes).
- Downstream:
  - Observed by UIs listening to `UIInventoryEventsSO.OnInventoryUpdated`.
  - Accessed by transaction systems (e.g., ShopManager) or interactables (e.g., WorldItem).

Data Schema:
- InventoryContainerSO ContainerBlueprint -> Rules and constraints (e.g., slot count, screen type).
- List<InventorySlot> LiveSlots -> Active runtime data container.
- UIInventoryEventsSO _uiInventoryEvents -> Broadcaster for state changes.
- GameSessionSO GlobalSession -> Global registry.

Side Effects & Lifecycle:
- Awake: Synchronizes `LiveSlots` count to `ContainerBlueprint.SlotCount` via list mutation.
- OnValidate (Editor only): Re-synchronizes `LiveSlots` count to prevent inspector drift.
- OnEnable: If container is assigned as Player Inventory (via blueprint enum), injects `this` into `GlobalSession.PlayerInventory`.
- OnDisable: Clears `this` from `GlobalSession.PlayerInventory` if currently set.
- AddItem: Allocates `ItemInstance` clones on heap for new stacks. Triggers `OnInventoryUpdated` event.
- RemoveItem: Modifies counts or calls `Clear()` on `LiveSlots`. Triggers `OnInventoryUpdated` event.
