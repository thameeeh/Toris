# General Project Changelog

**Rules:**
* Archive previous changes and add new ones at the top to continue the log.
* Shortly describe what was done.
* Enumerate or mark different changes; if changes are too big, divide them into smaller ones.

---

## [Current/Recent] - UI Toolkit Drag-and-Drop System
This update introduces a robust drag-and-drop mechanism for the inventory using Unity's UI Toolkit, complete with a drag threshold, a dedicated global overlay for dragging, and cross-container logic.

### 1. Updated Event Architecture
* Added `OnRequestMoveItem` to `UIInventoryEventsSO` to pass cross-container item transfer requests (source/target managers and slots).

### 2. Transitioned to Pointer Events
* Updated `InventorySlotView` to listen to `PointerDownEvent`, `PointerMoveEvent`, and `PointerUpEvent` instead of basic clicks.
* Implemented a 10px drag threshold. If the pointer moves less than this, it correctly falls back to firing the legacy `OnItemClicked` event.
* Added `SlotDropData` to `VisualElement.userData` to uniquely identify drop targets during raycast picking (`panel.Pick`).

### 3. Added Dedicated `UIDragManager`
* Created a clean, singleton `UIDragManager` component to isolate pointer tracking and visual drag state from `UIManager`.
* Programmatically injects a root `#Drag_Layer` and a `#Ghost_Icon` at runtime.
* Ensures the ghost icon has `picking-mode: ignore` so it does not block the drop target raycast.

### 4. Added Centralized `InventoryTransferManagerSO`
* Created a centralized, event-driven manager to handle logic between two distinct `InventoryManager` instances.
* Added logic to evaluate target slots for available space (partial stack merging), empty slots (direct moves), and mismatched items (item swaps).
* Fires `OnInventoryUpdated` on success.

### 5. Updated Views for Dependency Injection
* `PlayerInventoryView` and `PlayerEquipmentView` now pass the required `InventoryManager` and `UIInventoryEventsSO` dependencies directly into the `InventorySlotView` constructor to enable self-contained logic mapping.

---

## [Previous] - Equipment Click Interactions
This update implements click-to-equip and click-to-unequip functionality for the player's inventory, improving the usability of equipment management.

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
