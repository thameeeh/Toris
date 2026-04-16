# Player Ability Slots And Unlocking Notes

This note captures the current player ability flow, what already exists for future locking, and the clean path for scaling from a couple abilities to a larger roster.

The goal is to avoid painting the player system into a corner while keeping the current "everything unlocked by default" setup simple.

## Current State

The current bow ability system already has a few good building blocks:

* `PlayerAbilitySO` defines metadata, cooldown, and a virtual `IsUnlocked(...)` check
* `PlayerAbilityRuntime` owns runtime cooldown and bow-draw blocking state
* `PlayerAbilityController` owns equipped ability slots and forwards input into them
* `PlayerBowController` asks `PlayerAbilityController.IsBowDrawBlocked` before starting draw

That means the player-side execution path is already aware that an ability can be:

* equipped
* on cooldown
* temporarily occupying the bow
* unavailable because it is not unlocked

The missing part is not the hook. The missing part is the real unlock source.

## Current Slot Model

Right now the runtime is hard-wired to two ability slots:

* `_ability1`
* `_ability2`

And it is also hard-wired to two input flows:

* `Ability1` -> button down only
* `Ability2` -> button down and button up

Current keyboard bindings in `InputSystem_Actions.inputactions` are:

* `Q` -> `Ability1`
* `R` -> `Ability2`

Also important:

* `1` is currently used by `Previous`
* `2` is currently used by `Next`

For future slot expansion, the preferred player ability slot keys should be:

* `Q`
* `R`
* `Z`
* `X`
* `C`

That keeps the slot keys grouped together on keyboard and avoids conflicting with the current `1` and `2` usage.

## Unlocking: What Already Exists

The player ability files already expose a future unlock seam:

* `PlayerAbilitySO.IsUnlocked(PlayerAbilityContext context)`

That is good.

But the actual project-wide persistent unlock system currently lives elsewhere:

* `GameSessionSO` holds `PlayerSkillTracker`
* `PlayerSkillTracker` stores unlocked skill IDs and SP
* `SkillManager` and the skills UI already talk to that system

So the project already has a broader progression source of truth.

## What Belongs To Player Code

The player-side ability code should own:

* what abilities are equipped into active slots
* how slot input triggers the equipped ability
* whether an equipped ability can currently execute
* whether an active ability blocks bow draw
* how hold, press, and release behaviors are handled per equipped ability

This is definitely player-side work.

## What Probably Should Not Be Owned By Player Code Alone

The player-side ability code should not become the long-term owner of:

* purchased or unlocked progression state
* skill tree progression
* SP costs
* shop or upgrade UI logic
* save/load authority for unlock ownership

Those already have a stronger home in the broader progression side of the project through `GameSessionSO`, `PlayerSkillTracker`, and the skills UI flow.

So the clean split is:

* player code asks "is this ability unlocked?"
* progression code decides and persists the answer

## Recommendation For Unlocking

When locking gets introduced later, the cleanest path is:

1. keep `PlayerAbilitySO.IsUnlocked(...)`
2. give each ability a stable ID
3. optionally give each ability a required skill ID
4. let `IsUnlocked(...)` query the real progression source

In practice that likely means adding fields like:

* `abilityID`
* `requiredSkillID`

Then:

* empty `requiredSkillID` means unlocked by default
* non-empty `requiredSkillID` means check `GameSessionSO.PlayerSkills.HasSkill(requiredSkillID)`

That keeps the player system flexible without duplicating a second unlock database inside the bow scripts.

## Important Observation About The Current Code

`RambowBowConfig` already has an `IsUnlocked(...)` override, but it currently just returns `true`.

That is a good reminder that the system hook exists, but the real progression bridge is still missing.

So yes, some of this is your side of the coin, but not all of it.

### Your Side

* slot execution
* input routing
* equipped ability handling
* local gating through `IsUnlocked(...)`

### Teammate / Broader Systems Side

* persistence of unlocked abilities
* skills screen integration
* SP cost and purchase flow
* save/load ownership of unlock state

## Recommendation For Scaling Past Two Abilities

Do not map one input button per ability once the roster gets bigger.

The clean model is:

* many unlockable abilities
* a small number of equipped active slots
* input triggers slots, not individual abilities

That means:

* player may own 10 or 15 abilities
* only 5 are equipped at a time
* keyboard `Q`, `R`, `Z`, `X`, `C` trigger the equipped slots

This is the standard scalable model and matches what you are already thinking.

## Recommended Future Structure

The next stable architecture should be:

### Ability Library

All ability definitions that can exist for the player.

This is the full roster.

### Unlock State

A persistent progression source that says which ability IDs are owned.

This should likely piggyback on the existing skill system rather than inventing a separate one unless design explicitly wants abilities and skills to be unrelated.

### Equipped Slots

A player-owned list of active ability slots such as:

* slot 1
* slot 2
* slot 3
* slot 4
* slot 5

Each slot references one equipped `PlayerAbilitySO` or one equipped ability ID.

### Input

Input should call:

* activate slot 1
* activate slot 2
* activate slot 3
* activate slot 4
* activate slot 5

Not:

* cast MultiShot
* cast Rambow
* cast SomeFutureAbility

That decouples input from the actual roster size.

## Recommended Change On The Player Side

When you decide to scale this, the `PlayerAbilityController` should evolve from:

* `_ability1`
* `_ability2`

to something more like:

* `AbilitySlot[] _equippedSlots`

or:

* a serialized list of equipped slots

That controller would then expose logic like:

* `TryActivateSlot(int slotIndex)`
* `TryReleaseSlot(int slotIndex)`

This is much easier to scale than adding `_ability3`, `_ability4`, `_ability5`, and so on.

## One More Important Design Detail

Not every ability has the same activation style.

Right now:

* `MultiShot` behaves like a press
* `Rambow` behaves like a hold

That means the future slot system should not assume every slot is "press only."

A safe direction is:

* slots forward both press and release
* each ability decides what it cares about

That matches the current runtime model well and avoids special casing slot input later.

## Input Migration Notes

The planned slot key layout should be:

* slot 1 -> `Q`
* slot 2 -> `R`
* slot 3 -> `Z`
* slot 4 -> `X`
* slot 5 -> `C`

This keeps:

* `Q` and `R` as the existing first two ability keys
* `Z`, `X`, and `C` as the natural expansion keys
* `1` and `2` free for the current `Previous` and `Next` bindings

If the slot-input refactor happens later, the current input stack must be updated in all of these places:

* `Assets/InputSystem_Actions.inputactions`
* `Assets/Scripts/Player/Player/Input/InputSystem_Actions.cs`
* `Assets/Scripts/ScriptableObjects/PlayerInputReaderSO.cs`
* `Assets/Scripts/Input/InputManager.cs`
* `PlayerAbilityController`

This matters because the project already relies on the generated input wrapper and the repo conventions require the JSON asset and generated C# file to stay in sync.

## Suggested Migration Path

When it is time to implement this for real, the clean order is:

1. add stable ability IDs and optional required skill IDs to `PlayerAbilitySO`
2. decide whether abilities unlock through `PlayerSkillTracker` or a parallel progression source
3. change `PlayerAbilityController` from fixed `_ability1/_ability2` fields to equipped slot data
4. change input from named abilities to named slots
5. remap the slot actions to `Q`, `R`, `Z`, `X`, and `C`
6. keep `1` and `2` untouched for `Previous` and `Next`
7. add UI later for equipping unlocked abilities into slots

## Recommendation Right Now

For the current project state, the best direction is:

* keep all current abilities unlocked by default
* do not build purchase UI into the player scripts
* plan around 5 equipped ability slots on `Q`, `R`, `Z`, `X`, and `C`, not unlimited direct keybinds
* use the existing `IsUnlocked(...)` hook as the player-side query point
* use the existing skill/progression system as the likely source of truth later

That gives the player code a clean job without making it responsible for the whole progression stack.
