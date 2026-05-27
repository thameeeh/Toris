# Damage Numbers System Plan

## Goal

Show a short-lived damage number at the world position where a direct combat hit is resolved.

The game is a primarily 32x32 isometric pixel-art game, so damage text must remain crisp, small, and readable above compact actors without visually overwhelming combat.

The first implementation must cover:

- player hits enemy
- enemy hits player
- direct hits only; damage-over-time ticks are deferred
- settings-menu integration initially deferred until the runtime presentation was stable

## Architectural Direction

Damage numbers are presentation feedback. They must not own combat rules, mutate health, or decide whether damage is valid. Combat resolution may report a zero-damage result for an invulnerable or blocked direct hit so the presenter can display `0`.

The preferred implementation is a pooled world-space text effect:

- use `TextMeshPro`, not `TextMeshProUGUI`
- do not add a Canvas for damage numbers
- spawn through an event-driven presentation bridge
- pool instances through the existing effect/presentation pipeline
- feed each popup a simple payload: damage amount, world position, target kind, and optional variant data
- animate each popup upward while fading out

This keeps the feature aligned with the existing SFX/VFX architecture, where gameplay exposes confirmed events and presentation systems react.

## Implementation Status

The first-pass runtime system is implemented through:

- `DamageNumberEventsSO`, a shared ScriptableObject event channel for resolved direct-hit feedback
- `IDirectHitDamageable`, which lets attacks pass known enemy impact positions without changing existing `IDamageable` callers
- `DamageNumberPresenter`, attached to the effect-manager prefabs and responsible for requesting pooled display effects
- `DamageNumberPopup`, a pooled world-space TMP effect that rounds the value, drifts upward, and fades out
- `FX_DamageNumber`, registered in the existing `EffectLibrary` under `damage_number`

The current enemy, player, and effect-manager prefabs are wired to the shared event channel. The prologue wolf variant inherits its channel binding from the minion wolf prefab.

## Why Not Canvas

The project uses UI Toolkit for screen UI and world-space sprite/effect systems for gameplay feedback. A Canvas-based damage-number layer would introduce a separate UI path, extra coordinate conversion, and another lifecycle surface.

World-space `TextMeshPro` uses a mesh renderer and can sit naturally above actors in the scene. That matches the desired "where it was hit" behavior without creating a Canvas dependency.

## Font Asset Workflow

The game already uses a pixel-art font in UI Toolkit USS, currently referenced from:

`Assets/Art/PixelArtGUI/Fonts/bitcell_memesbruh03.ttf`

To reuse that look for world-space damage numbers:

1. In the Unity Editor, open `Window > TextMeshPro > Font Asset Creator`.
2. Set `Source Font File` to `bitcell_memesbruh03.ttf`.
3. Use `Custom Characters` containing `0123456789-+!` for the first pass.
4. Start with a small atlas, such as `512 x 512`, because the first pass renders only numbers and basic punctuation.
5. Prefer a raster-style TMP render mode for crisp pixel-art text; verify that the saved atlas texture uses point filtering and does not blur at gameplay scale.
6. Keep future feedback words such as `Invulnerable!` or `Blocked!` out of the first atlas unless they are implemented at the same time; the font asset can be regenerated with those letters later.
7. Generate the font atlas.
8. Save the generated TMP font asset under a project-owned art or UI asset folder.
9. Assign that TMP font asset to the damage-number prefab's `TextMeshPro` component.

The runtime system should reference the prefab or effect definition, not the raw font file.

The first-pass TMP font asset is generated and assigned to the damage-number effect at:

`Assets/Art/PixelArtGUI/Fonts/bitcell_memesbruh03_TMP_RASTER.asset`

Use this `RASTER` asset for the initial damage-number prefab. `DamageNumberPopup` creates and configures its world-space `TextMeshPro` mesh once per pooled instance, using this asset and no Canvas. Regenerate a `RASTER_HINTED` comparison only if the gameplay-scale preview shows blurred or uneven digits.

## Event Flow

The intended flow is:

1. Combat system resolves a direct hit.
2. The target emits a resolved direct-hit presentation event with the final applied amount, including `0` for a blocked or invulnerable direct hit in the first implementation.
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

Accepted direct damage should produce its final applied amount. A rejected direct hit due to invulnerability or blocking should produce zero-damage presentation feedback without changing health. This may require a resolved-hit feedback event before the current i-frame early return.

## Presentation Effect

The damage number effect should be a prefab with:

- `TextMeshPro`
- a small runtime component that implements effect parameter receiving or a dedicated setup method
- configurable lifetime, upward float distance, spread, scale, fade, and color
- a light gray or white outgoing-damage style for damage dealt to enemies
- a red incoming-damage style for damage dealt to the player
- display of whole-number damage only
- no gameplay dependencies

The first pass can use fixed animation in script. If designers later want authored timing, an Animator can be added without changing the gameplay event flow.

## Deferred Behavior

- Damage-over-time ticks do not produce popups in the first implementation.
- Blocked or invulnerable direct hits display `0` in the first implementation without applying damage.
- Richer feedback labels such as `Invulnerable!` or `Blocked!` are deferred until the direct-hit system is established.
- Damage-number visibility is now exposed as a global Gameplay setting after the initial runtime pass.

Displayed damage uses conventional whole-number rounding through `Mathf.RoundToInt`, so `47.534` displays as `48`.

## First Implementation Pass

Implemented first pass:

1. Added a small damage-number payload type and ScriptableObject event channel.
2. Added contextful direct-hit enemy damage without removing `Damaged(float)`.
3. Emitted player damage-number requests from `PlayerDamageReceiver` after direct-hit resolution.
4. Added a pooled world-space `TextMeshPro` damage-number prefab and presenter.
5. Wired enemy and player damage events through the existing EffectManager host.
6. Passed exact enemy impact positions from arrow, arrow rain, and chain shot attacks.
7. Excluded necromancer sustained-contact ticks from first-pass display.
8. Formatted displayed damage as whole numbers using `Mathf.RoundToInt`.
9. Resolved wolf bite hit payloads at attack time so incoming popups anchor on the bite-facing side of the player.
10. Added active-popup separation so rapid hits near the same area fan into readable adjacent positions.

## Rapid Hit Separation

`DamageNumberPopup` tracks only currently playing popup instances for presentation layout. A new number first tries the actual requested hit position. If that position is too close to a visible damage number, it selects the first free offset slot in an upward side-to-side fan.

The prefab exposes minimum separation distance, horizontal step, vertical step, and maximum alternate slots so the layout can be tuned alongside font size and camera zoom. This changes presentation placement only; resolved damage values and combat events remain authoritative and unchanged.

## Decisions Confirmed

- First version shows direct-hit damage only.
- Outgoing enemy damage numbers use a white or light-gray style; incoming player damage numbers use red.
- Damage numbers drift upward and fade out.
- The first version shows `0` for blocked or invulnerable direct-hit feedback; named labels are future work.
- A later settings pass adds a global Damage Numbers toggle; the presenter suppresses popups while combat resolution remains unchanged.
- Decimal damage is displayed using conventional whole-number rounding (`47.534` displays `48`).

## Verification

Local automated verification is limited because the environment does not have a headless Unity Editor.

Static verification:

- no Canvas or `TextMeshProUGUI` dependency is introduced
- existing `Damaged(float)` subscribers still compile
- damage numbers only spawn after a resolved direct-hit outcome
- blocked or invulnerable direct hits display `0` without mutating health
- damage-over-time does not spawn a number in the first implementation
- the Damage Numbers setting gates presentation spawning only and does not mutate combat outcomes
- new debug logs, if any, are wrapped in `#if UNITY_EDITOR`
- magic values are constants or serialized fields
- pooled instances reset text, color, alpha, position, and scale on reuse

Manual Unity checks:

- let Unity import the new scripts/assets and confirm no Console compile or serialization errors
- arrow hit on enemy shows a number near the hit
- arrow rain and chain shot show numbers at their resolved hit points
- wolf, boar, necromancer, and blood mage hits on player show numbers near the player hurt area
- i-frame-blocked or otherwise invulnerable direct hits show `0` without reducing health
- outgoing damage is light colored and incoming damage is red
- popups remain crisp and legible at the normal isometric gameplay zoom
- popups float upward and fade out
- pooled numbers reset correctly during rapid hits
- numbers render above actors without requiring a Canvas
- tune `FX_DamageNumber` font size, offset, rise distance, and sorting order if the normal gameplay zoom asks for it
- toggle Damage Numbers off in Settings and confirm direct hits still apply damage without spawning popups; toggle it back on and confirm popups resume
