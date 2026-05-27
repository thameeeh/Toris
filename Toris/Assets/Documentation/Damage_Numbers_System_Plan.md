# Damage Numbers System Plan

## Goal

Show a short-lived damage number at the world position where confirmed damage lands.

The system must cover:

- player hits enemy
- enemy hits player
- damage-over-time ticks, if enabled for the final feature
- future settings support for turning damage numbers on or off

## Architectural Direction

Damage numbers are presentation feedback. They must not own combat rules, mutate health, or decide whether damage is valid.

The preferred implementation is a pooled world-space text effect:

- use `TextMeshPro`, not `TextMeshProUGUI`
- do not add a Canvas for damage numbers
- spawn through an event-driven presentation bridge
- pool instances through the existing effect/presentation pipeline
- feed each popup a simple payload: damage amount, world position, target kind, and optional variant data

This keeps the feature aligned with the existing SFX/VFX architecture, where gameplay exposes confirmed events and presentation systems react.

## Why Not Canvas

The project uses UI Toolkit for screen UI and world-space sprite/effect systems for gameplay feedback. A Canvas-based damage-number layer would introduce a separate UI path, extra coordinate conversion, and another lifecycle surface.

World-space `TextMeshPro` uses a mesh renderer and can sit naturally above actors in the scene. That matches the desired "where it was hit" behavior without creating a Canvas dependency.

## Font Asset Workflow

The game already uses a pixel-art font in UI Toolkit USS, currently referenced from:

`Assets/Art/PixelArtGUI/Fonts/bitcell_memesbruh03.ttf`

To reuse that look for world-space damage numbers:

1. In the Unity Editor, open `Window > TextMeshPro > Font Asset Creator`.
2. Set `Source Font File` to `bitcell_memesbruh03.ttf`.
3. Use an atlas size large enough for digits and common symbols. A small atlas is enough if the damage text only needs `0-9`, `-`, `+`, and optional crit/status symbols.
4. Generate the font atlas.
5. Save the generated TMP font asset under a project-owned art or UI asset folder.
6. Assign that TMP font asset to the damage-number prefab's `TextMeshPro` component.

The runtime system should reference the prefab or effect definition, not the raw font file.

## Event Flow

The intended flow is:

1. Combat system applies validated damage.
2. The damaged target emits a confirmed damage event with the final applied amount.
3. The event includes the best available world hit position.
4. A damage-number presenter or effect bridge receives the event.
5. The presenter requests a pooled damage-number effect.
6. The effect instance formats the number, animates, fades, and returns to the pool.

## Enemy Damage Events

Enemies currently expose `Damaged(float damage)` from `Enemy`.

That is enough for "enemy took damage" but not enough for exact hit location. To support accurate placement, add a contextful event while preserving the existing one:

- keep `Damaged(float damage)` for existing SFX/VFX subscribers
- add a new damage context payload containing amount and world position
- route player attacks through context-aware damage calls where the hit point is known

Direct callers that only know an amount can fall back to the enemy target position or transform position.

## Player Damage Events

Enemy-to-player damage already funnels through `PlayerDamageReceiver`.

That is the best place to emit player damage-number events because it has:

- the incoming `HitData`
- final damage after incoming damage modifiers
- the target hurtbox/collider position
- i-frame acceptance logic

Only accepted damage should produce a number.

## Presentation Effect

The damage number effect should be a prefab with:

- `TextMeshPro`
- a small runtime component that implements effect parameter receiving or a dedicated setup method
- configurable lifetime, float distance, spread, scale, fade, and color
- no gameplay dependencies

The first pass can use fixed animation in script. If designers later want authored timing, an Animator can be added without changing the gameplay event flow.

## Settings Hook

The Settings plan already reserves a damage-numbers toggle for after this feature exists.

Add the toggle only after the core system is functional. Store it through a small settings owner using `PlayerPrefs`, following the existing `LootMagnetSettings` pattern.

The presenter should check the setting before spawning a number.

## First Implementation Pass

Recommended first pass:

1. Add a small damage-number payload type.
2. Add a contextful enemy damage event without removing `Damaged(float)`.
3. Emit player damage-number events from `PlayerDamageReceiver` after final damage is confirmed.
4. Add a pooled world-space `TextMeshPro` damage-number prefab and presenter.
5. Wire enemy and player damage events to the presenter.
6. Add fallback placement for older call sites that only know the target.
7. Add the settings toggle after the feature is visible and working.

## Open Decisions

- Whether damage-over-time ticks should show numbers by default.
- Whether blocked, absorbed, or zero-damage hits should show special text.
- Whether player damage and enemy damage use different colors.
- Whether critical hits exist now or should be left as a future variant.
- Whether damage numbers should spawn exactly at contact points or slightly above target centers for readability.

## Verification

Local automated verification is limited because the environment does not have a headless Unity Editor.

Static verification:

- no Canvas or `TextMeshProUGUI` dependency is introduced
- existing `Damaged(float)` subscribers still compile
- damage numbers only spawn after accepted damage
- new debug logs, if any, are wrapped in `#if UNITY_EDITOR`
- magic values are constants or serialized fields
- pooled instances reset text, color, alpha, position, and scale on reuse

Manual Unity checks:

- arrow hit on enemy shows a number near the hit
- arrow rain and chain shot show numbers at their resolved hit points
- wolf, boar, necromancer, and blood mage hits on player show numbers near the player hurt area
- i-frame-blocked hits do not show numbers
- pooled numbers reset correctly during rapid hits
- numbers render above actors without requiring a Canvas
