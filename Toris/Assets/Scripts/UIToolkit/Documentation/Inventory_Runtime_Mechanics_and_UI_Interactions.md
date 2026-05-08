# Inventory Runtime Mechanics and UI Interactions

This document serves as a comprehensive deep dive into the runtime mechanics and the UI-to-Logic bridge within the Outland Haven inventory system. It explains the interaction lifecycles, event-driven data flow, the exact roles of specific scripts, and highlights critical technical edge cases and architectural quirks.

## 1. Preamble: The UI Toolkit & MVP Architecture Context

The Outland Haven user interface is built on Unity's **UI Toolkit** and adheres to a strict **MVP (Model-View-Presenter)** architectural pattern. This design prevents "god objects" and heavily couples logic directly to UI visual elements.

*   **Model (The Data Layer):** This encompasses classes like `InventoryManager` (a `MonoBehaviour` managing a list of slots) and `InventorySlot` (a raw data structure holding an `ItemInstance` and a `Count`). The Model is completely ignorant of the UI.
*   **View (The Presentation Layer):** Pure C# classes such as `InventorySlotView`, `PlayerInventoryView`, and `PlayerEquipmentView`. These classes wrap `.uxml` visual elements and manage low-level UI Toolkit input events (like pointers and clicks). Their sole responsibility is to translate user input into C# events and visually reflect data state.
*   **Presenter (The Logic & Mediation Layer):** Controllers like `InventoryActionController`, `InventoryTransferManagerSO`, and various `ScreenController` scripts. They listen to the generic intents broadcast by the Views and execute actual game logic by interacting with the Model.

To facilitate communication between these layers without creating tight coupling, the architecture heavily utilizes **ScriptableObject-based event buses** (specifically `UIInventoryEventsSO`).

## 2. Script Roles in the Architecture

To fully grasp the UI interaction loop, one must understand how the different scripts collaborate:

1.  **`InventorySlotView` (The Raw Input Receptor):** This is the lowest-level UI component. It attaches to a single `.uxml` item slot. It handles `PointerDownEvent`, `PointerMoveEvent`, and `PointerUpEvent`. It knows nothing about the game context (e.g., whether it's a shop or a player's bag). It simply broadcasts raw local events like `OnLocalClicked`, `OnLocalRightClicked`, and `OnLocalDragStarted`.
2.  **`PlayerInventoryView` / `PlayerEquipmentView` (The Translators):** These parent views manage a grid of `InventorySlotView` instances. They subscribe to the local events of their children and translate them into global, semantic intents. For example, when a child fires `OnLocalRightClicked`, `PlayerInventoryView` checks the global `InventoryInteractionContext` (is the shop open?) and broadcasts either `OnRequestSell` or `OnRequestUse` via `UIInventoryEventsSO`.
3.  **`UIDragManager` (The Visual Coordinator):** A global `MonoBehaviour` that listens to `OnGlobalDragStarted`. It entirely manages the visual "ghost icon" that follows the mouse cursor on an absolute UI layer. It doesn't move data; it only moves pixels.
4.  **`InventoryTransferManagerSO` (The Authoritative Bank):** The core Presenter for drag-and-drop. It listens to `OnRequestMoveItem`. When an item drop is requested, it performs strict validation (`CanAccept`), handles fractional splits, validates stack merges, and ensures same-slot drops are safely aborted before mutating the `InventoryManager` data.
5.  **`InventoryActionController` (The Executor):** A `MonoBehaviour` listening for usage intents like `OnRequestEquip`, `OnRequestUse`, or `OnRequestUnequip`. It serves as the bridge between the inventory data and the player's physical avatar, interacting with `PlayerStats` or `PlayerConsumableController` to apply effects.

## 3. The Drag-and-Drop State Machine

The drag-and-drop lifecycle is a complex, multi-script state machine driven by low-level pointer events.

### 3.1 The Lifecycle Breakdown

1.  **Initiation (`InventorySlotView.OnPointerDown`):**
    *   A left (0) or right (1) click on an occupied slot triggers this event.
    *   The `VisualElement` immediately captures the pointer (`_root.CapturePointer(evt.pointerId)`). This guarantees that subsequent move and up events route to this specific slot, even if the mouse leaves its visual bounds.
    *   *Note:* The visual drag ghost does not appear here. The system waits for movement to pass a threshold to distinguish a drag from a click.

2.  **Movement & Splitting (`InventorySlotView.OnPointerMove`):**
    *   If the pointer moves beyond `DragThreshold` (10 pixels) while captured, the drag state is confirmed.
    *   The slot calculates the drag amount. Holding `Shift` splits the stack. It uses `Mathf.CeilToInt(_slotData.Count / 2f)` so that odd-numbered stacks yield the larger half to the active drag.
    *   It fires `OnLocalDragStarted`, which propagates to `UIInventoryEventsSO.OnGlobalDragStarted`.
    *   `UIDragManager` receives this event, instantiates/shows the ghost icon (`_ghostIcon`), and continuously updates its absolute position.

3.  **Release & Resolution (`InventorySlotView.OnPointerUp`):**
    *   The originating slot releases pointer capture and the ghost icon is hidden.
    *   A programmatic raycast (`_root.panel.Pick(evt.position)`) is executed to identify the underlying UI element at the exact drop coordinate.
    *   The system traverses up the visual tree (`FindTargetDropData`) to locate a `SlotDropData` object (which holds the target `InventorySlot` and `InventoryManager`) or a `proxySlotID`.
    *   If a valid data target is found, `OnLocalMoveItemRequested` fires, propagating globally.
    *   `InventoryTransferManagerSO` intercepts the request. It queries the target data slot (`targetSlot.CanAccept(sourceSlot.HeldItem)`). If validated, it executes the mathematical data swap or stack merge.

### 3.2 Undiscovered Functionalities, Edge Cases, & Quirks

*   **`evt.button` Unreliability (`FACTUAL FIX 2`):** During `PointerMoveEvent`s in UI Toolkit, querying `evt.button` directly is notoriously unreliable and can return incorrect states. `InventorySlotView` bypasses this bug by utilizing the `evt.pressedButtons` bitmask to ensure the left mouse button is continuously held down (`(evt.pressedButtons & 1) != 0`).
*   **UI Toolkit Flexbox Resolution Failsafe:** When a drag initiates very quickly, the UI Toolkit layout engine may not have finished resolving the flexbox dimensions of the slot icon, resulting in width/height values of `NaN` or `0`. A failsafe in `InventorySlotView` detects this and forces a default `80f x 80f` size for the ghost icon to prevent invisible drags.
*   **Raycast Interference (`FACTUAL FIX 1`):** The `panel.Pick()` raycast stops at the first element it hits. Therefore, child visual elements of the slot (such as the item image or quantity text label) *must* have their `pickingMode` set to `Ignore`. If they are set to `Position`, the raycast hits the child, and because the `SlotDropData` is attached to the parent root, the drop will silently fail.
*   **Partial Stack Swapping Block:** `InventoryTransferManagerSO` enforces a strict rule: you cannot perform a swap action (dropping an item onto a different item type) if you are currently dragging a partial stack (via Shift-drag). This prevents data duplication/loss edge cases.
*   **Contextual Right-Click Rules:** Right-clicking serves as a Contextual Fast-Action. While `PlayerInventoryView` relies on the `InventoryInteractionContext` (Shop, Salvage, Normal) to decide whether to sell, scrap, or consume, `PlayerEquipmentView` strictly enforces an RPG UX rule: it *ignores* context and always interprets a right-click as an unequip action.

## 4. Data Binding & Syncing

To ensure the UI is decoupled from core game logic, the system utilizes a hybrid architecture for data binding and performance-optimized UI updates.

### 4.1 The Hybrid Architecture: `PlayerHUDBridge`

The UI must accurately reflect player stats (health, stamina, level) and inventory states without tightly coupling to the underlying gameplay MonoBehaviours (e.g., `PlayerStats`, `PlayerProgression`).

*   **The Problem:** If UI Views directly referenced `PlayerStats`, changing how health is calculated would break the UI, violating the separation of concerns.
*   **The Solution:** `PlayerHUDBridge` acts as an intermediary facade attached to the player prefab. It subscribes to internal, gameplay-specific events (`_playerStats.OnHealthChanged`).
*   **The Re-emission:** When an internal event fires, the Bridge re-emits it as a clean, generic C# Action (`OnHealthChanged(current, max)`).
*   **The Consumption:** UI Views (`HUDView`, `PlayerStatsView`, `ShopSubView`) are passed the `PlayerHUDBridge` during their initialization setup. They subscribe strictly to the Bridge's generic events. This completely insulates the UI; it is entirely ignorant of how the game logic calculates the values it displays.

### 4.2 Targeted Redraws: The `OnSpecificSlotsUpdated` Optimization

Redrawing the entire UI Toolkit layout is a costly operation. The inventory UI prevents severe performance penalties by avoiding full grid rebuilds when single items are moved.

*   When `InventoryTransferManagerSO` completes a drag-and-drop transaction, it intentionally avoids firing a generic `OnInventoryUpdated` event. Instead, it fires `_uiInventoryEvents.OnSpecificSlotsUpdated(sourceSlot, targetSlot)`.
*   Views that display grids (`PlayerInventoryView`, `PlayerEquipmentView`) maintain a private Dictionary (`_slotDictionary`). This dictionary maps the raw data structure (`InventorySlot`) directly to its corresponding visual wrapper (`InventorySlotView`).
*   Upon receiving `OnSpecificSlotsUpdated`, the View performs an O(1) dictionary lookup for the `sourceSlot` and `targetSlot`. It then calls `Update()` exclusively on those two `InventorySlotView` instances. The rest of the grid remains untouched, massively optimizing rendering overhead.

### 4.3 Architectural Risks & Limitations

*   **Memory Leaks via Unsubscription Negligence:** This heavy reliance on an event-driven architecture makes proper lifecycle management critical. Views are pure C# classes implementing `IDisposable`. They *must* unsubscribe from global `UIInventoryEventsSO` and `PlayerHUDBridge` events in their `Hide()` or `Dispose()` methods. Failure to do so creates memory leaks and causes `NullReferenceExceptions` when the game attempts to update visual elements that have been destroyed or removed from the DOM.
*   **Dictionary Stale State:** The `OnSpecificSlotsUpdated` optimization relies on the `_slotDictionary` being perfectly synchronized with the `InventoryManager`'s backend list. If a slot is completely destroyed or the data structure is radically altered without a full `OnInventoryUpdated` event forcing a complete UI rebuild, the dictionary lookups will fail or update the wrong elements, leading to a broken UI state.
