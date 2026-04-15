# UI Interactions Documentation

This document covers the standards and technical implementation for User Interface interactions within the game, specifically focusing on UI Toolkit elements, Drag-and-Drop operations, and Inventory Slot Interactions.

## 1. Abstraction of Raw Inputs

To keep the UI views strictly separated from game logic, raw hardware inputs (e.g., left-clicks, right-clicks, drags) are abstracted into semantic, game-specific events.

*   **Raw Input Handling:** All direct hardware interactions occur at the base level component, such as `InventorySlotView.cs`.
*   **Event Emitting:** Instead of directly resolving what a "right-click" does, `InventorySlotView` determines it was a right-click and fires a specific semantic event via a ScriptableObject channel (e.g., `UIInventoryEventsSO.OnItemRightClicked`).
*   **Decoupled Logic:** Other systems (like the `ShopSubView` or `EquipmentEffectBridge`) subscribe to these semantic events. This way, if a player is in a shop, right-clicking might mean "Sell", while in the normal inventory it might mean "Equip". The slot itself does not need to know this context.

### Semantic Events (`UIInventoryEventsSO`)
Examples of abstracted events include:
*   `OnItemClicked(InventorySlot slotData)`: Typically fired on a standard left-click (pointer up).
*   `OnItemRightClicked(InventorySlot slotData)`: Typically fired on a standard right-click (pointer up with right mouse button). Used for auto-fill in crafting or quick-sell in shops.
*   `OnRequestMoveItem(InventorySlot source, InventorySlot target, int amountToMove)`: Emitted when a drag-and-drop operation successfully finishes over a valid target, explicitly stating the quantity to move (e.g., accounting for Shift key stack splitting).

## 2. Drag-and-Drop Implementation

Implementing Drag-and-Drop in Unity's UI Toolkit requires careful management of pointer events to ensure smooth performance, avoid breaking normal clicks, and prevent visual bugs.

### Event Usage
Drag-and-Drop is driven by the following events within `InventorySlotView.cs`:
1.  **`PointerDownEvent`:** Records the initial click position and the state of the slot (whether it has an item). It *does not* immediately start the drag.
2.  **`PointerMoveEvent`:** Checks the distance between the current pointer position and the initial click position.
3.  **`PointerUpEvent`:** If the pointer goes up before the drag threshold is met, it evaluates as a normal click. If it's a drag, it finalizes the drop based on the cursor's location.

### Drag Threshold
A crucial rule for drag-and-drop implementations is the use of a **Drag Threshold** in `PointerMoveEvent`.
*   **Why:** Without a threshold, any micro-movement of the mouse during a click will accidentally trigger a drag, ruining the standard click functionality.
*   **How:** The system checks `Vector2.Distance(evt.position, initialClickPosition)`. If this exceeds a predefined threshold (e.g., 5 pixels), the drag formally begins.

### The "Ghost" Icon and Drag Layer
To prevent clipping issues where a dragged item disappears behind other UI panels (due to flexbox layout constraints or standard hierarchy drawing order):
*   **Dedicated Root Layer:** A temporary "ghost" visual element is instantiated representing the dragged item.
*   **Global Position:** This ghost icon is placed in a dedicated root-level `#Drag_Layer` managed by an injected event-driven pattern. Visual slots no longer hard-reference global singletons (like `UIDragManager.Instance`) but instead rely on event broadcasts or controller delegation.
*   **Picking Mode:** Crucially, the ghost icon must have `pickingMode = PickingMode.Ignore` set programmatically. If it does not, the ghost icon itself will intercept the `PointerUpEvent` raycasts, making it impossible to drop the item onto another slot.

## 3. Context Overrides and Generic Views

Generic inventory views (like `PlayerInventoryView`) should remain agnostic to current game state. They blindly broadcast interaction events (`OnItemRightClicked`).

Context-specific windows (like `ShopSubView`, `SalvageSubView`, `ForgeSubView`) act as active "Context Overrides":
*   **Subscription Scope:** They subscribe to the generic events (e.g., `OnItemRightClicked`) *only* during their `Show()` lifecycle phase and unsubscribe during `Hide()`.
*   **Execution:** When they hear the event, they execute their specific logic (e.g., auto-filling a target processing slot or instantly selling the item to the shop).

## 4. Cross-Container Transfers

When an item is dropped from one inventory (e.g., player) to another (e.g., chest), it involves two separate `InventoryManager` instances.
*   **Arbitration:** To prevent hard-coupling between containers, cross-container transfers are handled by a centralized arbitrator (e.g., `InventoryTransferManagerSO`).
*   **Execution:** This manager listens to the global `OnRequestMoveItem` event, taking both the full source context, target context, and the `amountToMove`. It evaluates the rules (can these merge? is there space?), calculates the `actualAmount` via `Mathf.Min` logic to prevent overfilling and block partial-stack swap bugs, and then manipulates the respective managers accordingly.

## 5. Cleaning Up Event Listeners

Any custom view wrapper objects (like `InventorySlotView`) that register event callbacks (`PointerDownEvent`, etc.) must implement an explicit `Dispose()` method.
*   **Memory Leaks:** Parent views must call `Dispose()` on these child objects before clearing them from lists (e.g., `_slotViews.Clear()`) or regenerating the grid. Failing to unregister these callbacks causes memory leaks and orphaned listeners that can execute logic multiple times.
