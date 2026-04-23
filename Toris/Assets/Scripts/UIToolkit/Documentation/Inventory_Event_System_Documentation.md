# Event System on Item Management and Inventory Control

This document explains the architecture and event-driven data flow of the item management and inventory control system within the game. It outlines how players interact with items in the world, how data is persisted, and how the UI reacts to these changes via decoupled events.

## Core Concepts & Philosophy

The system relies on a **Data-Driven Event Architecture** using ScriptableObjects. The fundamental principle is:
> **UI and World Interactors do not update each other directly. They interact with Data (ScriptableObjects), and Data emits Events when changed.**

This prevents tight coupling between the `WorldItem` objects on the ground, the `Player` collecting them, and the `PlayerInventoryView` displaying them.

## 1. Item Interaction Flow (Picking up an Item)

The process of picking up a world item is handled through a sequence of interactions decoupled by an interface and an event channel.

### A. Scanning & UI Prompt (`ItemPicker.cs`)
- Attached to the Player.
- **Scanning**: Uses an `OverlapCircleAll` physics cast to constantly scan for objects implementing `IContainerInteractable` (like `WorldItem`) near an interaction point.
- **UI Updates**: If an interactable is found, it calls `GetInteractionPrompt()` and updates an `InteractionPromptUI` world-space canvas with the prompt ("E") and position.

### B. Triggering the Pickup (`ItemPickEventSO.cs`)
- Instead of the input system directly telling `ItemPicker` to interact, input triggers a global `ItemPickEventSO` (a ScriptableObject containing an `Action OnItemPick`).
- `ItemPicker` listens to this event (`_itemPickerSO.OnItemPick += PickItem;`).
- When fired, if `ItemPicker` has a valid `_currentSelection`, it calls `_currentSelection.Interact(_myInventorySO)`, passing the Player's main inventory data container.

### C. Adding to Data (`WorldItem.cs`)
- `WorldItem` implements `IContainerInteractable`.
- When `Interact(InventoryManager)` is called, it attempts to insert its payload (`InventoryItemSO` and `_quantity`) into the passed container using `targetContainer.AddItem(...)`.
- If successful, the `WorldItem` destroys its GameObject.

## 2. Inventory Data Management

### `InventoryItemSO`
- Defines the static data of an item type (Name, Description, Icon, MaxStackSize, GoldValue).

### `InventoryManager`
- The core data model holding the state of an inventory (e.g., Player's Backpack, a Chest, a Shop's stock).
- Contains a list of `InventorySlot` classes (which hold an `InventoryItemSO` reference and a count).
- Contains the core logic for insertion (`AddItem`) and removal (`RemoveItem`).
  - **Stacking Logic**: `AddItem` first checks if the item exists in an incomplete stack before filling an empty slot.
- **The Crucial Event Dispatch**: Whenever `AddItem` or `RemoveItem` successfully mutates the state of the slots, the container invokes `_uiInventoryEvents.OnSpecificSlotsUpdated.Invoke(sourceSlot, targetSlot)`.

## 3. UI Reactivity (The Event System)

The UI needs to know when to redraw itself, but it should not constantly poll the `InventoryManager`. To ensure high performance and scalability, the UI employs targeted updates rather than full redraws.

### `UIInventoryEventsSO`
This ScriptableObject acts as the central event bus specifically for inventory and economy-related UI updates.

**Key Events:**
*   `OnSpecificSlotsUpdated(InventorySlot sourceSlot, InventorySlot targetSlot)`: Fired whenever specific items are moved, added, or removed. The `PlayerInventoryView` listens to this event to trigger a visual rebuild only for the exact slots that were modified, avoiding massive Managed Heap allocations and GC spikes.
*   `OnRequestMoveItem(InventorySlot source, InventorySlot target, int amountToMove)`: Dispatched when a UI action (like drag-and-drop or shift-click) attempts to transfer an item, including the explicit quantity requested to move.
*   `OnShopInventoryUpdated`: Fired when a vendor's stock changes. Shop UI views listen to this.
*   `OnRequestBuy(InventoryItemSO, int amount)`: Dispatched by UI Views when a player attempts to buy an item, carrying the requested quantity.
*   `OnRequestSell(InventoryItemSO, int amount)`: Dispatched by UI views to sell an item.
*   `(int)`: Fired when player gold increases/decreases. Currency displays listen to this.

## 4. UI View Lifecycle Example (PlayerInventoryView)

1.  **Opening the UI**: A separate system (like `InputManager` or an NPC interaction) fires `UIEventsSO.OnRequestOpen(ScreenType.Inventory, containerData)`.
2.  **Setup & Bind**: The `InventoryScreenController` instantiates the `PlayerInventoryView`. During the view's `Show()` or `Setup()` phase, it subscribes to `UIInventoryEventsSO.OnSpecificSlotsUpdated`. The view utilizes a Dictionary to map backing data slots to visual elements.
3.  **The Event Loop**:
    *   Player presses "E" near a sword (`ItemPickEventSO.OnItemPick` fired).
    *   Sword added to `InventoryManager` at a specific index.
    *   `InventoryManager` fires `UIInventoryEventsSO.OnSpecificSlotsUpdated(modifiedSlot, null)`.
    *   `PlayerInventoryView` hears the event and rebuilds *only* the visual representation mapped to `modifiedSlot`.
4.  **Cleanup**: When the UI is closed (`Hide()`), the view *must* unsubscribe from `OnSpecificSlotsUpdated` to prevent memory leaks and null reference errors when the UI is not active.

## Summary Diagram

```text
[Input System] --fires--> [ItemPickEventSO]
                               |
                               v
                        [ItemPicker.cs] --calls--> WorldItem.Interact(PlayerContainerSO)
                                                         |
                                                         v
                                              [InventoryManager.AddItem()]
                                                         | (If successful, mutates data)
                                                         v
                                              fires [UIInventoryEventsSO.OnSpecificSlotsUpdated(slot, null)]
                                                         |
                                                         v
                                              [PlayerInventoryView] (Hears event, redraws ONLY the modified slot via Dictionary lookup)
```
