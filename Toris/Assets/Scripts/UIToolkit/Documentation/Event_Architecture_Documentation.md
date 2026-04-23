# Event Architecture Documentation

This document outlines the ScriptableObject-based Event Architecture used throughout the UI and Inventory Systems. This setup decouples core game logic from UI representation, relying entirely on events rather than hardcoded references.

## Core Philosophy

*   **Data Decoupling:** UI classes NEVER directly modify the counts or stats inside `InventoryManager` or other Data ScriptableObjects. They only listen to events and dispatch requests.
*   **Observer Pattern:** Systems subscribe to specific events (e.g., `OnInventoryUpdated`) to respond automatically without knowing the source of the event.

## 1. UI Window Events (`UIEventsSO.cs`)

`UIEventsSO` handles generic window management, screen opening requests, and closing.

*   **Location:** `Toris/Assets/Scripts/UIToolkit/UI/Events/UIEventsSO.cs`
*   **`OnRequestOpen(ScreenType, object payload)`:**
    *   Fired when a system (e.g., player input, an NPC interaction) wants to open a screen.
    *   The payload can pass contextual data (like a vendor's inventory).
*   **`OnRequestClose(ScreenType)`:** Fired to close a specific screen.
*   **`OnRequestCloseAll()`:** Closes all non-HUD screens.
*   **`OnScreenOpen(ScreenType)`:** Fired by a `GameView` when it successfully opens, allowing other systems to react.

## 2. Inventory and Economy Events (`UIInventoryEventsSO.cs`)

`UIInventoryEventsSO` handles all inventory-specific actions and transaction requests.

*   **Location:** `Toris/Assets/Scripts/UIToolkit/UI/Events/UIInventoryEventsSO.cs`
*   **`OnInventoryUpdated`**: Fired by `InventoryManager` when its internal list of items changes (addition, removal, count updates). UI views like `PlayerInventoryView` listen to this to refresh the grid.
*   **`OnShopInventoryUpdated`**: Fired by `ShopManagerSO` when vendor stock changes.
*   **`OnRequestBuy` / `OnRequestSell`**: Dispatched by the UI Views when a user clicks (e.g., right-clicking a shop item). This request is caught and processed securely by `ShopManagerSO`.
*   **`OnItemClicked`**: Fired by `InventorySlotView` when an item is selected. Equipment Managers and Crafting views subscribe to this to know which item the player is targeting.
*   **`OnRequestSalvage` / `OnRequestForge`**: Similar to buy/sell requests, these events pass data to specific processing managers (like `CraftingManagerSO` or `SalvageManagerSO`) to execute the logic behind the scenes.

## 3. Best Practices

*   **Null Checks on Event Payloads:** When views dispatch events like `OnRequestBuy` or `OnItemClicked`, ensure they are passing robust references. Consider adding validation to verify `slotData.HeldItem` is not null before dispatching to prevent unexpected behaviors in listeners.
*   **Event Unbinding Optimization:** Ensure `PlayerInventoryView`, `ShopSubView`, and other UI event listeners use `private bool _eventsBound` to prevent double subscriptions during lifecycle changes, particularly if views are repeatedly shown and hidden rather than destroyed. Unbind during `Hide()` and bind during `Show()`.
*   **Secure Transactions**: Passing payloads directly prevents views from generating isolated, unverified items. For shop transactions (e.g., via `OnRequestBuy`, `OnRequestSell`), pass the specific `ItemInstance` object instead of generating a new variable from the `InventoryItemSO` blueprint. This preserves unique dynamic states (like durability) and ensures correct stackability evaluation.
