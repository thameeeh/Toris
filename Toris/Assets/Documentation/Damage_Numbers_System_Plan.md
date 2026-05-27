# Damage Numbers System Plan

## Goal

Show short-lived world-space combat feedback where a direct hit is resolved or status damage is applied to the player, plus immediate weapon-use failure feedback above the player.

The game is a primarily 32x32 isometric pixel-art game, so damage text must remain crisp, small, and readable above compact actors without visually overwhelming combat.

The first implementation must cover:

- player hits enemy
- enemy hits player
- player status application labels for poison, burning, and bleeding
- player poison, burning, and bleeding damage-over-time ticks
- bow underdraw feedback through the existing bow event channel
- settings-menu integration initially deferred until the runtime presentation was stable

## Architectural Direction

Damage numbers are presentation feedback. They must not own combat rules, mutate health, or decide whether damage is valid. Combat and status resolution report authoritative applied results, allowing the presenter to display a number, a defensive result such as `Invulnerable!`, or numeric `0` for the player's post-hit grace placeholder.

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

- `DamageNumberEventsSO`, a shared ScriptableObject event channel for resolved direct-hit and player status-tick feedback
- `IDirectHitDamageable`, which lets attacks pass known enemy impact positions without changing existing `IDamageable` callers
- `DamageNumberPresenter`, attached to the effect-manager prefabs and responsible for requesting pooled display effects
- `DamageNumberPopup`, a pooled world-space TMP effect that renders numeric or implemented message feedback, drifts upward, and fades out
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
3. Use `Custom Characters` containing `0123456789-+.! InvulnerableShot FailedPoisonedBurningBleeding` for the current feedback set.
4. Start with a small atlas, such as `512 x 512`, because the current pass renders numbers, punctuation, and short outcome/status messages.
5. Prefer a raster-style TMP render mode for crisp pixel-art text; verify that the saved atlas texture uses point filtering and does not blur at gameplay scale.
6. Add future feedback words such as `Blocked!` only when their gameplay outcome is implemented, then regenerate the atlas with the additional letters.
7. Generate the font atlas.
8. Save the generated TMP font asset under a project-owned art or UI asset folder.
9. Assign that TMP font asset to the damage-number prefab's `TextMeshPro` component.

The runtime system should reference the prefab or effect definition, not the raw font file.

The first-pass TMP font asset is generated and assigned to the damage-number effect at:

`Assets/Art/PixelArtGUI/Fonts/bitcell_memesbruh03_TMP_RASTER.asset`

Use this `RASTER` asset for the damage-number prefab. Regenerate it with the current custom-character string before testing the status application labels, as those labels require additional `P`, `B`, `s`, and `g` glyphs beyond the prior set. `DamageNumberPopup` creates and configures its world-space `TextMeshPro` mesh once per pooled instance, using this asset and no Canvas. Regenerate a `RASTER_HINTED` comparison only if the gameplay-scale preview shows blurred or uneven text.

## Event Flow

The intended flow is:

1. Combat system resolves a direct hit.
2. The target emits a resolved direct-hit presentation event with the final applied amount or an authoritative rejected-hit outcome.
3. The event includes the best available world hit position.
4. A damage-number presenter or effect bridge receives the event.
5. The presenter requests a pooled damage-number effect.
6. The effect instance formats a number or resolved message, animates, fades, and returns to the pool.

Bow underdraw feedback uses the existing `PlayerBowEventsSO.UnderdrawReleased` event. `DamageNumberPresenter` observes that neutral bow outcome and requests a `Shot Failed` popup above the player without creating a damage event.

Player status feedback uses the existing authoritative `PlayerStatusController` events. When a new condition succeeds, `OnStatusApplied` becomes a matching `Poisoned!`, `Burning!`, or `Bleeding!` popup above the player. A refresh of an already-active condition does not repeat that announcement. After health changes, `OnStatusDamageTick` becomes a colored numeric status-tick request. Missed-frame catch-up remains aggregated by the status system, so presentation receives one popup request for the actual applied tick amount.

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

Accepted direct damage should produce its final applied amount. The player's post-hit grace rejection remains numeric `0` and does not claim a defensive ability occurred.

## Presentation Effect

The damage number effect should be a prefab with:

- `TextMeshPro`
- a small runtime component that implements effect parameter receiving or a dedicated setup method
- configurable lifetime, upward float distance, spread, scale, fade, and color
- a light gray or white outgoing-damage style for damage dealt to enemies
- a red incoming-damage style for damage dealt to the player
- a green poison-tick style, orange burning-tick style, and red bleeding-tick style for player status damage
- matching green `Poisoned!`, orange `Burning!`, and red `Bleeding!` labels when a status first begins
- display of whole-number damage plus implemented outcome messages
- no gameplay dependencies

The first pass can use fixed animation in script. If designers later want authored timing, an Animator can be added without changing the gameplay event flow.

## Deferred Behavior

- Player poison, burning, and bleeding damage-over-time ticks display numeric popups after damage is applied.
- Newly applied player statuses display `Poisoned!`, `Burning!`, or `Bleeding!`; reapplying an already active status only refreshes gameplay state.
- Repeating contact or sustain damage, such as the necromancer projectile overlap tick, remains silent to prevent continuous popup clutter.
- Outgoing damage-over-time against enemies remains deferred until an authoritative enemy status-damage path exists.
- The necromancer's active summon-protection shield displays `Invulnerable!` when it rejects a direct player hit.
- Player post-hit grace still displays `0`; it is not labeled as defensive invulnerability.
- `Blocked!`, dodge, and evasion feedback remain deferred until authoritative mechanics produce those outcomes.
- Bow underdraw displays `Shot Failed` above the player.
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
11. Added `Invulnerable!` feedback for active necromancer summon-protection rejections.
12. Added `Shot Failed` feedback for underdraw bow releases through `PlayerBowEventsSO`.
13. Added player poison, burning, and bleeding tick popups with status-specific colors.
14. Added one-time status application labels for newly acquired poison, burning, and bleeding effects.

## Rapid Hit Separation

`DamageNumberPopup` tracks only currently playing popup instances for presentation layout. A new number first tries the actual requested hit position. If that position is too close to a visible damage number, it selects the first free offset slot in an upward side-to-side fan.

The prefab exposes minimum separation distance, horizontal step, vertical step, and maximum alternate slots so the layout can be tuned alongside font size and camera zoom. This changes presentation placement only; resolved damage values and combat events remain authoritative and unchanged.

## Decisions Confirmed

- First version shows direct-hit damage only.
- Outgoing enemy damage numbers use a white or light-gray style; incoming player damage numbers use red.
- Damage numbers drift upward and fade out.
- Active necromancer defensive invulnerability displays `Invulnerable!`; player post-hit grace continues to display `0`.
- Underdraw releases display `Shot Failed`; no projectile or damage event is fabricated.
- Applied player poison ticks use green, burning ticks use orange, and bleeding ticks reuse incoming red.
- Newly applied poison, burning, and bleeding show matching colored status labels once per newly started condition.
- Enemy outgoing DoT and repeating sustain/contact damage are intentionally not displayed yet.
- `Blocked!` and dodge/evasion messages are intentionally not wired yet.
- A later settings pass adds a global Damage Numbers toggle; the presenter suppresses popups while combat resolution remains unchanged.
- Decimal damage is displayed using conventional whole-number rounding (`47.534` displays `48`).

## Verification

Local automated verification is limited because the environment does not have a headless Unity Editor.

Static verification:

- no Canvas or `TextMeshProUGUI` dependency is introduced
- existing `Damaged(float)` subscribers still compile
- damage numbers only spawn after a resolved direct-hit outcome
- active necromancer shield rejections emit `Invulnerable!` without mutating health
- player post-hit grace rejections emit numeric `0` without mutating health
- bow underdraw emits `Shot Failed` without spawning a projectile
- player status damage spawns one numeric popup for each applied poison, burning, or bleeding tick event
- successful new player status applications spawn their matching colored status labels
- sustained-contact projectile damage remains excluded from popups
- the Damage Numbers setting gates presentation spawning only and does not mutate combat outcomes
- new debug logs, if any, are wrapped in `#if UNITY_EDITOR`
- magic values are constants or serialized fields
- pooled instances reset text, color, alpha, position, and scale on reuse

Manual Unity checks:

- let Unity import the new scripts/assets and confirm no Console compile or serialization errors
- arrow hit on enemy shows a number near the hit
- arrow rain and chain shot show numbers at their resolved hit points
- wolf, boar, necromancer, and blood mage hits on player show numbers near the player hurt area
- a direct hit rejected by active necromancer summon protection shows `Invulnerable!` without reducing health
- player post-hit grace rejects follow-up direct hits with `0`, not `Invulnerable!`
- an underdraw release shows `Shot Failed` above the player without firing an arrow
- poison ticks show green damage numbers above the player
- burning ticks show orange damage numbers above the player
- bleeding ticks show red damage numbers above the player
- first poison application shows green `Poisoned!`, first burning application shows orange `Burning!`, and first bleeding application shows red `Bleeding!`
- refreshing an already active status does not repeatedly show its application label
- necromancer projectile sustained-contact overlap does not repeatedly produce popups
- outgoing damage is light colored and incoming damage is red
- popups remain crisp and legible at the normal isometric gameplay zoom
- popups float upward and fade out
- pooled numbers reset correctly during rapid hits
- numbers render above actors without requiring a Canvas
- tune `FX_DamageNumber` font size, offset, rise distance, and sorting order if the normal gameplay zoom asks for it
- toggle Damage Numbers off in Settings and confirm direct hits still apply damage without spawning popups; toggle it back on and confirm popups resume
