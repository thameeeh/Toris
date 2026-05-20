# Boar Wildlife Hazard Setup

This document is the working implementation reference for the Boar enemy.
It is not a changelog.

## Overview

The Boar is a light wildlife danger for early biomes.

It is not meant to fight like a wolf. Its behavior should read as:

- idle or roam normally
- notice the player at close range
- aim for a short moment
- charge through the player
- continue running away along the same charge line
- eventually calm down and return to ambient roaming

The Boar should feel territorial, startled, and physical, not tactical.

## Current Behavior Model

### Role

- wildlife hazard
- short commitment attack
- no sustained combat loop
- no pack or den logic
- no animation-event driven attack timing

The Boar does not chase the player after hitting them. It commits to a line and leaves.

### State Flow

Runtime flow:

- `IdleState`
  - stands still
  - watches for player aggro
  - transitions to wander after a random idle duration
- `WanderState`
  - picks a reachable local wander target
  - uses `GridPathAgent` for steering
  - transitions to idle when the target is reached
- `ChargeState`
  - enters when the player is detected
  - aims for `aimDuration`
  - locks a charge direction at the end of the aim pause
  - runs toward a point beyond the player
  - applies damage once if the player is inside `StrikingDistanceCheck`
- `FleeState`
  - inherits the locked charge direction
  - keeps moving in that same direction
  - slows down compared to the charge
  - ignores re-startle for a short window
- `DeadState`
  - stops movement
  - fades the sprite renderers
  - despawns after the fade

The important design detail is that flee does not recompute "away from player" after impact. It continues the boar's momentum.

## Architecture

### Runtime Class

[Boar.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Boar/Boar.cs)

`Boar` inherits from `Enemy` and owns:

- charge, wander, and flee speeds
- charge damage and knockback
- animator parameter updates
- last threat position
- last committed charge direction
- behavior SO instance creation
- spawn/despawn runtime reset

The serialized behavior assets are required. If any Boar SO reference is missing, `Boar` logs an editor setup error and disables itself instead of running into null behavior.

### State Wrappers

Files in `Enemy Types/Boar/Boar States/` are thin wrappers:

- `BoarIdleState` -> `BoarIdleSO`
- `BoarWanderState` -> `BoarWanderSO`
- `BoarChargeState` -> `BoarChargeSO`
- `BoarFleeState` -> `BoarFleeSO`
- `BoarDeadState` -> `BoarDeadSO`

Keep movement math and target selection inside behavior SOs, not inside wrappers.

### Behavior ScriptableObjects

[BoarIdleSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Boar/Boar%20Behaviour/BoarIdleSO.cs)

- stops movement
- idles for a random duration
- starts charge when the player enters aggro range
- transitions to wander when idle time ends

[BoarWanderSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Boar/Boar%20Behaviour/BoarWanderSO.cs)

- chooses a local wander target
- rejects non-walkable or unreachable targets when `TileNavWorld` exists
- uses `GridPathAgent.GetMoveDirection(...)`
- falls back to direct movement only when no `TileNavWorld` exists

[BoarChargeSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Boar/Boar%20Behaviour/BoarChargeSO.cs)

- handles aim pause
- tracks the player while aiming
- locks the final charge direction
- picks a point past the player using `runThroughDistance`
- moves at `Boar.ChargeSpeed`
- applies one damage event while in striking distance
- records the committed charge direction for flee

[BoarFleeSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Boar/Boar%20Behaviour/BoarFleeSO.cs)

- reads the last charge direction from `Boar`
- selects flee targets along that direction
- starts near `Boar.ChargeSpeed` and eases down to `Boar.FleeSpeed`
- keeps movement going for `minimumFleeDuration`
- prevents instant re-trigger through `postFleeChargeIgnoreDuration`

[BoarDeadSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Boar/Boar%20Behaviour/BoarDeadSO.cs)

- stops movement
- starts the fallback death fade
- despawns after the fade and short delay

## Charge Model

The charge is a three-part behavior:

1. Aim

   The boar has detected the player but does not move yet. It faces the target and idles briefly.

2. Commit

   At the end of the aim pause, the boar locks its current direction toward the player. This direction should not keep tracking the player during the run.

3. Run Through

   The target point is the player position plus `runThroughDistance` in the locked direction. After the charge ends, flee continues in that same direction.

This avoids the bad behavior where the boar hits the player and then turns around because flee recomputed an "away from player" vector.

## Navigation Rules

Boar wandering and fleeing should respect navigation:

- use `TileNavWorld` to reject unwalkable points
- use `TilePathfinder` to reject unreachable points
- use `GridPathAgent` for steering
- avoid direct fallback while `TileNavWorld` exists

If `TileNavWorld` is missing, direct movement is allowed so small test scenes can still validate the prefab.

If the Boar spawns on an invalid tile, path checks can fail. Treat that as a spawn/setup problem.

## Animator Contract

Controller:

[Boar.controller](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Animations/Boar/Anims/Boar.controller)

Active parameters:

- `DirectionX`
- `DirectionY`
- `IsMoving`

Active states:

- `Idle_BT`
- `Run_BT`

Current clips:

- `Boar_Idle_NE.anim`
- `Boar_Idle_NW.anim`
- `Boar_Idle_SE.anim`
- `Boar_Idle_SW.anim`
- `Boar_Run_NE.anim`
- `Boar_Run_NW.anim`
- `Boar_Run_SE.anim`
- `Boar_Run_SW.anim`

The Boar currently does not require animation events.

Important prefab binding rule:

- The `Animator` and `SpriteRenderer` must be on the same GameObject.

The current clips animate `SpriteRenderer.m_Sprite` with an empty binding path. If the `Animator` is placed on a child and the `SpriteRenderer` is placed on the parent, the boar will move but remain stuck on one sprite frame.

## Prefab Contract

Prefab:

[Boar.prefab](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Boar/Boar.prefab)

Expected hierarchy:

- `Boar`
  - `Visual`
  - `AggroCheck`
  - `StrikingDistanceCheck`
  - `WorldCollision`

Root components:

- `Boar`
- `Rigidbody2D`
- trigger hurtbox collider
- `GridPathAgent`
- optional `EnemyHealthBar`
- optional `EnemyAlertIndicator`

`Visual`:

- `SpriteRenderer`
- `Animator`

The `Animator` should use `Boar.controller`.

`AggroCheck`:

- trigger collider
- `EnemyAggroCheck`
- `Detect Player = true`
- `Detect Passive Prey = false`
- `Detect Threats To Passive Creatures = false`

`StrikingDistanceCheck`:

- trigger collider
- `EnemyStrikingDistanceCheck`
- `Detect Player = true`
- `Detect Passive Prey = false`

`WorldCollision`:

- non-trigger collider
- used for physical collision footprint

Required Boar SO references:

- `BoarIdleBase` -> `Boar_Idle_Stand.asset`
- `BoarWanderBase` -> `Boar_Wander_Roam.asset`
- `BoarChargeBase` -> `Boar_Charge_Run.asset`
- `BoarFleeBase` -> `Boar_Flee_AfterCharge.asset`
- `BoarDeadBase` -> `Boar_Dead_Despawn.asset`

Suggested layers:

- root `Boar`: `EnemyHurtBox`
- `Visual`: `EnemyHurtBox`
- `AggroCheck`: `EnemyAggro`
- `StrikingDistanceCheck`: `EnemyStrikingbox`
- `WorldCollision`: `EnemyItselfCollision`

## Current Tuning

Runtime values on `Boar`:

- `MaxHealth = 100`
- `ChargeDamage = 12`
- `WanderSpeed = 1.4`
- `ChargeSpeed = 3`
- `FleeSpeed = 2.2`
- `ChargeKnockback = 4`

`Boar_Charge_Run`:

- `aimDuration = 0.5`
- `chargeDuration = 2.5`
- `runThroughDistance = 2.5`
- `chargeTargetTolerance = 0.25`
- `chargeCooldown = 2.25`

`Boar_Flee_AfterCharge`:

- `minimumFleeDuration = 2`
- `fleeTargetDistance = 6`
- `retargetInterval = 0.75`
- `decelerationDuration = 0.75`
- `postFleeChargeIgnoreDuration = 1.5`

`Boar_Dead_Despawn`:

- `holdDuration = 0.15`
- `fadeDuration = 0.35`
- `despawnDelay = 0.1`

Recommended feel adjustments:

- If the boar starts too far away, lower `AggroCheck` radius.
- If the charge feels too slow, raise `ChargeSpeed` slightly.
- If the boar runs too far through the player, lower `runThroughDistance` or `chargeDuration`.
- If the post-charge slowdown feels too sudden, raise `decelerationDuration`.
- If the boar turns too much while escaping, lower `fleeAngleSpread`.
- If the boar re-triggers too soon, raise `postFleeChargeIgnoreDuration`.

## Known Intentional Omissions

- no real stun payload yet
- no dedicated death animation yet
- no boar-specific sound setup yet
- no attack animation events
- no group/herd logic
- no den or home-return logic

The current player hit pipeline supports damage and knockback. A real stun should be added deliberately through the player movement/status system if the Boar needs one later.
