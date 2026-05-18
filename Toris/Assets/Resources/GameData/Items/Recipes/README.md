# Recipes

Inspector-friendly planning notes for crafting recipes and forge outputs.

## Status

- Current pass: recipe asset drafts authored.
- Authored recipe count: `8`
- Registry status: registered in `Crafting Registry SO.asset`.
- Keep recipes simple: one base item, one material type, one output item.
- Multi-material recipes can come later after the forge UI and manager are intentionally expanded.

## System Fit

Current `CraftingRecipeSO` supports:

- One `BaseItemRequirement`
- A list of `MaterialRequirements`
- One `OutputItem`
- One `GoldCost`

The current forge manager can match either selected slot as the base item and the other as a material. For low-risk implementation, prefer one material type per recipe.

## Priority 1

### Brew Minor Healing Potion

- Recipe asset ID: `Recipe_Minor_Healing_Potion`
- Base item: `Empty_Flask`
- Material: `Wild_Herb` x1
- Gold cost: `2`
- Output: `Minor_Healing_Potion`
- Output quantity: `1`
- Notes: Simple healing potion craft.

### Brew Minor Stamina Potion

- Recipe asset ID: `Recipe_Minor_Stamina_Potion`
- Base item: `Empty_Flask`
- Material: `Tasty_Cupcake` x1
- Gold cost: `4`
- Output: `Minor_Stamina_Potion`
- Output quantity: `1`
- Notes: Uses the existing instant stamina path.

### Craft Hunter Bow

- Recipe asset ID: `Recipe_Hunter_Bow`
- Base item: `Training_Bow`
- Material: `Wood_Scrap` x3
- Gold cost: `10`
- Output: `Hunter_Bow`
- Output quantity: `1`
- Notes: Turns starter bow into basic shop-tier bow.

### Craft Wolfskin Hood

- Recipe asset ID: `Recipe_Wolfskin_Hood`
- Base item: `Padded_Cap`
- Material: `Wolf_Pelt` x2
- Gold cost: `8`
- Output: `Wolfskin_Hood`
- Output quantity: `1`
- Notes: Simple armor upgrade.

## Priority 2

### Craft Fang-Tipped Bow

- Recipe asset ID: `Recipe_Fang_Tipped_Bow`
- Base item: `Hunter_Bow`
- Material: `Wolf_Fang` x3
- Gold cost: `20`
- Output: `Fang_Tipped_Bow`
- Output quantity: `1`
- Notes: Crafted bow upgrade for the archer-only weapon set.

### Craft Reinforced Leather Vest

- Recipe asset ID: `Recipe_Reinforced_Leather_Vest`
- Base item: `Leather_Vest`
- Material: `Tough_Hide` x3
- Gold cost: `16`
- Output: `Reinforced_Leather_Vest`
- Output quantity: `1`
- Notes: Main chest armor upgrade.

### Craft Hide Leggings

- Recipe asset ID: `Recipe_Hide_Leggings`
- Base item: `Traveler_Trousers`
- Material: `Tough_Hide` x2
- Gold cost: `10`
- Output: `Hide_Leggings`
- Output quantity: `1`
- Notes: Early legs upgrade.

### Craft Hide Bracers

- Recipe asset ID: `Recipe_Hide_Bracers`
- Base item: `Cloth_Wraps`
- Material: `Tough_Hide` x2
- Gold cost: `10`
- Output: `Hide_Bracers`
- Output quantity: `1`
- Notes: Early arms upgrade.

## Deferred

### Major Potions

- Defer until the basic potion craft loop feels correct.
- Could use minor potions as base items later.

### Multi-Material Bow Recipes

- Defer until the forge flow intentionally supports more complex recipe presentation.
- `Binding_Scroll` and `Wolf_Fang` can become combined requirements later.

### Occult Or Bone Recipes

- Defer until those materials exist again.
