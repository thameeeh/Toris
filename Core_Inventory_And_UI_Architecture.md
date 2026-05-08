# Core Inventory and UI Architecture

This document serves as the master reference for the Item, Inventory, and UI systems in Outland Haven. It consolidates technical documentation with real-time analysis of the current implementation.

---

## 1. Architectural Overview & Patterns

The system is built on a strict **Model-View-Presenter (MVP)** pattern, heavily leveraging **ScriptableObject-based Event Channels** to maintain decoupling.

### 1.1 Model-View-Presenter (MVP)
*   **Model**: Data containers like `InventoryManager` and ScriptableObjects (`ShopManagerSO`, `CraftingManagerSO`). They own the state and logic but have no reference to the UI.
*   **View**: Pure C# classes (`UIView`, `GameView`, `PlayerInventoryView`, `InventorySlotView`) that manipulate UI Toolkit `VisualElement`s. They are "dumb" and only broadcast user intents.
*   **Presenter (Controller)**: `MonoBehaviour` classes (`InventoryScreenController`, `SmithScreenController`) that bridge the gap. they instantiate Views, inject dependencies (Models, Events), and manage the Unity lifecycle.

### 1.2 Data Flow & Synchronization
The UI remains synchronized with the backend via an **Observer Pattern**:
1.  **Mutation**: A backend manager (e.g., `InventoryTransferManagerSO`) modifies an `InventorySlot`.
2.  **Notification**: The `InventoryManager` or Manager SO invokes an event on a global channel (`UIInventoryEventsSO`).
    *   `OnInventoryUpdated`: Triggers a full redraw of a container (expensive).
    *   `OnSpecificSlotsUpdated(source, target)`: Triggers a targeted redraw of only the affected visual slots (performant).
3.  **Reception**: Views subscribed to these channels receive the data and update their internal `InventorySlotView` instances.

---

## 2. Item Components & Inventory Logic

### 2.1 Item Architecture (Blueprint/State Pattern)
The system uses a **Blueprint/State** pattern to separate static definitions from mutable runtime data.
*   **`InventoryItemSO` (Blueprint)**: A ScriptableObject containing static data (Name, Icon, MaxStackSize) and a list of `ItemComponent` modules.
*   **`ItemComponent`**: Defines modular logic. If it requires tracking data (e.g., Durability, Charges), it generates an `ItemComponentState`.
*   **`ItemInstance` (Runtime)**: A C# class wrapping the SO. It holds a list of `ItemComponentState` objects.
*   **`ItemComponentState`**: Holds the actual mutable data. It implements `IsStackableWith` to determine if two instances can merge based on their current state (e.g., same charges, same level).

### 2.2 Inventory Logic
*   **`InventoryManager`**: Manages a list of `InventorySlot`s. It handles authoritative addition and removal of items, including stack calculation.
*   **`InventorySlot`**: Holds the `ItemInstance`, `Count`, and a `SlotFilterType`.
    *   **Smart Validation**: The `CanAccept(ItemInstance)` method is the gatekeeper for slot restrictions (e.g., ensuring only "Head" items go in the "Head" slot).
*   **`InventoryTransferManagerSO`**: The central authority for moving items. It handles:
    *   **Stacking**: Merging items into existing stacks.
    *   **Splitting**: Moving partial stacks (triggered by Shift-Click).
    *   **Swapping**: Exchanging items between slots.

---

## 3. UI Runtime Interactions

### 3.1 Click & Drag Mechanics (`InventorySlotView`)
The UI uses Unity's **UI Toolkit Event System** to capture hardware input:
1.  **`PointerDownEvent`**: Captures the pointer and records the start position.
2.  **`PointerMoveEvent`**: Checks the `DragThreshold`. If exceeded, it initiates a visual drag by broadcasting `OnGlobalDragStarted`.
    *   **Shift-Click**: Calculates `Mathf.CeilToInt(count / 2f)` for splitting.
3.  **`PointerUpEvent`**: Resolves the drop.
    *   **Drop Detection**: Uses `panel.Pick(evt.position)` to find the element under the cursor.
    *   **Data Retrieval**: It traverses up the visual tree to find `userData` containing `SlotDropData` (for standard slots) or a `string` (for Proxy Slots like Forge/Salvage).
    *   **Request Emission**: If a target is found, it fires `OnRequestMoveItem` or `OnRequestSelectForProcessing`.

### 3.2 Contextual Actions
Right-clicks are routed based on the global `InventoryInteractionContext`:
*   **Shop**: Triggers `OnRequestSell`.
*   **Salvage**: Triggers `OnRequestSalvage`.
*   **Normal**: Triggers `OnRequestUse` (for consumables) or `OnRequestEquip` (for equipment).
*   **Equipment Rule**: `PlayerEquipmentView` ignores global context and always interprets right-clicks as `OnRequestUnequip`.

---

## 4. Undiscovered / Unmentioned Functionalities

*   **`EvolvingItemModule`**: A "hidden" mechanic where items track kills (`EvolvingState.CurrentKills`). Once a threshold (`KillsRequired`) is met, the item becomes "Awakened," gaining a damage bonus.
*   **`UpgradeableModule`**: Supports item leveling (`UpgradeableState.CurrentLevel`). While the UI for upgrading is minimal, the data structures support per-item level tracking and stacking restrictions for items of different levels.
*   **`ProgressionModule`**: Categorizes items (Material, QuestItem, Key, Junk) to support future sorting and filtering logic.
*   **Proxy Visual Slots**: Found in the Smithy (Forge/Salvage). These are visual-only containers that pass `null` as their `owningContainer`. They allow users to "place" items for processing without actually moving them out of the player's inventory until the final action (Forge/Salvage) is executed.
*   **`InventoryManager` Auto-Binding**: The `InventoryManager` attempts to "guess" if it is the Player Backpack or Equipment container by checking `ContainerBlueprint` properties or searching for "Equip" in the GameObject name, automatically binding itself to the `GameSessionSO`.

---

## 5. Critical Analysis (Flaws, Bugs, and Patterns)

### Good Patterns
*   **Event-Driven Decoupling**: The use of ScriptableObject event channels (`UIInventoryEventsSO`) allows systems like `ShopManagerSO` to work without ever knowing the UI exists.
*   **Targeted Redraws**: The `OnSpecificSlotsUpdated` event prevents the common "Unity UI lag" by only updating the two slots involved in a move rather than the entire grid.
*   **Modular Items**: The Blueprint/State pattern makes it trivial to add new item behaviors (e.g., a "SocketableModule") without changing the core `ItemInstance` class.

### Bad Patterns & Technical Debt
*   **Hardcoded Equipment Mapping**: Both `PlayerEquipmentController` and `InventoryActionController` contain hardcoded integer mappings (0=Head, 1=Chest, 2=Legs, 3=Arms, 4=Weapon). If the equipment layout changes, logic breaks in multiple disconnected scripts.
*   **Fragile Container Identification**: `InventoryManager.LooksLikeEquipmentContainer()` relies on string-matching the GameObject name for "Equip". This is highly prone to human error and scene reorganization.
*   **Inconsistent Input Handling**: While `PlayerInventoryView` reads `evt.shiftKey` from the event payload (Good), `ShopSubView` queries the global `Input.GetKey(KeyCode.LeftShift)` (Bad). This creates coupling to the old Input system and can lead to race conditions.

### Known Bugs & Edge Cases
*   **Null Drop Handling**: When an item is dropped outside the UI bounds, `panel.Pick` returns null. The current implementation in `InventorySlotView` logs a debug message but does not have a "Drop into World" or "Cancel Drag" fallback logic clearly defined beyond just stopping the visual ghost.
*   **Partial Stack Swap Block**: `InventoryTransferManagerSO` explicitly blocks swapping items if the player is dragging a partial stack. While intended to prevent logic complexity, it can feel like a "dead" interaction to the user.
*   **Shop Refund Risk**: If a player buys an item but their inventory is full, the item is "refunded" to the shop. However, if the shop is also full (unlikely but possible), the item could potentially be deleted or cause a stack overflow.
*   **Constructor Defaulting**: `InventorySlot` defaults `AllowedFilter` to `Any` in its constructor. However, `InventoryManager` initializes slots using `new InventorySlot()`, which may bypass intended Blueprint filters if not carefully managed during initialization.
