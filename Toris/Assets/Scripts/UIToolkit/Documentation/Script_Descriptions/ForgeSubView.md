- **Identifier:** `OutlandHaven.UIToolkit.ForgeSubView : UIView`
- **Architectural Role:** Component Logic / Processing UI SubView.

- **Core Logic (The 'Contract'):**
  - **Abstract/Virtual Methods:**
    - `SetVisualElements()`: Overridden to bind UI elements (`forge-slot-1`, `forge-slot-2`, `forge-result-slot`, `btn-forge-items`) and instantiate slot templates.
    - `Setup(object payload = null)`: Overridden to clear slots and update the visual result.
    - `Show()`: Overridden to subscribe to inventory events and button clicks.
    - `Hide()`: Overridden to unsubscribe from inventory events and button clicks.
    - `Dispose()`: Overridden to clean up event subscriptions to prevent memory leaks.
  - **Public API:**
    - `ForgeSubView(...)`: Constructor for dependency injection (`VisualTreeAsset`, `UIInventoryEventsSO`, `CraftingManagerSO`).

- **Dependency Graph (Crucial for Scaling):**
  - **Upstream:**
    - Requires `OutlandHaven.UIToolkit.UIView` (Base Class).
    - Requires `UnityEngine.UIElements.VisualTreeAsset` (Slot template).
    - Requires `OutlandHaven.Inventory.UIInventoryEventsSO` (Event channel for item interactions).
    - Requires `OutlandHaven.Inventory.CraftingManagerSO` (Logic arbitrator for recipes).
  - **Downstream:**
    - Handled/instantiated by parent views containing a forge UI.

- **Data Schema:**
  - `_slotTemplate` (VisualTreeAsset): Template for generating UI inventory slots.
  - `_uiInventoryEvents` (UIInventoryEventsSO): Reference to global UI inventory events.
  - `_craftingManager` (CraftingManagerSO): Reference to the crafting manager logic.
  - `_currentSlot1Data`, `_currentSlot2Data` (InventorySlot): Proxy data representing items selected for forging.
  - `_cachedSlot1`, `_cachedSlot2` (InventorySlot): Original inventory slots of the selected items.

- **Side Effects & Lifecycle:**
  - **Lifecycle:** Standard `UIView` manual lifecycle. Instantiates `InventorySlotView` wrappers dynamically during `SetVisualElements()`. Subscribes to events in `Show()` and unsubscribes in `Hide()`/`Dispose()`.
  - **Side Effects:** Evaluates crafting recipes via `_craftingManager`. Triggers `OnRequestForge` event via `_uiInventoryEvents`. Implements 'Right-Click to Auto-Fill' and ghost slots (proxy data) instead of directly mutating the player's actual inventory until confirmed. Allocates proxy `InventorySlot` and `ItemInstance` objects on the managed heap when assigning items.
