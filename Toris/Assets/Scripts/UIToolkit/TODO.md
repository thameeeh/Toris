# Project Roadmap: UI & Save Integration

This document outlines our high-level goals and progress for connecting the Main Menu to the core gameplay experience.

---

## 🤖 Session Hand-off Message
**Status:** Potion HUD and Hotkey integration task is complete, but experiencing functionality issues. The Save/Load system and Pause menus are stable.

**Key Achievements Today:**
- **Potion HUD Integration:** Added a central potion quickbar to the HUD with two slots. The HUD dynamically tracks the Potion Inventory, allowing for visual feedback and immediate right-click usage.
- **Standardization:** Universal right-click consumption confirmed across all item containers.

**Next Steps:**
- Implement hotkey-driven potion consumption ('1' and '2') to trigger item usage directly from the Potion quickbar.
- The UI architecture is now highly modular and ready for further screen implementations using the established MVP patterns.

---

---

## 🚩 Known Issues & Architectural Debt
- [X] **Registration Race Condition** (COMPLETED) - Resolved via `UIBootstraper` scene handshake.
- **Passive UI Updates:** UI relies on manual pokes from controllers rather than the Model (`ItemInstance`) broadcasting its own state changes. Durability or background timer updates will be difficult to synchronize.

---

## 🛠 Plan: Fix PlayerEquipmentController Scene Persistence

### Problem Description
The `PlayerEquipmentController` currently relies on a statically serialized `[SerializeField]` reference to the `InventoryManager` (equipment container). During scene transitions, the `GameObject` containing this manager is destroyed. The controller instance, if it persists or is re-instantiated, retains a "stale" (null/destroyed) reference. This causes the controller to fail silently during `RefreshEquipmentState()`, effectively halting all stat propagation and event broadcasting for equipment changes after the first scene load.

### Implementation Strategy
To align with the project's existing architectural patterns (as seen in `InventoryActionController`), we will implement a dynamic resolution mechanism:

1.  **Introduce Resolution Helper:** Add a `ResolveRuntimeReferences()` method to `PlayerEquipmentController`.
2.  **Bind via Resolver:** Inside `Awake()`, call `PlayerInventorySceneResolver.ResolveEquipmentInventory()` to re-bind the `_equipmentInventory` reference to the active instance in the new scene.
3.  **Lifecycle Safety:** Update the component to ensure this resolution happens before `Start()` and `RefreshEquipmentState()` execution to prevent null reference early-exits.

---

## Main Objective: Item System Scalability & UX Polish
Ensure the inventory and item systems are robust, easily extensible for new content, and provide excellent player feedback.

## Main Objective: Item System Scalability & UX Polish
Ensure the inventory and item systems are robust, easily extensible for new content, and provide excellent player feedback.

## Core Milestones (Next Phase)

- [X] Implement hotkey-driven potion consumption ('1' and '2') via the Potion HUD quickbar.

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
- [X] **Scene Transition Reliability** (COMPLETED) - Fixed equipment data loss and implemented robust container identification (`IsEquipment` flag).