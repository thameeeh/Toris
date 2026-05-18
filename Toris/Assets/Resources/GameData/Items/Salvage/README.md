# Salvage

Inspector-friendly notes for salvage recipe drafts, gold returns, and material recovery.

## Status

- Current pass: salvage recipe drafts authored.
- Authored recipe count: `19`
- Recipe asset type: `SalvageRecipeSO`
- Registry status: registered in `Crafting Registry SO.asset`.
- Junk salvage: skipped for now.
- Raw material salvage: skipped for now.
- Food salvage: skipped for now.

## System Fit

- Asset type: `SalvageRecipeSO`
- Target item: one `InventoryItemSO`
- Gold output: `GoldYield`
- Material output: `MaterialYields`
- UI can request either gold salvage or material salvage.
- Each recipe is registered in `Crafting Registry SO.asset`.

## Bow Salvage

### Salvage Training Bow

- Salvage asset ID: `Salvage_Training_Bow`
- Target item: `Training_Bow`
- Gold yield: `4`
- Material yield: `Wood_Scrap` x1
- Notes: Low-value starter refund.

### Salvage Hunter Bow

- Salvage asset ID: `Salvage_Hunter_Bow`
- Target item: `Hunter_Bow`
- Gold yield: `8`
- Material yields: `Wood_Scrap` x2, `Binding_Scroll` x1
- Notes: Basic bow recovery.

### Salvage Longbow

- Salvage asset ID: `Salvage_Longbow`
- Target item: `Longbow`
- Gold yield: `10`
- Material yields: `Wood_Scrap` x2, `Binding_Scroll` x1
- Notes: Heavy bow recovery.

### Salvage Fang-Tipped Bow

- Salvage asset ID: `Salvage_Fang_Tipped_Bow`
- Target item: `Fang_Tipped_Bow`
- Gold yield: `14`
- Material yields: `Wolf_Fang` x1, `Binding_Scroll` x1
- Notes: Recovers one special input from the crafted bow.

## Armor Salvage

### Salvage Padded Cap

- Salvage asset ID: `Salvage_Padded_Cap`
- Target item: `Padded_Cap`
- Gold yield: `4`
- Material yield: `Cloth_Scrap` x1
- Notes: Basic head armor refund.

### Salvage Leather Vest

- Salvage asset ID: `Salvage_Leather_Vest`
- Target item: `Leather_Vest`
- Gold yield: `8`
- Material yield: `Tough_Hide` x1
- Notes: Basic chest armor refund.

### Salvage Traveler Trousers

- Salvage asset ID: `Salvage_Traveler_Trousers`
- Target item: `Traveler_Trousers`
- Gold yield: `5`
- Material yield: `Cloth_Scrap` x1
- Notes: Basic legs armor refund.

### Salvage Cloth Wraps

- Salvage asset ID: `Salvage_Cloth_Wraps`
- Target item: `Cloth_Wraps`
- Gold yield: `3`
- Material yield: `Cloth_Scrap` x1
- Notes: Basic arms armor refund.

### Salvage Wolfskin Hood

- Salvage asset ID: `Salvage_Wolfskin_Hood`
- Target item: `Wolfskin_Hood`
- Gold yield: `8`
- Material yields: `Wolf_Pelt` x1, `Cloth_Scrap` x1
- Notes: Crafted head armor recovery.

### Salvage Reinforced Leather Vest

- Salvage asset ID: `Salvage_Reinforced_Leather_Vest`
- Target item: `Reinforced_Leather_Vest`
- Gold yield: `12`
- Material yields: `Tough_Hide` x2, `Iron_Scrap` x1
- Notes: Recovers reinforced armor inputs without giving a full refund.

### Salvage Hide Leggings

- Salvage asset ID: `Salvage_Hide_Leggings`
- Target item: `Hide_Leggings`
- Gold yield: `8`
- Material yield: `Tough_Hide` x1
- Notes: Crafted legs armor recovery.

### Salvage Hide Bracers

- Salvage asset ID: `Salvage_Hide_Bracers`
- Target item: `Hide_Bracers`
- Gold yield: `8`
- Material yield: `Tough_Hide` x1
- Notes: Crafted arms armor recovery.

### Salvage Armor Ring

- Salvage asset ID: `Salvage_Armor_Ring`
- Target item: `Armor_Ring`
- Gold yield: `4`
- Material yield: `Iron_Scrap` x1
- Notes: Temporary accessory-style item recovery.

## Bottled Consumable Salvage

### Salvage Minor Healing Potion

- Salvage asset ID: `Salvage_Minor_Healing_Potion`
- Target item: `Minor_Healing_Potion`
- Gold yield: `8`
- Material yield: `Empty_Flask` x1
- Notes: Flask recovery test.

### Salvage Major Healing Potion

- Salvage asset ID: `Salvage_Major_Healing_Potion`
- Target item: `Major_Healing_Potion`
- Gold yield: `20`
- Material yield: `Empty_Flask` x1
- Notes: Higher-value bottle recovery.

### Salvage Minor Stamina Potion

- Salvage asset ID: `Salvage_Minor_Stamina_Potion`
- Target item: `Minor_Stamina_Potion`
- Gold yield: `6`
- Material yield: `Empty_Flask` x1
- Notes: Flask recovery test.

### Salvage Major Stamina Potion

- Salvage asset ID: `Salvage_Major_Stamina_Potion`
- Target item: `Major_Stamina_Potion`
- Gold yield: `18`
- Material yield: `Empty_Flask` x1
- Notes: Higher-value bottle recovery.

### Salvage Herbal Tonic

- Salvage asset ID: `Salvage_Herbal_Tonic`
- Target item: `Herbal_Tonic`
- Gold yield: `12`
- Material yield: `Empty_Flask` x1
- Notes: Timed-effect consumable flask recovery.

### Salvage Fleetfoot Tonic

- Salvage asset ID: `Salvage_Fleetfoot_Tonic`
- Target item: `Fleetfoot_Tonic`
- Gold yield: `12`
- Material yield: `Empty_Flask` x1
- Notes: Timed-effect consumable flask recovery.

## Skipped For Now

### Junk

- Junk should mostly sell through shop/economy flow.
- Do not add salvage recipes for `Cracked_Gem`, `Buckle`, or other junk until there is a real reason.

### Raw Materials

- Materials should be recipe inputs and salvage outputs.
- Do not add salvage recipes for materials in this pass.

### Food

- `Field_Ration` should be consumed or sold, not salvaged.

## Final Checks Before Wiring

1. Confirm gold salvage and material salvage both behave as expected in the Smith salvage UI.
2. Tune yields after recipes and shops exist.
