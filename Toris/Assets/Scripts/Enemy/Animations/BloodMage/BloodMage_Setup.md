# Blood Mage

This document is the working implementation reference for the Blood Mage enemy.
It is not a changelog.

## Overview

The Blood Mage is a summoned-only phase-two support minion for the Necromancer in its first pass.

- It is not a standalone world enemy yet.
- It exists to complete the Necromancer summon loop.
- Its main jobs are:
  - apply ranged pressure to the player
  - keep the Necromancer's summon protection active while alive
  - create a meaningful add-clear phase for the player

## Current Combat Model

First-pass Blood Mage behavior:

- summoned only by the Necromancer
- summons in a group of `3`
- spawns in an even ring around the Necromancer
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
- it does not need full Necromancer-style spacing logic in the first pass

### Attack

- first-pass attack is a targeted blood bubble/pool placed at the player's feet
- use `Attack_01` only
- `Attack_02` and `Attack_03` are reserved for future expansion
- the bubble should appear at the player's current position and pop on its own animation timing
- the pop should only damage the player if they are still inside the bubble when the pop event fires
- keep the damage path simple and consistent with existing enemy-to-player direct damage flow

## Spawn And Owner Contract

### Summon Contract

When the Necromancer uses `Summon`:

- it should spawn `3` Blood Mages
- the Blood Mages should appear in a readable even ring around the Necromancer
- each Blood Mage must receive an owner reference to the Necromancer
- each Blood Mage must receive summon slot / group information for guard positioning
- each Blood Mage must register itself with the Necromancer after a successful spawn/initialize
- summon uses pooled enemy spawn first, then instantiate fallback if no pool is available

### Protection Contract

- Necromancer summon protection becomes real on the first Blood Mage registration
- summon protection remains active while at least one registered Blood Mage is alive
- when the final Blood Mage is removed, summon protection ends
- the Necromancer summon cooldown begins when summon protection ends, not when `Summon` is cast
- if the Necromancer returns to human form, active Blood Mages despawn
- if the Necromancer dies, active Blood Mages despawn

## Animation And Controller Contract

### Active First-Pass Parameters

- `DirectionX`
- `DirectionY`
- `IsMoving`
- `Attack`
- `Dead`

### Active First-Pass States

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

## Prefab And Script Structure

### Main Prefab

Expected child structure:

- `Animator`
- `AttackRange`
- `CastPoint`

Current first-pass prefab wiring:

- `BloodMage` component on the root
- `Animator` + `EnemyAnimationEventRelay` on `Animator`
- `CircleCollider2D` + `EnemyStrikingDistanceCheck` on `AttackRange`
- SO assets assigned for idle, chase, attack, and dead

### Main Scripts

- `BloodMage.cs`
  - shared runtime state
  - owner reference
  - registration/unregistration hooks
  - leash helpers
  - spawned spell ignore handling for both Blood Mage and Necromancer colliders
- `BloodMageIdleSO`
  - summoned settle / idle behavior
- `BloodMageChaseSO`
  - move into attack range
  - light kiting
  - leash-to-owner behavior
- `BloodMageAttackSO`
  - attack cooldown
  - bubble spell spawn / spell fire
- `BloodMageBubbleSpell`
  - pooled ground bubble that pops under the player
- `BloodMageDeadSO`
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
- owner reference
- leash distance
- summon ring index / spawn position

### Chase SO

- light-kite retreat threshold
- leash radius around the Necromancer
- leash hysteresis
- guard radius around the Necromancer
- guard position tolerance
- guard movement speed
- guard anchor start angle
- leash return strength / movement speed

### Attack SO

- cast cooldown
- bubble spell prefab
- bubble target offset
- bubble damage multiplier
- bubble knockback

## Implementation Order

1. create the Blood Mage gameplay animation/controller setup
2. create the Blood Mage prefab shell
3. implement `BloodMage`, states, and SOs
4. implement summon spawn flow from Necromancer
5. implement owner registration / unregister flow
6. implement leash logic and light kiting
7. implement first-pass bubble spell attack
8. implement despawn on owner human return and owner death
9. verify summon protection ends correctly when all Blood Mages are gone

## What Needs To Be Done

- verify the Blood Mage summon flow end-to-end from the Necromancer summon animation
- verify Blood Mages enter chase/attack correctly once configured by the owner
- verify Blood Mages unregister cleanly on death/despawn
- verify owner-human-return and owner-death despawn paths in play mode
- verify summon protection ends correctly when all Blood Mages are gone
- tune bubble timing, radius, damage, and leash behavior
- update the main project changelog when implementation is complete

## Future Ideas

- use `Attack_02` and `Attack_03` for later Blood Mage variants
- expand Blood Mage into a standalone world enemy later
- add blood-themed VFX using the available blood-bubble art
- give Blood Mage more distinct support or debuff behavior
- let Blood Mages participate in richer phase-two formations or synchronized casts

## Not Implemented Yet

- Blood Mage VFX/readability pass
- standalone/world-enemy Blood Mage behavior
