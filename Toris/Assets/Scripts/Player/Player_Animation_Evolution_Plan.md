# Player Animation Evolution Plan

This document explains why the current player animation setup is a good starting point, why it should evolve as the game grows, and what the recommended target architecture should be.

The intent is not to throw away the current structure. The intent is to preserve the parts that are working well and shift more of the animation-state responsibility into Unity's Animator where it belongs.

## Assumptions

This plan assumes the player already has all core directional animations authored and available:

* `Idle`
* `Walk`
* `Hurt`
* `Shoot`
* `Dash`
* `Death`

It also assumes the player remains a 4-direction character and that the project wants to keep gameplay logic separate from direct Animator manipulation.

## Current Setup

At the moment, the player animation stack is effectively split into four parts:

### 1. Presentation Bridge

`PlayerAnimationPresenter` listens to gameplay-owned systems such as movement, bow usage, hurt, and death, then forwards animation intent.

This is a good pattern.

It keeps gameplay systems from directly calling `Animator` all over the project and gives animation a single presentation-facing entry point.

### 2. Custom Animation Brain

`PlayerAnimationController` currently acts as the primary state machine.

It:

* decides locomotion state
* resolves directional state names
* handles hold, lock, and release logic
* manually tracks hurt and death timing
* manually decides when to crossfade back to locomotion

This works, but it means the real animation state machine lives in C# instead of in Animator.

### 3. Animator Wrapper

`PlayerAnimationView` is a thin wrapper around Unity's `Animator`.

It:

* caches clip hashes
* checks whether states exist
* crossfades by hash
* plays states directly
* pauses the Animator by setting `Animator.speed`

This wrapper is clean and useful. The main issue is not the wrapper itself. The issue is that most state behavior is still custom-driven above it.

### 4. Data Assets

`CharacterAnimSO` and `WeaponProfile` hold naming and timing data.

This is also a good pattern.

These assets make the system more data-driven and keep hardcoded timing out of unrelated gameplay scripts.

## What Is Good Enough To Keep

These parts should stay:

* Keep the `Presenter -> Controller -> View` separation.
* Keep `CharacterAnimSO` and `WeaponProfile` as data assets.
* Keep gameplay as the source of truth for animation intent.
* Keep directional discrete states for a 4-direction sprite character.

The current architecture is strongest where it separates responsibilities clearly.

## Why The System Needs To Evolve

The current setup should evolve because it is already using Animator, but it is not really using Animator as a state machine.

Right now the Animator is mostly acting as:

* a clip database
* a hash lookup target
* a playback surface for C# decisions

That becomes fragile as the animation set grows.

### 1. The C# FSM Will Scale Poorly

As long as the player only has a small set of actions, a custom FSM is manageable.

Once the player gains more combat states, more weapons, interrupt rules, layered reactions, or combo logic, the controller will grow into a second animation graph written in code.

That means:

* more manual timing checks
* more special-case branching
* more naming dependencies
* more maintenance cost when clips or behaviors change

### 2. State Names Are Too Important

The current flow depends heavily on clip and state naming conventions such as `U_Idle`, `R_Shoot`, and `D_Dash`.

That is workable at small scale, but it means:

* animation changes are tightly coupled to code expectations
* errors are discovered at runtime instead of being obvious in an Animator graph
* animators and designers cannot confidently rework flow without code impact

### 3. The Whole Animator Gets Paused

The current hold-lock behavior pauses the Animator globally via `Animator.speed = 0`.

That is acceptable in a tiny setup, but it becomes risky once the player has:

* multiple Animator layers
* additive reactions
* secondary motion
* future upper-body and lower-body separation

Freezing the whole Animator is broader than the actual gameplay need.

### 4. Animator Is Not Giving You Its Real Value

Unity Animator is strongest when it handles:

* state transitions
* interruption rules
* transition timing
* trigger handling
* state visualization
* designer iteration

The current controller asset is close to a container of states rather than a designed runtime graph.

That is the biggest reason the system should evolve.

## Recommended Direction

The recommended destination is a hybrid Animator-driven system.

That means:

* keep gameplay deciding intent
* keep the Presenter as the bridge
* keep small amounts of code for rare, gameplay-specific timing rules
* move the main animation flow into Animator parameters, transitions, and sub-state machines

This is not a recommendation to move away from Animator.

It is the opposite.

The recommendation is to use Animator more fully while keeping the current code architecture clean.

## Recommended Responsibilities

### Keep In Code

Code should keep ownership of:

* deciding high-level intent from gameplay
* facing direction from movement or aim
* special charge-shot lock behavior if `Shoot` is still one clip
* weapon-specific timing data
* any authoritative gameplay event timing

### Move Into Animator

Animator should own:

* locomotion flow
* idle to walk transitions
* hurt transitions
* death transitions
* dash entry and exit flow
* most shoot playback flow
* interruption rules
* return-to-locomotion behavior

## Recommended Animator Parameters

A solid starting parameter set would be:

* `FacingIndex` as an `int`
* `IsMoving` as a `bool`
* `IsDrawing` as a `bool`
* `DashTrigger` as a `trigger`
* `HurtTrigger` as a `trigger`
* `IsDead` as a `bool`

Optional additions later:

* `WeaponAction` as an `int`
* `MoveSpeed` as a `float`
* `IsLocked` as a `bool`

For a 4-direction sprite game, `FacingIndex` is usually cleaner than trying to force blend trees where the art is not meant to blend.

## Recommended Animator Structure

### Base Layer

Use the base layer for full-body player states.

Suggested sub-state machines:

* `Locomotion`
* `Action`
* `Damage`
* `Death`

### Locomotion

Locomotion should handle:

* `U_Idle`
* `D_Idle`
* `L_Idle`
* `R_Idle`
* `U_Walk`
* `D_Walk`
* `L_Walk`
* `R_Walk`

This can be done with either:

* four directional idle and walk states with transitions driven by `FacingIndex` and `IsMoving`
* or grouped sub-state machines per facing direction

For sprite work, explicit directional states are usually clearer than trying to blend.

### Action

Action should handle:

* `U_Shoot`
* `D_Shoot`
* `L_Shoot`
* `R_Shoot`
* `U_Dash`
* `D_Dash`
* `L_Dash`
* `R_Dash`

These can be entered from triggers or booleans, then return to locomotion naturally through transition rules.

### Damage

Damage should handle:

* `U_Hurt`
* `D_Hurt`
* `L_Hurt`
* `R_Hurt`

Hurt should interrupt locomotion and action states according to clear transition rules.

### Death

Death should handle:

* `U_Death`
* `D_Death`
* `L_Death`
* `R_Death`

Death should be terminal and not require the controller to manually babysit the return path.

## Special Note About Shoot

The answer depends on what `Shoot` really means in your game.

### If `Shoot` Is One Clip

If you only have one directional `Shoot` clip and you are using a lock point to fake a draw-hold-release behavior, then keeping a small amount of code-driven control is still reasonable.

In that case:

* the Presenter should still call into animation intent
* the Controller may still keep a small lock-point helper
* Animator should still own entry, interruption, and return flow where possible

### If You Later Split Shoot Into Phases

If you later author separate clips such as:

* `Draw`
* `DrawHold`
* `Release`

then the lock behavior should move out of C# and into Animator almost entirely.

That would let the Animator own charge-state flow much more naturally.

## Suggested Evolution Plan

### Phase 1: Keep The Current Scripts, Thin The Behavior

Do not replace everything at once.

First:

* keep `PlayerAnimationPresenter`
* keep `PlayerAnimationController`
* keep `PlayerAnimationView`
* keep `CharacterAnimSO`
* keep `WeaponProfile`

Then move locomotion, hurt, dash, and death flow into Animator parameters and transitions.

### Phase 2: Stop Resolving Most States By Name In C#

Reduce direct name-based playback for common states.

The controller should stop being responsible for manually crossfading between most core clips every frame.

Instead, it should mostly set parameters such as:

* `FacingIndex`
* `IsMoving`
* `HurtTrigger`
* `DashTrigger`
* `IsDead`

### Phase 3: Keep Only Rare Logic In Code

Once the core states are Animator-driven, leave only the genuinely special cases in code.

That likely means:

* charge-shot lock timing
* weapon-specific override rules
* unusual interrupt rules driven by gameplay state

### Phase 4: Revisit The Shoot Flow

When the rest of the graph is stable, decide whether `Shoot` should remain a single clip with a code lock point or become a fuller Animator state flow with dedicated hold and release states.

## Clear Recommendation

Do not replace Animator with a different animation runtime for this player right now.

Do not keep growing the current C#-driven FSM indefinitely either.

The right move is:

* keep the clean separation of responsibilities
* keep Animator as the playback backend
* evolve Animator into the real state machine
* leave only the hard-to-express gameplay-specific timing in code

## Final Summary

The current system is a good prototype architecture.

It is not yet the best long-term production architecture.

If the player is only ever going to support `Death`, `Walk`, `Hurt`, `Idle`, `Shoot`, and `Dash`, the current setup can survive for a while.

If the player is expected to grow in combat depth, weapon variety, or layered animation complexity, the system should evolve now into a more Animator-driven hybrid before the custom controller logic becomes the real bottleneck.
