# Player VFX Authoring Tutorial

This tutorial covers the data-driven player VFX workflow using `PlayerVfxRuleSO`.

## One-Time Setup

The player prefab should have:

- `PlayerVfx`
- `PlayerVfxEventBridge`

`PlayerVfxEventBridge` listens to player gameplay events. `PlayerVfx` owns runtime persistent effect handles and evaluates rule assets.

New authored player visuals should usually go into `PlayerVfx.rules`.

## Add A New One-Shot VFX

1. Create a visual effect prefab using particles, sprites, animation, VFX Graph, or custom scripts.
2. Add `EffectInstancePool` to the root if needed.
3. Configure sorting layer and order.
4. Add `IEffectPoolListener` reset scripts if the prefab has mutable state.
5. Add `IEffectParametersReceiver` scripts if variant or magnitude should affect visuals.
6. Add an `EffectDefinition` entry to the active `EffectLibrary`.
7. Choose a unique effect ID, such as `player_heal_potion`.
8. Create an asset with `Create > Effects > Player VFX Rule`.
9. Configure the rule:
   - `Trigger`: the gameplay event that should spawn the visual.
   - `Playback Mode`: usually `AttachedOneShot`, `OneShotAtPlayer`, or `OneShotAtEventPosition`.
   - `Effect Id`: the ID from the `EffectLibrary`.
   - `Offset`, `Rotation Mode`, `Variant`, and `Magnitude` as needed.
10. Add the rule asset to the player prefab's `PlayerVfx.rules` list.

## Add A Healing Potion VFX

Create `PlayerVfxRule_HealPotion`.

- Trigger: `Healed`.
- Playback Mode: `AttachedOneShot`.
- Effect ID: `player_heal_potion`.
- Minimum Amount: set this above passive regeneration if passive regen should not spawn it.
- Rotation Mode: usually `PlayerRotation` or `Identity`.
- Offset: local offset from the player center.

## Add A Mana Or Stamina Potion VFX

Create `PlayerVfxRule_StaminaPotion`.

- Trigger: `StaminaRestored`.
- Playback Mode: `AttachedOneShot`.
- Effect ID: `player_stamina_potion`.
- Minimum Amount: set above normal regeneration if only potion-sized restores should spawn it.

If the game later has a separate mana resource, add a new player event in the bridge for mana and reuse the same rule pattern.

## Add A Poison Persistent VFX

Create start rule `PlayerVfxRule_Poison_Start`.

- Trigger: `StatusApplied`.
- Filter Status Type: enabled.
- Status Type: `Poison`.
- Playback Mode: `StartPersistentAttached`.
- Effect ID: `player_poison_loop`.
- Persistent Key: `status_poison`.

Create stop rule `PlayerVfxRule_Poison_Stop`.

- Trigger: `StatusRemoved`.
- Filter Status Type: enabled.
- Status Type: `Poison`.
- Playback Mode: `ReleasePersistent`.
- Persistent Key: `status_poison`.

Both rules must use the same persistent key.

## Add A Bleed Tick VFX

Create `PlayerVfxRule_BleedTick`.

- Trigger: `StatusDamageTick`.
- Filter Status Type: enabled.
- Status Type: `Bleeding`.
- Playback Mode: `AttachedOneShot` or `OneShotAtPlayer`.
- Effect ID: `player_bleed_tick`.
- Use Event Amount As Magnitude: optional.
- Event Amount Magnitude Scale: tune so normal tick damage does not over-scale the effect.

## Add A Dash Burst VFX

The animation-authored dash afterimage still lives in `PlayerAnimationController` and uses the `DashP` animation strip. Keep that for the sprite trail.

For an additional pooled effect:

- Trigger: `DashStarted` or `DashCompleted`.
- Playback Mode: `OneShotAtPlayer` or `AttachedOneShot`.
- Rotation Mode: `EventDirection` for dash-start direction.
- Effect ID: dash burst effect ID.

## Rule Field Guide

- `Trigger`: the player event that activates the rule.
- `Filter Status Type`: restricts status events to Poison, Burning, or Bleeding.
- `Minimum Amount`: ignores small resource changes or small damage ticks.
- `Cooldown Seconds`: prevents rapid repeat spam.
- `Playback Mode`: one-shot, attached one-shot, start persistent, or release persistent.
- `Persistent Key`: the stable key used to release a persistent effect later.
- `Offset`: local player offset for attached/player playback; world offset for event-position playback.
- `Rotation Mode`: identity, player rotation, facing direction, event direction, or bow aim direction.
- `Variant`: optional effect-specific variant data.
- `Use Event Amount As Magnitude`: scales effect intensity from heal/damage/tick amount.

## Checklist

- Effect ID exists in `EffectLibrary`.
- Rule is assigned to `PlayerVfx.rules`.
- Persistent start and release rules share the same persistent key.
- Cooldown is not blocking the effect during testing.
- Minimum amount is not higher than the actual event amount.
- The effect prefab returns to the pool through lifetime, animation event, or manual release.
