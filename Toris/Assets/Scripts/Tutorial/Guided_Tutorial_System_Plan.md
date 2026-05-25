# Guided Tutorial System Plan

This document plans a reusable tutorial overlay system for UI and gameplay introductions.

The goal is to support first-time explanations such as:

- Smith Forge tab
- Smith Market tab
- Smith Salvage tab
- inventory stats panel
- inventory equipment panel
- inventory potion slots
- HUD ability bar / ability slots
- future panels and features we have not named yet

The system should be generic. It should not be hardcoded only for the Smith.

## Design Goals

- Show tutorial steps once, then remember they were completed.
- Darken the screen while leaving one target area visually highlighted.
- Show a title/body tooltip near the highlighted target.
- Work with UI Toolkit views and subviews.
- Use data-driven tutorial definitions.
- Keep views dumb and keep tutorial decisions in manager / presenter code.
- Avoid direct hard references like `GameObject.Find()`.
- Avoid one-off tutorial logic inside Smith, Inventory, HUD, or ability views.
- Allow future tutorial steps to be added mostly through data and anchor registration.

## Current UI Context

The project already has a useful UI structure:

- `UIManager` owns the main UI root and zones.
- `GameView.Show()` emits `UIEventsSO.OnScreenOpen`.
- `GameView.Hide()` emits `UIEventsSO.OnScreenClose`.
- Screens are opened through `UIEventsSO.OnRequestOpen`.
- Smith tabs are lazy subviews:
  - `ShopSubView`
  - `ForgeSubView`
  - `SalvageSubView`
- HUD is always shown in the HUD zone.
- Inventory, Smith, and similar screens are normal `GameView` screens.

That means tutorial triggers can listen to existing screen-open events, but target anchors must be registered by the views themselves.

## Core Idea

Tutorial steps target stable anchor IDs instead of direct C# references.

Example anchor IDs:

| Anchor ID | Expected Target |
|---|---|
| `smith.forge_tab` | Forge tab button |
| `smith.market_tab` | Market tab button |
| `smith.salvage_tab` | Salvage tab button |
| `smith.forge_panel` | Forge subview content |
| `smith.market_panel` | Market subview content |
| `smith.salvage_panel` | Salvage subview content |
| `inventory.stats_panel` | Stats drawer/panel |
| `inventory.stats_toggle` | Stats toggle button |
| `inventory.equipment_panel` | Equipment panel |
| `inventory.potion_slots` | Potion slots |
| `inventory.backpack_grid` | Player backpack grid |
| `hud.ability_bar` | Ability bar container |
| `hud.ability_slot_0` | First ability slot |
| `hud.potion_slots` | HUD potion slots |

The tutorial system asks:

1. Which tutorial steps are triggered by this event?
2. Has this step already been completed?
3. Are its prerequisites complete?
4. Is its anchor registered and visible?
5. If yes, show the overlay. If no, queue it briefly and retry after layout settles.

## Recommended Runtime Pieces

### TutorialStepSO

ScriptableObject for one tutorial step.

Suggested fields:

| Field | Purpose |
|---|---|
| `StepId` | Stable save ID, such as `smith.forge_tab.intro` |
| `Trigger` | When to consider this step |
| `RequiredScreen` | Optional screen that must be open |
| `AnchorId` | Target anchor, such as `smith.forge_tab` |
| `Title` | Tooltip title |
| `Body` | Tooltip body |
| `Placement` | Auto, left, right, above, below |
| `BlocksInput` | Whether user must dismiss before interacting |
| `DismissMode` | Got It button, click anywhere, next button |
| `PrerequisiteStepIds` | Optional previous tutorial IDs |
| `Priority` | Tie-breaker when multiple steps are eligible |
| `OneShot` | Usually true |

### TutorialCatalogSO

ScriptableObject containing all tutorial steps.

Responsibilities:

- hold a list of `TutorialStepSO`
- return steps by trigger
- validate duplicate IDs in editor

This keeps the manager from needing a manually wired list on every scene object.

### TutorialTrigger

Use an enum for common triggers.

Starting set:

| Trigger | Example Use |
|---|---|
| `ScreenOpened` | Inventory, Smith, Skills |
| `SmithOpened` | First time entering Smith |
| `InventoryOpened` | First time opening inventory |
| `HudReady` | Ability bar / potion slot introduction |
| `SkillUnlocked` | Ability/tutorial after a skill unlock |
| `AbilitySlotsUpdated` | First time an ability appears on HUD |
| `ItemEquipped` | First time equipment changes stats |
| `PotionAssigned` | First time potion slot is populated |
| `Custom` | Future one-off feature triggers |

We do not need every trigger on day one. The enum can start small and grow.

### TutorialAnchorRegistry

Pure C# service that maps string IDs to `VisualElement`s.

Responsibilities:

- register anchors by stable ID
- unregister anchors on hide/dispose
- resolve an anchor to its current `VisualElement`
- reject anchors that are hidden or detached from a panel
- expose `worldBound` so the overlay can position itself

Example usage inside views:

```csharp
TutorialAnchorRegistry.Register("smith.forge_tab", _forgeTab);
TutorialAnchorRegistry.Register("inventory.stats_panel", _statsPanel);
```

Views should unregister anchors when they hide or dispose, especially lazy Smith subviews.

### TutorialManager

MonoBehaviour scene-side coordinator.

Responsibilities:

- listen to event buses:
  - `UIEventsSO.OnScreenOpen`
  - `UISkillEventsSO.OnSkillUnlocked`
  - `UISkillEventsSO.OnAbilitySlotsUpdated`
  - future tutorial event bus if needed
- ask the presenter which tutorial step should show
- wait until the anchor exists and layout is resolved
- open/update the overlay
- mark steps complete when dismissed
- persist completed IDs

The manager should not directly calculate panel visuals. It should pass resolved display data into the overlay view.

### TutorialPresenter

Pure C# decision logic.

Responsibilities:

- filter steps by trigger
- check completion state
- check prerequisites
- sort by priority
- choose the next step

Keeping this pure makes the rules easier to test later.

### TutorialOverlayView

Dumb UI Toolkit view.

Responsibilities:

- draw four dark panels around the highlighted rectangle
- draw highlight border/pulse over the target rectangle
- place the tooltip near the target
- expose dismiss/next callbacks
- update when screen size or target bounds change

The first implementation should use four dark panels instead of a shader mask:

- top panel
- bottom panel
- left panel
- right panel

That creates a rectangular cutout and is reliable in UI Toolkit.

## Overlay Layer

Use the existing full screen UI layer.

`MasterLayout.uxml` has:

- `Layer_HUD`
- `Layer_Windows`
- `FullScreen_Zone`

The tutorial overlay should be placed in `FullScreen_Zone` or registered through `UIManager` as a Modal/FullScreen view.

Because `FullScreen_Zone` currently has `picking-mode="Ignore"`, the overlay implementation must explicitly set picking mode on the tutorial root / blocking panels when it needs to block input.

## Persistence

Tutorial completion should eventually be per save slot.

Recommended final save shape:

```csharp
public SavedTutorialProgressData TutorialProgress;

[System.Serializable]
public class SavedTutorialProgressData
{
    public List<string> CompletedStepIds = new List<string>();
}
```

This belongs in `GameSaveData`, then routes through `SaveDataOrchestrator` and `GameSessionSO`.

For a first prototype only, `PlayerPrefs` would be faster, but it would be global across saves. Since the desired behavior is "once for this playthrough/save", the better project fit is save-data integration.

## Suggested First Milestone

Build the smallest reusable version:

1. `TutorialStepSO`
2. `TutorialCatalogSO`
3. `TutorialAnchorRegistry`
4. `TutorialManager`
5. `TutorialOverlayView`
6. save-data completion IDs
7. three first tutorial steps:
   - `smith.forge_tab.intro`
   - `inventory.equipment_panel.intro`
   - `hud.ability_bar.intro`

This proves:

- a screen-open tutorial works
- a persistent inventory panel tutorial works
- a HUD tutorial works
- completed tutorial IDs are saved

After that, adding more steps should mostly be:

- register the anchor
- create or fill a tutorial step asset
- choose the trigger

## First Content Batch Candidates

| Step ID | Trigger | Anchor | Purpose |
|---|---|---|---|
| `smith.forge_tab.intro` | Smith opened | `smith.forge_tab` | Explain combining recipe ingredients into crafted items |
| `smith.market_tab.intro` | Smith opened | `smith.market_tab` | Explain buying/selling |
| `smith.salvage_tab.intro` | Smith opened | `smith.salvage_tab` | Explain breaking items into materials |
| `inventory.equipment_panel.intro` | Inventory opened | `inventory.equipment_panel` | Explain equipping gear |
| `inventory.stats_panel.intro` | Inventory opened or stats opened | `inventory.stats_panel` | Explain item stat tradeoffs |
| `inventory.potion_slots.intro` | Inventory opened | `inventory.potion_slots` | Explain assigned potion slots |
| `inventory.backpack_grid.intro` | Inventory opened | `inventory.backpack_grid` | Explain normal inventory storage |
| `hud.ability_bar.intro` | HUD ready or first ability unlocked | `hud.ability_bar` | Explain active abilities |
| `hud.potion_slots.intro` | HUD ready or potion assigned | `hud.potion_slots` | Explain hotkey potion slots |

Not all of these should ship immediately. The first implementation should use a very small set so behavior can be tested.

## UI Authoring Notes

Tutorial overlay UXML should live in:

`Assets/UI_Toolkit/UXML Templates/Tutorial/`

Tutorial USS should live in:

`Assets/UI_Toolkit/USS/Tutorial/`

Tutorial scripts should live in:

`Assets/Scripts/Tutorial/`

Tutorial step assets can later live in a data folder, likely:

`Assets/Resources/GameData/Tutorial/`

or another project-approved ScriptableObject data location.

## Anchor Registration Rules

- Register anchors only after elements are queried.
- Register anchors again after lazy subviews are instantiated.
- Unregister anchors on hide/dispose.
- Do not let the tutorial manager query random view internals.
- Use stable IDs that do not depend on visible text.
- Avoid registering every tiny label; register panels, tabs, buttons, and slots that matter.

## Edge Cases

### Target Not Ready Yet

Some anchors are created lazily. Example: Smith forge content exists only after the Forge tab is shown.

The manager should retry for a short time after a trigger, such as a few scheduled UI frames, before giving up.

### Target Offscreen Or Hidden

If a target is hidden or has a zero-size `worldBound`, do not show the step. Either wait or skip until the trigger happens again.

### Multiple Eligible Steps

Only show one tutorial at a time.

Use priority and prerequisites to order steps.

### User Closes The Screen

If the target screen closes, the overlay should close too and not mark the step complete unless the user dismissed it.

### Input Blocking

Some tutorials should block input. Others may only point something out.

First version should probably block input and require a "Got it" click. It is simpler and avoids interaction bugs.

## Open Implementation Questions

Answered decisions for the first implementation:

1. Tutorial progress is saved per save slot.
2. New save slots ask: "Would you like tutorial guidance?" with Yes/No.
3. Active tutorial steps pause gameplay and lock gameplay hotkeys.
4. Highlighted regions should remain clickable where possible; the first Smith sequence uses the Next button so it can move through tabs cleanly.
5. Ordered multi-step sequences are required.
6. The first real sequence is Smith Forge, Market, and Salvage.
7. Tutorial text is authored through a `TutorialCatalogSO` asset instead of hardcoded in the runtime.

## First Slice Implementation

- Runtime scripts live in `Assets/Scripts/Tutorial/`.
- Overlay styling lives in `Assets/UI_Toolkit/USS/Tutorial/TutorialOverlay.uss`.
- The first tutorial catalog lives in `Assets/Resources/GameData/Tutorial/DefaultTutorialCatalog.asset`.
- Save data stores `TutorialsEnabled` and completed step IDs.
- `GameSessionSO` owns the runtime tutorial state for the active save slot.
- `UIManager` creates and binds the tutorial runtime from the shared UI root.
- `SmithView` registers stable tutorial anchors for the Forge, Market, and Salvage tabs.
