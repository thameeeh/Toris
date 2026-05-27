# Settings Menu Expansion Plan

## Goal

Expand the existing Settings menu with practical options for a simple 2D isometric pixel-art game. The first implementation pass should prioritize stable, low-risk settings that improve player comfort without introducing complex graphics systems the game does not need.

Confirmed settings:

- Resolution selection.
- Windowed and fullscreen mode selection.
- Key rebinding for keyboard and mouse first, with gamepad support planned later.
- Damage numbers toggle later, after the damage number system exists.

Current settings already implemented:

- Master Volume.
- Music Volume.
- SFX Volume.
- Loot Vacuum toggle.

## Existing Architecture

The Settings menu is currently shared by the main menu and gameplay pause flow.

- `SettingsMenuController` instantiates the UXML template in `Start()`, creates `SettingsMenuView`, initializes it, injects current values, subscribes to view events, and registers the view with either `MainMenuUIManager` or gameplay `UIManager`.
- `SettingsMenuView` is a pure UI Toolkit view. It queries controls, forwards UI changes through C# events, and does not own gameplay or persistence logic.
- `SettingsMenu.uxml` owns the menu structure.
- `SettingsMenu.uss` owns layout and styling.
- `AudioVolumeSettings` stores audio values in `PlayerPrefs` and applies master volume through `AudioListener.volume`.
- `LootMagnetSettings` stores the Loot Vacuum toggle in `PlayerPrefs`; `WorldItemMagnet` reads it at runtime.

The expansion should keep the same pattern:

- View: only query UI controls, set displayed values, and emit user intent events.
- Controller: translate view intents into setting changes.
- Settings model classes: own persistence and runtime application.
- UXML/USS: own visual structure and layout.

## Architectural Rules To Preserve

- Keep UI Toolkit views dumb. No direct gameplay mutation or heavy logic in `SettingsMenuView`.
- Do not use `UIDocument` inside the settings controller. Keep the current `VisualTreeAsset` instantiation pattern.
- Instantiate templates in `Start()`, not `OnEnable()`.
- Keep layout and sizing changes in UXML/USS rather than inline C# style changes, except for existing runtime wrapper setup.
- Use event-driven communication between view and controller.
- Unsubscribe all callbacks in `Dispose()` and `OnDestroy()`.
- Avoid hard references such as `GameObject.Find()` in new settings code.
- Wrap any new debug logging in `#if UNITY_EDITOR`.
- Use named constants for PlayerPrefs keys and defaults.

## Phase 1: Display Settings

Display settings are global game preferences, not per-save-slot data. Store them through `PlayerPrefs`, matching the existing audio and Loot Vacuum settings.

### New Settings Model

Create a small static settings owner, likely `DisplaySettings` or `GameDisplaySettings`, responsible for:

- Loading saved display preferences from `PlayerPrefs`.
- Applying resolution and fullscreen mode through Unity's `Screen` API.
- Saving values when the user confirms display changes.
- Reverting pending display changes if the user rejects or ignores the confirmation prompt.
- Exposing read-only current values for the controller.

Suggested stored values:

- Resolution width.
- Resolution height.
- Fullscreen mode.
- Display index for multi-monitor setups.

Suggested defaults:

- Use the current screen resolution if no preference is saved.
- Prefer the current Unity fullscreen mode if already set by the platform.

### Resolution Selection

Recommended behavior:

- Use a curated popular-resolution list instead of exposing every mode reported by the monitor.
- Supported windowed resolutions are `1280 x 720`, `1366 x 768`, `1600 x 900`, `1920 x 1080`, and `2560 x 1440`.
- Hide curated resolutions that exceed the selected display's native resolution.
- Sort the list consistently.
- Display options as plain labels such as `1920 x 1080`.
- Keep selected values pending until the player presses `Apply`.
- After `Apply`, temporarily apply the selected resolution and ask the player to confirm.
- If the player confirms, save the new values.
- If the player rejects or the confirmation times out, restore the previous monitor, resolution, and window mode.

For a pixel-art game, avoid quality-style settings such as texture quality, shadows, lighting, anti-aliasing, and post-processing unless a specific art or performance issue appears later.

### Monitor Selection

Use Unity's display layout API to detect available desktop displays. The Settings menu should expose a `Monitor` dropdown before resolution and window mode.

Recommended behavior:

- Display each monitor as its name and native size when Unity provides that data.
- Store the selected Unity display index in `PlayerPrefs`.
- Filter the curated resolution list against the selected monitor's native resolution.
- When applying changes, move the main window to the selected display first, wait for Unity's async move operation to finish, then apply the selected resolution and window mode.
- Keep monitor changes inside the same apply, keep, and revert flow as resolution/window mode changes.
- If Unity cannot report display layout data, fall back to a single current-display option.

### Window Mode Selection

Use a dropdown or segmented control in UXML.

Recommended options:

- Fullscreen.
- Windowed.

Unity mapping:

- Fullscreen: `FullScreenMode.FullScreenWindow` for broad desktop compatibility.
- Windowed: `FullScreenMode.Windowed`.

Platform note:

- Some platforms may not support every mode exactly. The settings model should apply the requested mode using Unity's API and then read back the effective mode if needed.
- Windowed mode is the most reliable place for explicit resolution changes. Fullscreen uses the platform's fullscreen behavior and may resolve to the display's native resolution.
- Resolution selection should be disabled while Fullscreen is selected. Fullscreen should apply using the display's native resolution to avoid render-darkening and other platform-specific fullscreen/backbuffer issues.

### Apply And Revert Flow

Display changes should use an explicit apply flow instead of applying permanently on selection.

Recommended flow:

1. Player selects a monitor, resolution, or window mode.
2. `Apply` becomes enabled when pending display values differ from the saved/effective values.
3. Player presses `Apply`.
4. Controller snapshots the previous display settings.
5. Controller moves the window to the selected monitor when needed.
6. Controller applies the pending resolution and window mode temporarily.
7. A confirmation prompt appears, asking the player to keep the changes.
8. If confirmed, save the new display settings.
9. If rejected or timed out, revert to the snapshot and restore the dropdown values.

The project already has a confirmation modal pattern in the main menu. Reuse that pattern if it is available in both main menu and gameplay settings contexts; otherwise, add a small settings-specific confirmation view that still follows MVP rules.

### UI Changes

Keep the existing Audio, Display, and Gameplay controls grouped under a `Main` tab.

Suggested controls:

- `VisualElement` named `Settings_MainTab`.
- `VisualElement` named `Settings_ControlsTab`.
- `ScrollView` named `Settings_MainContent`.
- `ScrollView` named `Settings_ControlsContent`.
- `DropdownField` named `Dropdown_Display`.
- `DropdownField` named `Dropdown_Resolution`.
- `DropdownField` named `Dropdown_WindowMode`.
- `Button` named `Btn_ApplyDisplay`.

Keep the current right-side panel structure. Use two top-level tabs only: `Main` for current settings, and `Controls` for keyboard and mouse rebinding work.

## Phase 2: Key Rebinding

Key rebinding is valuable, but it has more risk than display settings because this project uses Unity's generated `InputSystem_Actions.cs`. The first pass should support keyboard and mouse only. Gamepad rebinding should be planned, but not implemented until keyboard and mouse rebinding is stable.

Project rule:

- When modifying Input System mappings outside the Unity Editor, update both `Assets/InputSystem_Actions.inputactions` and the generated `InputSystem_Actions.cs` synchronously.

Important current behavior:

- `InputManager` creates a new `InputSystem_Actions` instance at runtime.
- `MainMenuController` and `SettingsMenuController` also create `InputSystem_Actions` instances for menu Escape handling.
- `PlayerAbilityHUDView` reads generated bindings to display hotkeys.

Recommended first implementation:

- Do not modify default bindings directly for user preferences.
- Store binding overrides in `PlayerPrefs` using Unity Input System binding override JSON.
- Load overrides immediately after each `InputSystem_Actions` instance is created.
- Save overrides after a successful rebind.
- Provide reset-to-defaults for individual bindings and all bindings.
- Keep the first pass to primary keyboard and mouse bindings; leave alternate keyboard bindings and gamepad for later.
- Reject duplicate primary keyboard and mouse bindings, restoring the previous binding and showing a short conflict message.

Likely support class:

- `InputBindingSettings`, responsible for loading, saving, applying, and clearing binding overrides.
- `InputBindingSettings.OnBindingsChanged`, responsible for notifying HUD/menu systems that displayed hotkey labels need to refresh.

Likely UI flow:

- A `Controls` tab lists high-value keyboard and mouse actions only at first:
  - Move.
  - Attack.
  - Interact.
  - Dash.
  - Ability 1 through Ability 5.
  - Potion 1 and Potion 2.
  - Inventory.
  - Skills.
  - Quest Journal.
  - Pause.
- Each row shows the action name, current binding, and a rebind button.
- When rebinding, the view enters a "listening" state and the controller starts Unity's interactive rebinding operation.
- Escape should cancel the pending rebind instead of closing the settings menu.
- Prevent duplicate primary keyboard and mouse bindings.
- Do not auto-swap duplicate bindings in the first pass; reject the duplicate so movement and hotkeys cannot become ambiguous.
- Leave gamepad binding display and rebinding out of the first implementation, but avoid designing the support class in a way that blocks gamepad later.
- Ability and potion HUD labels should refresh from the active binding display strings after rebinds or resets.

Because this requires coordinated changes across all input action creation sites, key rebinding should be implemented after display settings are stable.

## Optional Extra Settings Worth Considering

These fit the game better than graphics quality settings:

- Screen shake intensity: `Off`, `Low`, `Normal`.
- Damage numbers toggle, but only after the damage number system exists. Do not add a non-functional toggle ahead of the feature.
- Floating pickup text toggle, if item pickup popups exist or are planned.
- Auto-pickup distance or Loot Vacuum strength, if the existing loot magnet should become tunable rather than only enabled or disabled.
- UI scale, if UI Toolkit layout supports it safely.
- Tutorial hints enabled, if tutorials remain a player preference after new-game selection.
- Quest tracking popups enabled, if quest UI becomes noisy.

Recommended near-term extra:

- Screen shake intensity only if there is already a centralized camera shake system.
- Damage numbers once implemented.
- Otherwise, keep the first pass to Display, Audio, Gameplay, and later Controls.

## Tabbed Settings Direction

The Settings menu should use two top-level tabs:

- `Main`: Audio, Display, Gameplay, and other general comfort settings.
- `Controls`: Keyboard and mouse rebinding first, gamepad later.

The tab implementation should follow existing UI Toolkit MVP rules:

- Main settings view owns tab visual switching only.
- Controller owns setting application and persistence.
- Tab layout and visibility styling live in UXML/USS.
- The menu should open on `Main` by default.

## Suggested Implementation Order

1. Add `GameDisplaySettings`.
2. Add Display controls to `SettingsMenu.uxml`.
3. Add USS layout for dropdown rows and scrollable content if needed.
4. Extend `SettingsMenuView` with display control queries, setters, events, and cleanup.
5. Extend `SettingsMenuController` to populate monitor/resolution/window mode options and track pending display values.
6. Add display `Apply` handling with confirm/revert behavior.
7. Save display settings only after confirmation.
8. Update documentation with the final behavior.
9. Add key rebinding in a separate pass after confirming the display settings UX.

## Verification Plan

Local verification is limited because this environment does not have a headless Unity Editor executable.

Static checks:

- Confirm no empty Unity lifecycle methods are added.
- Confirm new UI callbacks are unsubscribed.
- Confirm PlayerPrefs keys are constants.
- Confirm no new gameplay logic is placed in the view.
- Confirm tab switching only changes visible UI content and active tab styling.
- Confirm display changes can revert to the previous monitor, resolution, and window mode.
- Confirm key rebinding stores overrides in `PlayerPrefs` and does not directly edit `.inputactions` or generated C#.
- Confirm all `InputSystem_Actions` instances apply saved binding overrides after construction.
- Confirm duplicate keyboard/mouse bindings are rejected and restore the previous binding.

Unity manual checks:

- Open Settings from Main Menu.
- Open Settings from Pause Menu.
- Switch between `Main` and `Controls`, then confirm returning to `Main` preserves the current control values.
- Select a new resolution and confirm it.
- Select a new resolution and reject or time out the confirmation, then verify it reverts.
- Switch between fullscreen and windowed modes.
- On a multi-monitor setup, switch the monitor selection and confirm the window moves to the selected display.
- On a multi-monitor setup, switch the monitor selection and reject or time out the confirmation, then verify it returns to the previous display.
- Switch to `Controls`, rebind an ability key, and verify the HUD ability key label updates.
- Rebind potion keys and verify the HUD potion key labels update.
- Reset one binding and reset all bindings.
- Try rebinding `Move Up` to the same key as `Move Left` and confirm the duplicate is rejected.
- Press Escape while rebinding and confirm the pending rebind cancels without closing Settings.
- Close and reopen Settings to confirm displayed values persist.
- Restart the game and confirm saved display settings load.
- Confirm Escape closes Settings correctly.
- Confirm audio sliders and Loot Vacuum still work.
