Identifier: OutlandHaven.Inventory.WorldItem : MonoBehaviour, IContainerInteractable

Architectural Role: Component Logic

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - Vector3 InteractionPosition: Property returning offset interaction location.
  - bool Interact(InventoryManager targetContainer): Core interaction logic. Attempts to instantiate `ItemInstance` from `_itemData` and injects it into `targetContainer`. Returns boolean success. On success, calls `Destroy(gameObject)`.
  - string GetInteractionPrompt(): Returns raw string prompt for UI (e.g., "E").

Dependency Graph (Crucial for Scaling):
- Upstream:
  - Requires `SpriteRenderer` (auto-configured).
  - Requires `Collider2D` (configured as trigger).
  - Depends on `InventoryItemSO` (blueprint data).
  - Depends on `InventoryManager` (interaction target container).
  - Depends on `ItemInstance` (runtime data wrapper).
- Downstream:
  - Observed by any player interaction raycaster or overlap query expecting `IContainerInteractable`.

Data Schema:
- InventoryItemSO _itemData -> Blueprint defining the item.
- int _quantity -> Amount to add on pickup (default 1).

Side Effects & Lifecycle:
- Awake: Caches `SpriteRenderer` reference.
- Start: Mutates `SpriteRenderer.sprite` and `gameObject.name` based on `_itemData`. Mutates `Collider2D.isTrigger` to true.
- OnValidate: Emits console warning if `_itemData` is unassigned.
- Interact: Allocates new `ItemInstance` on heap. Destroys `gameObject` upon successful transfer.
