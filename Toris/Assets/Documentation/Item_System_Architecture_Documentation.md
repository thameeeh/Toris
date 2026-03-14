# Item System Architecture

This document describes the component-based architecture of the `InventoryItemSO` and `ItemInstance` system in OutlandHaven, detailing how static blueprints translate into dynamic runtime states.

## 1. The Core Philosophy
The item system is designed to adhere to the Open-Closed Principle. To add entirely new mechanics (e.g., weapon enchantments, poison coatings, charges), you do not need to modify the base `InventoryItemSO` or `ItemInstance` scripts.

Instead, functionality is decoupled into a paired structure:
1. **The Blueprint (`ItemComponent`)**: Attached to the `InventoryItemSO` in the Editor. Defines constants and limits.
2. **The Runtime Data (`ItemComponentState`)**: Generated dynamically and stored in `ItemInstance`. Tracks mutable numbers (e.g., current durability, current level).

---

## 2. The Blueprint Layer (`InventoryItemSO` & `ItemComponent`)

### `InventoryItemSO`
The `InventoryItemSO` acts as the flyweight blueprint. It defines global static properties shared by all instances of an item:
*   `ItemName`, `Description`, `Icon`, `MaxStackSize`, `GoldValue`.
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

---

## 3. The Runtime Layer (`ItemInstance` & `ItemComponentState`)

### `ItemInstance`
A blind, generic container class that wraps the `InventoryItemSO`.
*   **Initialization**: In its constructor, it iterates over all `Components` in its `BaseItem` blueprint. It calls `CreateInitialState()` and populates its `States` list.
*   **State Access**: Systems interact with specific properties via `GetState<T>()`. For example, a blacksmith script will call `item.GetState<UpgradeableState>()` to check its current level.
*   **Identification**: Contains an `InstanceID` (GUID) used by save systems to uniquely track this specific sword or apple.

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

---

## 4. Dynamic Stackability
The `ItemInstance.IsStackableWith(ItemInstance other)` method dynamically evaluates stackability without hardcoded rules:
1.  **Blueprint Parity:** They must share the exact same `BaseItem` reference.
2.  **State Count Parity:** If one item has an extra state (e.g., one was enchanted, the other wasn't), they cannot stack.
3.  **State-by-State Evaluation:** It loops through every active state and asks it, "Are you stackable with the corresponding state on the other item?" If *any* state says no (e.g., Durability doesn't match, Level doesn't match), the items will occupy separate inventory slots.
