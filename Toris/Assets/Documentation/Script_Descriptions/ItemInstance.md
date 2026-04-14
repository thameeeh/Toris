Identifier: OutlandHaven.Inventory.ItemInstance

Architectural Role: Data Container (Runtime State Wrapper)

Core Logic:
* Abstract/Virtual Methods: None
* Public API:
  * `NotifyStateChanged()`: Invokes `OnStateChanged` action.
  * `GetState<T>()`: Retrieves a specific typed state at runtime.
  * `IsStackableWith(ItemInstance other)`: Checks if this instance can merge with another (requires matching blueprint and identical state composition).
  * `Clone()`: Creates a deep copy of the item instance and its states with a new `InstanceID`.

Dependency Graph:
* Upstream: Requires `InventoryItemSO`, `ItemComponentState`.
* Downstream: Consumed by `InventorySlot`, Inventory Managers, Equipment System.

Data Schema:
* `string InstanceID`: Unique identifier for saving/loading.
* `InventoryItemSO BaseItem`: Reference to the base item blueprint.
* `List<ItemComponentState> States`: Serialized list of runtime component states (e.g., Durability, Consumable).
* `Action<ItemInstance> OnStateChanged`: Event triggered on state mutation.

Side Effects & Lifecycle:
* Initialization: Manual initialization via constructors. The blueprint constructor allocates and adds initial states based on `BaseItem.Components`.
* Allocations: Instantiates a Guid string on creation. Instantiates a new state list upon cloning. Allocates specific state instances when initialized from a blueprint.
* Lifecycle: No Unity lifecycle methods (pure C# object). Relies on explicit event invocation (`NotifyStateChanged`).
