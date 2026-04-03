Identifier: Global.IContainerInteractable : None
Architectural Role: Interface / Component Logic Contract

Core Logic:
- Abstract/Virtual Methods:
  - `bool Interact(InventoryManager targetContainer)` -> Execute interaction logic against a target container. Returns success state. Implementation should destroy item on success if consumable.
  - `Vector3 InteractionPosition { get; }` -> Retrieve world space position for interaction range checks.
  - `string GetInteractionPrompt()` -> Retrieve UI text prompt for interaction.
- Public API: N/A (Interface definition)

Dependency Graph:
- Upstream:
  - `UnityEngine.Vector3`
  - `OutlandHaven.Inventory.InventoryManager`
- Downstream:
  - Implemented by interactive world objects (e.g. World Item Drops, Chests).

Data Schema: N/A (Interface)

Side Effects & Lifecycle:
- Implementations expected to handle their own lifecycle (e.g., self-destruction upon successful `Interact`).
