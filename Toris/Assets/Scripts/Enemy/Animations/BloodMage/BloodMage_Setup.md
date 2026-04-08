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
- uses a single ranged spell
- uses `Attack_01` only
- performs light kiting
- stays leashed to the Necromancer
- registers itself back to the Necromancer on spawn
- unregisters itself on death/despawn
- despawns on owner human return or owner death

### Role

The Blood Mage is a ranged support minion.

- it should pressure the player
- it should not overshadow the Necromancer
- it should be dangerous enough that the player wants to kill it quickly

### Movement

- it is leashed to the Necromancer
- it should not freely wander far away from the summoner
- it uses light kiting when the player closes in
- it does not need full Necromancer-style spacing logic in the first pass

### Attack

- first-pass attack is a simple ranged spell
- use `Attack_01` only
- `Attack_02` and `Attack_03` are reserved for future expansion
- the ranged spell should use a simple projectile or a shared enemy-projectile path
- keep the damage path simple and consistent with existing enemy-to-player direct damage flow

## Spawn And Owner Contract

### Summon Contract

When the Necromancer uses `Summon`:

- it should spawn `3` Blood Mages
- the Blood Mages should appear in a readable even ring around the Necromancer
- each Blood Mage must receive an owner reference to the Necromancer
- each Blood Mage must register itself with the Necromancer after a successful spawn/initialize

### Protection Contract

- Necromancer summon protection becomes real on the first Blood Mage registration
- summon protection remains active while at least one registered Blood Mage is alive
- when the final Blood Mage is removed, summon protection ends
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

- `Idle`
- `Run`
- `Attack`
- `Dead`

### Current Asset Set

- `Idle.anim`
- `Walk_Up.anim`
- `Walk_Down.anim`
- `Walk_Left.anim`
- `Walk_Right.anim`
- `Attack_01.anim`
- `Die.anim`

Reserved for later:

- `Attack_02.anim`
- `Attack_03.anim`

### Required Animation Events

- `Attack_01.anim`
  - `Anim_AttackHit()`
  - `Anim_AttackFinished()`
- `Die.anim`
  - `Anim_Despawn()`

## Prefab And Script Structure

### Main Prefab

Expected child structure:

- `Animator`
- `AggroCheck`
- `AttackRange`
- `CastPoint`

### Main Scripts

- `BloodMage.cs`
  - shared runtime state
  - owner reference
  - registration/unregistration hooks
  - leash helpers
- `BloodMageIdleSO`
  - summoned settle / idle behavior
- `BloodMageChaseSO`
  - move into attack range
  - light kiting
  - leash-to-owner behavior
- `BloodMageAttackSO`
  - attack cooldown
  - projectile spawn / spell fire
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

- preferred attack distance
- light-kite retreat threshold
- leash radius around the Necromancer
- leash return strength / movement speed

### Attack SO

- cast cooldown
- projectile prefab
- projectile speed
- projectile lifetime
- projectile damage multiplier
- projectile knockback

## Implementation Order

1. create the Blood Mage gameplay animation/controller setup
2. create the Blood Mage prefab shell
3. implement `BloodMage`, states, and SOs
4. implement summon spawn flow from Necromancer
5. implement owner registration / unregister flow
6. implement leash logic and light kiting
7. implement first-pass ranged spell attack
8. implement despawn on owner human return and owner death
9. verify summon protection ends correctly when all Blood Mages are gone

## What Needs To Be Done

- create the Blood Mage gameplay animation/controller setup
- create the Blood Mage prefab shell
- implement Blood Mage enemy/state/SO structure
- implement summon spawn flow from Necromancer
- implement owner registration / unregister flow
- implement leash logic to keep Blood Mages near the Necromancer
- implement first-pass ranged spell attack
- implement despawn-on-owner-human-return
- implement despawn-on-owner-death
- verify summon protection ends correctly when all Blood Mages are gone
- update the main project changelog when implementation is complete

## Future Ideas

- use `Attack_02` and `Attack_03` for later Blood Mage variants
- expand Blood Mage into a standalone world enemy later
- add blood-themed VFX using the available blood-bubble art
- give Blood Mage more distinct support or debuff behavior
- let Blood Mages participate in richer phase-two formations or synchronized casts

## Not Implemented Yet

- Blood Mage gameplay prefab
- Blood Mage code/state implementation
- Blood Mage summon spawn flow
- Blood Mage owner registration
- Blood Mage unregister-on-death flow
- Blood Mage leash behavior
- Blood Mage first-pass ranged spell
- Necromancer real summon protection tied to living Blood Mages
- Blood Mage pooling registration
- Blood Mage VFX/readability pass
