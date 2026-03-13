# Inventory & Event System Documentation

This document provides a detailed overview of every single element within the inventory system in this project. It outlines how items are created, stored, managed in the shop, their underlying behavior logic, and their integration with the UI framework.

---

## 1. Item Architecture: Creation & Representation

Items in the game are strictly separated into two concepts: **Base Definitions (Blueprints)** and **Runtime Instances**.

### `InventoryItemSO` (Base Definition)
*   **What it is:** A `ScriptableObject` that acts as the read-only blueprint for an item type.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/ScritableObjects/InventoryItemSO.cs`
*   **How it's created:** Created in the Unity Editor via the `Create -> UI -> Inventory -> Item` asset menu.
*   **Properties:**
    *   `ItemName`: Display name of the item.
    *   `Description`: Text area for flavor text or stats.
    *   `Icon`: The 2D Sprite used in the UI.
    *   `MaxStackSize`: The maximum number of this item that can fit in a single slot (defaults to 99).
    *   `GoldValue`: Base economic value used for buying/selling.

### `ItemInstance` (Runtime State)
*   **What it is:** A standard serializable C# class that wraps an `InventoryItemSO` and holds data that can change during gameplay.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/Template controlls/ItemInstance.cs`
*   **Why it exists:** Since `InventoryItemSO` is a global asset, changing a stat on it would change it for all instances in the game. `ItemInstance` allows two identical swords to have different durability or levels.
*   **Properties:**
    *   `BaseItem`: A reference to the underlying `InventoryItemSO` blueprint.
    *   `CurrentLevel`: Represents upgrading mechanics.
    *   `Durability`: Health of the item before breaking.
*   **Core Logic:**
    *   `IsStackableWith(ItemInstance other)`: Critical function that determines if two items can merge into one stack. They must share the exact same `BaseItem` reference AND have the exact same `CurrentLevel`.

### `InventorySlot` (The Container Slot)
*   **What it is:** A standard serializable C# class representing a single physical square in an inventory grid.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/Template controlls/InventorySlot.cs`
*   **Properties:**
    *   `HeldItem`: Reference to the `ItemInstance` currently occupying the slot.
    *   `Count`: The quantity of the item in the stack.
    *   `IsEmpty`: A helper property returning true if `HeldItem` or its `BaseItem` is null.
*   **Core Logic:**
    *   Contains methods like `SetItem`, `Clear`, `IncreaseCount`, and `DecreaseCount` to safely manipulate the stack size. If `Count` drops to 0 or below, it automatically calls `Clear()` to reset the slot.

---

## 2. Inventory Storage & Management

The player's inventory (and NPC inventories) are managed by dedicated `ScriptableObject` containers, avoiding complex MonoBehaviours on the player object.

### `InventoryContainerSO`
*   **What it is:** A `ScriptableObject` that holds a list of `InventorySlot`s.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/ScritableObjects/InventoryContainerSO.cs`
*   **How it's created:** Created in the Editor via `Create -> UI -> Inventory -> Container`.
*   **Properties:**
    *   `SlotCount`: Total number of slots available (e.g., 20).
    *   `Slots`: A `List<InventorySlot>` representing the actual inventory grid.
    *   `AssociatedView`: An enum (`ScreenType`) linking this container to a specific UI window type.
*   **Core Logic:**
    *   **Initialization (`OnEnable` & `OnValidate`)**: Ensures the `Slots` list is populated up to `SlotCount` with empty `InventorySlot` objects. It also prevents editor crashes by enforcing sensible defaults (like level 1 and 100 durability) if an item is manually placed in a slot via the inspector but left with 0 values.
    *   **Adding Items (`AddItem`)**:
        1.  *First Pass:* Iterates through slots to find existing stacks of the *same* item (`IsStackableWith`). Fills those stacks up to the `MaxStackSize` defined in the blueprint.
        2.  *Second Pass:* If there is still quantity leftover, it looks for the first `IsEmpty` slot and places the remainder there.
        3.  Returns `true` if all items fit, `false` if the inventory is full. Crucially, it invokes `_uiInventoryEvents.OnInventoryUpdated` when successful.
    *   **Removing Items (`RemoveItem`)**:
        1.  *First Pass:* Calculates the total available quantity across all stacks of that item to ensure the player actually has enough to remove.
        2.  *Second Pass:* Iterates through and decreases stack counts, clearing slots if they hit 0, until the requested quantity is removed. Invokes `OnInventoryUpdated`.

---

## 3. UI Integration & Visuals

The system strictly adheres to the principle of **Data Decoupling**. UI classes NEVER directly modify the counts or stats inside `InventoryContainerSO`. They only listen to events and dispatch requests.

### `InventorySlotView`
*   **What it is:** A wrapper class that binds an `InventorySlot` data object to the actual UI Toolkit `VisualElement`s (the icon and quantity label).
*   **Location:** `Toris/Assets/Scripts/UIToolkit/Template controlls/InventorySlotView.cs`
*   **Core Logic:** The `Update(InventorySlot slotData)` method reads the data. If empty, it hides the icon. If populated, it sets the Sprite and updates the text label if the count > 1.

### `PlayerInventoryView`
*   **What it is:** The visual representation of the player's `InventoryContainerSO`. Inherits from `GameView`.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/UI/UIViews/PlayerInventoryView.cs`
*   **Core Logic:**
    *   Listens to `UIInventoryEventsSO.OnInventoryUpdated` in `Show()` and unbinds in `Hide()`.
    *   `RefreshGrid()`: When triggered, it clears the entire grid, iterates through every slot in the `InventoryContainerSO`, instantiates a new UI `TemplateContainer` for each, and creates an `InventorySlotView` to bind the data.
    *   **Interaction:** Registers a Left-Click (`MouseUpEvent` button 0) on the visual slot. When clicked, it does NOT do anything directly; it simply fires `_uiInventoryEvents.OnItemClicked` with the slot data as a payload.

---

## 4. The Shop System & Economy

The shop acts as a bridge between the player's inventory, the player's wallet (`PlayerDataSO.Gold`), and an NPC's inventory container.

### `ShopSubView`
*   **What it is:** A UI component that displays a dynamic inventory (like a Smith's stock). Inherits from `UIView`.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/UI/UIViews/ShopSubView.cs`
*   **Core Logic:**
    *   Receives an `InventoryContainerSO` (the shop's specific stock) as a payload during `Setup()`.
    *   Listens to `OnCurrencyChanged` to update the player's gold display.
    *   Listens to `OnShopInventoryUpdated` to redraw the shop's grid if an item is bought/sold.
    *   **Interaction:** Registers a Right-Click (`MouseUpEvent` button 1) on the visual slot. Holding Shift requests a quantity of 10, otherwise 1. It then fires `_uiInventoryEvents.OnRequestBuy(item, quantity)`. Note that it only *requests* the buy; it does not perform it.

### `ShopManagerSO`
*   **What it is:** The authoritative backend controller that handles the logic of transactions. It is a `ScriptableObject`.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/ScritableObjects/ShopManagerSO.cs`
*   **Core Logic:**
    *   Listens to global `UIEvents.OnRequestOpen` to intercept payloads. If the requested screen is a vendor (e.g., `ScreenType.Smith`), it captures the `InventoryContainerSO` passed in the payload and sets it as `CurrentShopInventory`. This allows a single Shop Manager to handle multiple different NPCs dynamically.
    *   Listens to `OnRequestBuy` and `OnRequestSell` events.
    *   **Buy Logic (`HandleRequestBuy`)**:
        1.  Calculates total cost (`Item.GoldValue * quantity`).
        2.  Verifies the player has enough Gold.
        3.  Attempts to remove the item from the `CurrentShopInventory` (the shop's stock).
        4.  If successful, attempts to add the item to the Player's inventory.
        5.  If the player's inventory is full, it *refunds* the item back to the shop's stock.
        6.  If added successfully, deducts gold from the player and fires `OnCurrencyChanged` and `OnShopInventoryUpdated` events to force the UI to redraw.
    *   **Sell Logic (`HandleRequestSell`)**: Follows a similar verifiable process in reverse. Removes from player, adds to NPC stock (so they can resell it), adds gold, and fires events.

---

## 5. Event Architecture (`UIEventsSO` & `UIInventoryEventsSO`)

The entire system relies on ScriptableObject-based event channels to prevent tight coupling.

*   **`UIEventsSO`:** Handles generic window management (`OnRequestOpen`, `OnRequestClose`). It is used to broadcast that an interaction has occurred in the world (e.g., clicking the Smith NPC), passing the relevant data payload (like the Smith's specific `ShopContainerSO`) along with the request to open the `ScreenType.Smith`.
*   **`UIInventoryEventsSO`:** Handles all inventory-specific actions:
    *   `OnInventoryUpdated`: Fired by `InventoryContainerSO` when its data changes.
    *   `OnShopInventoryUpdated`: Fired by `ShopManagerSO` when vendor stock changes.
    *   `OnRequestBuy` / `OnRequestSell`: Dispatched by the UI Views when a user clicks, caught and processed by `ShopManagerSO`.
    *   `OnCurrencyChanged`: Fired when gold values update.
    *   `OnItemClicked`, `OnRequestSalvage`, `OnRequestForge`: Used by other crafting systems (not covered in detail here) following the identical request-listen-validate pattern.

---

## 6. Recommendations

*   **Null Checks on Event Payloads:** When views dispatch events like `OnRequestBuy` or `OnItemClicked`, ensure they are passing robust references. Consider adding validation to verify `slotData.HeldItem` is not null before dispatching to prevent unexpected behaviors in listeners like `ShopManagerSO` or `CraftingManagerSO`.
*   **Event Unbinding Optimization:** Ensure `PlayerInventoryView`, `ShopSubView`, and other UI event listeners use `private bool _eventsBound` to prevent double subscriptions during lifecycle changes, particularly if views are repeatedly shown and hidden rather than destroyed.
*   **Cache Base Items During Iteration:** If multiple removals are processed simultaneously by managers (e.g., crafting multiple items at once), cache the `InventoryItemSO` base references first to prevent `NullReferenceException` if the underlying stack is completely cleared during iteration.
