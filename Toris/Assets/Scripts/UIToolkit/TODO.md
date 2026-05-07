# Project Roadmap: UI & Save Integration

This document outlines our high-level goals and progress for connecting the Main Menu to the core gameplay experience.

---

## 🤖 Session Hand-off Message
**Status:** The Main Menu now successfully displays dynamic player statistics by "peeking" at save files. The core plumbing for both loading existing saves and starting new games is fully operational.

**Next Immediate Task:** Implement **Save Deletion**.
- **Goal:** Add a "Delete" button to the Save Slot cards in the Main Menu.
- **Logic:** Create a `DeleteSave(SaveSlotIndex slot)` method in `SaveManager.cs` that removes the physical file.
- **UI update:** Refresh the `SaveSlotView` after deletion to show it as an "Empty Slot".

---

## Main Objective: Seamless Game Loading
Ensure that selecting a save slot from the Main Menu instantly restores the player's progress and transports them exactly where they left off in the game world.

## Core Milestones

- [X] **System Wiring** (COMPLETED)
- [X] **Data Restoration** (COMPLETED)
- [X] **World Transition** (COMPLETED)
- [X] **Fresh Starts** (COMPLETED)
- [X] **Dynamic Menu Information** (COMPLETED)

---

## Backlog & Future Polish

- [ ] **Save Deletion**
  Allow players to clear save slots directly from the menu. This requires file system operations in `SaveManager` and a UI update in `MainMenuController`.

