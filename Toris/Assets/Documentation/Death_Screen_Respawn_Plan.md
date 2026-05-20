# Death Screen and Respawn Plan

## Goal

Add a real death flow for the run/exploration loop:

- player reaches 0 HP
- existing death animation plays for a short delay
- player control is disabled, but the game world keeps running
- a "You Died." UI overlay appears above the HUD
- Escape and gameplay hotkeys cannot dismiss or bypass the screen
- player chooses either Respawn or Main Menu
- Respawn returns the player to MainArea at a configured anchor
- death restores HP/stamina and clears active statuses
- death applies punishment by restoring the pre-run checkpoint, then applying stat/progression/item penalties

This should be implemented through the existing ScriptableObject, MVP UI, event-driven, and scene-snapshot patterns already used in the project.

## Current Project Shape

### Death spine already exists

Relevant files:

- `Assets/Scripts/Player/Player/Status/PlayerStats.cs`
- `Assets/Scripts/Player/Player/Combat/PlayerLifeGate.cs`
- `Assets/Scripts/Player/Player/View/PlayerAnimationPresenter.cs`
- `Assets/Scripts/Player/Player/View/PlayerAnimationController.cs`
- player audio/VFX bridges that already react to player death events

`PlayerStats` owns current HP/stamina and raises `OnPlayerDied` once when HP reaches 0. `PlayerLifeGate` already listens and disables configured gameplay behaviours/colliders while keeping the GameObject and animation stack alive. That matches the desired "let the death animation play visibly" requirement.

The death screen should not replace this. It should subscribe to the same death event and layer UI/respawn behavior on top.

### UI stack already supports full-screen screens

Relevant files:

- `Assets/Scripts/UIToolkit/UI/Events/ScreenTypes.cs`
- `Assets/Scripts/UIToolkit/UI/Events/UIEventsSO.cs`
- `Assets/Scripts/UIToolkit/UI/UIViews/UIManager.cs`
- `Assets/Scripts/UIToolkit/UI/UIViews/GameView.cs`
- `Assets/Scripts/UIToolkit/UI/UIViews/UIView.cs`
- `Assets/Scripts/UIToolkit/UI/Pause Menu/PauseMenuController.cs`
- `Assets/Scripts/UIToolkit/UI/Pause Menu/PauseMenuView.cs`
- `Assets/UI_Toolkit/UXMLs/MasterLayout.uxml`

`MasterLayout.uxml` has `FullScreen_Zone`, and `UIManager` already handles mutual exclusion for non-HUD screen zones. The death screen belongs in `ScreenZone.FullScreen`, with a new `ScreenType.DeathScreen`.

The view should be dumb:

- query static elements only in `SetVisualElements()`
- bind/unbind button callbacks in `Show()` / `Hide()` or with a guarded `_eventsBound`
- expose events like `OnRespawnClicked` and `OnMainMenuClicked`
- never mutate stats, inventory, save data, scenes, or penalties directly

### Input needs a death-specific lock

Relevant file:

- `Assets/Scripts/Input/InputManager.cs`

`InputManager` already tracks blocking screens and gameplay input locks through `UIEventsSO.OnGameplayInputLockRequested` and `OnGameplayInputUnlockRequested`.

Important current behavior:

- all non-HUD screens count as gameplay blockers
- Escape closes all open blocking UI
- inventory/skills/vendor hotkeys currently open screens without checking gameplay blockers
- quick save/load exist as testing paths and should not be part of this design

Death needs a stricter path:

- request a named gameplay input lock immediately on death, before the overlay delay
- make Escape do nothing while the death lock/screen is active
- ensure `UIManager.CloseAllWindows()` cannot hide the death screen once it is visible
- block inventory, skills, shop, quest, potion, attack, movement, interact, and ability hotkeys while dead
- still allow UI pointer/click/controller submit for the death screen buttons

### MainArea and ProceduralTiles use snapshot transfer

Relevant files:

- `Assets/Scripts/MapGeneration/Sites/Gate/RunGateInteractable.cs`
- `Assets/Scripts/MapGeneration/Runtime/Transitions/SceneTransitionService.cs`
- `Assets/Scripts/UIToolkit/ScriptableObjects/GameSessionSO.cs`
- `Assets/Scripts/Save System/RuntimeSnapshotRegistry.cs`
- `Assets/Scripts/Save System/SaveDataOrchestrator.cs`
- `Assets/Scripts/Save System/SaveManager.cs`
- `Assets/Scripts/Enemy/MainArea_ProceduralTiles_State_Transfer_Analysis.md`
- `Assets/Documentation/Architecture_GameSession_and_Persistence.md`

`SceneTransitionService` does normal single-scene loads. Scene objects are destroyed and rebuilt. `GameSessionSO` holds runtime references plus snapshot data so new scene objects can restore inventory, progression, stats, equipment, and potion state.

That is the correct model for death too. We should not carry the player object or UI objects across scenes.

### Existing save behavior needs a policy boundary

`PauseMenuController.QuitToMainMenu()` currently invokes quick save before loading MainMenu. That is not safe for the death flow because death from ProceduralTiles must not autosave the run state.

The death screen Main Menu button should use its own handler, not the pause menu handler.

For the broader autosave rule:

- autosave to disk should be allowed from MainArea
- autosave to disk should be blocked from ProceduralTiles
- entering ProceduralTiles from MainArea should create the run-start checkpoint
- death should not write a new save point from ProceduralTiles

## Proposed Runtime Flow

### Before entering ProceduralTiles

When the player uses the MainArea run gate into ProceduralTiles:

1. detect that the active scene is `MainArea` and the destination is `ProceduralTiles`
2. capture/save the active slot as the pre-run checkpoint
3. continue through `SceneTransitionService.UseRunGate()`

This checkpoint is the state death should reset from. It prevents ProceduralTiles loot/progress from accidentally becoming the respawn baseline.

Implementation options:

- add a small `RunStartCheckpointService` used by `RunGateInteractable`
- or extend `SceneTransitionService.UseRunGate()` with an optional checkpoint callback/policy

The first option is cleaner because it keeps scene loading separate from save policy.

### On player death

1. `PlayerStats.OnPlayerDied` fires
2. `PlayerLifeGate` disables movement/combat/colliders and zeroes velocity
3. animation presenter plays the death animation
4. `DeathScreenController` or `DeathFlowCoordinator` requests gameplay input lock, for example `"Death"`
5. close other UI screens without closing HUD
6. wait a serialized delay, for example `1.25f` to `2.0f`
7. request open `ScreenType.DeathScreen`
8. death screen displays above HUD, semi-transparent, with Respawn and Main Menu buttons

No `Time.timeScale = 0`. The world and events keep running.

### Respawn button

1. UI view raises `OnRespawnClicked`
2. controller raises an event on a death-specific event channel or `UIEventsSO`
3. `DeathRespawnCoordinator` handles the request
4. load the pre-run checkpoint into `GameSessionSO`
5. mark a pending death respawn with:
   - target scene: `MainArea`
   - target anchor id: configured death respawn anchor
   - pending penalty policy
   - full HP/stamina restore request
   - clear statuses request
6. load `MainArea` through `SceneTransitionService` so loading/fade hooks can run
7. after MainArea objects register with `GameSessionSO`, apply:
   - move player to respawn anchor
   - full HP/stamina
   - clear `PlayerStatusController`
   - apply progression/stat penalties
   - apply inventory penalties
   - refresh inventory/HUD events
8. clear the pending death respawn state and unlock gameplay input

### Main Menu button

1. UI view raises `OnMainMenuClicked`
2. controller raises a death-main-menu event
3. handler unlocks/reset time scale defensively
4. load `MainMenu`

Do not call the pause menu quit path. Do not autosave from ProceduralTiles.

## Proposed Files

### UI scripts

Create:

- `Assets/Scripts/UIToolkit/UI/Death Screen/DeathScreenView.cs`
- `Assets/Scripts/UIToolkit/UI/Death Screen/DeathScreenController.cs`

Touch:

- `Assets/Scripts/UIToolkit/UI/Events/ScreenTypes.cs`
- `Assets/Scripts/UIToolkit/UI/Events/UIEventsSO.cs`
- `Assets/Scripts/UIToolkit/UI/UIViews/UIManager.cs`
- `Assets/Scripts/Input/InputManager.cs`

The view owns only UI element references and button events. The controller instantiates the UXML in `Start()`, initializes the view, registers it with `UIManager`, listens for death, and opens the screen after delay.

### UI assets

Create:

- `Assets/UI_Toolkit/UXML Templates/Death_Screen/DeathScreen.uxml`
- `Assets/UI_Toolkit/USS/Death_Screen/DeathScreen.uss`

Style target:

- large semi-transparent full-screen overlay
- "You Died." as the main title
- Respawn button as default/focused action
- Main Menu button as secondary action
- HUD can remain behind it but should not be interactive

### Death/respawn gameplay scripts

Create:

- `Assets/Scripts/Player/Player/Death/DeathRespawnCoordinator.cs`
- `Assets/Scripts/Player/Player/Death/DeathPenaltyConfigSO.cs`
- `Assets/Scripts/Player/Player/Death/DeathRespawnAnchor.cs`
- optional: `Assets/Scripts/Player/Player/Death/RunStartCheckpointService.cs`

Depending on final implementation, a small runtime state object may also be useful:

- `Assets/Scripts/Player/Player/Death/DeathRespawnStateSO.cs`

This would hold pending respawn state across the scene load without making the coordinator persistent.

### Save/session touch points

Likely touch:

- `Assets/Scripts/UIToolkit/ScriptableObjects/GameSessionSO.cs`
- `Assets/Scripts/Save System/RuntimeSnapshotRegistry.cs`
- `Assets/Scripts/Save System/SaveDataOrchestrator.cs`
- `Assets/Scripts/Save System/SaveManager.cs`

Purpose:

- support a pre-run checkpoint/reset source
- make autosave scene policy explicit
- avoid saving ProceduralTiles death state
- optionally persist death stat penalties if they are meant to survive quitting/reloading

## Penalty Model

### Inventory penalty

Recommended first pass:

- remove from backpack inventory and potion inventory
- do not remove equipped items by default
- make percentages serialized in `DeathPenaltyConfigSO`
- cache item instances/counts before mutating slots
- call `NotifyInventoryUpdated()` after changes

Questions for later tuning:

- should quest/key items be protected if such item flags exist later
- should item loss be random, lowest value first, highest value first, or category weighted
- should stackable and non-stackable items use different rules

### Progression penalty

Use `PlayerProgression` public APIs:

- `RemoveExperience(float amount)`
- `TrySpendGold(int amount)` or `SetGold(int value)`

Recommended config fields:

- `experienceLossPercent`
- `goldLossPercent`
- minimum/maximum clamps

### Stat penalty

"Stats" needs a careful implementation because current combat stats are resolved through:

- `PlayerEffectSourceController`
- `PlayerEffectDefinitionSO`
- `IPlayerEffectSource`
- equipment effect sources

Do not mutate `PlayerBaseEffectsSO` assets at runtime. That would damage authored data.

Recommended model:

- create a death penalty runtime state stored in session/save data
- apply it as a named effect source, for example `"DeathPenalty"`
- expose serialized percentage fields in `DeathPenaltyConfigSO`
- rebuild resolved effects through `PlayerEffectSourceController`

This makes death penalties visible to the existing HUD/stat UI and keeps them compatible with equipment and consumable effects.

If first pass needs to stay smaller, we can interpret "stats" as XP/gold/resource economy first, then add persistent combat-stat penalties in a second pass.

## Unity Setup Needed

The user will need to create/assign these scene/prefab references in Unity after scripts/assets exist:

1. Add a `DeathScreenController` prefab or child object under the UI controller setup.
2. Add it to both `Assets/Prefabs/UI/UIControllers.prefab` and `Assets/Prefabs/UI/UIControllers_Underworld.prefab` for safety.
3. Assign:
   - `DeathScreen.uxml`
   - `UIEventsSO`
   - `PlayerStatsAnchorSO` or another player stats source
   - death screen delay
4. Add a `DeathRespawnCoordinator` scene object or prefab child in both gameplay UI controller prefabs.
5. Assign:
   - `GameSessionSO`
   - `SaveManager`
   - `SceneTransitionService` or let it resolve `SceneTransitionService.Instance`
   - `DeathPenaltyConfigSO`
   - MainArea scene name: `MainArea`
   - MainMenu scene name: `MainMenu`
   - death respawn anchor id
6. Add a `DeathRespawnAnchor` GameObject in MainArea at the desired respawn location.
7. Give that anchor a stable id, for example `MainArea_DeathRespawn`.
8. Ensure the loading screen or fade object is connected to `SceneTransitionService.onTransitionStart` and `onTransitionEnd`, or add a small loading overlay in a later pass.

## Implementation Order

1. Add death screen UI type, UXML, USS, view, and controller.
2. Add a non-dismissible death input lock and UI close protection.
3. Add Respawn/Main Menu UI events.
4. Add run-start checkpoint capture before MainArea -> ProceduralTiles transition.
5. Add respawn coordinator that restores checkpoint, loads MainArea, moves player to anchor, restores resources, clears statuses, and unlocks input.
6. Add inventory/progression penalty config and first-pass penalty application.
7. Add combat-stat penalty effect source only if we decide the first pass should include max HP/damage/move/etc penalties.
8. Wire controller prefabs and MainArea anchor in Unity.
9. Static verification with search/logical review, then Unity play-mode testing by hand.

## Risks and Edge Cases

- `UIManager.CloseAllWindows()` currently hides every non-HUD view. DeathScreen must be excluded or Escape can soft-unlock the screen.
- `InputManager` currently lets inventory/skills/shop hotkeys request screens even while blockers exist. Death needs a stricter lock.
- `PauseMenuController` autosaves before MainMenu. Death Main Menu must not reuse that path.
- `SceneTransitionService` has transition hooks but no obvious dedicated loading screen script in the scanned code. Respawn should use the service now and wire loading visuals through the hooks.
- `GameSaveData.CurrentSceneName` exists, but export currently does not populate it. Main menu load falls back to MainArea when it is empty. If save scene fidelity matters later, save export needs a clear scene policy.
- Active timed consumable effects are runtime-only today. Death can clear player statuses immediately, but clearing timed buff sources needs a precise rule so equipment sources are not wiped.
- `PlayerEffectSourceController.ClearAllSources()` would also remove equipment effects, so death should not call it blindly.
- Inventory penalty must cache item references/counts before removal because slot mutation can clear stack data.

## Open Design Decisions Before Coding

These are the remaining choices worth confirming before implementation:

1. Should item loss include potion inventory, or backpack only?
2. Should equipped items be protected from death loss in the first pass?
3. Should death penalties apply to the pre-run checkpoint only, meaning ProceduralTiles gains are discarded first?
4. Which exact stats should be reduced if we do combat-stat penalties: max HP, max stamina, damage, speed, regen, or all configured through `DeathPenaltyConfigSO`?
5. Should penalties be saved to disk immediately after respawn in MainArea, or only when the next normal MainArea save happens?

## First-Pass Recommendation

For the first implementation pass, keep it practical:

- death overlay and input lock
- pre-run checkpoint before MainArea -> ProceduralTiles
- respawn to MainArea anchor through `SceneTransitionService`
- restore full HP/stamina
- clear `PlayerStatusController` statuses
- discard ProceduralTiles progress by restoring checkpoint
- apply gold/XP/backpack/potion penalties
- protect equipped items
- leave persistent combat-stat penalties for a second pass unless we decide the exact stats now

That gives the complete player-facing death loop without overloading the first code change.
