# Player Animation Evolution Notes

This document now reflects the current player animation direction after the BowGuy bow flow cleanup.

The earlier evolution question was whether the player should keep leaning on a code-owned animation state machine or move into a cleaner Animator-driven hybrid. The bow system has now moved in that direction.

## What Is Now Locked In

The current player animation stack keeps the same broad separation:

* `PlayerAnimationPresenter` is the presentation bridge
* `PlayerAnimationController` is animation-facing runtime glue
* `PlayerAnimationView` wraps Animator access
* `CharacterAnimSO` and `WeaponProfile` keep animation data externalized

That architecture still makes sense and remains the recommended baseline.

## What Changed In Practice

The bow flow is no longer based on a single clip with a code-paused hold frame.

The current setup uses authored phases:

* `ShootDraw`
* `ShootHold`
* `ShootRelease`

That is the most important evolution that actually landed, because it fixed the biggest source of brittleness in the previous setup.

## Current Hybrid Model

The player animation model is now:

### Gameplay Owns

* draw validation
* cooldowns
* shot readiness timing
* interrupt cancellation
* projectile spawning
* ability-based bow occupation rules

### Animation Owns

* draw playback
* hold playback
* release playback
* directional clip switching
* locomotion presentation
* dash, hurt, and death one-shot presentation

### Presenter Owns

* event routing between the two
* translating gameplay events into animation intent without leaking Animator calls into gameplay systems

## Why This Is Better

The old system worked, but it had a fragile contract:

* gameplay asked whether the shot was ready
* animation froze at an authored frame inside a longer clip
* repeated shots and interrupts had too many edge cases

The current system is easier to reason about because each clip has one clear job:

* `Draw` gets to ready
* `Hold` represents ready
* `Release` follows through

That is the core improvement.

## Current Interrupt Policy

The player now treats active bow draw as something that can be cleanly canceled by higher-priority gameplay states.

Current baseline:

* dash cancels draw
* hurt cancels draw
* death cancels draw
* drawing is blocked while dashing
* drawing is blocked while an ability is actively using the bow

This should remain gameplay-driven. Animator should show the result, not decide the rule.

## Current Animator Direction

The project has not moved to a fully parameter-driven Animator graph for every state, and that is fine.

The active direction is a practical hybrid:

* use Animator as the playback and transition surface
* keep explicit code control where gameplay timing matters
* avoid rebuilding the entire gameplay state machine inside Animator

For this sprite-based 4-direction setup, that is still the right tradeoff.

## What Is Good Enough To Keep

These decisions still look correct:

* keep the `Presenter -> Controller -> View` layering
* keep directional authored sprite states instead of overusing blend trees
* keep data-driven animation naming and tuning in ScriptableObjects
* keep gameplay as the authority on combat validity and interrupts

## What We Explicitly Moved Away From

These are no longer the recommended baseline:

* `ShootF` / `ShootS` as the core bow runtime model
* freezing the bow at a guessed normalized time inside one larger clip
* letting animation events act as the authority for whether a shot is valid

That older model was useful for iteration, but it should not be treated as the target architecture anymore.

## Future Evolution Worth Remembering

Nothing urgent is left for the current bow flow, but a few future directions are still valid if the game needs them later:

* upper-body and lower-body split if ranged movement while holding becomes important
* dedicated hold-move clips if split-body animation is not worth the complexity
* more Animator-owned locomotion transitions if the action set grows significantly
* additional ability-specific presentation layers if bow abilities become more visually unique

Those are future upgrades, not current requirements.

## Bottom Line

The player animation system is in a healthier place now.

The bow flow especially has crossed the important threshold from:

* prototype logic that worked but felt fragile

to:

* a clean, understandable hybrid that matches the authored clips and gameplay rules

That should be the reference point for future player animation work.
