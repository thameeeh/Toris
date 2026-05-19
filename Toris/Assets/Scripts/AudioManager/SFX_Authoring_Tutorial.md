# Player SFX Authoring Tutorial

This tutorial covers the data-driven player SFX workflow using `PlayerSfxRuleSO`.

## One-Time Setup

The player prefab should have:

- `PlayerSfx`
- `PlayerSfxEventBridge`

`PlayerSfxEventBridge` listens to player gameplay events. `PlayerSfx` owns runtime loop handles and evaluates rule assets.

Existing bow, dash, and footstep module assets can remain in `PlayerSfx.legacyModules`. New authored player sounds should usually go into `PlayerSfx.rules`.

## SFX-Only Code Rule

When adding an SFX hook inside gameplay, UI, or manager code, mark the block with a short comment that says it is for SFX only. The hook must not become the owner of gameplay state, transaction success, inventory mutation, quest progress, or UI visibility.

Good SFX hooks should sit after the authoritative action succeeds. For example, play a shop coin sound after `ShopManagerSO` confirms the buy/sell transaction, not when the button is clicked.

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

- Trigger: `HealthConsumableUsed`.
- Playback Mode: `OneShotAtEventPosition`, `AttachedOneShot`, or `OneShot2D`.
- SFX ID: `Player_HealPotion`.
- Minimum Amount: optional. Leave it at `0` unless only larger HP consumables should play this rule.
- Cooldown Seconds: optional, such as `0.05`.
- Force 2D: enabled if the heal should always sound centered.

## Add A Mana Or Stamina Potion Sound

Create `PlayerSfxRule_StaminaPotion`.

- Trigger: `ManaConsumableUsed`.
- Playback Mode: `AttachedOneShot` or `OneShot2D`.
- SFX ID: `Player_StaminaPotion`.
- Minimum Amount: optional. Leave it at `0` unless only larger mana/stamina consumables should play this rule.

Use `Healed`, `Damaged`, `StaminaRestored`, and `StaminaSpent` for general resource-change sounds. Use the consumable triggers for potion/item-use sounds so startup restoration, passive regeneration, and stat recalculation stay silent.
Resource-change triggers are silent during initialization, world-transfer restoration, and resolved-stat recalculation.
For a broad `HealthChanged` or `StaminaChanged` rule that should react to direct damage/restores but not passive regen or heal-over-time ticks, enable `Ignore Regeneration Resource Changes`.

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

## Add UI Screen Open And Close Sounds

Use `UISfxEventBridge` for screen lifecycle sounds such as inventory zipper open/close.

1. Import and define the clips in `SfxLibrary`.
2. Add `UISfxEventBridge` to a scene object that lives beside the UI manager or audio manager.
3. Assign the active `UIEventsSO`.
4. For inventory, use the built-in fields:
   - `Inventory Open Sfx Id`: for example `ui_inventory_open`.
   - `Inventory Close Sfx Id`: for example `ui_inventory_close`.
5. For other screens, add a screen rule:
   - `Screen`: for example `Smith`.
   - `Open Sfx Id`: the SFX ID to play when that screen opens.
   - `Close Sfx Id`: the SFX ID to play when that screen closes.
   - `Force 2D`: usually enabled for UI sounds.

The bridge listens to `OnScreenOpen` and `OnScreenClose`, so it plays only after a view actually shows or hides.

## Add UI Button Hover And Confirm Sounds

Default UI Toolkit button hover/click sounds are emitted by `GameView` through `UIEventsSO`.

1. Import and define `ui_menu_hover` and `ui_menu_confirm`.
2. Add one `UISfxEventBridge` to a scene object that lives beside the UI manager or audio manager.
3. Assign the active `UIEventsSO`.
4. On that `UIEventsSO`, set:
   - `Button Hover Sfx Id`: for example `ui_menu_hover`.
   - `Button Confirm Sfx Id`: for example `ui_menu_confirm`.
   - `Button Hover Cooldown Seconds`: a small value such as `0.04`.
5. On `UISfxEventBridge`, keep `Force 2D` enabled for normal UI sounds.

Any class derived from `GameView` now routes `Button` hover and click events through the same UI event channel. Do not add a per-`UIDocument` button SFX component.

The scene still needs an active `AudioManagerBehaviour`; otherwise `AudioBootstrap.Sfx` does not exist and no UI sound can play.

## Add Shop And Gold Sounds

Shop coin sounds are confirmed by `ShopManagerSO`, not by the clicked button. Set `Coin Transaction Sfx Id` on the shop manager asset to `ui_coin_purchase`; it plays only after a buy or sell transaction succeeds.

Enemy gold reward sounds are configured per `EnemyLootTableSO`. Set `Gold Reward Sfx Id` to `item_coin_pickup`; it plays only when that loot table grants immediate gold.

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
