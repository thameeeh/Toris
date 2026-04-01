# Equipment System Architecture Documentation

This document describes the flow and architecture of the Equipment System in Outland Haven, detailing how the player's inventory directly interfaces with their gameplay statistics.

## Overview

The equipment system strictly integrates the player's inventory with their runtime stats. It uses a series of decoupled controllers and bridges to pass data from a standard `InventoryManager` all the way to the `PlayerResolvedEffects` struct, which dictates actual gameplay numbers (like strength and defense).

## 1. Data Layer: The Equipment Container

*   **Component:** `InventoryManager` (MonoBehaviour)
*   **Role:** The core data structure for the player's worn equipment. Instead of creating a custom "Equipment" class, the project repurposes the standard `InventoryManager`.
*   **Configuration:** It is configured with a specific number of slots (e.g., 5 slots) to represent the available equipment positions.

## 2. Logic Layer: Mapping Inventory to Equipment

*   **Component:** `PlayerEquipmentController`
*   **Role:** This acts as the logic layer that gives context to the generic `InventoryManager` slots.
*   **Mapping:** It reads the `InventoryManager` container and hardcodes its inventory indices to specific `EquipmentSlot` enums:
    *   `0 = Head`
    *   `1 = Chest`
    *   `2 = Legs`
    *   `3 = Arms`
    *   `4 = Weapon`
*   **Event Handling:** It listens to standard inventory events (like `UIInventoryEventsSO.OnInventoryUpdated`) occurring on the equipment container.
*   **Dispatching:** When a change is detected, it evaluates the change and fires specialized equipment events:
    *   `OnEquippedItemChanged`
    *   `OnItemEquipped`
    *   `OnItemUnequipped`

## 3. Integration Layer: Pushing Stats to the Player

*   **Component:** `EquipmentEffectBridge`
*   **Role:** The crucial bridge between an item's data and the player's stat system.
*   **Operation:** It listens for the `OnEquippedItemChanged` events fired by the `PlayerEquipmentController`.
*   **Stat Extraction:** When an item is equipped, it reads the item's `EquipableComponent` (from its `InventoryItemSO` blueprint) to extract stat bonuses (e.g., `StrengthBonus`, `DefenceBonus`).
*   **Injection:** It injects these stat bonuses into the player's active effect system via `EquippedItemEffectSource` and mapping them to `PlayerEffectType`.

## 4. Resolution Layer: Applying Stats

*   **Component:** `PlayerEffectResolver`
*   **Role:** The final destination for equipment stats.
*   **Operation:** It takes the injected numeric modifiers (from the `EquipmentEffectBridge` and any other active sources like potions or buffs) and applies them mathematically to update the player's core `PlayerResolvedEffects` struct. This struct is what the combat system reads to calculate damage and mitigation.

## 5. UI Layer: Displaying Equipment

*   **Component:** `PlayerEquipmentView`
*   **Role:** Handles the visual rendering of the equipment slots on the UI.
*   **Implementation:** It dynamically creates slot instances via standard `InventorySlotView` components.
*   **Mapping:** It maps these standard slot views to the specific equipment slots embedded in the `PlayerEquipment__Panel` of the `PlayerInventory.uxml` file:
    *   `#slot-head`
    *   `#slot-chest`
    *   `#slot-legs`
    *   `#slot-arms`
    *   `#slot-weapon`
*   **Interaction:** Click-to-equip and unequip functionality is managed by the `PlayerEquipmentController` subscribing to `UIInventoryEventsSO.OnItemClicked`. It handles the logic of swapping valid `EquipableComponent` items between the main `GameSessionSO.PlayerInventory` and the equipment `InventoryManager`.

## Summary Diagram

```text
[InventoryManager] (Data Container, e.g., 5 slots)
       |
       | (Reads data & indices)
       v
[PlayerEquipmentController] (Maps indices to Head, Chest, Weapon, etc.)
       |
       | (Fires OnEquippedItemChanged)
       v
[EquipmentEffectBridge] (Extracts EquipableComponent stats from item)
       |
       | (Injects PlayerEffectType modifiers)
       v
[PlayerEffectResolver] (Calculates final numbers)
       |
       | (Updates struct)
       v
[PlayerResolvedEffects] (Used by combat scripts)
```
