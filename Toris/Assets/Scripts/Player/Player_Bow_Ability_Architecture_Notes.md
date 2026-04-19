# Player Bow Ability Architecture Standard

This document is the source of truth for bow ability architecture.

It is not a suggestion.
It defines the required structure for all current and future bow abilities.

If an implementation conflicts with this document, the implementation is wrong and should be refactored.

## Purpose
The goal of this standard is to prevent ability logic from drifting back into `PlayerBowController` and to keep each ability locally maintainable as it gains upgrades, variants, and more complex behavior.

`Rambow` is the reference shape.
Every other bow ability must align with the same architectural boundaries unless there is a very strong, explicit reason not to.

## Core Rule
`PlayerBowController` is a bow primitive provider, not an ability host.

That means:

- it may expose reusable aiming, spawning, and generic fire helpers
- it may expose shared bow-facing utilities used by multiple abilities
- it must not contain the orchestration logic for a specific ability

If a method or field exists only because one ability needs it, it probably does not belong in `PlayerBowController`.

## Required Ability Structure
Every bow ability must be built from these layers.

### 1. Ability Config
The config is responsible for:

- exposing tunable values
- interpreting button input
- validating cooldown, unlocks, and resource cost
- creating a per-cast settings snapshot
- starting or stopping runtime behavior

The config must not:

- own active per-cast state
- manage timed sequences directly
- contain long-running coroutines or delayed scheduling
- search targets every frame for an active cast

### 2. Ability Runtime
The runtime is responsible for:

- active state
- timers
- per-cast session state
- delayed actions
- target visit history
- active-session cleanup
- deciding when to call shared bow primitives

The runtime is where stateful behavior belongs.

If an ability has any of the following, it should have a custom runtime:

- repeated firing
- burst scheduling
- strike queues
- delayed bounces
- active duration
- visit tracking
- staged upgrades that alter cast behavior

### 3. Bow Controller
`PlayerBowController` is responsible for:

- aiming
- muzzle selection
- pointer world resolution
- building fully drawn shot stats
- resolving outgoing damage modifiers
- spawning arrows from aim
- spawning arrows from explicit world positions
- generic release-animation bridging
- generic shared hit-effect playback

The bow controller must not own:

- chain logic
- rain logic
- per-ability overlap damage logic
- per-ability coroutines
- per-ability target filtering rules beyond shared generic utilities
- per-ability visited-target bookkeeping

## Allowed Patterns
### Simple Cast Ability
Use this pattern when the ability is just a different one-shot bow command.

Flow:

1. config validates cast
2. config builds shot data
3. config calls a generic bow primitive
4. runtime can remain the base `PlayerAbilityRuntime`

Example:

- `MultiShot`

### Stateful Ability
Use this pattern when the ability has internal progression after activation.

Flow:

1. config validates cast
2. config builds a settings snapshot
3. config passes that snapshot into a custom runtime
4. runtime owns the cast from that point onward
5. runtime uses bow primitives to produce arrows/effects/results

Examples:

- `Rambow`
- `ArrowRain`
- `ChainShot`

## Ownership Rules
These are mandatory.

### Targeting
- Shared target classification rules belong in a shared utility
- Ability-specific target selection belongs in the ability runtime
- Enemy-specific exception lists should be avoided in favor of systemic rules like layer-based filtering

### Damage
- Generic outgoing damage scaling belongs in `PlayerBowController`
- Ability-specific damage distribution belongs in the ability config or runtime
- Example: `ChainShot` hit multipliers belong to `ChainShot`, not the bow controller

### Visuals
- Generic projectile spawning belongs in the bow controller
- Ability-specific visual timing belongs in the ability runtime
- Dedicated ability visuals should remain ability-owned and must not change the controller into a visual scheduler for that ability

### Timers And Sequencing
- Timed behavior belongs in runtimes
- Configs may start a timed behavior
- Configs must not become mini state machines
- The bow controller must not become a scheduler for ability timelines

## Hard Bans
The following are not allowed unless explicitly re-approved as architecture exceptions:

- adding methods to `PlayerBowController` named after a specific ability
- adding per-ability coroutine runners to `PlayerBowController`
- adding per-ability temporary target buffers to `PlayerBowController`
- adding per-ability session state to `PlayerBowController`
- solving enemy helper collider issues with one-off enemy-specific exceptions when a general target-layer rule can solve it
- implementing upgrades by branching `PlayerBowController` further for one ability

## Upgrade Rule
When an ability gets upgraded, the new behavior must be implemented in that ability's config, runtime, or dedicated helper types first.

It must not be implemented in `PlayerBowController` unless the upgrade genuinely creates a reusable primitive that multiple abilities need.

Example:

- future `ChainShot` pierce-plus-branch behavior must extend `ChainShotRuntime`
- it must not add branch logic into `PlayerBowController`

## When A New Bow Helper Is Allowed
A new helper may be added to `PlayerBowController` only if all of the following are true:

1. it is reusable by multiple abilities or by normal bow fire
2. it does not encode one ability's rules
3. it has no per-ability temporal state
4. it reads like a generic bow primitive, not an ability workflow step

Good examples:

- `SpawnArrowFromAim(...)`
- `SpawnArrowFromWorld(...)`
- `ResolveOutgoingDamage(...)`

Bad examples:

- `StartArrowRain(...)`
- `ContinueChainShot(...)`
- `QueueExplosiveVolleyWave(...)`

## Review Checklist
Before finishing any bow ability work, check these questions:

1. Does the bow controller now know anything specific about one named ability?
2. Did any timer, queue, burst loop, or visit history land outside a runtime?
3. Did the config stay focused on validation and cast setup?
4. Is the runtime clearly the owner of ongoing behavior?
5. Would a future upgrade to this ability be local to the ability code?
6. If another engineer opened `PlayerBowController`, would they see primitives instead of ability scripts?

If any answer is wrong, refactor before considering the work done.

## Current Project Application
### Rambow
Compliant reference implementation.

### MultiShot
Compliant simple-cast implementation.

### ArrowRain
Must remain runtime-owned.

Required ownership:

- targeting resolution in config
- timed bursts and pending strikes in runtime
- generic spawn/effect helpers in bow controller

### ChainShot
Must remain runtime-owned.

Required ownership:

- initial cast setup in config
- active chain sessions, visited targets, delayed bounces, and next-target selection in runtime
- generic projectile spawning in bow controller

## Final Standard
Bow abilities are local systems.

Configs decide whether a cast starts.
Runtimes own what happens after it starts.
`PlayerBowController` provides the reusable bow tools.

That boundary is the standard.
Do not drift from it.
