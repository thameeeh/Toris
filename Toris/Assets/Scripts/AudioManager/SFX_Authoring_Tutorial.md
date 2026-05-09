# Player SFX Authoring Tutorial

This tutorial covers the data-driven player SFX workflow using `PlayerSfxRuleSO`.

## One-Time Setup

The player prefab should have:

- `PlayerSfx`
- `PlayerSfxEventBridge`

`PlayerSfxEventBridge` listens to player gameplay events. `PlayerSfx` owns runtime loop handles and evaluates rule assets.

Existing bow, dash, and footstep module assets can remain in `PlayerSfx.legacyModules`. New authored player sounds should usually go into `PlayerSfx.rules`.

## Add A New One-Shot SFX

1. Import the audio clip.
2. Create or update a `SfxDefinition`.
3. Add the `SfxDefinition` to the active `SfxLibrary`.
4. Choose a unique SFX ID, such as `Player_HealPotion`.
5. Create an asset with `Create > Audio > Player SFX Rule`.
6. Configure the rule:
   - `Trigger`: the gameplay event that should play the sound.
   - `Playback Mode`: usually `AttachedOneShot` or `OneShot2D`.
   - `Sfx Id`: the ID from the `SfxLibrary`.
   - `Volume Multiplier`, `Pitch Offset`, `Pitch Multiplier`, and `Force 2D` as needed.
7. Add the rule asset to the player prefab's `PlayerSfx.rules` list.

## Add A Healing Potion Sound

Create `PlayerSfxRule_HealPotion`.

- Trigger: `Healed`.
- Playback Mode: `AttachedOneShot`.
- SFX ID: `Player_HealPotion`.
- Minimum Amount: set this above passive regeneration if passive regen should stay silent.
- Cooldown Seconds: optional, such as `0.05`.
- Force 2D: enabled if the heal should always sound centered.

## Add A Mana Or Stamina Potion Sound

Create `PlayerSfxRule_StaminaPotion`.

- Trigger: `StaminaRestored`.
- Playback Mode: `AttachedOneShot` or `OneShot2D`.
- SFX ID: `Player_StaminaPotion`.
- Minimum Amount: set above normal regeneration if only potion-sized restores should play.

If the game later has a separate mana resource, add a new player event in the bridge for mana and reuse the same rule pattern.

## Add A Poison Loop

Create start rule `PlayerSfxRule_PoisonLoop_Start`.

- Trigger: `StatusApplied`.
- Filter Status Type: enabled.
- Status Type: `Poison`.
- Playback Mode: `StartAttachedLoop`.
- SFX ID: `Player_PoisonLoop`.
- Loop Key: `status_poison`.
- Fade Out Seconds: unused by the start rule.

Create stop rule `PlayerSfxRule_PoisonLoop_Stop`.

- Trigger: `StatusRemoved`.
- Filter Status Type: enabled.
- Status Type: `Poison`.
- Playback Mode: `StopLoop`.
- Loop Key: `status_poison`.
- Fade Out Seconds: choose a small fade such as `0.15`.

Both rules must use the same loop key.

## Add A Bleed Tick Sound

Create `PlayerSfxRule_BleedTick`.

- Trigger: `StatusDamageTick`.
- Filter Status Type: enabled.
- Status Type: `Bleeding`.
- Playback Mode: `AttachedOneShot` or `OneShot2D`.
- SFX ID: `Player_BleedTick`.
- Use Event Amount As Volume: optional.
- Event Amount Volume Scale: tune so normal tick damage does not become too loud.

## Add Footsteps With Rules

The legacy footstep module can stay in use. If replacing it with rules:

Create start rule:

- Trigger: `MovementStarted`.
- Playback Mode: `StartAttachedLoop`.
- SFX ID: `Player_Footstep`.
- Loop Key: `movement_footsteps`.

Create stop rule:

- Trigger: `MovementStopped`.
- Playback Mode: `StopLoop`.
- Loop Key: `movement_footsteps`.
- Fade Out Seconds: `0.08`.

Tune `PlayerSfxEventBridge.movementStartSpeed` on the player prefab if footsteps start too early or too late.

## Rule Field Guide

- `Trigger`: the player event that activates the rule.
- `Filter Status Type`: restricts status events to Poison, Burning, or Bleeding.
- `Minimum Amount`: ignores small resource changes or small damage ticks.
- `Cooldown Seconds`: prevents rapid repeat spam.
- `Playback Mode`: one-shot, attached one-shot, start loop, or stop loop.
- `Loop Key`: the stable key used to stop a loop later.
- `Offset`: local player offset for attached playback; world offset for event-position playback.
- `Force 2D`: plays without spatial positioning.
- `Use Event Amount As Volume`: scales volume from heal/damage/tick amount.

## Checklist

- SFX ID exists in `SfxLibrary`.
- Rule is assigned to `PlayerSfx.rules`.
- Loop start and stop rules share the same loop key.
- Cooldown is not blocking the sound during testing.
- Minimum amount is not higher than the actual event amount.
