# Item System Architecture

This document describes the component-based architecture of the `InventoryItemSO` and `ItemInstance` system in OutlandHaven, detailing how static blueprints translate into dynamic runtime states.

## 1. The Core Philosophy

The item system is designed to adhere to the Open-Closed Principle. To add entirely new mechanics (e.g., weapon enchantments, poison coatings, charges), you do not need to modify the base `InventoryItemSO` or `ItemInstance` scripts.

Instead, functionality is decoupled into a paired structure:
1. **The Blueprint (`ItemComponent`)**: Attached to the `InventoryItemSO` in the Editor. Defines constants and limits.
2. **The Runtime Data (`ItemComponentState`)**: Generated dynamically and stored in `ItemInstance`. Tracks mutable numbers (e.g., current durability, current level).

## 2. The Blueprint Layer

### `InventoryItemSO` (Base Definition)
*   **What it is:** A `ScriptableObject` that acts as the read-only blueprint for an item type. It acts as the flyweight blueprint.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/ScriptableObjects/InventoryItemSO.cs`
*   **How it's created:** Created in the Unity Editor via the `Create -> UI -> Inventory -> Item` asset menu.
*   **Properties:**
    *   `ItemName`: Display name of the item.
    *   `Description`: Text area for flavor text or stats.
    *   `Icon`: The 2D Sprite used in the UI.
    *   `MaxStackSize`: The maximum number of this item that can fit in a single slot (defaults to 99).
    *   `GoldValue`: Base economic value used for buying/selling.
    *   `Components`: A `[SerializeReference]` list of `ItemComponent` objects defining modular behaviors.

### `ItemComponent` (Abstract)
The base class for all item behaviors. By default, it does not generate runtime state (e.g., a simple stat boost component without durability). If it *does* need dynamic tracking, it overrides the `CreateInitialState()` method.

**Example Blueprint Component (`UpgradeableComponent`)**
```csharp
[Serializable]
public class UpgradeableComponent : ItemComponent
{
    public int MaxLevel = 5;

    public override ItemComponentState CreateInitialState()
    {
        return new UpgradeableState(1); // Factory Method generating the starting state
    }
}
```

## 3. The Runtime Layer

### `ItemInstance` (Runtime State)
*   **What it is:** A standard serializable C# class that wraps an `InventoryItemSO` and dynamically holds state data that can change during gameplay.
*   **Location:** `Toris/Assets/Scripts/UIToolkit/Template controlls/ItemInstance.cs`
*   **Why it exists:** Since `InventoryItemSO` is a global asset, changing a stat on it would change it for all instances in the game. `ItemInstance` uses a component-based architecture to hold a dynamic list of `ItemComponentState` objects.
*   **Properties:**
    *   `InstanceID`: A unique string (GUID) identifying the specific item instance, crucial for saving/loading.
    *   `BaseItem`: A reference to the underlying `InventoryItemSO` blueprint.
    *   `States`: A `[SerializeReference]` list of `ItemComponentState` objects containing the actual runtime data.
*   **Core Logic:**
    *   **Initialization**: In its constructor, it iterates over all `Components` in its `BaseItem` blueprint. It calls `CreateInitialState()` and populates its `States` list.
    *   **State Access**: Systems interact with specific properties via `GetState<T>()`. For example, a blacksmith script will call `item.GetState<UpgradeableState>()` to check its current level.
    *   **Identification**: Contains an `InstanceID` (GUID) used by save systems to uniquely track this specific sword or apple.
    *   **Hardcoded Properties**: Hardcoded properties like Level or Durability must not exist in `ItemInstance`. They must be held within `ItemComponentState` derivatives.

### `ItemComponentState` (Abstract)
The base class for all runtime data. It enforces the `IsStackableWith` contract, meaning every individual state defines its own rules for when it can merge with another item.

**Example Runtime State (`UpgradeableState`)**
```csharp
[Serializable]
public class UpgradeableState : ItemComponentState
{
    public int CurrentLevel;

    public UpgradeableState(int startLevel)
    {
        CurrentLevel = startLevel;
    }

    public override bool IsStackableWith(ItemComponentState other)
    {
        if (other is UpgradeableState otherUpgrade)
        {
            // Upgradeable items only stack if their current levels match
            return this.CurrentLevel == otherUpgrade.CurrentLevel;
        }
        return false;
    }
}
```

## 4. Dynamic Stackability

The `ItemInstance.IsStackableWith(ItemInstance other)` method dynamically evaluates stackability without hardcoded rules:
1.  **Blueprint Parity:** They must share the exact same `BaseItem` reference.
2.  **State Count Parity:** If one item has an extra state (e.g., one was enchanted, the other wasn't), they cannot stack.
3.  **State-by-State Evaluation:** It loops through every active state and asks it, "Are you stackable with the corresponding state on the other item?" If *any* state says no (e.g., Durability doesn't match, Level doesn't match), the items will occupy separate inventory slots.
