Identifier: OutlandHaven.Inventory.InventorySlotView : IDisposable (Implicit via Dispose method)

Architectural Role: Component Logic (UI Controller)

Core Logic:
* Abstract/Virtual Methods: None
* Public API:
  * `Update(InventorySlot slotData)`: Binds data, updates visual state (icon, quantity), and sets user data for drag/drop.
  * `Dispose()`: Unregisters pointer callbacks to prevent memory leaks.

Dependency Graph:
* Upstream: Requires `UnityEngine.UIElements`, `InventorySlot`, `InventoryManager`.
* Downstream: Exposes events (`OnLocalClicked`, `OnLocalRightClicked`, `OnLocalDragStarted`, etc.) consumed by Parent Views (e.g., `PlayerInventoryView`, `PlayerEquipmentView`).

Data Schema:
* `VisualElement _root`: Reference to the root slot container element.
* `Image _icon`: Reference to the item icon element.
* `Label _qtyLabel`: Reference to the item quantity label.
* `InventorySlot _slotData`: Cached reference to the bound slot data.
* `InventoryManager _owningContainer`: Reference to the container managing the slot.
* `bool _isDragging`: Tracks active drag state.
* `Vector2 _dragStartPosition`: Tracks the pointer position where the click originated.

Side Effects & Lifecycle:
* Initialization: Manual initialization via constructor (binds UI elements and registers callbacks).
* Allocations: Instantiates `SlotDropData` during `Update` if not using a proxy ID.
* Lifecycle: Subscribes to `PointerDownEvent`, `PointerMoveEvent`, `PointerUpEvent` on the root element. Triggers local events (e.g., `OnLocalRightClicked(slotData, evt.shiftKey)`). Must be explicitly disposed via `Dispose()`. Modifies `pickingMode` of UI elements and manages internal pointer capture constraints.
