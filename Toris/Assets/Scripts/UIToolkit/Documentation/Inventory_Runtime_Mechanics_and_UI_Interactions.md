# Inventory Runtime Mechanics and UI Interactions

This document acts as a deep dive into the runtime mechanics and the UI-to-Logic bridge within the Outland Haven inventory system, focusing on interaction lifecycles, event-driven data flow, and specific technical implementation details.

## 1. Preamble: UI Toolkit & MVP Architecture

The project employs an **MVP (Model-View-Presenter)** architectural pattern combined with Unity's UI Toolkit.

*   **Model:** Data containers like `InventoryManager`, `InventorySlot`, and item states.
*   **View:** Pure C# classes (`InventorySlotView`, `PlayerInventoryView`, `PlayerEquipmentView`) that wrap UXML visual elements. They handle low-level UI input (pointers, clicks) and reflect data state visually. They do not contain game logic.
*   **Presenter:** Controllers (`InventoryScreenController`, `InventoryActionController`) that mediate between the UI Views and the core gameplay systems.

This separation of concerns relies heavily on ScriptableObject-based event buses (e.g., `UIInventoryEventsSO`) to communicate intent without direct references, maintaining a decoupled and modular architecture.

## 2. The Drag-and-Drop State Machine

The drag-and-drop lifecycle is a complex state machine driven by low-level UI Toolkit pointer events within `InventorySlotView` and orchestrated globally by `UIDragManager` and `InventoryTransferManagerSO`.

### 2.1 The Lifecycle

1.  **Initiation (`OnPointerDown`):**
    *   A left (0) or right (1) click on an occupied slot triggers `OnPointerDown`.
    *   The `VisualElement` captures the pointer (`_root.CapturePointer(evt.pointerId)`). This prevents other UI elements from receiving pointer events until release.
    *   *Note:* The drag does not visually start here. It waits for movement to pass a threshold.

2.  **Movement (`OnPointerMove`):**
    *   If the pointer moves beyond `DragThreshold` (10 pixels) while captured, the drag officially begins.
    *   The slot calculates the drag amount. Holding `Shift` splits the stack (`Mathf.CeilToInt(_slotData.Count / 2f)`).
    *   It fires `OnLocalDragStarted`, which propagates to `UIInventoryEventsSO.OnGlobalDragStarted`.
    *   `UIDragManager` listens to this event, creating a ghost icon (`_ghostIcon`) on an absolute UI layer and binding its position to the pointer.

3.  **Release & Resolution (`OnPointerUp`):**
    *   The slot releases the pointer capture.
    *   The ghost icon is hidden.
    *   `_root.panel.Pick(evt.position)` performs a raycast to find the underlying UI element at the drop location.
    *   The system traverses up the visual tree (`FindTargetDropData`) to find an element containing `SlotDropData` (a valid inventory slot container) or a `proxySlotID`.
    *   If a valid target is found, `OnLocalMoveItemRequested` fires, propagating to `UIInventoryEventsSO.OnRequestMoveItem`.
    *   `InventoryTransferManagerSO` intercepts the request, validates it via `targetSlot.CanAccept(sourceSlot.HeldItem)` and handles the actual data swap, stack merge, or rejection.

### 2.2 Edge Cases and Technical Quirks

*   **`evt.button` Unreliability during `PointerMoveEvent`:** `evt.button` is unreliable during pointer movement. The implementation relies on the `evt.pressedButtons` bitmask to ensure the left mouse button is held down (`(evt.pressedButtons & 1) != 0`).
*   **UI Toolkit Flexbox Resolution Delay:** When initiating a drag, the UI Toolkit may not have resolved the flexbox dimensions of the slot icon, resulting in `NaN` or `0` values. A failsafe in `InventorySlotView` forces a default `80f x 80f` size if resolution fails.
*   **Raycast Interference:** Child elements of the slot (like the icon or quantity label) must have their `pickingMode` set to `Ignore`. If they don't, the `panel.Pick()` raycast hits the child instead of the root, and the `SlotDropData` might not be found on that specific child element, causing the drop to fail silently.
*   **Partial Stack Swapping:** A swap action (dropping an item onto a different item type) will fail if the user is dragging a partial stack (via Shift-drag). `InventoryTransferManagerSO` blocks this operation.
*   **Same Slot Drop:** Dropping an item onto the exact slot it originated from is safely aborted by `InventoryTransferManagerSO`.

## 3. Data Binding & Syncing

The UI system reflects data changes immediately through an event-driven observer pattern, utilizing a hybrid architecture for performance and decoupling.

### 3.1 The Hybrid Architecture: `PlayerHUDBridge`

The UI must accurately reflect player stats and inventory without tightly coupling to the gameplay logic (e.g., `PlayerStats`, `PlayerProgression`).

*   `PlayerHUDBridge` acts as a facade attached to the player prefab. It subscribes to internal gameplay events (like `_playerStats.OnHealthChanged`).
*   When a gameplay event fires, the Bridge re-emits a generic C# Action (e.g., `OnHealthChanged(current, max)`).
*   UI Views (like `HUDView` or `PlayerStatsView`) receive the `PlayerHUDBridge` during initialization and subscribe to its events. This ensures the UI is strictly a consumer of presentation-ready data.

### 3.2 Targeted Redraws: `OnSpecificSlotsUpdated`

To avoid severe performance penalties, the inventory UI does not rebuild its entire grid when a single item moves.

*   When `InventoryTransferManagerSO` completes a move/swap, it fires `_uiInventoryEvents.OnSpecificSlotsUpdated(sourceSlot, targetSlot)`.
*   Views like `PlayerInventoryView` and `PlayerEquipmentView` maintain a dictionary mapping data (`InventorySlot`) to visual representations (`InventorySlotView`).
*   Instead of calling `RefreshGrid()` and re-instantiating UXML templates, the View looks up the specific `sourceSlot` and `targetSlot` in the dictionary and calls `Update()` only on those two specific `InventorySlotView` instances.

### 3.3 Architectural Risks

*   **Memory Leaks via Unsubscription:** The event-driven architecture relies heavily on proper disposal. Views implement `IDisposable` and must unsubscribe from global `UIInventoryEventsSO` events in their `Hide()` or `Dispose()` methods. Failure to do so will result in memory leaks and NullReferenceExceptions as the game attempts to update destroyed visual elements.
*   **Dictionary Stale State:** The optimization in `OnSpecificSlotsUpdated` requires the `_slotDictionary` to remain perfectly synchronized with the underlying `InventoryManager`. If a slot is completely removed or the inventory structure changes drastically without a full `OnInventoryUpdated` event to rebuild the dictionary, the UI will break.
