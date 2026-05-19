# Junk

Inspector-friendly notes for vendor junk, valuables, and low-stakes sell items.

## Status

- Current pass: asset drafts authored.
- Authored asset count: `6`
- Main purpose: sellable loot and small economy rewards.
- Icons: left empty on generated assets unless manually assigned in the Editor.
- Recipes: avoid for the first pass.
- Salvage: usually skip.

## System Fit

- Asset type: `InventoryItemSO`
- Required component: `ProgressionComponent`
- Category: `Junk`
- Serialized category value: `3`
- Stackable: yes
- Runtime state: none
- Primary sink: sell to shops for gold.

## Authored Assets

### Cracked Gem

- Asset ID: `Cracked_Gem`
- Item name: `cracked_gem`
- Description: A flawed gem with enough color left to interest a merchant.
- Components: `ProgressionComponent`
- Category: `Junk`
- Stack: `20`
- Gold: `18`
- Source plan: rare enemy loot
- Notes: Highest-value junk item in this first pass.

### Bent Copper Ring

- Asset ID: `Bent_Copper_Ring`
- Item name: `bent_copper_ring`
- Description: A cheap copper ring bent out of shape.
- Components: `ProgressionComponent`
- Category: `Junk`
- Stack: `10`
- Gold: `10`
- Source plan: humanoid loot
- Notes: Small valuable, not equipment.

### Tarnished Locket

- Asset ID: `Tarnished_Locket`
- Item name: `tarnished_locket`
- Description: A worn locket too damaged to be worth much, but not worthless.
- Components: `ProgressionComponent`
- Category: `Junk`
- Stack: `10`
- Gold: `14`
- Source plan: rare humanoid loot
- Notes: Valuable sell item with a slightly more personal flavor.

### Buckle

- Asset ID: `Buckle`
- Item name: `useless_buckle`
- Description: A buckle that does not fit.
- Components: `ProgressionComponent`
- Category: `Junk`
- Stack: `30`
- Gold: `3`
- Source plan: common enemy loot, debris loot
- Notes: Cheap filler junk.

### Dull Arrowhead

- Asset ID: `Dull_Arrowhead`
- Item name: `dull_arrowhead`
- Description: A blunted arrowhead too worn for clean work.
- Components: `ProgressionComponent`
- Category: `Junk`
- Stack: `30`
- Gold: `4`
- Source plan: archer-themed loot, camp loot, debris loot
- Notes: Fits the archer theme without becoming a crafting material yet.

### Chipped Bone Charm

- Asset ID: `Chipped_Bone_Charm`
- Item name: `chipped_bone_charm`
- Description: A small bone charm with its markings worn nearly smooth.
- Components: `ProgressionComponent`
- Category: `Junk`
- Stack: `20`
- Gold: `8`
- Source plan: wolf-related loot, strange enemy loot
- Notes: Flavor junk. Keep separate from real bone materials for now.

## Source Plan

### Common Loot

- `Buckle`
- `Dull_Arrowhead`
- Notes: These can appear in low-value drops without changing progression.

### Uncommon Loot

- `Chipped_Bone_Charm`
- `Bent_Copper_Ring`
- Notes: Good for uncommon enemy drops.

### Rare Junk Loot

- `Tarnished_Locket`
- `Cracked_Gem`
- Notes: Use sparingly so rare enemy loot still feels meaningful.

## Shop Plan

### Selling

- All junk should be sellable.
- No junk should be required for the first recipe pass.
- No junk should be needed to unlock systems.

### Buying

- Shops probably do not need to stock junk.
- Exceptions can be made later for flavor vendors.

## Design Notes

### Should junk salvage into materials?

Usually no. Junk should mostly sell for gold.

### Should junk appear in early loot?

Yes, but lightly. It gives the economy a small reward without adding more systems.

### Should junk be used in recipes?

Not in the first pass. Keep it separate from materials until the economy has shape.

## Final Checks Before Wiring

1. Assign final icons in the Editor.
2. Add junk to enemy loot tables.
3. Confirm shops can buy the items.
4. Avoid recipe references unless the item is intentionally promoted to material.
