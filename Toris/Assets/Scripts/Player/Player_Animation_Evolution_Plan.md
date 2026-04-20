# Player Animation Evolution Guide

Use this document as the higher-level reference for how the player animation system should evolve.

This guide is about structure and direction, not only about the current bow implementation.

## Purpose

Keep the player animation stack understandable, data-driven, and safe to expand.

Use these principles:

* keep gameplay authority outside the Animator
* keep presentation logic layered and readable
* prefer authored directional clips over overbuilt graph logic for this sprite setup
* evolve toward cleaner contracts, not more special cases

## Core Layering

Keep this separation:

* `PlayerAnimationPresenter` is the presentation bridge
* `PlayerAnimationController` is animation-facing runtime glue
* `PlayerAnimationView` wraps Animator access
* `CharacterAnimSO` and `WeaponProfile` hold animation-facing data

Use this layering when adding new animation-driven actions.

Do not let gameplay scripts talk directly to the Animator when the presenter and controller can carry the intent cleanly.

## Bow Standard

Use authored phases for bow presentation:

* `ShootDraw`
* `ShootHold`
* `ShootRelease`

This should remain the standard approach for the bow because each clip has one clear job:

* `Draw` gets to ready
* `Hold` represents ready
* `Release` follows through

## Gameplay And Animation Contract

Use this ownership split:

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

* routing gameplay events into animation intent
* keeping Animator calls out of gameplay scripts

## Animator Direction

Use a practical hybrid model.

For this project, that means:

* use Animator as the playback and transition surface
* keep explicit code control where gameplay timing matters
* avoid rebuilding the gameplay state machine inside Animator parameters and transitions

This is the right tradeoff for a 4-direction sprite character.

## Design Rules

Follow these rules when extending the player animation system:

* keep the `Presenter -> Controller -> View` layering
* keep directional authored sprite states
* keep animation naming and tuning data-driven
* keep gameplay as the authority on combat validity and interrupts

## Patterns To Avoid

Do not build future work around these patterns:

* multiple overlapping bow shoot variants whose semantic moments drift apart
* freezing a character at a guessed normalized time inside a longer clip
* letting animation events decide whether a combat action is valid

Those patterns make the system harder to reason about and harder to debug.

## Interrupt Policy

Treat interrupts as gameplay-driven rules that animation responds to.

Current baseline behavior should stay in that direction:

* dash cancels draw
* hurt cancels draw
* death cancels draw
* drawing is blocked while dashing
* drawing is blocked while an ability is actively using the bow

Animator should show the outcome of the rule.
Animator should not own the rule itself.

## Future Growth

If the action set expands later, these are valid directions:

* split upper and lower body if ranged movement while holding becomes important
* author dedicated hold-move clips if split-body animation is not worth the complexity
* move more locomotion transitions into Animator if the state set grows large
* add ability-specific presentation layers if bow abilities become more visually distinct

Treat those as upgrades, not as current requirements.

## Working Standard

When evaluating a future animation change, ask:

1. does gameplay still own validity and timing?
2. does each clip have one clear job?
3. does the presenter remain the bridge?
4. does the change make the system easier or harder to explain?

If a change weakens those answers, it is probably the wrong direction.
