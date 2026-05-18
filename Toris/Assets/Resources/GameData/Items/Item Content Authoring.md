# Item Content Authoring

This guide is the refresher for creating item content and making it real in Toris. It is about item authoring only: item blueprints, icons, starting inventory, shops, loot, crafting, salvage, pickups, and the checks that keep those assets correct.

The important mental model is simple:

- `InventoryItemSO` is the static item blueprint.
- `ItemInstance` is the runtime item with an instance ID and optional dynamic state.
- `ItemComponent` objects define item behavior and static stats.
- `ItemComponentState` objects hold only runtime data that changes during play.
- An item is not real in the game until it is registered and referenced by gameplay content.

## Canonical Locations

Create and maintain item content in these project locations:

- Item blueprints: `Assets/Resources/GameData/Items/`
- Progression/material items: `Assets/Resources/GameData/Items/Progression Items/`
- Timed player effect definitions: `Assets/Resources/GameData/Items/TimedEffects/`
- Crafting recipes: `Assets/Resources/GameData/Items/Crafting Recipies/`
- Salvage recipes: `Assets/Resources/GameData/Items/Salvage Recipies/`
- Crafting registry: `Assets/Resources/GameData/ItemManagers/Crafting Registry SO.asset`
- Master item database: `Assets/Resources/GameData/Item Database SO.asset`
- Inventory container blueprints: `Assets/Resources/GameData/InventoryContainers/`
- Starter/player inventory prefabs: `Assets/Prefabs/InventoryManagers/individualManagers/`
- NPC shop inventories: NPC prefabs such as `Assets/Prefabs/NPC/SmithNPC.prefab`
- Enemy loot tables: enemy `EnemyLootTableSO` assets
- World pickups: scene or prefab objects with `WorldItem`

Keep project markdown documentation under `Assets/Documentation/`.

## Before Authoring

Decide what kind of item you are making before touching assets:

- Material or junk: stackable, usually `ProgressionComponent`.
- Quest/key item: stackable only if the design allows duplicates, usually `ProgressionComponent` with the right category.
- Consumable: stackable, `ConsumableComponent`, optionally instant or timed.
- Equipment: non-stackable, `EquipableComponent`, often plus offensive or defensive stats.
- Weapon: non-stackable, `EquipableComponent` targeting `Weapon`, usually `OffensiveComponent`, optionally `UpgradeableComponent` or `EvolvingComponent`.
- Armor: non-stackable, `EquipableComponent` targeting the armor slot, usually `DefensiveComponent`.
- Crafting output: any of the above, but must be registered in recipes and the item database.

Write down the intended gameplay role first:

- Where does it come from?
- What is it used for?
- Can it stack?
- Can it be sold, salvaged, crafted, dropped, picked up, or equipped?
- Does it need runtime state?
- Does a quest need to identify it by exact ID?

If an answer is "yes", there is probably another asset reference to update.

## Stable IDs And Save Safety

The save system uses the item asset name, `InventoryItemSO.name`, as the item ID. This is the `.asset` filename without the extension.

Rules:

- Every item asset filename must be unique.
- Do not casually rename shipped item assets, because old saves will look for the old ID.
- If replacing temporary content before release, renaming is acceptable only when no persistent saves need compatibility.
- `ItemName` is player-facing display text and may be changed more freely than the asset filename.
- If a quest relies on an item, use stable exact IDs and avoid depending on mutable display names.

Example:

- Asset filename: `Iron_Ore.asset`
- Save ID: `Iron_Ore`
- Display name: `Iron Ore`

## Creating The Item Blueprint

In Unity:

1. Create an `InventoryItemSO` using `Create > UI > Inventory > Item`.
2. Place it in the correct `Assets/Resources/GameData/Items/` subfolder.
3. Give the asset a stable, unique filename.
4. Set `ItemName` to the player-facing name.
5. Add a short `Description`.
6. Assign an icon sprite.
7. Set `GoldValue`.
8. Set `MaxStackSize`.
9. Add the correct components in the `Components` list.
10. Save the project and inspect for validation warnings.

Descriptions should explain the item from the player's point of view. Prefer compact item text:

- "A common vein-metal used by smiths for basic repairs."
- "Restores a small amount of health."
- "A practical hunting bow with steady draw weight."

Avoid debug names, empty descriptions, and joke placeholder text once the item is game content.

## Component Guide

### ProgressionComponent

Use for materials, quest items, keys, and junk.

Fields:

- `Category`: `Material`, `QuestItem`, `Key`, or `Junk`.

Notes:

- Usually stackable.
- No runtime state is needed.
- Good for ore, herbs, food ingredients, badges, relic fragments, keys, and vendor junk.

### ConsumableComponent

Use for items consumed from inventory or potion slots.

Fields:

- `EffectMode`: `InstantResource` or `TimedPlayerEffect`.
- `EffectPayload`: `HP` or `Mana`.
- `amount`: resource amount for instant effects.
- `TimedEffectDefinition`: effect definition for timed buffs.
- `TimedEffectDuration`: buff duration in seconds.
- `CooldownDuration`: per-item cooldown in seconds.

Rules:

- Consumables can stack.
- Potion slots accept items with `ConsumableComponent`.
- `InstantResource` needs a valid payload and amount.
- `TimedPlayerEffect` needs a valid `PlayerEffectDefinitionSO` and duration greater than zero.
- Timed effect definitions belong in `Assets/Resources/GameData/Items/TimedEffects/`.

### EquipableComponent

Use for anything that can be equipped.

Fields:

- `TargetSlot`
- `StrengthBonus`
- `DefenceBonus`

Equipment slots:

- `0` = Head
- `1` = Chest
- `2` = Legs
- `3` = Arms
- `4` = Weapon

Rules:

- Equippable items must have `MaxStackSize = 1`.
- The component enforces this through validation.
- Equipment is moved by inventory events and should not be directly mutated by UI.
- `StrengthBonus` contributes to outgoing damage modifiers.
- `DefenceBonus` contributes to incoming damage reduction.

### OffensiveComponent

Use for weapons or offensive equipment.

Fields:

- `BaseDamage`
- `AttackSpeed`

Notes:

- `BaseDamage` feeds computed weapon damage.
- `AttackSpeed` is attacks per second.
- Usually paired with `EquipableComponent` targeting `Weapon`.

### DefensiveComponent

Use for armor and defensive equipment.

Fields:

- `PhysicalDefense`
- `MagicalDefense`

Notes:

- Physical defense is currently included in equipped item effect calculations.
- Magical defense is available as authored data, even if a specific combat path does not consume it yet.

### UpgradeableComponent

Use when an item has a level that changes at runtime.

Fields:

- `MaxLevel`

Runtime state:

- Creates `UpgradeableState`.
- New items start at level 1.
- Upgrade cost is currently calculated by `UpgradeSalvageManagerSO`.

Rules:

- Use only when the item should preserve upgrade state through transactions and saves.
- Keep level data in `UpgradeableState`, not in the item blueprint.

### EvolvingComponent

Use for items that awaken after progress, such as kill tracking.

Fields:

- `KillsRequired`
- `AwakenedDamageBonus`

Runtime state:

- Creates `EvolvingState`.
- Tracks `CurrentKills` and `IsAwakened`.

Rules:

- Use only when the item needs per-instance progression.
- These items should generally be non-stackable.

## Dynamic State Rules

Use this test:

Does the value change during gameplay for one specific copy of an item?

- No: put it on the `ItemComponent` blueprint.
- Yes: put it in an `ItemComponentState`.

Runtime state must:

- Be serializable.
- Have a public parameterless constructor.
- Hold no Unity object references.
- Implement value-based `IsStackableWith`.
- Implement deep `Clone`.

Never store `GameObject`, `Transform`, `ScriptableObject`, `Sprite`, or other Unity references in item state. Keep Unity references in the blueprint component.

## Stack Size Rules

General guidance:

- Materials: `20`, `50`, or `99`, depending on economy.
- Consumables: usually `5`, `10`, or `16`.
- Quest items: usually `1` unless duplicates are intentional.
- Equipment and weapons: always `1`.
- Items with meaningful per-instance state: usually `1`.

Stacking is based on:

- Same `BaseItem`.
- Same number of runtime states.
- Matching state values by `IsStackableWith`.

That means two upgraded weapons only stack if their runtime states match. Since equippables are capped at `1`, this usually matters most for future stateful stackable items.

## Making An Item Real

Creating an item asset is only step one. Use this checklist to make sure the game can actually use it.

### 1. Add It To The Item Database

Open `Assets/Resources/GameData/Item Database SO.asset` and add the item to `AllItems`.

Why this matters:

- Save/load restores items by database lookup.
- Missing database entries can make saved item IDs fail to resolve.

Use the database context menu `Auto-Populate Database` if appropriate, then review the result for unwanted test assets.

### 2. Assign A Real Icon

Set the `Icon` field on the item.

Why this matters:

- Inventory slots display `BaseItem.Icon`.
- Drag visuals use the same icon.
- World drops spawned by loot tables use the icon as their sprite.

Check that the sprite import settings produce a usable sprite, not just a texture.

### 3. Put It In Starting Inventory If Needed

For default player items, edit the relevant inventory manager prefab:

- Backpack: `Player_InventoryManager_Prefab.prefab`
- Potion slots: `Potion_Inventorymanager_Prefab.prefab`
- Equipment slots: `Equipment_InventoryManager_Prefab.prefab`

Rules:

- Use an `ItemInstance` with the desired `BaseItem`.
- Set `Count` greater than zero.
- Leave empty slots with `BaseItem: {fileID: 0}` and `Count: 0`.
- Equipment inventory slots must match their filters.
- Potion slots should contain consumables.

Prefer authoring starter equipment in backpack unless the player should begin with it already equipped.

### 4. Put It In Shops If Sold

For NPC shops, edit the NPC prefab inventory, such as `Assets/Prefabs/NPC/SmithNPC.prefab`.

Rules:

- Shop stock is an `InventoryManager` with live slots.
- Shop transactions pass the specific `ItemInstance`; this preserves runtime state for unique shop items.
- Use `GoldValue` as the unit buy/sell value.
- Make sure stack counts respect `MaxStackSize`.

If an NPC inventory is dynamic, ensure the open-screen payload or controller injects the correct shop container into `ShopManagerSO`.

### 5. Add It To Loot Tables If Dropped

Use `EnemyLootTableSO` assets for enemy drops.

Each drop entry needs:

- `item`
- `dropChance`
- `minQuantity`
- `maxQuantity`

Rules:

- Use stackable items for quantities above `1`.
- For equipment drops, keep quantity `1`.
- Loot tables spawn `WorldItem` objects at enemy death.
- The world item uses the item icon as its sprite.

Tune gold and XP ranges in the same loot table only if the item drop changes encounter rewards.

### 6. Add Crafting Recipes If Crafted

Create or update a `CraftingRecipeSO` in `Assets/Resources/GameData/Items/Crafting Recipies/`.

Fields:

- `BaseItemRequirement`
- `MaterialRequirements`
- `GoldCost`
- `OutputItem`

Then add the recipe to:

- `Assets/Resources/GameData/ItemManagers/Crafting Registry SO.asset`

Rules:

- The forge manager matches recipes by base item plus material item.
- The current UI supports selected inventory slots as visual proxies.
- Crafting removes requirements from the authoritative player inventory only when the action is confirmed.
- Cache item base references before mutation when changing crafting logic.

### 7. Add Salvage Recipes If Salvageable

Create or update a `SalvageRecipeSO` in `Assets/Resources/GameData/Items/Salvage Recipies/`.

Fields:

- `TargetItem`
- `GoldYield`
- `MaterialYields`

Then add the recipe to:

- `Assets/Resources/GameData/ItemManagers/Crafting Registry SO.asset`

Rules:

- Salvage removes one target item.
- Gold salvage uses `GoldYield`.
- Material salvage uses `MaterialYields`.
- If the recipe has no material yield, material salvage should not be offered.

### 8. Add World Pickups If Found In The World

Use a GameObject with `WorldItem`.

Fields:

- `_itemData`
- `_quantity`
- Optional quest fact override fields.

Rules:

- Successful pickup creates a new `ItemInstance` from the blueprint.
- Pickup succeeds only if the target inventory can accept the item.
- The object is destroyed only after inventory accepts it.
- Quest pickup facts are reported after successful inventory mutation.

Use explicit quest IDs for quest-critical pickups. Do not rely on GameObject names.

### 9. Wire Quest References If Needed

If quests need to detect buying, selling, or pickup:

- Use the item asset filename as the stable exact ID where possible.
- Use clear type/tag strings for broader objective matching.
- Keep quest-critical item display names stable once authored.

Quest facts should follow authoritative inventory state, not UI selection state.

## Balance Pass Checklist

For each item, review:

- Acquisition source.
- Gold value.
- Stack size.
- Drop rate.
- Shop quantity.
- Crafting cost.
- Salvage yield.
- Equipment slot.
- Stat budget.
- Whether it competes with existing items.
- Whether it has a reason to exist in the current biome or encounter.

Simple starting budgets:

- Common material: low gold value, high stack size, frequent drops.
- Uncommon material: moderate value, moderate drop rate, used in recipes.
- Consumable: low to moderate value, useful but limited by stack and cooldown.
- Basic weapon: modest base damage, no evolving component unless special.
- Rare weapon: stronger identity, maybe upgradeable or evolving.
- Armor: clear defensive role and target slot.
- Quest item: low or zero sell value unless intentionally sellable.

## Validation Checklist

Before calling the item done:

- The item asset has a unique filename.
- `ItemName` is player-facing and not a debug name.
- `Description` is filled in.
- `Icon` is assigned and visible in inventory.
- `MaxStackSize` matches item behavior.
- Equipables have `MaxStackSize = 1`.
- Component fields are configured.
- Runtime state is created only when needed.
- The item is in `Item Database SO.asset`.
- Starting inventory, shop, loot, crafting, salvage, or world pickup references are added as needed.
- Recipe and salvage assets are registered in the crafting registry.
- Quantity values do not exceed stack rules.
- Quest fact IDs are stable for quest-critical items.
- No UI script directly mutates item content data.
- No `GameObject.Find()` or hard coupling was added.
- Debug logs added during testing are wrapped in `#if UNITY_EDITOR` or removed.
- `Assets/Documentation/Changelog/CHANGELOG.md` is updated for the content change.

## Static Verification Without Unity Editor

This environment may not have a headless Unity Editor. When the Editor is not available, use static checks:

- Search for the item asset GUID to confirm references.
- Search for the item asset filename to confirm save ID usage.
- Inspect YAML for `m_Script` references and component data.
- Check that all new `.asset` files have `.meta` files.
- Confirm the item database contains the new item GUID.
- Confirm recipes and salvage entries are in the registry.
- Confirm loot table quantities make sense for stackability.

Then verify in Unity or CI when available.

## Common Pitfalls

- Creating an item asset but forgetting the item database.
- Renaming an asset after saves or quests depend on the old ID.
- Leaving `Description` empty.
- Setting equipment stack size above `1`.
- Giving stackable quantities to equipment loot.
- Forgetting to register a recipe in the crafting registry.
- Using display names as quest IDs.
- Putting dynamic data on the blueprint instead of item state.
- Putting Unity references in item state.
- Mutating inventory directly from UI views.
- Changing input mappings or unrelated UI while authoring item content.

## Suggested Authoring Order

Use this order for most item batches:

1. Define item roster and roles.
2. Select icons.
3. Create or update `InventoryItemSO` assets.
4. Configure components and stack sizes.
5. Add all items to the item database.
6. Add acquisition sources: loot, shops, world pickups, starting inventory.
7. Add usage routes: crafting, salvage, quests.
8. Review balance values.
9. Run static checks.
10. Verify in Unity or CI when available.
11. Update the changelog.

## Quick Template

Use this mini-template when planning an item:

```text
Asset ID:
Display name:
Description:
Role:
Type/components:
Icon:
Stack size:
Gold value:
Acquisition:
Usage:
Crafting:
Salvage:
Quest facts:
Balance notes:
Verification notes:
```
