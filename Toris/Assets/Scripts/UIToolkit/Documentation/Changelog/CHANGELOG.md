# UI Toolkit Changelog

### [2026-05-19] Ability Snapshot Integration & UI Refactor
- **Ability Snapshots:** Integrated `PlayerAbilitySlotSnapshot` as the single source of truth for the Ability HUD, enabling dynamic mana cost and cooldown scaling.
- **Timer Fix:** Resolved a bug where ability cooldowns ran at half-speed; switched to precise delta time tracking in the UI schedule loop.
- **Constructor Refactor:** Standardized UI View constructors (HUD, Mage, Skill, Inventory, Smith) to a multi-line, parameter-dense style for improved readability.
- **Styling Mandates:** Cleaned up `Main_Menu.uxml`, removing all inline styles and centralizing background tinting in `MainMenu.uss` and `theme-variables.uss`.
- **UI Event Bus:** Added `OnAbilitySlotsUpdated` to `UISkillEventsSO` to synchronize live ability state between backend and UI.

---

### [2024-05-23] Ability HUD Implementation
- **Ability Slots:** Created `AbilitySlot.uxml` and `AbilitySlot.uss` for player ability visualization.
- **Dynamic Cooldowns:** Implemented `PlayerAbilityHUDView` with real-time cooldown overlays using the UI Toolkit schedule API.
- **Visual Feedback:** Added scale-based "pressed" effect and "ready" glow for ability slots.
- **HUD Integration:** Refactored `HUDView` and `HudScreenController` to manage both Potion and Ability HUD components.
- **Decoupled Architecture:** Communication between player system and UI now flows through `UISkillEventsSO`.

---

### [2024-05-22] UI Toolkit Structural & Styling Refactor
- **MasterLayout Refactor:** Extracted all inline styles into `MasterLayout.uss` to comply with project mandates.
- **Responsive Layout Fix:** Changed `Layer_HUD` from fixed `20%` height to `auto` height, resolving potential clipping on 720p and detached visuals on 4K.
- **HUD Modularity:** Removed redundant `hud__screen` wrapper to allow HUD components to slot natively into `MasterLayout` zones.
- **Potion Bar Centering:** Re-anchored `.hud-potion-bar` using `position: absolute` and `translate: -50% 0` for perfect symmetrical centering.
- **Visual Contrast:** Increased `--color-bg-panel` alpha to `0.8` for better readability against dark textures.
- **Documentation:** Updated `UI_Architecture_And_Styling.md` with absolute positioning centering best practices.

---

### [Previous Entries]
(The main project CHANGELOG.md contains historical entries. This file focuses on UIToolkit specific architectural shifts.)
