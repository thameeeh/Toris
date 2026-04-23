Identifier: OutlandHaven.Inventory.InventoryContainerSO : ScriptableObject

Architectural Role: Data Container / Abstract Blueprint (Inventory Configuration)

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None (Pure Data Container)

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on ScreenType (Enum).
- Downstream: Referenced by InventoryManager to determine initialization constraints and identity.

Data Schema:
- int SlotCount -> The maximum number of slots this container supports (e.g., 20).
- ScreenType AssociatedView -> The UI context this container belongs to (e.g., Inventory, Shop, Equipment).

Side Effects & Lifecycle:
- Lifecycle: Inspector-initialized configuration asset.
- Side Effects: Strictly enforced at runtime and in the Editor by InventoryManager to constrain `LiveSlots` array size to prevent dynamic growth bugs.