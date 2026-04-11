# Shop Architecture Documentation

This document outlines the architecture for the shop system using the existing UI Toolkit infrastructure. The system is designed to seamlessly integrate with the current `InventoryManager` and the Event-driven UI framework.

## 1. Data Layer

### `PlayerDataSO` (Player Data/State)
*   **Currency Tracking**: A `public int Gold` currency variable is tracked in `PlayerDataSO` to persist the player's available funds across scenes/sessions. This keeps the player's wealth decoupled from their physical inventory slots.

### `InventoryItemSO` (Item Definition)
*   **Pricing**: The `GoldValue` property defined in `InventoryItemSO` is utilized.
    *   **Selling**: Items sold by the player use `GoldValue`.
    *   **Buying**: Items bought from the shop also use `GoldValue`.

### `ShopContainerSO` (Shop Inventory)
*   A ScriptableObject, utilizing `InventoryManager`, holds a list of `InventorySlot`s representing the items available for purchase and their quantities.
*   The `AssociatedView` is set to `ScreenType.Smith` or similar vendor screens.

## 2. Event System

To maintain decoupling, interactions between the Player, Shop, and UI rely on `ScriptableObject` events.

### `UIEventsSO`
The event system handles opening and closing. When a player interacts with a Shop NPC (`WorldContainer` or similar interactive object), it invokes:
*   `OnRequestOpen(ScreenType.Smith, shopContainerData)`
*   *Crucially*, listening to a vendor screen opening also triggers the Player Inventory to open, so both windows are visible.

### `UIInventoryEventsSO`
Events are used to handle the transactions securely without UI Views directly changing data:
*   `OnRequestBuy(ItemInstance item, int quantity)`
*   `OnRequestSell(ItemInstance item, int quantity)`

*Note: A centralized `ShopManagerSO` listens to these `Buy/Sell` events, verifies currency/inventory space, and updates the data containers before firing `OnInventoryUpdated` and ``.*

## 3. UI Layer (View & Controller)

Following the standard UI architecture, utilizing a modular component approach:

### The Modular Shop UXML (`ShopSubView.uxml`)
Instead of a dedicated, standalone screen, the Shop UI is a modular `VisualTreeAsset` embedded within other NPC interfaces (e.g., the Smith).

### `ShopSubView` (Inherits `UIView`)
*   **Role**: Because the shop is a sub-component of a larger screen (like the Smith), it inherits from `UIView` rather than `GameView`.
*   **Setup**: The parent view (e.g., `SmithView`) passes the necessary dependencies (`ShopContainerSO`, `UIEventsSO`, etc.) to the `ShopSubView` during its own initialization.
*   **Responsibilities**:
    *   Binds the shop data to visual slots.
    *   Displays the player's current currency.
    *   Listens to `` to update the currency label.
    *   Provides public `Show()` and `Hide()` methods to be called by the parent view.

### Integration in Parent Views (e.g., `SmithView` / `SmithController`)
*   **Dependencies**: The `SmithScreenController` requires the main Smith UXML, the modular `ShopSubView` UXML, and the Slot `VisualTreeAsset`.
*   **Layout**: The parent UXML includes a designated container element and navigation buttons (e.g., "Shop", "Upgrade", "Salvage").
*   **Navigation Logic**:
    *   Clicking the "Shop" button calls `shopSubView.Show()` and hides the other sub-views (Forge, Salvage).
    *   Clicking "Forge" hides the shop (`shopSubView.Hide()`) and shows the forge sub-view.

## 4. Interaction Logic

### Click Interactions (Right-Click)
*   Right-clicking an item in the `PlayerInventoryView` while the Shop is open attempts to sell the item (firing `OnRequestSell`).
*   Right-clicking an item in the `ShopSubView` attempts to buy the item (firing `OnRequestBuy`).

### Splitting Stacks
*   Holding Shift requests a quantity of 10, otherwise 1. This dispatches the `OnRequestBuy`/`OnRequestSell` event with the chosen quantity.

## 5. Flow Example: Interacting with the Smith's Shop

1. Player interacts with the Smith NPC (`WorldContainer` or specialized logic).
2. NPC invokes `_uiEvents.OnRequestOpen?.Invoke(ScreenType.Smith, smithDataPayload)`.
    *   *(The `smithDataPayload` contains references to the `InventoryManager` representing the shop).*
3. `UIManager` receives the event.
4. `UIManager` finds the registered `SmithView` (in `ScreenZone.Left`) and calls its `Setup(payload)` and `Show()` methods.
5. The `SmithView` initializes the `ShopSubView` with the shop data.
6. *Simultaneously*, opening `ScreenType.Smith` triggers the `PlayerInventoryView` to open (in `ScreenZone.Right`).
7. The player clicks the "Shop" tab in the Smith UI. The `SmithView` hides the default crafting screen and calls `ShopSubView.Show()`.
8. The player can right-click to instantly buy/sell items between `ScreenZone.Right` (Player) and `ScreenZone.Left` (Smith's Shop Sub-View).
9. Clicking the "Forge" tab hides the `ShopSubView` and shows the forging interface, but the Player Inventory remains open on the right.

---

## 6. Recommendations

*   **Currency Check Robustness:** The current implementation correctly handles buy/sell requests within the `ShopManagerSO`. Ensure this manager is thoroughly tested to prevent exploiting concurrent buy/sell actions that might bypass currency checks.
*   **Transaction Feedback:** Ensure there is adequate UI/audio feedback for successful or failed transactions (e.g., "Not enough gold" or "Inventory full").