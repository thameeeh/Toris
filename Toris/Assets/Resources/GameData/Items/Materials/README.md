# Materials

Inspector-friendly notes for stackable crafting and progression materials.

## Status

- Current pass: asset drafts authored.
- Authored asset count: `10`
- Main purpose: recipe inputs, salvage outputs, shop stock, world pickups, and loot rewards.
- Icons: left empty on generated assets unless manually assigned in the Editor.
- Recipes: not authored yet.
- Salvage: not authored yet.

## System Fit

- Asset type: `InventoryItemSO`
- Required component: `ProgressionComponent`
- Category: `Material`
- Serialized category value: `0`
- Stackable: yes
- Runtime state: none
- Main sink: crafting recipes.
- Secondary sink: sale for low gold value.

## Potion Materials

### Wild Herb

- Asset ID: `Wild_Herb`
- Item name: `wild_herb`
- Description: A bitter field herb used in simple healing mixtures.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `50`
- Gold: `3`
- Source plan: world pickups, shop, light loot
- Recipe plan: `Minor_Healing_Potion`, `Herbal_Tonic`
- Notes: Core healing ingredient.

### Tasty Cupcake

- Asset ID: `Tasty_Cupcake`
- Item name: `tasty_cupcake`
- Description: A small red mushroom with a sharp smell and a short-lived energizing effect.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `50`
- Gold: `4`
- Source plan: forest pickups, shop, uncommon loot
- Recipe plan: `Minor_Stamina_Potion`
- Notes: Core stamina ingredient for now, despite the playful renamed asset.

### Empty Flask

- Asset ID: `Empty_Flask`
- Item name: `empty_flask`
- Description: A plain glass flask cleaned and ready for brewing.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `20`
- Gold: `2`
- Source plan: shop, potion salvage
- Recipe plan: basic potions
- Notes: Keeps potion recipes grounded and gives potion salvage a useful output.

## Bow Materials

### Wood Scrap

- Asset ID: `Wood_Scrap`
- Item name: `wood_scrap`
- Description: Usable scraps of dry wood, good for simple repairs and bow work.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `99`
- Gold: `1`
- Source plan: debris loot, salvage, shop
- Recipe plan: `Hunter_Bow`
- Notes: Cheapest bow material.

### Binding Scroll

- Asset ID: `Binding_Scroll`
- Item name: `binding_scroll`
- Description: A scroll with a magical speel used for binding items.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `50`
- Gold: `2`
- Source plan: shop, camp loot, salvage
- Recipe plan: `Hunter_Bow`, `Fang_Tipped_Bow`
- Notes: Flexible recipe filler for bows and armor.

### Wolf Fang

- Asset ID: `Wolf_Fang`
- Item name: `wolf_fang`
- Description: A sharp fang taken from a wolf, useful for simple bow fittings.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `20`
- Gold: `8`
- Source plan: uncommon wolf loot
- Recipe plan: `Fang_Tipped_Bow`
- Notes: Premium wolf material for the crafted bow.

## Armor Materials

### Cloth Scrap

- Asset ID: `Cloth_Scrap`
- Item name: `cloth_scrap`
- Description: Salvaged cloth strips, still useful for padding and binding.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `50`
- Gold: `1`
- Source plan: salvage, humanoid loot, shop
- Recipe plan: `Wolfskin_Hood`
- Notes: Cheap armor padding and salvage output.

### Tough Hide

- Asset ID: `Tough_Hide`
- Item name: `tough_hide`
- Description: Durable hide suitable for reinforcing light armor.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `20`
- Gold: `5`
- Source plan: wildlife loot, salvage
- Recipe plan: `Reinforced_Leather_Vest`, `Hide_Leggings`, `Hide_Bracers`
- Notes: Main crafted armor input.

### Wolf Pelt

- Asset ID: `Wolf_Pelt`
- Item name: `wolf_pelt`
- Description: A coarse pelt that can be cut into padding or traded as scrap hide.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `20`
- Gold: `5`
- Source plan: wolf loot
- Recipe plan: `Wolfskin_Hood`
- Notes: Common wolf material, distinct from `Wolf_Fang`.

### Iron Scrap

- Asset ID: `Iron_Scrap`
- Item name: `iron_scrap`
- Description: Small iron fragments suitable for repairs and rough reinforcement.
- Components: `ProgressionComponent`
- Category: `Material`
- Stack: `50`
- Gold: `4`
- Source plan: salvage, shop, debris loot
- Recipe plan: `Reinforced_Leather_Vest`
- Notes: Metal reinforcement without adding ore/mining complexity yet.

## Source Plan

### Starter-Friendly Sources

- `Wild_Herb`
- `Empty_Flask`
- `Wood_Scrap`
- `Binding_Scroll`
- Notes: These can appear early without opening the whole crafting economy.

### Wildlife Sources

- `Wolf_Pelt`
- `Wolf_Fang`
- `Tough_Hide`
- Notes: Ties wolf combat into armor and bow crafting.

### Shop Sources

- `Empty_Flask`
- `Binding_Scroll`
- `Wood_Scrap`
- `Wild_Herb`
- Notes: Keep shop stocks small so gathering still matters.

### Salvage Outputs

- `Cloth_Scrap`
- `Wood_Scrap`
- `Binding_Scroll`
- `Iron_Scrap`
- `Empty_Flask`
- Notes: Useful later when salvage recipes are authored.

## Deferred Materials

### Redcap Mushroom

- Renamed to `Tasty_Cupcake`.
- Keep recipe notes pointed at the current asset name.

### Bone Shard

- Defer until a bone-themed recipe exists.
- Earlier weapon plans used this, but the bow-only set no longer needs it.

### Iron Ore

- Defer until mining or ore nodes exist.
- `Iron_Scrap` covers the current low-tech crafting need.

### Grave Dust

- Defer until occult or poison/status potion content exists.

## Final Checks Before Wiring

1. Assign final icons in the Editor.
2. Add early materials to shop stock if desired.
3. Add materials to enemy loot tables and pickups.
4. Reference these exact asset names when creating recipe assets.
5. Reference these exact asset names when creating salvage outputs.
