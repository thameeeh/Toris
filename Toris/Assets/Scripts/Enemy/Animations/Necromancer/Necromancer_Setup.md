# Necromancer

This document is the working implementation reference for the Necromancer enemy.
It is not a changelog.

## Overview

The Necromancer is a hostile two-form enemy.

- In human form, it roams the world and uses grounded movement.
- In floater form, it becomes an aggressive caster that attacks at range, repositions to keep space, and gains access to phase-two behavior.
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
  - phase one fires a single shot
  - phase two can switch to a forward projectile volley
- `PanicSwing`
  - selected in `StrikingDist`
  - intended as a panic response when the player pushes too close
  - applies close-range damage on `Anim_AttackHit()`
- `Summon`
  - phase-two ability
  - unlocked at the configured health threshold
  - spawns Blood Mages in an even ring around the Necromancer
  - can optionally route through separate Blood Mage spawn effects before the real Blood Mages appear
  - can release a weaker radial projectile burst on the summon hit frame
  - applies summon protection through Blood Mage registration

### Summon Protection Rules

- `Summon` spawns `3` Blood Mages in an even ring around the Necromancer
- `Summon` can optionally create a temporary spawn effect at each summon point and defer the real Blood Mage spawn until the spawn animation completes
- each Blood Mage is configured with the Necromancer as owner and registers back to it
- summon protection becomes active when the first Blood Mage registers
- while summon protection is active, the Necromancer cannot take damage
- while summon protection exists, the Necromancer cannot select `Summon` again
- summon cooldown begins when summon protection ends, not at summon cast time
- Blood Mages unregister on death/despawn
- if a summon enters pending state but no Blood Mage actually registers, the pending protection state is cleared
- Blood Mages are command-leashed bodyguards:
  - when the Necromancer is not actively commanding combat, they hold guard positions around it
  - when the Necromancer is actively commanding combat, they attack and kite while staying owner-leashed

## Current Runtime Behavior

### No Player Nearby

- the Necromancer roams in human form
- after a randomized `10-15` second no-player window, it can transform into floater form
- floater no-player duration is currently `10` seconds
- after that, it returns to human form

### Player Far Away

- human form runs toward the player
- floater form floats toward the player to enter casting range

### Player In Casting Range

- if human, the Necromancer waits for an adjustable combat delay, then requests `BecomeFloater`
- if floater and attack-ready, it selects either `SpellCast` or `Summon`
- if summon protection is already active, `Summon` is blocked until that protection ends and the summon cooldown becomes ready again

### Player Too Close

- if the player enters `StrikingDist`, the Necromancer selects `PanicSwing`
- if the Necromancer is still human when close-range combat pressure begins, it also respects the adjustable combat human-to-floater delay before starting the transformation
- after the swing finishes, it retreats quickly
- while still under close-range pressure, it can keep re-swinging on cooldown instead of waiting to fully complete the retreat

### Death

- movement is stopped before the death animation begins
- death animation is form-specific:
  - `Human_Dead`
  - `Floater_Dead`
- death clips call `Anim_Despawn()`
- active Blood Mages despawn if the Necromancer dies

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
- summon protection currently uses the shield visual child for readability
- summon can optionally pause the Necromancer animator for a short impact hold after `Anim_AttackHit()` so the cast lands with more weight before the animation finishes

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
  - Blood Mage command state
  - projectile aim target resolution
- [NecromancerIdleSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Idle/NecromancerIdleSO.cs)
  - no-player roaming and idle-form timing
- [NecromancerChaseSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Chase/NecromancerChaseSO.cs)
  - spacing, retreating, form-change pressure, attack selection
- [NecromancerAttackSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Attack/NecromancerAttackSO.cs)
  - cooldowns
  - projectile spawn
  - panic swing damage
  - Blood Mage summon spawn / spawn-effect routing
  - summon cooldown timing
  - post-attack reposition requests
- [NecromancerDeadSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Dead/NecromancerDeadSO.cs)
  - death-state movement shutdown / dead trigger flow
- [NecromancerShotProjectile.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/NecromancerShotProjectile.cs)
  - pooled spell projectile
  - direct player-damage projectile
- [NecromancerSummonProtectionState.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/NecromancerSummonProtectionState.cs)
  - pending / active summon-protection runtime state
- [NecromancerSummonProtectionVisual.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/NecromancerSummonProtectionVisual.cs)
  - shield visual show/hide + tint
- [NecromancerCastingRangeCheck.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/NecromancerCastingRangeCheck.cs)
  - middle casting-range trigger

## Projectile Status

- [Necromancer_Shot.prefab](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer_Shot.prefab) is assigned into:
  - [Necromancer_Attack_BoltCast.asset](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Enemy/Enemy%20Types/Necromancer/Necromancer%20Behaviour/Attack/Necromancer_Attack_BoltCast.asset)
- `SpellCast` spawns the shot on `Anim_AttackHit()`
- the projectile prefers `GameplayPoolManager` and falls back to instantiate if no gameplay pool manager exists in the scene
- the projectile applies damage directly to `PlayerDamageReceiver` on contact
- the shot uses the `Projectiles` layer
- the shot can use a burst-decay travel profile:
  - launch at a higher initial multiplier
  - hold that burst for a short travel distance
  - then decay exponentially toward a minimum speed
- the shot can optionally keep damaging the player while the player remains inside its hit area
- sustained contact damage is throttled by a serialized interval so it does not tick every frame
- projectile impact-despawn behavior is controlled on the projectile prefab

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
- `panicSwingDamageMultiplier`
- `panicSwingKnockback`
- `summonCooldown`
- `spellProjectileDamageMultiplier`
- `spellProjectileKnockback`
- `spellProjectileSpeed`
- `spellProjectileLifetime`
- `enablePhaseTwoSpellVolley`
- `phaseTwoSpellProjectileCount`
- `phaseTwoSpellSpreadAngle`
- `phaseTwoSpellProjectileDamageMultiplier`
- `enableSummonProjectileBurst`
- `summonProjectileBurstCount`
- `summonProjectileBurstDamageMultiplier`
- `enableSummonImpactHold`
- `summonImpactHoldDuration`
- `spellCastRepositionSpeedMultiplier`
- `panicSwingRepositionSpeedMultiplier`

### Summon SO Fields

- `bloodMageSummonPrefab`
- `bloodMageSpawnEffectPrefab`
- `bloodMageSummonCount`
- `bloodMageSummonRadius`
- `bloodMageSummonStartAngleDegrees`

### Summon Protection Visual

- `activeTint`
- `hideRendererWhenInactive`

### Necromancer Shot Projectile

- `rotateTowardVelocity`
- `rotateOffsetDegrees`
- `despawnOnFirstImpact`
- `enableSustainContactDamage`
- `sustainDamageInterval`
- `sustainDamageMultiplier`
- `sustainKnockbackMultiplier`
- `sustainDamageBypassesIFrames`
- `useBurstDecaySpeedProfile`
- `launchSpeedMultiplier`
- `burstTravelDistance`
- `exponentialDecayRate`
- `minimumSpeedMultiplier`

## What Needs To Be Done

- tune and validate the full phase-two loop in play mode
- decide how much visual feedback summon protection should have beyond the current shield layer
- add hit VFX / summon VFX / shield break feedback
- decide whether the rescue variant stays, changes, or is removed
- verify the intended summon cadence once Blood Mage combat tuning is final

## Future Ideas

- rescue-variant branch where the human facade can become a real NPC/rescue event
- more readable shield presentation than the current placeholder if needed
- richer spell behavior beyond the current projectile implementation

## Not Implemented Yet

- projectile hit VFX
- summon VFX
- shield break feedback
- rescue NPC conversion / rescue reward flow
- optional standalone enemy-pool registration verification if that spawn path is used later
