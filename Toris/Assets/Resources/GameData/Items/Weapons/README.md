# Weapons

Inspector-friendly notes for bow-only weapon drafts.

## Status

- Current pass: bow-only asset drafts authored.
- Authored asset count: `4`
- Weapon theme: archer-only
- Icons: left empty on generated assets unless manually assigned in the Editor.
- Recipes and salvage: plan after the material list is stable.
- Deferred: melee weapons, crossbows, evolving weapons, and non-player weapon categories.

## System Fit

- Asset type: `InventoryItemSO`
- Required components: `EquipableComponent`, `OffensiveComponent`
- Optional crafted component: `UpgradeableComponent`
- Deferred component: `EvolvingComponent`
- Target slot: `Weapon`
- Weapon slot value: `4`
- Stack size: `1`
- Evolving kill tracking: skip for this first boring pass.

## Bow Set

### Training Bow

- Asset ID: `Training_Bow`
- Item name: `training_bow`
- Description: A worn practice bow with low draw weight. Good enough to survive with.
- Components: `EquipableComponent`, `OffensiveComponent`
- Stack: `1`
- Gold: `18`
- Slot: `Weapon`
- Strength bonus: `1`
- Defense bonus: `0`
- Base damage: `6`
- Attack speed: `1.1`
- Source plan: starter inventory
- Notes: Baseline starter bow.

### Hunter Bow

- Asset ID: `Hunter_Bow`
- Item name: `hunter_bow`
- Description: A practical bow used by hunters and patrols.
- Components: `EquipableComponent`, `OffensiveComponent`
- Stack: `1`
- Gold: `35`
- Slot: `Weapon`
- Strength bonus: `3`
- Defense bonus: `0`
- Base damage: `9`
- Attack speed: `1`
- Source plan: shop, recipe output
- Notes: Main early upgrade over the training bow.

### Longbow

- Asset ID: `Longbow`
- Item name: `longbow`
- Description: A longer bow with a heavier draw and stronger shot.
- Components: `EquipableComponent`, `OffensiveComponent`
- Stack: `1`
- Gold: `48`
- Slot: `Weapon`
- Strength bonus: `4`
- Defense bonus: `0`
- Base damage: `12`
- Attack speed: `0.85`
- Source plan: shop, loot
- Notes: Slower heavy bow for a different archer feel.

### Fang-Tipped Bow

- Asset ID: `Fang_Tipped_Bow`
- Item name: `fang_tipped_bow`
- Description: A hunting bow fitted with wolf fang tips and rough bindings.
- Components: `EquipableComponent`, `OffensiveComponent`, `UpgradeableComponent`
- Stack: `1`
- Gold: `60`
- Slot: `Weapon`
- Strength bonus: `5`
- Defense bonus: `0`
- Base damage: `13`
- Attack speed: `1`
- Max level: `3`
- Source plan: crafting
- Notes: First crafted bow and first bow upgrade-state test.

## Source Plan

### Starter Inventory

- `Training_Bow`
- Notes: Best default weapon for early equip and combat flow.

### Shop Stock

- `Hunter_Bow`: common upgrade
- `Longbow`: limited heavy option
- Crafted bow: no shop stock for the first pass

### Loot

- Early enemy loot or spawnpoint reward: `Hunter_Bow`
- Stronger enemy loot or spawnpoint reward: `Longbow`
- Wolf-related reward path: materials for `Fang_Tipped_Bow`

## Later Recipe Ideas

### Craft Hunter Bow

- Recipe asset ID: `Recipe_Hunter_Bow`
- Inputs: `Training_Bow`, `Wood_Scrap`, `Binding_Scroll`
- Output: `Hunter_Bow`

### Craft Fang-Tipped Bow

- Recipe asset ID: `Recipe_Fang_Tipped_Bow`
- Inputs: `Hunter_Bow`, `Wolf_Fang`, `Binding_Scroll`
- Output: `Fang_Tipped_Bow`

## Later Salvage Ideas

### Bow Salvage

- Target items: all bow weapons
- Yield idea: low gold, maybe `Wood_Scrap` or `Binding_Scroll`
- Notes: Only author after material names are final.

### Crafted Bow Salvage

- Target item: `Fang_Tipped_Bow`
- Yield idea: partial recovery of `Wolf_Fang` or scrap
- Notes: Useful after recipes exist.

## Final Checks Before Wiring

1. Assign final icons in the Editor.
2. Add `Training_Bow` to starter inventory if desired.
3. Add shop stock for early upgrades.
4. Add enemy loot entries.
5. Tune recipes and salvage after gameplay testing.
