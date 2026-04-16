# Player Ability Slots And Unlocking Guide

Use this document as the reference for how player bow abilities should scale over time.

The current project keeps abilities unlocked by default, but the system should be shaped so locking, equipping, and slot expansion can be added cleanly later.

## Purpose

Use the player ability system to do three jobs well:

* execute equipped abilities cleanly in moment-to-moment gameplay
* scale from a small roster to a larger one without exploding the input map
* leave unlock ownership in the right part of the project

## Current Runtime Structure

The current bow ability setup already has the main building blocks:

* `PlayerAbilitySO` defines metadata, cooldown, and an `IsUnlocked(...)` hook
* `PlayerAbilityRuntime` owns cooldown and bow-draw blocking state
* `PlayerAbilityController` owns equipped abilities and forwards input into them
* `PlayerBowController` checks `PlayerAbilityController.IsBowDrawBlocked` before starting draw

Use that as the baseline execution path.

## Responsibilities

### Player Ability Code Should Own

* which abilities are equipped into active slots
* how slot input triggers the equipped ability
* whether an equipped ability can currently execute
* whether an active ability blocks bow draw
* how press and release behaviors are handled per equipped ability

### Broader Progression Code Should Own

* whether an ability is purchased or unlocked
* skill tree progression
* SP costs
* shop or upgrade UI logic
* save and load authority for unlock state

Use player code to ask whether an ability is unlocked.
Use progression code to decide and persist the answer.

## Unlocking Guidance

Use `PlayerAbilitySO.IsUnlocked(PlayerAbilityContext context)` as the player-side query point.

When ability locking is added later, support it with stable IDs.

Recommended fields:

* `abilityID`
* `requiredSkillID`

Recommended behavior:

* empty `requiredSkillID` means the ability is available by default
* non-empty `requiredSkillID` means query the progression source, likely through `GameSessionSO.PlayerSkills`

Do not create a second separate unlock database inside the bow ability scripts unless design explicitly requires it.

## Slot Model

Treat the current two-slot setup as a temporary runtime shape, not the long-term target.

Right now the runtime is still built around:

* `_ability1`
* `_ability2`

Current input behavior is:

* `Ability1` -> button down
* `Ability2` -> button down and button up

That is fine for the current project state, but future work should be aimed at equipped slots rather than one hardcoded field per ability.

## Input Plan

Use slot-based input, not one key per named ability.

The planned slot key layout should be:

* slot 1 -> `Q`
* slot 2 -> `R`
* slot 3 -> `Z`
* slot 4 -> `X`
* slot 5 -> `C`

This keeps the keys grouped together and avoids conflict with the current:

* `1` -> `Previous`
* `2` -> `Next`

## Scaling Rule

When the roster gets larger, use this model:

* many abilities may be unlocked
* only a small number are equipped at one time
* input triggers slots, not named abilities

That means the player can own a large roster without needing a large number of direct keybinds.

## Recommended Future Structure

Use these concepts:

### Ability Library

The full set of ability definitions that can exist for the player.

### Unlock State

A persistent progression source that says which ability IDs are owned.

### Equipped Slots

A player-owned set of active ability slots, such as:

* slot 1
* slot 2
* slot 3
* slot 4
* slot 5

Each slot should reference one equipped ability definition or ability ID.

### Slot Input

Input should activate slots, not concrete abilities.

Use commands like:

* activate slot 1
* release slot 1
* activate slot 2

Do not build input around commands like:

* cast MultiShot
* cast Rambow

## Activation Style Rule

Do not assume every ability is press-only.

The current system already has both styles:

* `MultiShot` behaves like a press
* `Rambow` behaves like a hold

Future slot input should forward both press and release, then let each ability decide what it cares about.

## Recommended Refactor Direction

When the slot system grows, evolve `PlayerAbilityController` from:

* `_ability1`
* `_ability2`

toward something like:

* `AbilitySlot[] _equippedSlots`

or:

* a serialized list of equipped slots

Recommended public behavior later:

* `TryActivateSlot(int slotIndex)`
* `TryReleaseSlot(int slotIndex)`

That scales much better than adding `_ability3`, `_ability4`, and so on.

## Input Migration Reminder

When the slot-input refactor happens, keep the project input stack in sync.

Update all of these together:

* `Assets/InputSystem_Actions.inputactions`
* `Assets/Scripts/Player/Player/Input/InputSystem_Actions.cs`
* `Assets/Scripts/ScriptableObjects/PlayerInputReaderSO.cs`
* `Assets/Scripts/Input/InputManager.cs`
* `PlayerAbilityController`

## Suggested Implementation Order

When this work starts for real, follow this order:

1. add stable ability IDs and optional required skill IDs to `PlayerAbilitySO`
2. choose the real progression source for unlock checks
3. refactor `PlayerAbilityController` toward equipped slot data
4. change input from named abilities to named slots
5. map slots to `Q`, `R`, `Z`, `X`, and `C`
6. keep `1` and `2` untouched for `Previous` and `Next`
7. add UI later for equipping unlocked abilities into slots

## Working Standard

For now, use these rules:

* keep all current abilities unlocked by default
* keep purchase and progression UI out of the player bow scripts
* shape new work toward 5 equipped slots
* use `IsUnlocked(...)` as the player-side gate
* expect the progression system to become the long-term source of truth
