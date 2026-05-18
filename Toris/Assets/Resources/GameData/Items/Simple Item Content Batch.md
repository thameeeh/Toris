# Simple Item Content Batch

This is the index for the first practical item-content pass. The detailed item tables now live in the specific class folders beside this file.

The goal of this batch is deliberately plain: replace placeholder item content with believable baseline materials, consumables, weapons, armor, recipes, and salvage rules that the existing systems can already support.

These numbers are starting points only. Expect balance values, descriptions, stack sizes, and recipe costs to change after playtesting.

## Folder Index

| Folder | Contents |
| --- | --- |
| `Materials/` | Crafting materials and common progression items. |
| `Junk/` | Vendor junk and low-stakes valuables. |
| `Consumables/` | Instant and timed consumable item candidates. |
| `Weapons/` | Bow-only weapon candidates and starting stats. |
| `Armor/` | Defensive equipment candidates for current equipment slots. |
| `Recipes/` | Simple forge recipe candidates. |
| `Salvage/` | Salvage recipe candidates and recovery values. |

## Authoring Assumptions

- Use existing `InventoryItemSO` item blueprints.
- Use existing item components only: `ProgressionComponent`, `ConsumableComponent`, `EquipableComponent`, `OffensiveComponent`, `DefensiveComponent`, `UpgradeableComponent`, and `EvolvingComponent`.
- Keep recipes simple: one base item plus one material where possible, because the current forge flow is built around two selected item slots.
- Keep equipment `MaxStackSize` at `1`.
- Use asset IDs that can become stable save IDs later.
- Prefer boring names over placeholder names.
- Prefer small numbers until combat and economy tuning are clearer.

## Item Categories

| Category | Purpose | Current Component Fit |
| --- | --- | --- |
| Common materials | Enemy drops, gathering rewards, recipe inputs | `ProgressionComponent`, category `Material` |
| Junk valuables | Low-stakes sell items | `ProgressionComponent`, category `Junk` |
| Consumables | Basic player sustain and buff testing | `ConsumableComponent` |
| Basic weapons | Simple equipment comparison and shop stock | `EquipableComponent` + `OffensiveComponent` |
| Basic armor | Simple defensive equipment for each current slot | `EquipableComponent` + `DefensiveComponent` |
| Upgrade/craft variants | Outputs for forge testing | Same as base item, optionally `UpgradeableComponent` |

## First Implementation Priority

For a first real pass, keep the asset batch small enough to wire correctly:

| Priority | Items | Why |
| --- | --- | --- |
| 1 | `Wild_Herb`, `Tasty_Cupcake`, `Empty_Flask`, `Wood_Scrap`, `Binding_Scroll`, `Wolf_Pelt`, `Wolf_Fang`, `Tough_Hide`, `Cloth_Scrap`, `Iron_Scrap` | Core materials for drops, shops, recipes, and salvage outputs. |
| 2 | `Minor_Healing_Potion`, `Major_Healing_Potion`, `Minor_Stamina_Potion`, `Major_Stamina_Potion` | Basic consumables for inventory and potion slot testing. |
| 3 | `Training_Bow`, `Hunter_Bow`, `Longbow`, `Fang_Tipped_Bow`, `Leather_Vest`, `Padded_Cap`, `Cloth_Wraps` | Archer-appropriate equipment with clear slots and simple stats. |
| 4 | `Wolfskin_Hood`, `Reinforced_Leather_Vest`, `Hide_Leggings`, `Hide_Bracers` | Crafted outputs once base items and materials exist. |

## Loot And Shop Suggestions

Current loot sources are enemy loot tables and their spawnpoints in the other world. Chest/lootable-object rewards are deferred until that feature exists.

| Source | Suggested Items | Notes |
| --- | --- | --- |
| Minion Wolf | `Wolf_Pelt`, `Wolf_Fang`, small gold/XP | Pelt common, fang uncommon. |
| Leader Wolf | `Wolf_Pelt`, `Wolf_Fang`, `Minor_Healing_Potion`, more gold/XP | Keep potion guaranteed only if needed for pacing. |
| Deer | `Tough_Hide`, `Field_Ration` | Non-hostile source if deer hunting is supported. |
| Necromancer | `Iron_Scrap`, chance for `Cracked_Gem` | Keep occult materials deferred until that item family is real. |
| Smith Shop | `Empty_Flask`, `Wood_Scrap`, `Binding_Scroll`, `Minor_Healing_Potion`, `Hunter_Bow`, `Longbow`, basic armor | Shop should sell boring essentials, not rare crafted upgrades. |
| Starter Backpack | `Training_Bow`, `Minor_Healing_Potion` x2, `Field_Ration` x2 | Enough to test inventory/use/equip loops. |

## Open Design Questions

| Question | Current Recommendation |
| --- | --- |
| Should shields exist now? | Wait. Current equipment slots do not have a dedicated shield slot. |
| Should food be consumable or material? | Use `Field_Ration` as a consumable now; keep raw foods as future materials. |
| Should all crafted items be upgradeable? | No. Start with only a few upgradeable outputs to keep state testing focused. |
| Should junk salvage into materials? | Usually no. Junk should mostly sell for gold. |
| Should potion salvage exist? | Optional. It is useful for testing salvage material yields but may be removed later. |
| Should display names match asset IDs exactly? | No. Asset IDs should be stable and code/save friendly; display names should be player friendly. |

## Implementation Checklist For This Batch

When the list is approved:

1. Create item assets for the selected batch.
2. Configure item components and starting values.
3. Add all new item assets to `Item Database SO.asset`. Done for this authored batch.
4. Add selected starter items to player inventory prefabs.
5. Add selected shop stock to smith shop inventory.
6. Create selected crafting recipes.
7. Register crafting recipes in `Crafting Registry SO.asset`. Done for this authored batch.
8. Create selected salvage recipes.
9. Register salvage recipes in `Crafting Registry SO.asset`. Done for this authored batch.
10. Update wolf, deer, and necromancer loot tables as needed.
11. Static-check item GUID references.
12. Verify in Unity when available.
