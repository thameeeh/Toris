# UI Toolkit Changelog

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
