# Blood Mage

This document is the working implementation reference for the Blood Mage enemy.
It is not a changelog.

## Overview

The Blood Mage is a summoned-only phase-two support minion for the Necromancer in its current form.

- It is not a standalone world enemy yet.
- It exists to complete the Necromancer summon loop.
- Its main jobs are:
  - apply ranged pressure to the player
  - keep the Necromancer's summon protection active while alive
  - create a meaningful add-clear phase for the player

## Current Combat Model

- summoned only by the Necromancer
- summons in a group of `3`
- spawns in an even ring around the Necromancer
- can optionally appear through a separate spawn-effect phase before the real Blood Mage becomes active
- uses a targeted ground bubble spell
- uses `Attack_01` only
- stays fully command-leashed to the Necromancer
- guards formation positions around the Necromancer while the Necromancer is not actively commanding combat
- performs light kiting only while the Necromancer is actively commanding combat
- registers itself back to the Necromancer on spawn
- unregisters itself on death/despawn
- despawns on owner human return or owner death

### Role

The Blood Mage is a ranged support minion.

- it should pressure the player
- it should not overshadow the Necromancer
- it should be dangerous enough that the player wants to kill it quickly

### Movement

- it is fully leashed to the Necromancer
- it does not freely chase the player on its own when the Necromancer is out of attack range
- when the Necromancer is not commanding combat, it returns to and holds a bodyguard slot around the Necromancer
- when the Necromancer is commanding combat, it can attack, kite, and reposition while still hovering around the Necromancer
- it uses leash and guard hysteresis so it does not flicker between tiny position corrections every frame

### Attack

- first-pass attack is a targeted blood bubble/pool placed at the player's feet
- use `Attack_01` only
- `Attack_02` and `Attack_03` are reserved for future expansion
- the bubble targets the player's current position with a small randomized placement radius for a more natural look
- the pop only damages the player if they are still inside the bubble when the pop event fires
- the damage path stays consistent with direct enemy-to-player damage flow

## Spawn And Owner Contract

### Summon Contract

When the Necromancer uses `Summon`:

- it spawns `3` Blood Mages
- it can first create a temporary `BloodMageSpawnEffect` at each summon point
- the spawn effect plays `BloodMage_Spawn` and only creates the real Blood Mage when the spawn animation completes
- the Blood Mages appear in a readable even ring around the Necromancer
- each Blood Mage receives an owner reference to the Necromancer
- each Blood Mage receives summon slot / group information for guard positioning
- each Blood Mage registers itself with the Necromancer after a successful spawn/initialize
- summon uses pooled enemy spawn first, then instantiate fallback if no pool is available

### Protection Contract

- Necromancer summon protection becomes real on the first Blood Mage registration
- summon protection remains active while at least one registered Blood Mage is alive
- when the final Blood Mage is removed, summon protection ends
- the Necromancer summon cooldown begins when summon protection ends, not when `Summon` is cast
- if the Necromancer returns to human form, active Blood Mages despawn
- if the Necromancer dies, active Blood Mages despawn

## Animation And Controller Contract

### Active Parameters

- `DirectionX`
- `DirectionY`
- `IsMoving`
- `Attack`
- `Dead`

### Active States

- `BloodMage_Idle`
- `BloodMage_Run_BT`
- `BloodMage_Attack_Bubble`
- `BloodMage_Dead`

### Current Asset Set

- `BloodMage_Idle.anim`
- `BloodMage_Run_Up.anim`
- `BloodMage_Run_Down.anim`
- `BloodMage_Run_Left.anim`
- `BloodMage_Run_Right.anim`
- `BloodMage_Attack_Bubble.anim`
- `BloodMage_Bubble.anim`
- `BloodMage_Dead.anim`
- `Summon.anim`

Reserved for later:

- `BloodMage_Attack_02.anim`
- `BloodMage_Attack_03.anim`
- `BloodMage_Hurt.anim`

### Required Animation Events

- `BloodMage_Attack_Bubble.anim`
  - `Anim_AttackHit()`
  - `Anim_AttackFinished()`
- `BloodMage_Bubble.anim`
  - `Anim_AttackHit()` or `Anim_Pop()`
  - `Anim_AttackFinished()` or `Anim_Finished()`
- `BloodMage_Dead.anim`
  - `Anim_Despawn()`
- `Summon.anim`
  - `Anim_SpawnComplete()`
  - `Anim_AttackFinished()` or `Anim_Finished()`

## Prefab And Script Structure

### Main Prefab

[BloodMage.prefab](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/BloodMage.prefab)

Expected child structure:

- `Animator`
- `AttackRange`
- `CastPoint`

Current first-pass prefab wiring:

- `BloodMage` component on the root
- `GridPathAgent` on the root for leash/chase pathing around blockers
- `Animator` + `EnemyAnimationEventRelay` on `Animator`
- optional `SpriteRevealDriver` on `Animator` for live-body summon reveal
- `CircleCollider2D` + `EnemyStrikingDistanceCheck` on `AttackRange`
- SO assets assigned for idle, chase, attack, and dead

### Bubble Spell Prefab

[BloodBubble.prefab](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/BloodBubble.prefab)

- spawned by `BloodMageAttackSO`
- placed at the player's current position plus optional offset
- uses its own animation timing to pop and despawn
- can use `GameplayPoolManager` projectile spawning

### Spawn Effect Prefab

Recommended separate summon-only prefab:

- `SpriteRenderer`
- `Animator`
- `BloodMageSpawnEffect`

Recommended flow:

- uses `Summon.anim` through the summon-only controller
- owns any bottom-to-top shader reveal
- fires `Anim_SpawnComplete()` or `Anim_AttackFinished()` at the end
- spawns the real Blood Mage only after the reveal completes
- is best registered in `GameplayPoolConfiguration` under `Projectile Pools` so the spawn effect can reuse active/inactive instances

### Main Scripts

- [BloodMage.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/BloodMage.cs)
  - shared runtime state
  - owner reference
  - registration/unregistration hooks
  - leash helpers
  - formation-slot helpers
  - spawned spell ignore handling for both Blood Mage and Necromancer colliders
- [BloodMageIdleSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/Behavior/Idle/BloodMageIdleSO.cs)
  - summoned settle / idle behavior
- [BloodMageChaseSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/Behavior/Chase/BloodMageChaseSO.cs)
  - owner-commanded guard / combat behavior
  - leash-to-owner behavior
  - guard-anchor formation holding
  - light kiting while combat is commanded
- [BloodMageAttackSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/Behavior/Attack/BloodMageAttackSO.cs)
  - attack cooldown
  - bubble spell spawn / spell fire
- [BloodMageBubbleSpell.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/BloodMageBubbleSpell.cs)
  - pooled ground bubble that pops under the player
- [BloodMageSpawnEffect.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/BloodMageSpawnEffect.cs)
  - temporary summon visual that plays the spawn animation and then creates the real Blood Mage
- [SpriteRevealDriver.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/SpriteRevealDriver.cs)
  - per-instance shader-property driver for live-body summon reveal
- [BloodMageDeadSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/BloodMage/Behavior/Dead/BloodMageDeadSO.cs)
  - unregister from owner
  - death / despawn flow

## Lifecycle Rules

### On Spawn

- receive Necromancer owner reference
- receive player target reference
- spawn into the summon ring
- register to the Necromancer after successful initialization
- enter its normal summoned combat loop

### On Death / Despawn

- unregister from the Necromancer exactly once
- registration is cleared as soon as the Blood Mage enters its death flow, not only after final despawn
- stop affecting summon protection after unregister
- play death and despawn cleanly
- avoid duplicate unregisters during pooled despawn/reset

## Tuning Surfaces

### Blood Mage

- `AttackDamage`
- `MovementSpeed`
- `spawnRevealDuration`
- owner reference at runtime
- summon ring index / group size at runtime

### Chase SO

- light-kite retreat threshold
- leash radius around the Necromancer
- leash hysteresis
- guard radius around the Necromancer
- guard position tolerance
- guard position hysteresis
- guard movement speed
- guard anchor start angle
- leash return strength / movement speed

### Attack SO

- cast cooldown
- bubble spell prefab
- bubble target offset
- random target radius
- bubble damage multiplier
- bubble knockback

## What Needs To Be Done

- tune bubble timing, radius, damage, and leash behavior in play mode
- validate the full Necromancer phase-two loop with multiple summon cycles
- add clearer hit / pop / spawn feedback if needed
- decide whether Blood Mage should eventually support standalone world spawning

## Future Ideas

- use `Attack_02` and `Attack_03` for later Blood Mage variants
- expand Blood Mage into a standalone world enemy later
- add blood-themed VFX using the available blood-bubble art
- give Blood Mage more distinct support or debuff behavior
- let Blood Mages participate in richer phase-two formations or synchronized casts

## Not Implemented Yet

- Blood Mage VFX/readability pass
- standalone/world-enemy Blood Mage behavior
- additional Blood Mage attack variants
