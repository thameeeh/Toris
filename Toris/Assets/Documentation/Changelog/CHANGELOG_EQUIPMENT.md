# Changelog: Equipment Interaction Improvements

## Overview
This update implements click-to-equip and click-to-unequip functionality for the player's inventory, improving the usability of equipment management. Previously, clicking an item in the inventory would emit a UI event but would not physically move the item to its target equipment slot. The `PlayerEquipmentController` has now been updated to respond to these interaction events.

## Changes

### 1. GameSessionSO Dependency Added
* Added a serialized reference to `GameSessionSO` (`_globalSession`) inside `PlayerEquipmentController`.
* **Reason:** This allows the controller to access the main player inventory (`_globalSession.PlayerInventory`) to verify if the clicked item belongs to the player's general storage.

### 2. UI Event Listeners Implemented
* Subscribed `PlayerEquipmentController` to the `_uiInventoryEvents.OnItemClicked` event.
* The system now listens for items that are clicked by the player anywhere in the UI that uses `InventorySlotView` components emitting this event.

### 3. Click-to-Equip Logic
* When an item in the main inventory is clicked, the system checks if its underlying `BaseItem` contains an `EquipableComponent`.
* If true, it extracts the `TargetSlot` enum from the component and attempts to move the item to the corresponding equipment slot (e.g., Head, Chest, Weapon).

### 4. Slot Swapping Mechanism
* When an equipment slot is already occupied, the logic now supports item swapping.
* Clicking on a new weapon in the inventory while a weapon is already equipped will seamlessly swap the two items, placing the old weapon back into the inventory slot that the new weapon just vacated.

### 5. Click-to-Unequip Logic
* When an item located inside an equipment slot (e.g., the currently equipped Weapon) is clicked, the system intercepts this and treats it as an unequip request.
* The system calls `_globalSession.PlayerInventory.AddItem(...)` to move the item back to the general inventory. If there is enough space, the equipment slot is cleared.
