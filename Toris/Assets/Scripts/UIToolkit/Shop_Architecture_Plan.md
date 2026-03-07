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

Following the standard UI architecture:

### `ShopScreenController` (MonoBehaviour)
*   **Dependencies**: Requires the Shop UXML `VisualTreeAsset`, the Slot `VisualTreeAsset`, `GameSessionSO` (for currency), `UIEventsSO`, and `UIInventoryEventsSO`.
*   **Responsibilities**: Instantiates the UXML in `OnEnable()`, creates the `ShopView`, and registers it with the `UIManager` to `ScreenZone.Left` (opposite the Player's inventory in `ScreenZone.Right`).

### `ShopView` (Inherits `GameView`)
*   **ScreenType**: Overrides `ID` to return `ScreenType.Shop`.
*   **Setup**: The `Setup(object payload)` method will cast the payload to `ShopContainerSO` (or `InventoryContainerSO`) to populate the shop's grid of items.
*   **Responsibilities**:
    *   Binds the shop data to visual slots.
    *   Displays the player's current currency.
    *   Listens to `OnCurrencyChanged` to update the currency label.

## 4. Interaction Logic

### Drag and Drop
*   The existing slot system can be extended to detect if a drag operation ends over the `ShopView` area (initiating a Sell) or the `PlayerInventoryView` area (initiating a Buy).

### Click Interactions (Right-Click)
*   Right-clicking an item in the `PlayerInventoryView` while the Shop is open will attempt to sell the item (firing `OnRequestSell`).
*   Right-clicking an item in the `ShopView` will attempt to buy the item (firing `OnRequestBuy`).

### Splitting Stacks
*   For stackable items, dragging or specific click combinations (e.g., Shift+Click) will open a separate "Quantity Selection" modal dialogue.
*   This modal will dispatch the `OnRequestBuy`/`OnRequestSell` event with the chosen quantity once confirmed.

## 5. Flow Example: Opening the Shop

1. Player interacts with an NPC (`WorldContainer` logic).
2. NPC invokes `_uiEvents.OnRequestOpen?.Invoke(ScreenType.Shop, thisNpcsShopData)`.
3. `UIManager` receives the event.
4. `UIManager` finds the registered `ShopView` (in `ScreenZone.Left`) and calls its `Setup(payload)` and `Show()` methods.
5. *Simultaneously*, an internal listener or the `UIManager` ensures `_uiEvents.OnRequestOpen?.Invoke(ScreenType.Inventory, null)` is called to display the player's backpack.
6. The player can now drag items between `ScreenZone.Right` (Player) and `ScreenZone.Left` (Shop).
