# Project Roadmap: UI & Save Integration

This document outlines our high-level goals and progress for connecting the Main Menu to the core gameplay experience.

---

## 🤖 Session Hand-off Message
**Status:** The UI polishing phase and Pause Menu implementation are complete. The Save/Load system is now fully integrated with a robust, event-driven architecture and user-friendly confirmation workflows.

**Key Achievements Today:**
- **In-Game Pause Menu:** Fully functional with time-scaling, input map switching (Player vs. UI), and auto-saving progress when returning to the Main Menu.
- **Generic Confirmation Modal:** Implemented a reusable modal system for destructive actions (Delete Save, Exit Game) with a centered, absolute-positioned UI.
- **Main Menu Polish:** 
    - Redesigned Save Slots to be larger and more readable.
    - Repositioned the Delete button to a dedicated footer.
    - Added subtle, professional colored hover effects to all menu buttons.
    - Implemented Escape key and a Close button for the Save Slots panel.
- **Architecture:** Migrated QuickSave/Load to a decoupled event-bus system using `UIEventsSO`.

**Next Steps:**
- Monitor the **Known Issues** below regarding the inventory drag-and-drop bug when you return to inventory systems.
- The UI architecture is now highly modular and ready for further screen implementations using the established MVP patterns.

---

## 🚩 Known Issues (Pending Later Phase)
- **Inventory Drag-and-Drop Bug:** Dynamically instantiated slots sometimes lose pointer interaction or fail to register drops due to `TemplateContainer` wrapper properties. (Requires targeted fix in `InventorySlotView.cs` and layout analysis).

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
