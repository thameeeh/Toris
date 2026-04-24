# UI Interaction and Event System Documentation

This document details the highly decoupled, event-driven architecture governing user interactions, drag-and-drop mechanics, and system-to-system communication within the UI and Inventory systems.

## 1. Event Architecture Philosophy

The project utilizes a ScriptableObject-based Event Architecture to decouple core game logic from UI representation.
*   **Data Decoupling:** UI classes (`Views`) NEVER directly modify data within `InventoryManager` or global managers. They solely listen to events and dispatch user intents (requests).
*   **Observer Pattern:** Systems subscribe to specific global events (e.g., `OnInventoryUpdated`) to respond automatically, completely agnostic to the source of the event.

## 2. Core Event Channels

### 2.1 Window Events (`UIEventsSO.cs`)
Handles generic window management, screen opening requests, and closing.
*   **`OnRequestOpen(ScreenType, object payload)`:** Dispatched when a system wants to open a screen. The payload passes contextual data (e.g., a vendor's inventory container).
*   **`OnRequestClose(ScreenType)` / `OnRequestCloseAll()`:** Requests to close specific or all non-HUD screens.

### 2.2 Inventory & Economy Events (`UIInventoryEventsSO.cs`)
Handles all inventory-specific actions and transaction requests.
*   **`OnInventoryUpdated`:** Fired by `InventoryManager` when its internal list changes. UI views listen to this to refresh.
*   **`OnSpecificSlotsUpdated(sourceSlot, targetSlot)`:** Used for targeted UI redraws (e.g., after a drag-and-drop swap) instead of full container rebuilds, maximizing performance.
*   **`OnRequestBuy` / `OnRequestSell`:** Dispatched by UI Views when a user initiates a transaction. Caught and processed securely by `ShopManagerSO`.
*   **`OnRequestMoveItem(sourceSlot, targetSlot, amount, sourceContainer, targetContainer)`:** The unified request for moving/swapping items, processed by the `InventoryTransferManagerSO`.

## 3. UI Bridge Pattern

The UI must accurately reflect the game state without ever owning it or using Global Anchors directly.
*   **`PlayerHUDBridge`:** A `MonoBehaviour` attached to the player prefab that acts as a UI-facing facade. It listens to internal player state components (`PlayerProgression`, `PlayerStats`) and re-emits them as generic C# events (`OnGoldChanged`, `OnHealthChanged`).
*   **Usage:** UI Views (like `HUDView` or `ShopSubView`) are explicitly passed the `PlayerHUDBridge` upon initialization. The view subscribes to the bridge's events, keeping the view entirely ignorant of gameplay logic and global managers.

## 4. Drag and Drop Architecture

The drag-and-drop system replaces rigid singletons with an event-driven pattern and controller delegation.

*   **Isolated Views:** Visual elements like `InventorySlotView` are completely "dumb". They do not reference a global `UIDragManager.Instance`. They translate low-level UI Toolkit hardware events into generic semantic events and broadcast them.
*   **Authoritative Validation:** When a drop occurs, the UI fires an `OnRequestMoveItem` event. The `InventoryTransferManagerSO` acts as the authoritative bank for the transaction. It validates quantities (`amountToMove`), blocks partial-stack swaps, and executes the data move.
*   **Contextual Fast-Actions:** Right-clicking items serves as the universal 'Contextual Fast-Action'.
    *   Auxiliary views (Shop, Salvage) introduce an `InventoryInteractionContext` enum value and broadcast it when opening/closing.
    *   Views like `PlayerInventoryView` read this context to route the right-click action accordingly (e.g., Sell vs. Equip).
    *   *Standard RPG UX Rule:* `PlayerEquipmentView` strictly ignores global context and always routes right-clicks to unequip actions.

## 5. Proxy Visual Slots

Proxy visual slots in auxiliary views (e.g., Shop, Forge, Salvage) are localized visual containers.
*   They must NOT broadcast click events to the global `UIInventoryEventsSO` event bus.
*   Instead, they bind left/right click events to trigger local clearing or local logic directly.
*   When validating drag-and-drop logic involving proxy slots, they explicitly pass `null` as their `InventoryManager owningContainer` to differentiate them from actual backend data containers.
