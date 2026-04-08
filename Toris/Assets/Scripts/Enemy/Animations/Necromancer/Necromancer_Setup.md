# Necromancer

This document is the working implementation reference for the Necromancer enemy.
It is not a changelog.

## Overview

The Necromancer is a hostile two-form enemy.

- In human form, it roams the world and uses grounded movement.
- In floater form, it becomes an aggressive caster that attacks at range, repositions to keep space, and gains access to later phase mechanics.
- The human appearance is intended to read as a disguise or facade for the monster reveal.

## Current Combat Model

### Forms

- `Human`
  - default roaming form
  - uses `Run` for movement
  - can transform into floater form
- `Floater`
  - casting/combat form
  - does not use `Run`
  - moves by floating while visually remaining in floater idle or attack states

### Range Bands

- `AggroCheck`
  - outer awareness / chase range
- `CastingRange`
  - middle range for `SpellCast` and `Summon`
- `StrikingDist`
  - close range for `PanicSwing`

Current first-pass radii:

- `AggroCheck = 8`
- `CastingRange = 6`
- `StrikingDist = 2`

### Spacing Rules

- preferred combat distance: `6`
- retreat threshold: `4`
- after `SpellCast`, the Necromancer repositions
- after `PanicSwing`, the Necromancer repositions faster and with higher priority than its normal retreat

### Attack Selection

- `SpellCast`
  - default ranged attack in `CastingRange`
- `PanicSwing`
  - selected in `StrikingDist`
  - intended as a panic response when the player pushes too close
- `Summon`
  - phase-two ability
  - unlocked at the configured health threshold
  - currently acts as an animation/cooldown/shield hook for future Blood Mage summoning

### Summon Protection Rules

- `Summon` applies summon protection state when the summon hit event fires
- while summon protection exists, the Necromancer cannot select `Summon` again
- current fallback rule:
  - if the Necromancer fully returns to human form, summon protection is cleared
- final intended rule:
  - summon protection should remain until the summoned Blood Mages are dead

## Current Runtime Behavior

### No Player Nearby

- the Necromancer roams in human form
- after a randomized `10-15` second no-player window, it can transform into floater form
- floater no-player duration is currently `10` seconds
- after that, it returns to human form
- there is no standing human idle gameplay loop right now; the enemy roams instead

### Player Far Away

- human form runs toward the player
- floater form floats toward the player to enter casting range

### Player In Casting Range

- if human, the Necromancer waits for an adjustable combat delay, then requests `BecomeFloater`
- if floater and attack-ready, it selects either `SpellCast` or `Summon`

### Player Too Close

- if the player enters `StrikingDist`, the Necromancer selects `PanicSwing`
- if the Necromancer is still human when close-range combat pressure begins, it also respects the adjustable combat human-to-floater delay before starting the transformation
- after the swing finishes, it retreats quickly before normal close-range logic can immediately retrigger the swing

### Death

- movement is stopped before the death animation begins
- death animation is form-specific:
  - `Human_Dead`
  - `Floater_Dead`
- death clips call `Anim_Despawn()`

## Animation And Controller Contract

### Active Animator Parameters

- `DirectionX`
- `DirectionY`
- `IsMoving`
- `BecomeFloater`
- `BecomeHuman`
- `SpellCast`
- `PanicSwing`
- `Summon`
- `Dead`

### Active Animator States

- `Necromancer_Human_Idle`
- `Necromancer_Human_To_Floater`
- `Necromancer_Floater_Idle`
- `Necromancer_Floater_To_Human`
- `Necromancer_Run`
- `Necromancer_Projectile`
- `Necromancer_Air_Slash`
- `Necromancer_Summon`
- `Necromancer_Human_Dead`
- `Necromancer_Floater_Dead`

### Required Animation Events

- `Necromancer_Projectile.anim`
  - `Anim_AttackHit()`
  - `Anim_AttackFinished()`
- `Necromancer_Air_Slash.anim`
  - `Anim_AttackHit()`
  - `Anim_AttackFinished()`
- `Necromancer_Summon.anim`
  - `Anim_AttackHit()`
  - `Anim_AttackFinished()`
- `Necromancer_Human_Dead.anim`
  - `Anim_Despawn()`
- `Necromancer_Floater_Dead.anim`
  - `Anim_Despawn()`

### Visual Rules

- shadow is hidden in human form by default
- shadow is visible in floater / transform / attack / dead states
- the main sprite flips horizontally using `SpriteRenderer.flipX`
- projectile aim uses the player hurtbox collider center when available
- a small serialized aim offset exists for visual tuning

## Prefab And Script Structure

### Main Prefab

[Necromancer.prefab](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer.prefab)

Expected child structure:

- `Animator`
- `AggroCheck`
- `CastingRange`
- `StrikingDist`
- `CastPoint`

Runtime-only visual children under `Animator`:

- `Shadow`
- `ShieldVisual`

### Main Scripts

- [Necromancer.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer.cs)
  - shared runtime state
  - form checks
  - attack trigger routing
  - summon protection state
  - visual hooks
  - projectile aim target resolution
- [NecromancerIdleSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Idle/NecromancerIdleSO.cs)
  - no-player roaming and idle-form timing
- [NecromancerChaseSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Chase/NecromancerChaseSO.cs)
  - spacing, retreating, form-change pressure, attack selection
- [NecromancerAttackSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Attack/NecromancerAttackSO.cs)
  - cooldowns
  - projectile spawn
  - post-attack reposition requests
- [NecromancerDeadSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Dead/NecromancerDeadSO.cs)
  - death-state movement shutdown / dead trigger flow
- [NecromancerShotProjectile.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/NecromancerShotProjectile.cs)
  - pooled spell projectile
  - player hit payload provider
- [NecromancerSummonProtectionState.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/NecromancerSummonProtectionState.cs)
  - pending / active summon-protection runtime state
- [NecromancerSummonProtectionVisual.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/NecromancerSummonProtectionVisual.cs)
  - shield visual show/hide + tint
- [NecromancerCastingRangeCheck.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/NecromancerCastingRangeCheck.cs)
  - middle casting-range trigger

## Projectile Status

### What Exists

- [Necromancer_Shot.prefab](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer_Shot.prefab) exists
- it is already assigned into:
  - [Necromancer_Attack_BoltCast.asset](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Attack/Necromancer_Attack_BoltCast.asset)
- `SpellCast` now spawns the shot on `Anim_AttackHit()`
- the projectile prefers `GameplayPoolManager` and falls back to instantiate if no gameplay pool manager exists in the scene
- the projectile builds `HitData` through `IHitPayloadProvider`

### What Still Needs Manual Unity Hookup

- add `Necromancer_Shot` to `GameplayPoolConfiguration.asset`
- verify the shot layer is correct for player damage
- verify `PlayerHurtbox.damagingLayers` includes that layer
- tune projectile collider size, layer, sorting, and impact behavior

## Tuning Surfaces

### Necromancer

- `AttackDamage`
- `MovementSpeed`
- `playerAimCollider`
- `projectileAimOffset`
- `hideShadowInHumanForm`
- `flipSpriteHorizontally`
- `horizontalFlipThreshold`
- `humanRescueVariantChance`
- `humanRescueVariantInvincible`
- `summonHealthThreshold`
- `enableBloodMageSummonProtection`

### Idle SO

- `roamRadius`
- `idleMoveSpeedMultiplier`
- `minHumanIdleBeforeFloat`
- `maxHumanIdleBeforeFloat`
- `floaterIdleDuration`

### Chase SO

- `preferredDistance`
- `retreatDistance`
- `humanToFloaterTriggerDelay`
- `floaterMoveSpeedMultiplier`
- `postCastRepositionDuration`

### Attack SO

- `castCooldown`
- `panicSwingCooldown`
- `summonCooldown`
- `spellProjectileDamageMultiplier`
- `spellProjectileKnockback`
- `spellProjectileSpeed`
- `spellProjectileLifetime`
- `spellCastRepositionSpeedMultiplier`
- `panicSwingRepositionSpeedMultiplier`

### Summon Protection Visual

- `activeTint`
- `hideRendererWhenInactive`

## What Needs To Be Done

- register `Necromancer_Shot` in the gameplay projectile pool config
- register `Necromancer` itself in the enemy pool config if it is meant to use gameplay pooling
- finish projectile-to-player damage hookup by setting the correct projectile layer and player hurtbox mask
- implement actual `PanicSwing` damage / hit logic
- implement Blood Mage spawning from `Summon`
- connect Blood Mage death/despawn back into summon protection removal
- replace or finalize the temporary summon shield presentation if needed
- decide whether the rescue variant stays, changes, or is removed
- add hit VFX / summon VFX / shield break feedback
- update the main project changelog when the current Necromancer pass is wrapped up

## Future Ideas

- Blood Mage summoning as the real phase-two mechanic
- summon protection tied strictly to living Blood Mages instead of the current human-form fallback reset
- rescue-variant branch where the human facade can become a real NPC/rescue event
- more readable shield presentation than the temporary `EarthShield` placeholder if needed
- richer spell behavior beyond the current first projectile implementation

## Not Implemented Yet

- `PanicSwing` damage payload
- Blood Mage enemy and summon-spawn flow
- Blood Mage owner registration / unregister-on-death flow
- final summon shield rules tied to Blood Mage life state
- projectile hit VFX
- summon VFX
- shield break feedback
- rescue NPC conversion / rescue reward flow
- verified gameplay-pool registration for the Necromancer enemy prefab
- verified gameplay-pool registration for the Necromancer shot prefab
- verified player hurtbox layer-mask hookup for enemy projectiles
