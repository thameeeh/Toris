# Deer Passive Mob Setup

This document is the implementation reference for the Deer passive creature.

The Deer uses the shared enemy FSM because that FSM is the project's general AI brain. Deer does not need every combat state. Its behavior is intentionally small:

- `IdleState`: stand and wait.
- `WalkState`: ambient wandering.
- `RunAwayState`: flee from known threats. This is the passive equivalent of a chase state.
- `DeadState`: temporary fallback death fade until real death art exists.

The Deer has no attack state and should not damage the player.

## Architecture

### Runtime Class

`Deer.cs` inherits from `Enemy` and owns Deer-specific runtime state:

- movement speeds
- fear timers
- last known threat position
- animator state names
- deer-specific behavior SO instances
- fallback death fade

The serialized behavior assets are required. If any Deer SO reference is missing, `Deer` logs a setup error and disables itself instead of throwing a null reference during startup.

### State Wrappers

The files in `Enemy Types/Deer/Deer States/` are thin wrappers. They should stay boring. Their job is only to call the matching behavior SO:

- `DeerIdleState` -> `DeerIdleSO`
- `DeerWalkState` -> `DeerWalkSO`
- `DeerRunAwayState` -> `DeerRunAwaySO`
- `DeerDeadState` -> `DeerDeadSO`

Do not put movement math or target selection in the wrappers.

### Behavior ScriptableObjects

The behavior SOs are where Deer decisions live.

`DeerIdleSO`:

- stops movement
- plays idle
- transitions to walk after a random idle duration
- transitions to run-away when scared

`DeerWalkSO`:

- chooses a reachable wander target
- uses `GridPathAgent` for movement
- returns to idle if the target is reached or no valid path can be produced

`DeerRunAwaySO`:

- chooses a reachable flee target away from the current or last known threat
- retargets while fear is active
- uses `GridPathAgent` for movement
- falls back to random flee only when there is no known threat

`DeerDeadSO`:

- starts fallback death fade and despawn
- should be replaced or extended when Deer death animation exists

## Fear Model

The Deer flees from:

- the player, through `EnemyAggroCheck`
- enemies marked as `Threatens Passive Creatures`
- damage events

When a threat is known, Deer stores the threat position and flees away from it. If damage occurs without an active aggro target, Deer uses the scene player as the fallback threat when available. If no threat can be resolved, `DeerRunAwaySO` picks a random flee direction.

Fear does not clear instantly when a trigger exits. `minimumFleeDuration` and `calmDelayAfterThreatLost` keep the Deer moving long enough to look intentional.

## Navigation Rules

Deer movement should not pick points just because they are walkable. A point must also be reachable from the Deer's current walkable cell.

`DeerWalkSO` and `DeerRunAwaySO` both:

- sample candidate positions
- reject non-walkable positions through `TileNavWorld`
- reject unreachable positions through `TilePathfinder`
- use `GridPathAgent.GetMoveDirection(...)` for actual steering
- avoid direct fallback while `TileNavWorld` exists

If `TileNavWorld` is missing, Deer falls back to direct movement so prefab test scenes without generated navigation still work.

If Deer starts off navigation, reachable path checks will fail. Treat that as a spawn/setup problem: the Deer should be placed or spawned on a walkable tile.

## Prefab Contract

Prefab:

`Assets/Scripts/Enemy/Animations/Deer/Deer.prefab`

Root components:

- `Deer`
- `Rigidbody2D`
- body collider
- `GridPathAgent`
- `EnemyHealthBar` if desired
- `EnemyAlertIndicator` if desired

Root flags:

- `Is Passive Prey = true`
- `Threatens Passive Creatures = false`

Required Deer SO references:

- `EnemyIdleBase` -> `Deer_Idle.asset`
- `EnemyWalkBase` -> `Deer_Walk.asset`
- `EnemyRunAwayBase` -> `Deer_RunAway.asset`
- `EnemyDeadBase` -> `Deer_Dead.asset`

`GridPathAgent`:

- `Allow Direct Fallback When No Path = false` for procedural scenes
- keep direct fallback off unless you are in a non-nav test scene

Children:

- `Animator`
- `AggroCheck`

The Deer does not need:

- `StrikingDist`
- attack SO
- attack animation events
- return-home state

## AggroCheck Contract

`AggroCheck` should have a trigger collider and `EnemyAggroCheck`.

Recommended values:

- `Detect Player = true`
- `Detect Passive Prey = false`
- `Detect Threats To Passive Creatures = true`

This makes Deer flee from the player and from predators such as wolves.

## Predator Contract

Predators that should scare or attack Deer must be configured separately.

For wolves:

- root `Threatens Passive Creatures = true`
- `AggroCheck -> Detect Passive Prey = true`
- `StrikingDist -> Detect Passive Prey = true`

This lets wolves target Deer through the shared aggro-target path instead of special-case Deer logic.

## Map Generation Integration

Deer can be spawned by the map generation wildlife lane.

Runtime flow:

- `WildlifeSpawnBuildStepDefinition` samples valid wildlife tiles when a biome is built.
- `WorldBuildOutput.WildlifeSpawns` stores those placements by chunk.
- `WorldWildlifeLifecycle` spawns the configured enemy prefab when that chunk loads.
- Chunk unload calls `RequestDespawn()` on owned wildlife so the existing `GameplayPoolManager` can reclaim the enemy.

Unity setup:

- Create a `WorldGen/Wildlife/Wildlife Spawn Definition` asset.
- Assign the Deer prefab to `Enemy Prefab`.
- Create a `WorldGen/Biomes/Build Steps/Wildlife Spawn Step` asset.
- Add one wildlife rule pointing at the Deer spawn definition.
- Put the wildlife build step into the biome definition after site and road placement steps so Deer avoids generated roads, stamps, blockers, and obstacle tiles.
- Add the Deer prefab to the `GameplayPoolConfiguration` enemy pools if you want prewarming or custom pool sizing.

Recommended starting values:

- `Min Spawn Count = 4`
- `Max Spawn Count = 8`
- `Min Spacing Tiles = 18`
- `Placement Radius Factor = 0.9`
- `Avoid Origin Radius Tiles = 18`
- `Avoid Terrain Overrides = true`
- `Avoid Navigation Blockers = true`
- `Avoid Obstacles = true`

## Animator Contract

Parameters:

- `DirectionX`
- `DirectionY`

State names used by `Deer.cs`:

- `Idle BT`
- `Walk BT`
- `Run BT`

The code directly plays these state names. If the controller state names change, update the serialized names on the Deer component.

Missing art:

- death animation
- hurt animation
- grazing animation
- startled animation

## Tuning

Deer component:

- `Walk Speed`
- `Run Speed`
- `Minimum Flee Duration`
- `Calm Delay After Threat Lost`

`DeerWalkSO`:

- `Wander Radius`
- `Destination Tolerance`
- `Max Candidate Checks`
- `Max Candidate Path Range`
- `Nearest Walkable Search Radius`
- `Min Target Distance From Current`
- `Direction Lerp Speed`

`DeerRunAwaySO`:

- `Retarget Interval`
- `Flee Target Distance`
- `Target Tolerance`
- `Max Candidate Checks`
- `Max Candidate Path Range`
- `Nearest Walkable Search Radius`
- `Flee Angle Spread`
- `Min Flee Distance Multiplier`
- `Direction Lerp Speed`

## Unity Verification

Validate in a generated scene with `TileNavWorld` active:

- Deer idles, then walks to reachable nearby points.
- Deer does not push into blocked tiles while wandering.
- Deer flees from the player.
- Deer flees from wolves or other `Threatens Passive Creatures` enemies.
- Deer keeps fleeing briefly after the threat leaves the trigger.
- Deer does not stall forever on an unreachable target.
- Deer stops safely if spawned off-nav, then fix the spawn placement.
- Wolf can chase and damage Deer when wolf predator flags are enabled.
- Deer fades and despawns on death until real death animation exists.
- Procedurally spawned Deer appear only on walkable generated land.
- Procedurally spawned Deer despawn when their chunk unloads.

Also validate setup failure:

- Temporarily clear one Deer SO reference on a test prefab instance.
- Enter play mode.
- Confirm the Deer logs a clear setup error instead of throwing a null reference.

## Future Work

- Add a real death animation and replace fallback fade.
- Add grazing or look-around idle variants.
- Decide whether Deer needs a spawn-home radius or should simply roam locally.
- Add loot only if Deer hunting should be part of the economy.
