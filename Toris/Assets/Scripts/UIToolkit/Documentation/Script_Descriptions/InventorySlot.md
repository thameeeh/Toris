Identifier: OutlandHaven.Inventory.InventorySlot

Architectural Role: Data Container

Core Logic:
* Abstract/Virtual Methods: None
* Public API:
  * `Clear()`: Nullifies `HeldItem` and sets `Count` to 0.
  * `SetItem(ItemInstance newItem, int amount)`: Sets `HeldItem` and `Count`.
  * `IncreaseCount(int amount)`: Increments `Count`.
  * `DecreaseCount(int amount)`: Decrements `Count`, calls `Clear()` if `Count` <= 0.

Dependency Graph:
* Upstream: Requires `ItemInstance`.
* Downstream: Consumed by `InventoryManager`, `InventorySlotView`.

Data Schema:
* `ItemInstance HeldItem`: Serialized reference to the runtime item state occupying the slot.
* `int Count`: Serialized integer representing the stack quantity.
* `bool IsEmpty` (Property): Evaluates if `HeldItem` is null or its `BaseItem` is null.

Side Effects & Lifecycle:
* Initialization: Manual initialization via constructor.
* Allocations: Instantiates a new empty `ItemInstance` upon construction.
* Lifecycle: No Unity lifecycle methods (pure C# object).