# BowGuy Animator Implementation Guide

Use this document as the working reference for the player bow animation setup.

The player bow flow should be built around authored `Draw`, `Hold`, and `Release` clips. Treat that as the standard BowGuy contract.

## Purpose

Use this setup to keep the bow readable, responsive, and easy to debug.

Follow these principles:

* let gameplay decide whether the shot is valid
* let animation decide how that state is shown
* keep interrupt behavior explicit
* keep directional clip naming aligned with the BowGuy assets

## Clip Set

Use these directional bow clips:

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

Keep the rest of the body states in the same directional naming style:

* `BowGuyIdle_*`
* `BowGuyRun_*`
* `BowGuyDash_*`
* `BowGuyDashP_*`
* `BowGuyHurt_*`
* `BowGuyDeath_*`

## Runtime Ownership

### `PlayerBowController`

Use `PlayerBowController` for gameplay authority:

* validate draw start
* check cooldown
* evaluate `nockTime`
* decide whether release fires a real arrow or becomes a dry cancel
* lock and unlock movement while drawing
* cancel draw on gameplay interrupts
* choose the active directional muzzle
* spawn projectiles

### `PlayerAnimationPresenter`

Use `PlayerAnimationPresenter` as the presentation bridge:

* listen to bow, dash, hurt, and death events
* forward `DrawStarted`, `ShootReady`, `ShotReleased`, and `DryReleased`
* cancel active bow draw before playing higher-priority interrupt visuals
* forward ability-triggered release playback

### `PlayerAnimationController`

Use `PlayerAnimationController` for animation-facing glue:

* resolve directional BowGuy state names
* play `ShootDraw`, `ShootHold`, and `ShootRelease`
* match draw playback speed to gameplay `nockTime`
* swap directional shoot clips while aiming during draw or hold
* return one-shot states back to locomotion
* spawn directional dash particle playback

## Shoot Flow

Follow this order:

1. input starts draw
2. `PlayerBowController` validates the attempt
3. `DrawStarted` fires
4. `PlayerAnimationController.BeginShoot(...)` plays `BowGuyShootDraw_*`
5. gameplay reaches `nockTime`
6. `ShootReady` fires
7. `PlayerAnimationController.EnterShootHold()` plays `BowGuyShootHold_*`
8. release after ready fires a real arrow and plays `BowGuyShootRelease_*`
9. release before ready becomes `DryReleased`

Use the `Hold` clip as a real authored state.

Do not treat the hold pose as a frozen frame inside a longer clip.

## Gameplay And Animation Rules

Follow this split:

* gameplay readiness comes from `BowSO.nockTime`
* visual readiness is represented by the `Hold` clip

Keep shot authority in gameplay code.

Do not let animation events decide whether the player is allowed to shoot.

## Interrupt Rules

Treat these as baseline behavior:

* dash cancels an active draw
* hurt cancels an active draw
* death cancels an active draw
* releasing after a canceled draw does nothing
* drawing is blocked while dashing
* drawing is blocked while an ability is occupying the bow

Use dry-cancel behavior for interrupted draws.

Do not allow delayed shots to leak through after an interrupt.

## Ability Rules

Use a shared bow-blocking rule for abilities.

Follow this structure:

* `PlayerAbilityController` exposes `IsBowDrawBlocked`
* ability runtimes decide whether they currently occupy the bow
* `PlayerBowController` checks that shared state before starting a draw

If an ability should play the normal bow release body animation, route it through the same release bridge instead of inventing a separate special-case animation path.

## Muzzle Rules

Use directional muzzle transforms:

* `Muzzle_D`
* `Muzzle_L`
* `Muzzle_R`
* `Muzzle_U`

Choose the active muzzle from current cardinal aim and use it for both:

* aim origin
* projectile spawn position

Keep projectile origin and directional body presentation aligned.

## Debugging

Use the shared `PlayerShootDebug` helper as the standard debugging entry point.

Control the logs from the inspector toggle on `PlayerBowController`.

That toggle should enable or disable logs for:

* `BowCtrl`
* `AnimPresenter`
* `AnimCtrl`

## Working Standard

When touching this system later, work from these assumptions:

* locomotion uses directional idle and run states
* bow uses authored `Draw / Hold / Release`
* gameplay owns combat validity and interrupts
* presentation owns visual playback
* interrupt behavior stays explicit and predictable

Use this setup as the starting point for future BowGuy work.
