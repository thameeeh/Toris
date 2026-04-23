# Inventory Management Architecture

This document outlines how inventory storage, container management, and slot logic are handled within Outland Haven. It strictly covers the underlying data structures for storing items and handling inventory logic.

## 1. Inventory Storage Strategy

The player's inventory (and NPC inventories) are managed by dedicated `ScriptableObject` containers (`InventoryManager`), avoiding complex MonoBehaviours on the player object where possible, except when acting as specialized containers (e.g., Equipment).

### `InventoryManager`
*   **What it is:** A component that holds a list of `InventorySlot`s. It acts as the container for items, whether it's the player's main inventory, their equipment slots, or an NPC's shop inventory.
*   **Location:** `Toris/Assets/Scripts/Player/Player/Core/InventoryManager.cs`
*   **How it's used:** Can be attached to the player or created as a ScriptableObject container (`Create -> UI -> Inventory -> Container`).
*   **Properties:**
    *   `SlotCount`: Total number of slots available (e.g., 20).
    *   `Slots`: A `List<InventorySlot>` representing the actual inventory grid.
    *   `AssociatedView`: An enum (`ScreenType`) linking this container to a specific UI window type (e.g., `ScreenType.Inventory`, `ScreenType.Smith`).
*   **Core Logic:**
    *   **Initialization (`OnEnable` & `OnValidate`)**: Ensures the `Slots` list is populated up to `SlotCount` with empty `InventorySlot` objects. It enforces basic constraints, like setting a minimum quantity of 1 if an item is manually placed via the inspector.
    *   **Player Registration**: When a player's `InventoryManager` activates (`OnEnable`), it registers itself to a global state (e.g., `GameSessionSO.PlayerInventory`) if `ContainerBlueprint != null && ContainerBlueprint.AssociatedView == ScreenType.Inventory`.
    *   **Quantity-Based Transactions (The "Bank" Authority):** Moving away from simple binary (all-or-nothing) transfers, the system now enforces precise quantity control.
        *   **Actual Amount Calculation:** Uses `Mathf.Min(amountToMove, maxSpaceInTargetSlot, currentStackInSourceSlot)` to explicitly calculate how many items can legally transfer. This prevents overfilling and logic errors.
        *   **Stack Splitting:** Permits transferring partial amounts from one slot to another, satisfying standard RPG quality-of-life expectations.
        *   **Blocking Partial-Stack Swaps:** When dragging an item onto a different item type, the transaction manager blocks partial swaps to protect the game economy from deletion bugs or duplication glitches, functioning as an authoritative validation layer before moving data.
    *   **Adding Items (`AddItem`)**:
        1.  *First Pass:* Iterates through existing slots to find stacks of the *same* item (`IsStackableWith`). It fills those stacks up to the `MaxStackSize` defined in the item's blueprint.
        2.  *Second Pass:* If quantity remains, it finds the first empty slot (`IsEmpty`) and places the remainder there.
        3.  *Events:* Returns `true` if successful, invoking targeted `UIInventoryEventsSO.OnSpecificSlotsUpdated` events rather than global redraws.
    *   **Removing Items (`RemoveItem`)**:
        1.  *First Pass:* Calculates the total available quantity across all stacks of that item to verify sufficiency.
        2.  *Second Pass:* Iterates through and decreases stack counts, clearing slots if they hit 0, until the requested quantity is removed. Invokes targeted `OnSpecificSlotsUpdated` events.

## 2. Inventory Slot Logic

### `InventorySlot`
*   **What it is:** A standard serializable C# class representing a single physical slot in an inventory grid.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/Template controlls/InventorySlot.cs`
*   **Properties:**
    *   `HeldItem`: Reference to the `ItemInstance` currently occupying the slot.
    *   `Count`: The quantity of the item in the stack.
    *   `IsEmpty`: A helper property returning true if `HeldItem` or its `BaseItem` is null.
*   **Core Logic:**
    *   Contains methods like `SetItem`, `Clear`, `IncreaseCount`, and `DecreaseCount` to safely manipulate the stack size.
    *   If `Count` drops to 0 or below during a decrease, it automatically calls `Clear()` to reset the slot.
    *   **Initialization constraints**: Enforces sensible defaults using parameterless constructors and `OnValidate()` checks wrapped in `#if UNITY_EDITOR` on the container class to prevent null references or 0-count stacks.

## 3. Proxy Slots

When displaying selected inventory items in crafting or input UI slots, the system uses proxy `InventorySlot` instances.
*   **Rationale:** Rather than passing the direct player inventory slot reference to the UI, a dummy proxy `InventorySlot` is instantiated with the exact *required* quantity for the action (e.g., crafting).
*   This prevents the UI from incorrectly displaying the player's full stack count in the input field, maintaining UI integrity.
