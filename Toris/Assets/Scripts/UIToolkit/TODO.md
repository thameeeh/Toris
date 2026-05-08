# Project Roadmap: UI & Save Integration

This document outlines our high-level goals and progress for connecting the Main Menu to the core gameplay experience.

---

## 🤖 Session Hand-off Message
**Status:** Potion HUD and Hotkey integration task is fully complete. The Save/Load system and Pause menus are stable.

**Key Achievements Today:**
- **Potion HUD Integration:** Added a central potion quickbar to the HUD with two slots. The HUD dynamically tracks the Potion Inventory, allowing for visual feedback and immediate right-click usage.
- **Hotkey Usage:** Mapped keyboard inputs ('1' and '2') to trigger item consumption directly from the Potion quickbar slots, properly handling charges and item depletion.
- **Standardization:** Universal right-click consumption confirmed across all item containers.

**Action Required (Unity Inspector Final Setup):**
1. **HudScreenController:** Assign `_slotTemplate` and `_potionInventory`.
2. **InventoryActionController:** Assign `_potionInventory` and `_inputReader`.

**Next Steps:**
- Monitor the **Known Issues** below regarding the inventory drag-and-drop bug when you return to inventory systems.
- The UI architecture is now highly modular and ready for further screen implementations using the established MVP patterns.

---

## 🚩 Known Issues & Architectural Debt
- **Inventory Drag-and-Drop Bug:** Dynamically instantiated slots sometimes lose pointer interaction or fail to register drops due to `TemplateContainer` wrapper properties. (Requires targeted fix in `InventorySlotView.cs` and layout analysis).
- **Logic Fragmentation (Consumables):** Consumption logic is split between `PlayerConsumableController` and `ConsumableManagerSO`. Future logic (sounds, cast times) must be duplicated or consolidated into an `IUsable` component pattern.
- **O(N) Component Lookups:** Frequent calls to `GetComponent<T>()` on `ItemInstance` use linear searches through `[SerializeReference]` lists. This may cause performance micro-stuttering in late-game scenarios with high item counts.
- **Closed-for-Extension Controllers:** `InventoryActionController` uses hardcoded type checks for `ConsumableComponent` and `EquipableComponent`. This should be refactored to a Command or Interface-based approach (`IInteractable`) to support new item behaviors without modifying the controller.
- **Passive UI Updates:** UI relies on manual pokes from controllers rather than the Model (`ItemInstance`) broadcasting its own state changes. Durability or background timer updates will be difficult to synchronize.

---

## Main Objective: Seamless Game Loading (STABLE)
Ensure that selecting a save slot from the Main Menu instantly restores the player's progress and transports them exactly where they left off in the game world.

## Core Milestones

- [X] **System Wiring** (COMPLETED)
- [X] **Data Restoration** (COMPLETED)
- [X] **World Transition** (COMPLETED)
- [X] **Fresh Starts** (COMPLETED)
- [X] **Dynamic Menu Information** (COMPLETED)
- [X] **Automatic Restoration (No F9 required)** (COMPLETED)
- [X] **Polymorphic State Serialization** (COMPLETED)
- [X] **Save Deletion** (COMPLETED)
- [X] **Confirmation Modal (Save Deletion & Exit)** (COMPLETED)
- [X] **Pause Menu Implementation** (COMPLETED)
- [X] **UI Polish & Consistency** (COMPLETED)
- [X] **Equippable Stacking Refactor** (COMPLETED) - Enforced MaxStackSize=1 for equippables and implemented swap-on-drag behavior.
- [X] **Hotkey-Driven Potion Consumption** (COMPLETED) - Added potion HUD quickbar, hotkey inputs ('1' and '2'), and unified consumable usage.
