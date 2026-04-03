Identifier: OutlandHaven.Inventory.InventoryItemSO : ScriptableObject

Architectural Role: Data Container / Abstract Blueprint (Item Definition)

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - GetComponent<T>(): Iterates through the attached `Components` list and returns the first matching `ItemComponent` of type T, or null.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on Sprite, ItemComponent (Abstract Base Class).
- Downstream: Wrapped by `ItemInstance` structs at runtime. Referenced globally by all Inventory, Shop, Equipment, and Crafting systems.

Data Schema:
- string ItemName -> Display name.
- string Description -> Lore or functional description.
- Sprite Icon -> Visual representation for UI.
- int MaxStackSize -> Maximum amount allowed in a single inventory slot.
- int GoldValue -> Base economic value for buying/selling.
- List<ItemComponent> Components -> Modular list of polymorphic behaviors (e.g., EquipableComponent, ConsumableComponent) utilizing the `[SerializeReference]` attribute.

Side Effects & Lifecycle:
- Lifecycle: Inspector-initialized data asset. Read-only at runtime.
- Side Effects (Editor Only): Implements `OnValidate` to safely iterate backward and remove null components from the `Components` list to clean up serialization glitches without index shifting bugs.