Identifier: OutlandHaven.UIToolkit.SalvageSubView : UIView

Architectural Role: Component Logic / Contextual UI SubView

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: Overrides `SetVisualElements()`, `Setup(object)`, `Show()`, `Hide()`, `Dispose()`.
- Public API:
  - `Setup(object payload)`: Clears input slot and updates visuals. Payload is ignored.
  - `Show()`: Binds events and button callbacks.
  - `Hide()`: Unbinds events and button callbacks.
  - `Dispose()`: Unbinds remaining events and calls base `Dispose()`.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on `VisualTreeAsset` (slot template), `UIInventoryEventsSO` (event channel), `SalvageManagerSO` (salvage logic/recipes), `InventorySlotView` (slot rendering).
- Downstream: Instantiated and managed by `SmithView`.

Data Schema:
- `InventorySlot _currentSlotData`: Proxy data for the item placed in the salvage slot.
- `InventorySlot _cachedSourceSlot`: Reference to the original slot in the player's inventory to prevent duplicates and handle callbacks.
- Visual elements and templates (`_slotTemplate`, `_inputSlotContainer`, `_goldYieldField`, `_itemYieldContainer`, etc.) for UI state.

Side Effects & Lifecycle:
- Instantiates UI Toolkit `TemplateContainer` clones for input and yield slots during `SetVisualElements`.
- Creates dummy `InventorySlot` and `ItemInstance` objects (heap allocation) when handling proxy drops or item clicks to represent the item without moving it.
- Subscribes to global `UIInventoryEventsSO` (`OnItemClicked`, `OnItemRightClicked`, `OnRequestSelectForProcessing`) when shown.
- Emits global events (`OnRequestSalvage`) when "Get Gold" or "Get Item" buttons are clicked.
- Does not use Unity `Update()` loop. Managed lifecycle via parent view.
