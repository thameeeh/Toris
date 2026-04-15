# BowGuy Animator Implementation Plan

This document turns the broader animation evolution notes into a concrete wiring plan for the BowGuy player set.

It assumes the authored animations already exist and that we want to preserve the clean gameplay-to-presentation split while pushing more runtime state flow into Unity Animator.

## Goal

Implement the first real BowGuy player animation system with these rules:

* Keep the BowGuy naming convention exactly as authored.
* Move core state routing into Unity Animator.
* Keep only special gameplay timing in C#.
* Support directional `Idle`, `Run`, `Hurt`, `Death`, `Dash`, `ShootF`, `ShootS`, and `DashP`.

## Naming Contract

The runtime-facing animation state names should match the authored clips:

* `BowGuyIdle_D`
* `BowGuyIdle_L`
* `BowGuyIdle_R`
* `BowGuyIdle_U`
* `BowGuyRun_D`
* `BowGuyRun_L`
* `BowGuyRun_R`
* `BowGuyRun_U`
* `BowGuyHurt_D`
* `BowGuyHurt_L`
* `BowGuyHurt_R`
* `BowGuyHurt_U`
* `BowGuyDeath_D`
* `BowGuyDeath_L`
* `BowGuyDeath_R`
* `BowGuyDeath_U`
* `BowGuyDash_D`
* `BowGuyDash_L`
* `BowGuyDash_R`
* `BowGuyDash_U`
* `BowGuyShootF_D`
* `BowGuyShootF_L`
* `BowGuyShootF_R`
* `BowGuyShootF_U`
* `BowGuyShootS_D`
* `BowGuyShootS_L`
* `BowGuyShootS_R`
* `BowGuyShootS_U`
* `BowGuyDashP_D`
* `BowGuyDashP_L`
* `BowGuyDashP_R`
* `BowGuyDashP_U`

The code should adapt to this naming. The naming should not be adapted back to the previous `U_Idle` style.

## Animator-First Direction

The Animator should now own:

* locomotion routing
* directional locomotion switching
* directional dash entry
* directional hurt entry
* directional death entry
* directional shoot entry

Code should still own:

* choosing facing from move or aim
* deciding whether the next draw uses `ShootF` or `ShootS`
* locking the shoot animation at the hold point
* resuming from the hold point on release
* spawning the stationary dash particle playback

## Runtime Parameters

The BowGuy body controller should use these Animator parameters:

* `FacingIndex` as `int`
* `IsMoving` as `bool`
* `IsInAction` as `bool`
* `ShootVariant` as `int`
* `ShootTrigger` as `trigger`
* `DashTrigger` as `trigger`
* `HurtTrigger` as `trigger`
* `IsDead` as `bool`

Recommended facing order:

* `0 = D`
* `1 = L`
* `2 = R`
* `3 = U`

## Controller Structure

The BowGuy body controller can stay on one base layer.

### Locomotion States

Use directional idle and run states:

* `BowGuyIdle_D`
* `BowGuyIdle_L`
* `BowGuyIdle_R`
* `BowGuyIdle_U`
* `BowGuyRun_D`
* `BowGuyRun_L`
* `BowGuyRun_R`
* `BowGuyRun_U`

These are entered through Animator Any State transitions gated by:

* `IsDead == false`
* `IsInAction == false`
* matching `FacingIndex`
* matching `IsMoving`

### Action States

Use directional action states:

* `BowGuyDash_*`
* `BowGuyHurt_*`
* `BowGuyDeath_*`
* `BowGuyShootF_*`
* `BowGuyShootS_*`

These are entered through Any State transitions gated by:

* action trigger or bool
* matching `FacingIndex`
* matching `ShootVariant` when entering shoot

Action states do not need to own return-to-locomotion transitions if code clears `IsInAction` when the body action is complete. Once `IsInAction` becomes `false`, the Animator can route back into the correct locomotion state through the locomotion Any State transitions.

## Shoot Rules

### Full Shot

`ShootF` plays when the first draw attempt begins.

### Short Shot

`ShootS` plays when the player starts another draw within a configurable follow-up window after the last shot release.

This keeps the first shot readable while making consecutive shots feel tighter.

### Shoot Hold Logic

`ShootF` and `ShootS` are still single clips with a lock point, so the system should keep a small amount of code-owned hold logic:

* begin draw
* trigger the chosen directional shoot state in Animator
* watch normalized time
* pause at the weapon-configured lock point
* resume from just after the lock point when release happens

This remains an intentional hybrid until the shoot flow is eventually split into dedicated `Draw`, `Hold`, and `Release` clips.

## Dash Particle Plan

`BowGuyDashP_*` is not body motion.

It should be spawned at dash start as a temporary visual object that:

* appears at the player's dash start position
* plays the directional `DashP` animation once
* stays in place while the player moves away
* destroys itself when playback finishes

It can reuse the same BowGuy controller asset as long as locomotion transitions are blocked for the spawned FX animator.

## Code Responsibilities

### PlayerAnimationPresenter

Responsibilities:

* read movement, dash, bow, hurt, and death events
* forward locomotion updates
* request dash playback
* request shoot begin and shoot release

### PlayerAnimationController

Responsibilities:

* resolve BowGuy state names
* convert facing vectors into `FacingIndex`
* set Animator parameters and triggers
* choose `ShootF` or `ShootS`
* manage hold-lock-resume timing
* track one-shot action timing for dash and hurt
* spawn dash particles

### PlayerAnimationView

Responsibilities:

* wrap Animator access
* set and reset parameters
* expose current state info
* expose clip lengths and hashed state names
* expose runtime controller and sprite data needed by dash particle playback

### CharacterAnimSO

Responsibilities:

* hold the BowGuy name prefix
* map logical action keys to BowGuy action tokens
* keep locomotion token names data-driven

### WeaponProfile

Responsibilities:

* hold lock timing for `ShootF`
* hold lock timing for `ShootS`
* hold follow-up timing for consecutive short-shot selection
* keep dash timing data-driven where useful

## Wiring Targets

The main runtime wiring should update:

* the body Animator controller on the player prefab
* the BowGuy animation controller asset in `TestingJunk/Anims`
* the current animation profile and weapon profile assets used by the player
* the runtime animation scripts

## Expected End Result

After wiring:

* idle and run should be chosen through Animator conditions
* dash, hurt, death, and shoot should enter through Animator transitions
* first shot should use `ShootF`
* follow-up shots should use `ShootS`
* directional aim changes during draw should still work
* dash particles should remain behind when dash starts
* the code should be thinner than before, but still own the special timing that Animator cannot express cleanly with the current authored clips
