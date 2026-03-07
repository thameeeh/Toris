# Shop Architecture Plan

This document outlines the planned architecture for implementing a shop system using the existing UI Toolkit infrastructure. The system is designed to seamlessly integrate with the current `InventoryContainerSO` and the Event-driven UI framework.

## 1. Data Layer Additions

### `GameSessionSO` (Player Data/State)
*   **Currency Tracking**: A `public int Gold;` (or similar currency variable) will be added to `GameSessionSO` to persist the player's available funds across scenes/sessions. This keeps the player's wealth decoupled from their physical inventory slots.

### `InventoryItemSO` (Item Definition)
*   **Pricing**: The existing `GoldValue` property already defined in `InventoryItemSO` will be utilized.
    *   **Selling**: Items sold by the player will use `GoldValue`.
    *   **Buying**: Items bought from the shop will also use `GoldValue` (or a derived price if a multiplier logic is needed in the future).

### `ShopContainerSO` (Shop Inventory)
*   Instead of hardcoding shop items in the View, a new ScriptableObject, `ShopContainerSO`, will be created (likely inheriting from `ScriptableObject`, or utilizing `InventoryContainerSO` directly if no unique logic is needed). It will hold a list of `InventorySlot`s representing the items available for purchase and their quantities.
*   The `AssociatedView` should be set to `ScreenType.Shop`.

## 2. Event System Extensions

To maintain decoupling, interactions between the Player, Shop, and UI will rely on `ScriptableObject` events.

### `UIEventsSO`
The existing event system handles opening and closing. When a player interacts with a Shop NPC (`WorldContainer` or similar interactive object), it will invoke:
*   `OnRequestOpen(ScreenType.Shop, shopContainerData)`
*   *Crucially*, listening to `ScreenType.Shop` opening should also trigger the Player Inventory to open, so both windows are visible.

### `UIInventoryEventsSO` (or a new `UIShopEventsSO`)
New events will be needed to handle the transactions securely without UI Views directly changing data:
*   `OnRequestBuy(InventoryItemSO item, int quantity)`
*   `OnRequestSell(InventoryItemSO item, int quantity)`
*   `OnCurrencyChanged(int newAmount)`

*Note: A centralized `ShopManager` or similar logic class will listen to these `Buy/Sell` events, verify currency/inventory space, and update the data containers before firing `OnInventoryUpdated` and `OnCurrencyChanged`.*

## 3. UI Layer (View & Controller)

Following the standard UI architecture, but utilizing a modular component approach:

### The Modular Shop UXML (`ShopSubView.uxml`)
Instead of a dedicated, standalone screen, the Shop UI will be a modular `VisualTreeAsset` designed to be embedded within other NPC interfaces (e.g., the Smith, the Alchemist).

### `ShopSubView` (Inherits `UIView`)
*   **Role**: Because the shop is a sub-component of a larger screen (like the Smith), it inherits from `UIView` rather than `GameView`.
*   **Setup**: The parent view (e.g., `SmithView`) will pass the necessary dependencies (`ShopContainerSO`, `UIEventsSO`, etc.) to the `ShopSubView` during its own initialization.
*   **Responsibilities**:
    *   Binds the shop data to visual slots.
    *   Displays the player's current currency.
    *   Listens to `OnCurrencyChanged` to update the currency label.
    *   Provides public `Show()` and `Hide()` methods to be called by the parent view.

### Integration in Parent Views (e.g., `SmithView` / `SmithController`)
*   **Dependencies**: The `SmithController` will require the main Smith UXML, the modular `ShopSubView` UXML, and the Slot `VisualTreeAsset`.
*   **Layout**: The parent UXML will include a designated container element (e.g., `#shop-container`) and navigation buttons (e.g., "Shop", "Upgrade", "Repair").
*   **Navigation Logic**:
    *   Clicking the "Shop" button calls `shopSubView.Show()` and hides the other sub-views (Upgrade, Repair).
    *   Clicking "Upgrade" hides the shop (`shopSubView.Hide()`) and shows the upgrade sub-view.

## 4. Interaction Logic

### Drag and Drop
*   The existing slot system can be extended to detect if a drag operation ends over the `ShopView` area (initiating a Sell) or the `PlayerInventoryView` area (initiating a Buy).

### Click Interactions (Right-Click)
*   Right-clicking an item in the `PlayerInventoryView` while the Shop is open will attempt to sell the item (firing `OnRequestSell`).
*   Right-clicking an item in the `ShopView` will attempt to buy the item (firing `OnRequestBuy`).

### Splitting Stacks
*   For stackable items, dragging or specific click combinations (e.g., Shift+Click) will open a separate "Quantity Selection" modal dialogue.
*   This modal will dispatch the `OnRequestBuy`/`OnRequestSell` event with the chosen quantity once confirmed.

## 5. Flow Example: Interacting with the Smith's Shop

1. Player interacts with the Smith NPC (`WorldContainer` or specialized `SmithInteractable` logic).
2. NPC invokes `_uiEvents.OnRequestOpen?.Invoke(ScreenType.Smith, smithDataPayload)`.
    *   *(The `smithDataPayload` will contain references to the `ShopContainerSO` as well as data needed for upgrading/repairing).*
3. `UIManager` receives the event.
4. `UIManager` finds the registered `SmithView` (in `ScreenZone.Left`) and calls its `Setup(payload)` and `Show()` methods.
5. The `SmithView` initializes the `ShopSubView` with the shop data.
6. *Simultaneously*, opening `ScreenType.Smith` triggers the `PlayerInventoryView` to open (in `ScreenZone.Right`).
7. The player clicks the "Shop" tab in the Smith UI. The `SmithView` hides the default crafting screen and calls `ShopSubView.Show()`.
8. The player can now drag items between `ScreenZone.Right` (Player) and `ScreenZone.Left` (Smith's Shop Sub-View), or right-click to instantly buy/sell.
9. Clicking the "Repair" tab hides the `ShopSubView` and shows the repairing interface, but the Player Inventory remains open on the right.
