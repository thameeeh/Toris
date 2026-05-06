# Deer

This document is the working implementation reference for the Deer passive creature.
It is not a changelog.

## Current Status

The Deer is partially implemented and now follows the existing enemy navigation direction.

It currently has:

- an `Enemy`-based runtime class
- idle, walk, run-away, and dead state wrappers
- idle, walk, run-away, and dead ScriptableObject behaviors
- a working directional animator contract
- passive-prey targeting flags
- fear memory so it does not instantly calm down on trigger exit
- first-pass `GridPathAgent` movement for walk and flee
- first-pass walkable target selection through `TileNavWorld`
- fallback death behavior because there is no deer death animation yet

It still needs play-mode validation before broad procedural spawning.

## Correct Architecture Direction

The Deer should continue to follow the existing enemy architecture.

Use:

- `Enemy` as the base class
- deer-specific state wrappers
- deer-specific behavior SOs
- `EnemyAggroCheck` for scare detection
- `GridPathAgent` for movement through procedural navigation
- `TileNavWorld` for walkability checks
- `TilePathfinder` indirectly through `GridPathAgent`

The closest reference is Wolf, especially:

- `WolfIdleSO`
- `WolfChaseSO`
- `WolfReturnHomeSO`
- `GridPathAgent`
- `TilePathfinder`
- `TileNavWorld`

Do not create a separate passive-creature navigation stack unless there is a strong reason later.

## Existing Navigation Stack

### TileNavWorld

`TileNavWorld` owns the procedural navigation data.

Important API:

- `IsWalkableWorldPos(Vector3 worldPos)`
- `IsWalkableCell(Vector2Int worldCell)`
- `WorldToCell(Vector3 worldPos)`
- `CellToWorldCenter(Vector2Int cell)`

### TilePathfinder

`TilePathfinder` performs A* pathfinding over `TileNavWorld`.

It rejects paths when:

- start cell is not walkable
- goal cell is not walkable
- no valid path exists in range
- diagonal movement would cut blocked corners

### GridPathAgent

`GridPathAgent` is the enemy-facing movement helper.

It:

- caches paths
- repaths on interval
- repaths when target moves enough
- returns a movement direction toward the next waypoint
- can stop when no path exists
- can optionally direct-fallback when no path exists

## Deer Navigation Implementation

`DeerWalkSO` currently:

- samples candidate wander points around the deer
- rejects non-walkable points through `TileNavWorld`
- falls back to the nearest walkable cell when needed
- moves through `GridPathAgent.GetMoveDirection(targetPosition)`
- direct-fallbacks only when `TileNavWorld` is unavailable

`DeerRunAwaySO` currently:

- computes a flee direction away from the current or last known threat
- samples candidate flee points in that general direction
- rejects non-walkable flee points through `TileNavWorld`
- falls back to the nearest walkable cell when needed
- moves through `GridPathAgent.GetMoveDirection(fleeTarget)`
- direct-fallbacks only when `TileNavWorld` is unavailable

This should prevent the old behavior where deer chose arbitrary points and pushed into blocked terrain.

## Prefab Setup Contract

### Root

`Assets/Scripts/Enemy/Animations/Deer/Deer.prefab`

Expected root setup:

- `Deer`
- `Rigidbody2D`
- root body collider
- `GridPathAgent`
- `Tag = Enemy`
- enemy root layer matching Wolf
- `Is Passive Prey = true`
- `Threatens Passive Creatures = false`
- Deer idle/walk/run-away/dead SOs assigned

Important `GridPathAgent` field:

- `Allow Direct Fallback When No Path`

For procedural correctness, this should stay off at first.
If it is on, Deer can still push directly into invalid terrain when no path exists.

### Children

Expected children:

- `Animator`
- `AggroCheck`

The Deer does not need:

- `StrikingDist`
- attack state
- attack SO
- return-home state unless we decide deer need an explicit territory return state

### AggroCheck

`AggroCheck` should have:

- trigger collider
- `EnemyAggroCheck`
- `Detect Player = true`
- `Detect Passive Prey = false`
- `Detect Threats To Passive Creatures = true`

This lets deer flee from:

- the player
- wolves
- any future hostile creature marked as threatening passive creatures

## Animator Contract

### Parameters

- `DirectionX`
- `DirectionY`

### States

- `Idle BT`
- `Walk BT`
- `Run BT`

The Deer code directly plays these states by name.
Transitions are not required for the current first pass.

### Art Available

Currently available:

- idle
- walk
- run

Missing:

- death animation
- hurt animation
- grazing animation
- startled animation

## Wolf Predator Contract

Wolf prefabs should have:

- `Threatens Passive Creatures = true`
- `AggroCheck -> Detect Passive Prey = true`
- `StrikingDist -> Detect Passive Prey = true`

This lets wolves:

- detect deer
- choose deer as an aggro target
- path/chase toward deer through `WolfChaseSO`
- damage deer through the shared aggro-target damage path

## HomeAnchor Decision

Wolf uses `HomeAnchor` for den territory.

Deer may also need a territory concept, but it does not need to be a wolf den.

Possible options:

- add `HomeAnchor` to the Deer prefab at spawn time or prefab time
- initialize the home center from the spawn position
- use home radius for ambient wandering
- keep deer from wandering forever across the map

If we do this, deer idle/walk target selection should use `HomeAnchor.Center` and `HomeAnchor.Radius`, similar to Wolf.

## Spawning Direction

Do not add Deer broadly to procedural spawning until navigation has been validated in generated terrain.

Good first spawn areas:

- open forest/grass clearings
- not directly inside monster sites
- not directly on roads unless intentionally scenic
- not inside water/lake tiles
- not inside reserved site footprints

Spawn density should be low at first.

The goal is wildlife presence, not enemy clutter.

## Tuning Surfaces

### Deer Component

- `MaxHealth`
- `Walk Speed`
- `Run Speed`
- `Minimum Flee Duration`
- `Calm Delay After Threat Lost`
- passive-prey/threat flags

### DeerWalkSO

- wander radius
- destination tolerance
- max candidate checks
- nearest-walkable search radius
- min target distance from current position
- direction lerp speed

### DeerRunAwaySO

- retarget interval
- flee target distance
- target tolerance
- max candidate checks
- flee angle spread
- nearest-walkable fallback radius
- min flee distance multiplier
- direction lerp speed

### GridPathAgent

- repath interval
- max path range
- waypoint reach threshold
- target change threshold
- direct fallback setting

### AggroCheck

- scare radius
- player detection
- threat detection

## What Needs To Be Done

- verify deer wander without pushing into blocked terrain
- verify deer flee from player without pushing into blocked terrain
- verify deer flee from wolves without pushing into blocked terrain
- verify wolves can chase and damage deer
- decide whether Deer should use `HomeAnchor` for local roaming territory
- tune fear, run speed, wander radius, and aggro radius
- add procedural spawn registration after movement is navigation-safe

## Future Ideas

- grazing idle variant
- startled hop / flinch animation
- herd behavior
- deer flee from nearby combat noise
- wolves prefer isolated deer
- deer as subtle signposting for wolf territory
- rare white stag variant for future mystery or quest content

## Not Implemented Yet

- Deer spawn registration
- death animation
- loot/drop table
- herd behavior
