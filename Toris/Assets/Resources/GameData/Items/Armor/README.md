# Armor

Inspector-friendly notes for the next item content pass.

## Status

- Current pass: asset drafts authored.
- Goal: create a small, boring, useful armor set that covers all non-weapon equipment slots.
- Authored asset count: `9`
- Basic armor count: `4`
- Crafted upgrade count: `4`
- Extra simple armor item count: `1`
- Icons: left empty on generated assets unless manually assigned in the Editor.
- Recipes and salvage: plan after the basic armor assets exist.

## System Fit

- Asset type: `InventoryItemSO`
- Required components: `EquipableComponent`, `DefensiveComponent`
- Optional later component: `UpgradeableComponent`
- Stack size: `1`
- Current equipment slots: `Head`, `Chest`, `Legs`, `Arms`, `Weapon`
- Armor slots to fill now: `Head`, `Chest`, `Legs`, `Arms`
- Shields: defer until a dedicated shield slot exists or a design decision says otherwise.

## First Armor Batch

These are the safest assets to author first because they fill the visible armor slots with plain starter gear.

### Padded Cap

- Asset ID: `Padded_Cap`
- Item name: `padded_cap`
- Description: A quilted cap that softens glancing blows.
- Components: `EquipableComponent`, `DefensiveComponent`
- Stack: `1`
- Gold: `18`
- Slot: `Head`
- Defense bonus: `1`
- Physical defense: `2`
- Magical defense: `0`
- Source plan: starter inventory, shop
- Notes: Basic head slot item.

### Leather Vest

- Asset ID: `Leather_Vest`
- Item name: `leather_vest`
- Description: Plain leather protection for the torso.
- Components: `EquipableComponent`, `DefensiveComponent`
- Stack: `1`
- Gold: `35`
- Slot: `Chest`
- Defense bonus: `2`
- Physical defense: `4`
- Magical defense: `0`
- Source plan: starter inventory, shop
- Notes: Basic chest slot item.

### Traveler Trousers

- Asset ID: `Traveler_Trousers`
- Item name: `traveler_trousers`
- Description: Thick travel trousers, better than fighting in town clothes.
- Components: `EquipableComponent`, `DefensiveComponent`
- Stack: `1`
- Gold: `20`
- Slot: `Legs`
- Defense bonus: `1`
- Physical defense: `2`
- Magical defense: `0`
- Source plan: starter inventory, shop
- Notes: Basic legs slot item.

### Cloth Wraps

- Asset ID: `Cloth_Wraps`
- Item name: `cloth_wraps`
- Description: Layered cloth wraps for the forearms and hands.
- Components: `EquipableComponent`, `DefensiveComponent`
- Stack: `1`
- Gold: `15`
- Slot: `Arms`
- Defense bonus: `1`
- Physical defense: `1`
- Magical defense: `0`
- Source plan: starter inventory, shop
- Notes: Basic arms slot item.

## Crafted Upgrade Batch

These should come after the first batch so we can test basic equip behavior before adding recipes.

### Wolfskin Hood

- Asset ID: `Wolfskin_Hood`
- Item name: `wolfskin_hood`
- Description: A rough hood sewn from wolf pelt and cloth.
- Components: `EquipableComponent`, `DefensiveComponent`
- Stack: `1`
- Gold: `32`
- Slot: `Head`
- Defense bonus: `2`
- Physical defense: `3`
- Magical defense: `0`
- Source plan: crafting
- Notes: Early crafted head upgrade.

### Reinforced Leather Vest

- Asset ID: `Reinforced_Leather_Vest`
- Item name: `reinforced_leather_vest`
- Description: A leather vest patched with extra hide at the ribs and shoulders.
- Components: `EquipableComponent`, `DefensiveComponent`
- Optional component: `UpgradeableComponent`
- Stack: `1`
- Gold: `55`
- Slot: `Chest`
- Defense bonus: `3`
- Physical defense: `6`
- Magical defense: `0`
- Max level: `3`
- Source plan: crafting
- Notes: Best first candidate for upgrade testing.

### Hide Leggings

- Asset ID: `Hide_Leggings`
- Item name: `hide_leggings`
- Description: Leggings strengthened with cut strips of hide.
- Components: `EquipableComponent`, `DefensiveComponent`
- Stack: `1`
- Gold: `36`
- Slot: `Legs`
- Defense bonus: `2`
- Physical defense: `4`
- Magical defense: `0`
- Source plan: crafting
- Notes: Early crafted legs upgrade.

### Hide Bracers

- Asset ID: `Hide_Bracers`
- Item name: `hide_bracers`
- Description: Stiff hide bracers that protect the forearms.
- Components: `EquipableComponent`, `DefensiveComponent`
- Stack: `1`
- Gold: `34`
- Slot: `Arms`
- Defense bonus: `2`
- Physical defense: `3`
- Magical defense: `0`
- Source plan: crafting
- Notes: Early crafted arms upgrade.

## Extra Authored Asset

### Armor Ring

- Asset ID: `Armor_Ring`
- Item name: `armor_ring`
- Description: A ring that gives soft protection against the incoming blows.
- Components: `EquipableComponent`, `DefensiveComponent`
- Stack: `1`
- Gold: `14`
- Slot: `Arms`
- Defense bonus: `1`
- Physical defense: `1`
- Magical defense: `0`
- Source plan: shop, loot, or testing
- Notes: Uses the `Arms` slot for now because there is no accessory slot yet.

## Source Plan

### Starter Inventory

- `Padded_Cap`
- `Leather_Vest`
- `Traveler_Trousers`
- `Cloth_Wraps`
- Notes: Use the full basic set if equipment flow needs immediate testing. Use only chest or head armor if starter power should stay lower.

### Shop Stock

- `Padded_Cap`: common
- `Leather_Vest`: common
- `Traveler_Trousers`: common
- `Cloth_Wraps`: common
- Crafted upgrades: no shop stock for the first pass

### Loot

- Enemy loot or spawnpoint reward: one basic armor piece
- Wolf-related reward: `Wolfskin_Hood` later
- Hide-heavy reward: `Hide_Leggings` or `Hide_Bracers` later

## Later Recipe Ideas

### Craft Wolfskin Hood

- Recipe asset ID: `Recipe_Wolfskin_Hood`
- Inputs: `Wolf_Pelt`, `Cloth_Scrap`
- Output: `Wolfskin_Hood`

### Craft Reinforced Leather Vest

- Recipe asset ID: `Recipe_Reinforced_Leather_Vest`
- Inputs: `Leather_Vest`, `Tough_Hide`, `Iron_Scrap`
- Output: `Reinforced_Leather_Vest`

### Craft Hide Leggings

- Recipe asset ID: `Recipe_Hide_Leggings`
- Inputs: `Traveler_Trousers`, `Tough_Hide`
- Output: `Hide_Leggings`

### Craft Hide Bracers

- Recipe asset ID: `Recipe_Hide_Bracers`
- Inputs: `Cloth_Wraps`, `Tough_Hide`
- Output: `Hide_Bracers`

## Later Salvage Ideas

### Basic Armor Salvage

- Target items: basic armor batch
- Yield idea: low gold, maybe `Cloth_Scrap` or `Leather_Scrap`
- Notes: Only author once material names are settled.

### Crafted Armor Salvage

- Target items: crafted upgrade batch
- Yield idea: partial recovery of hide or scrap
- Notes: Useful after recipes exist.

## Final Checks Before Wiring

1. Leave icons empty unless assigning manually in the Editor.
2. Confirm each item has `EquipableComponent`.
3. Confirm each item has `DefensiveComponent`.
4. Confirm each item has the correct equipment slot.
5. Add starter inventory or shop entries.
6. Add enemy loot entries if desired.
7. Tune recipes and salvage after gameplay testing.
