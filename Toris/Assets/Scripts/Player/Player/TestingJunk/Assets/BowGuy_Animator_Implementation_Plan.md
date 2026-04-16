# BowGuy Animator Implementation Notes

This document records the BowGuy player animation setup that is now locked in on the current player runtime.

The earlier `ShootF` and `ShootS` path is no longer the baseline. The active system is built around authored `Draw`, `Hold`, and `Release` clips.

## Current Goal

Keep the player bow flow readable, responsive, and decoupled:

* gameplay owns whether a shot is valid
* animation owns how that state is shown
* interrupt rules are explicit
* directional sprite naming stays aligned with the authored BowGuy clips

## Active Clip Contract

The current bow action set uses:

* `BowGuyShootDraw_D`
* `BowGuyShootDraw_L`
* `BowGuyShootDraw_R`
* `BowGuyShootDraw_U`
* `BowGuyShootHold_D`
* `BowGuyShootHold_L`
* `BowGuyShootHold_R`
* `BowGuyShootHold_U`
* `BowGuyShootRelease_D`
* `BowGuyShootRelease_L`
* `BowGuyShootRelease_R`
* `BowGuyShootRelease_U`

The rest of the BowGuy directional body states remain:

* `BowGuyIdle_*`
* `BowGuyRun_*`
* `BowGuyDash_*`
* `BowGuyDashP_*`
* `BowGuyHurt_*`
* `BowGuyDeath_*`

## Runtime Ownership

### Gameplay Owns

`PlayerBowController` owns:

* draw start validation
* cooldown checks
* `nockTime` readiness
* whether release fires a real arrow or becomes a dry cancel
* movement lock while drawing and holding
* draw cancellation on gameplay interrupts
* directional muzzle selection and arrow spawn origin

### Presentation Bridge Owns

`PlayerAnimationPresenter` owns:

* listening to bow, dash, hurt, and death events
* forwarding `DrawStarted`, `ShootReady`, `ShotReleased`, and `DryReleased`
* canceling active draw on dash, hurt, and death before playing interrupt visuals
* forwarding ability-triggered release playback

### Animation Controller Owns

`PlayerAnimationController` owns:

* resolving directional BowGuy state names
* playing `ShootDraw`, `ShootHold`, and `ShootRelease`
* matching draw playback speed to gameplay `nockTime`
* swapping directional shoot clips when aim direction changes during draw or hold
* returning one-shot states back to locomotion
* spawning directional dash particle playback

## Active Shoot Flow

The current bow flow is:

1. input starts draw
2. `PlayerBowController` validates the attempt
3. `DrawStarted` fires
4. `PlayerAnimationController.BeginShoot(...)` plays `BowGuyShootDraw_*`
5. gameplay reaches `nockTime`
6. `ShootReady` fires
7. `PlayerAnimationController.EnterShootHold()` plays `BowGuyShootHold_*`
8. release after ready fires a real arrow and plays `BowGuyShootRelease_*`
9. release before ready becomes `DryReleased`

This means the held bow pose is no longer a frozen frame inside a longer shoot clip. It is now a dedicated authored hold state.

## Decoupling Rules

The important split is:

* gameplay readiness comes from `BowSO.nockTime`
* animation readiness is represented by the `Hold` clip

Animation does not decide whether the shot is allowed.

That was the main reason the old hold-frame approach felt brittle. The current setup keeps the visual hold stable while leaving shot authority in gameplay code.

## Interrupt Rules

These rules are part of the current baseline:

* dash cancels an active draw
* hurt cancels an active draw
* death cancels an active draw
* releasing after a canceled draw does nothing
* drawing is blocked while the player is dashing
* drawing is blocked while an ability is actively occupying the bow

This makes draw cancellation behave like a dry cancel rather than a delayed shot.

## Ability Rules

Bow-using abilities do not get special-case bow checks in `PlayerBowController`.

Instead:

* `PlayerAbilityController` exposes `IsBowDrawBlocked`
* ability runtimes decide whether they currently occupy the bow
* the normal bow draw path simply checks that shared state

Ability shots can also request the normal bow release animation bridge so their projectile firing still lines up with the BowGuy body presentation.

## Muzzle Rules

The player now uses directional muzzle transforms:

* `Muzzle_D`
* `Muzzle_L`
* `Muzzle_R`
* `Muzzle_U`

The active muzzle is chosen from current cardinal aim and is used for both:

* aim origin
* projectile spawn position

That keeps the bow shot origin aligned with the actual sprite direction.

## Debugging Support

Shoot logs are still available, but they are no longer always on.

The current setup uses a shared `PlayerShootDebug` helper and one inspector toggle on `PlayerBowController` to enable or disable logs for:

* `BowCtrl`
* `AnimPresenter`
* `AnimCtrl`

This should stay as the standard debugging entry point for future shoot-flow issues.

## Cleanup Status

The old lock-based shoot implementation has been cleaned out of the player path:

* no `ShootF` / `ShootS` runtime branch remains in the active bow flow
* old normalized hold-lock timing fields were removed from the live weapon animation data
* unused animation view helpers from the old system were removed

## Recommended Baseline Going Forward

Treat this as the current stable BowGuy standard:

* locomotion through directional idle and run states
* bow through authored `Draw / Hold / Release`
* gameplay-owned shot authority
* presentation-owned state playback
* explicit interrupt cancellation

If the player animation set evolves again later, this should be the starting point rather than the older single-clip hold-lock approach.
