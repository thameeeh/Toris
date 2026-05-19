# Consumables

Inspector-friendly notes for the consumable content pass.

## Status

- Current pass: stopped here for now.
- Assets created: `7`
- Icons: manual ownership; leave future icon work to the Editor pass.
- Next item class: `Armor`
- Recipes and salvage: not authored yet.
- Item database registration: done.
- Shop, starter inventory, and enemy loot registration: still to do when we wire items into gameplay sources.

## System Fit

- Asset type: `InventoryItemSO`
- Main component: `ConsumableComponent`
- Runtime behavior: `PlayerConsumableController`
- Potion slots: accept items with `ConsumableComponent`
- Instant health payload: `HP`
- Instant stamina payload: `Mana`
- Timed effects: require a valid `PlayerEffectDefinitionSO`
- Existing timed effect assets used here: `HealthRegen.asset`, `MoveSpeed.asset`

## Authored Assets

### Minor Healing Potion

- Asset ID: `Minor_Healing_Potion`
- Item name: `minor_healing_potion`
- Description: A small potion that grants instant health
- Stack: `10`
- Gold: `50`
- Effect mode: `InstantResource`
- Payload: `HP`
- Amount: `15`
- Cooldown: `18`
- Timed effect: none
- Notes: Low-tier healing potion.

### Major Healing Potion

- Asset ID: `Major_Healing_Potion`
- Item name: `major_healing_potion`
- Description: A big potion that grants instant health
- Stack: `4`
- Gold: `150`
- Effect mode: `InstantResource`
- Payload: `HP`
- Amount: `35`
- Cooldown: `30`
- Timed effect: none
- Notes: Higher-tier healing potion.

### Minor Stamina Potion

- Asset ID: `Minor_Stamina_Potion`
- Item name: `minor_stamina_potion`
- Description: A small potion that gives stamina
- Stack: `12`
- Gold: `35`
- Effect mode: `InstantResource`
- Payload: `Mana`
- Amount: `15`
- Cooldown: `18`
- Timed effect: none
- Notes: Low-tier stamina potion. The enum says `Mana`, but current code maps this path to stamina.

### Major Stamina Potion

- Asset ID: `Major_Stamina_Potion`
- Item name: `major_stamina_potion`
- Description: A big potion that gives stamina
- Stack: `6`
- Gold: `125`
- Effect mode: `InstantResource`
- Payload: `Mana`
- Amount: `40`
- Cooldown: `30`
- Timed effect: none
- Notes: Higher-tier stamina potion.

### Field Ration

- Asset ID: `Field_Ration`
- Item name: `field_ration`
- Description: A plain ration that restores a little health in a pinch.
- Stack: `10`
- Gold: `20`
- Effect mode: `InstantResource`
- Payload: `HP`
- Amount: `8`
- Cooldown: `12`
- Timed effect: none
- Notes: Cheap food sustain. Keep out of potion slots unless food hotkeys become intentional.

### Herbal Tonic

- Asset ID: `Herbal_Tonic`
- Item name: `herbal_tonic`
- Description: A bitter tonic that grants slow health recovery for a short time.
- Stack: `5`
- Gold: `90`
- Effect mode: `TimedPlayerEffect`
- Payload: `HP`
- Amount: `0`
- Cooldown: `35`
- Timed effect: `HealthRegen.asset`
- Timed duration: `10`
- Notes: First health-over-time consumable.

### Fleetfoot Tonic

- Asset ID: `Fleetfoot_Tonic`
- Item name: `fleetfoot_tonic`
- Description: A sharp tonic that briefly improves movement speed.
- Stack: `5`
- Gold: `95`
- Effect mode: `TimedPlayerEffect`
- Payload: `Mana`
- Amount: `0`
- Cooldown: `35`
- Timed effect: `MoveSpeed.asset`
- Timed duration: `8`
- Notes: Keep this as a timed-effect test item. Review `MoveSpeed.asset` before expecting final behavior.

## Future Potion Colors

### Green Potion Asset

- Best reserved for poison, antidote, or regeneration-related content.
- Do not spend it yet unless poison or status cleansing becomes real.

### Purple Potion Asset

- Best reserved for magic, curse, resistance, or rare utility content.
- Do not spend it yet unless that effect category is implemented.

## Gameplay Source Plan

### Starter Inventory

- `Minor_Healing_Potion` x2
- `Minor_Stamina_Potion` x1 if stamina pressure is present early
- `Field_Ration` x1 or x2

### Potion Slots

- Slot 1: `Minor_Healing_Potion`
- Slot 2: `Minor_Stamina_Potion` if stamina is important at game start

### Shop Stock

- `Minor_Healing_Potion`: common
- `Minor_Stamina_Potion`: common
- `Field_Ration`: common
- `Major_Healing_Potion`: limited
- `Major_Stamina_Potion`: limited
- `Herbal_Tonic`: rare
- `Fleetfoot_Tonic`: rare or testing-only until movement tuning is confirmed

### Loot

- Early enemy loot: `Minor_Healing_Potion`
- Travel or survival loot: `Field_Ration`
- Stronger enemy loot: `Major_Healing_Potion`, `Major_Stamina_Potion`
- Rare reward: `Herbal_Tonic`, `Fleetfoot_Tonic`

## Later Recipe Ideas

### Brew Minor Healing Potion

- Recipe asset ID: `Recipe_Minor_Healing_Potion`
- Inputs: `Empty_Flask`, `Wild_Herb`
- Output: `Minor_Healing_Potion`

### Brew Minor Stamina Potion

- Recipe asset ID: `Recipe_Minor_Stamina_Potion`
- Inputs: `Empty_Flask`, `Tasty_Cupcake`
- Output: `Minor_Stamina_Potion`

### Brew Herbal Tonic

- Recipe asset ID: `Recipe_Herbal_Tonic`
- Inputs: `Minor_Healing_Potion`, `Wild_Herb`
- Output: `Herbal_Tonic`

## Later Salvage Ideas

### Potion Salvage

- Yield idea: `Empty_Flask` x1
- Gold yield: low
- Notes: Useful for testing material-yield salvage, but optional for final feel.

### Food Salvage

- Usually skip.
- Notes: Food salvage is likely less useful than simple selling or consuming.

## Final Checks Before Wiring

1. Assign final icons in the Editor.
2. Add starter inventory entries.
3. Add potion-slot entries if desired.
4. Add shop stock.
5. Add enemy loot entries.
6. Tune recipe and salvage values after gameplay testing.
