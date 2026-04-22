# Player Ability Slots And Unlocking Implementation Guide

Use this document as the working implementation reference for expanding the player bow ability system.

This guide is meant to keep gameplay runtime work, progression ownership, and future slot scaling aligned.

## Purpose

Use the player ability system to do three jobs cleanly:

* execute equipped abilities during moment-to-moment gameplay
* scale from the current small roster to a larger roster without increasing button count linearly
* leave unlock and progression authority in the correct part of the project

## Current Runtime Baseline

Treat the current runtime as the starting point, not the final shape.

The current structure already provides these useful building blocks:

* `PlayerAbilitySO` defines metadata, cooldown, unlock query, and behavior hooks
* `PlayerAbilityRuntime` owns cooldown timing, bow-draw blocking state, and movement-lock state
* `PlayerAbilityController` owns a 5-slot backend array and ticks equipped runtimes
* `PlayerBowController` asks `PlayerAbilityController.IsBowDrawBlocked` before beginning draw
* `PlayerMotor` respects both direct movement locks and active ability movement locks

Use that execution flow as the base that future slot work should preserve.

## Current Runtime Constraints

Treat these as the main constraints that future implementation must remove carefully:

* the backend now exposes 5 runtime slots and the keyboard input path now drives all 5
* the input asset still uses numbered action names rather than a renamed `AbilitySlot` action family
* legacy `_ability1` and `_ability2` data still exists only as a migration bridge for older serialized objects
* first-pass scene-transfer persistence now preserves equipped slots between `MainArea` and `ProceduralTiles`
* there is still no broader long-term saved equipped-slot model beyond the current runtime scene-transfer flow
* runtime unlock gating now reads `requiredSkillID` against `GameSessionSO.PlayerSkills`
* scene defaults can still make abilities appear equipped even when they are not unlocked, but locked abilities should no longer activate

These constraints are acceptable for now, but future work should not reintroduce more hardcoded named slot fields.

## Ownership Rules

### Player Ability Runtime Should Own

Use player-side code for:

* which abilities are equipped into active slots
* how slot input activates the equipped ability
* whether the equipped ability can execute right now
* whether an active ability blocks bow draw
* whether an active ability locks movement
* how press and release behaviors are forwarded to the equipped ability runtime

### Progression Systems Should Own

Use broader progression code for:

* whether an ability has been purchased
* skill tree progression
* SP costs
* save and load authority for unlock state
* UI for browsing and selecting unlocked abilities

Use player ability code as the query consumer, not as the long-term source of truth for unlock persistence.

## Current Input Shape

Use the current input shape only as the temporary bridge.

Right now:

* the controller consumes generic slot events internally
* the current keyboard input bridge feeds all 5 slots
* `Ability1` currently maps into slot 1 on `Q`
* `Ability2` currently maps into slot 2 on `R`
* `Ability3` currently maps into slot 3 on `Z`
* `Ability4` currently maps into slot 4 on `X`
* `Ability5` currently maps into slot 5 on `C`

That keeps existing gameplay stable and completes the keyboard-side slot-input migration.

Do not extend this bridge with more one-off controller fields.

## Target Slot Input Plan

Use slot-based input instead of named-ability input.

The planned slot key layout should be:

* slot 1 -> `Q`
* slot 2 -> `R`
* slot 3 -> `Z`
* slot 4 -> `X`
* slot 5 -> `C`

Keep these current bindings untouched:

* `1` -> `Previous`
* `2` -> `Next`

This keeps ability keys grouped together and avoids conflicts with the current item or selection bindings.

The old keyboard `Crouch` key has been reclaimed for slot 5 because crouch is not part of the player gameplay set.

## Slot Model

Treat the long-term model as:

* many abilities may exist
* many abilities may be unlocked
* only a small number of abilities are equipped at one time
* input activates slots, not ability names

That means the player can grow the roster without growing the keyboard mapping.

## Runtime Structure To Build Toward

Use these concepts as the target runtime model.

### Ability Library

The full set of ability definitions that may exist for the player.

This stays definition-driven through `PlayerAbilitySO`.

### Unlock State

A persistent progression source that answers whether a given ability ID is owned.

Use the player ability side only to ask that question.

### Equipped Slots

A player-owned set of active ability slots, such as:

* slot 1
* slot 2
* slot 3
* slot 4
* slot 5

Each slot should reference one equipped ability definition or stable ability ID.

### Slot Input

Input should call slot commands such as:

* activate slot 1
* release slot 1
* activate slot 2

Do not build the future input stack around commands like:

* cast MultiShot
* cast Rambow

## Activation Style Rule

Do not assume every ability is press-only.

The current system already proves both styles are needed:

* `MultiShot` behaves like a press
* `Arrow Rain` behaves like a press
* `Rambow` behaves like a hold

Future slot input should forward both press and release, then let each ability decide what it uses.

The same rule applies to movement locking:

* short one-shot abilities may use timed movement lock windows
* held abilities may keep movement locked while active

## Unlocking Rule

Use `PlayerAbilitySO.IsUnlocked(PlayerAbilityContext context)` as the player-side gate.

The unlock query now uses stable identifiers.

Recommended fields to add later:

* `abilityID`
* `requiredSkillID`

Current behavior:

* empty `requiredSkillID` means the ability is available by default
* non-empty `requiredSkillID` queries `GameSessionSO.PlayerSkills`

Do not add a second separate unlock database inside the bow ability scripts.

## Refactor Direction

Use `PlayerAbilityController` as a true equipped-slot owner.

Shape it around:

* `AbilitySlot[] _equippedSlots`

or:

* a serialized list of `AbilitySlot`

The backend controller now effectively follows that shape already.

Use public behavior shaped like:

* `TryActivateSlot(int slotIndex)`
* `TryReleaseSlot(int slotIndex)`

That keeps the controller scalable and removes the need for one field per ability button.

## Input Migration Rule

When slot-based ability input is implemented, keep the input stack synchronized.

Update all of these together:

* `Assets/InputSystem_Actions.inputactions`
* `Assets/Scripts/Player/Player/Input/InputSystem_Actions.cs`
* `Assets/Scripts/ScriptableObjects/PlayerInputReaderSO.cs`
* `Assets/Scripts/Input/InputManager.cs`
* `PlayerAbilityController`

Do not partially migrate only one layer of the input path.

## Implementation Phases

Use this order when the slot and unlock refactor begins.

### Phase 1: Stabilize Ability Identity

Add stable ability identification to `PlayerAbilitySO`.

Goal:

* make every ability referenceable by a stable ID
* keep all abilities unlocked by default until progression work begins

### Phase 2: Separate Unlock Query From Runtime Ownership

Choose the real progression source for unlock checks.

Goal:

* keep `IsUnlocked(...)` as the player-side query point
* avoid putting purchase or save logic into the bow runtime

### Phase 3: Convert Hardcoded Entries Into Equipped Slots

Refactor `PlayerAbilityController` from two named fields into slot data.

Goal:

* preserve current behavior for the first two abilities
* make room for slots 3 through 5 without adding more hardcoded members

Status:

* backend foundation complete
* legacy slot migration bridge still present for older serialized data
* movement lock is now available as part of the ability runtime path

### Phase 4: Migrate Input To Slot Commands

Replace named ability input with slot input.

Goal:

* `Q`, `R`, `Z`, `X`, and `C` trigger equipped slots
* each slot forwards both press and release when needed

Status:

* keyboard backend migration complete
* `Q`, `R`, `Z`, `X`, and `C` now drive slots 1 through 5
* the input asset and generated wrapper are synchronized
* future work is no longer about keybind count, but about progression and equip management

### Phase 5: Add Equip Management Later

Add UI and player-facing equip management only after runtime and input are stable.

Goal:

* allow selecting which unlocked abilities fill the 5 active slots
* keep this step separate from the core runtime refactor

## Current Working Standard

Until the slot refactor begins, use these rules:

* leave `requiredSkillID` empty for default-available abilities
* keep purchase and progression UI out of player bow scripts
* do not add more named ability fields to the controller
* treat the current numbered action names as slot input, not named ability input
* shape all future ability work toward 5 equipped slots on `Q`, `R`, `Z`, `X`, and `C`
