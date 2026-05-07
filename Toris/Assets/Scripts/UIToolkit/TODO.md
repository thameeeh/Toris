# Project Roadmap: UI & Save Integration

This document outlines our high-level goals and progress for connecting the Main Menu to the core gameplay experience.

---

## 🤖 Session Hand-off Message
**Status:** The core plumbing is finished. The Main Menu is now successfully wired to the Save System and can trigger scene transitions for both existing saves and new games.

**Next Immediate Task:** Implement **Dynamic Menu Information**. 
- Currently, `MainMenuController.GenerateMockSaveSlots` is still using hardcoded mock data.
- **Goal:** Create a "Peek" method in `SaveManager.cs` that can quickly deserialize only the metadata (Level, Gold, SaveTime) from `save_1.json`, `save_2.json`, etc., without fully importing them into the `GameSessionSO`.
- **UI update:** Pass this real data into the `SaveSlotData` DTOs so the Main Menu cards show the player's actual progress before they click "Play".

---

## Main Objective: Seamless Game Loading
Ensure that selecting a save slot from the Main Menu instantly restores the player's progress and transports them exactly where they left off in the game world.

## Core Milestones

- [X] **System Wiring** (COMPLETED)
- [X] **Data Restoration** (COMPLETED)
- [X] **World Transition** (COMPLETED)
- [X] **Fresh Starts** (COMPLETED)

---

## Backlog & Future Polish

- [ ] **Dynamic Menu Information**
  Replace the current placeholder text on the save slots with actual player statistics (like their current level, gold, and playtime). The system needs to "peek" at the save files to display this information without fully loading the entire game.
