# UI Toolkit & Save System Integration - TODO List

This document tracks the current implementation plan and progress for the **Toris** project, specifically focusing on the UI, Inventory, and Save/Load architecture.

## Current Objective: Connect Main Menu Save Slots to Gameplay

**Goal:** When a user clicks a "Save Slot" card in the Main Menu, the system should read the JSON save data from disk, inject it into the live `GameSessionSO`, and seamlessly transition to the correct gameplay scene.

### Implementation Status

- [ ] **Step 1: Inject Dependencies in MainMenuController**
  - Add a serialized reference (or runtime lookup) for `SaveManager` in `MainMenuController.cs`.
  - Ensure the `SaveManager` has access to the active `ItemDatabaseSO` for deserialization.

- [ ] **Step 2: Update `SaveManager` API**
  - Verify that `SaveManager` has a public method (or update `LoadGameData`) that can handle both reading the file *and* pushing it to `ActiveSession.ImportFromSaveData(loadedData, MasterItemDatabase)`.
  - Expose a method `SaveManager.LoadAndApplySlot(int slotIndex)` or similar.

- [ ] **Step 3: Wire the Event Handler**
  - In `MainMenuController.HandleSlotSelected(int slotIndex)`:
    - Invoke the `SaveManager` to load the data for the given `slotIndex`.
    - Check if the load was successful. If no save exists (new game scenario), handle the initialization of a fresh `GameSaveData` object.
    - Extract the target scene name from the loaded data (e.g., `loadedData.CurrentSceneName`).

- [ ] **Step 4: Execute Scene Transition**
  - Call `SceneTransitionService.Instance.LoadScene(targetSceneName)` to transition the player out of the Main Menu and into the game.

---

## Future Problems / Backlog
- [ ] **Display Real Save Data:** Currently, slots are populated with mock data. We need to implement a "Peek" mechanism in `SaveManager` to read metadata (Level, Gold, Date) for all slots without fully loading them into the live session.
- [ ] **New Game vs. Load:** Determine logic for initializing defaults if a JSON file is missing for a selected slot.
