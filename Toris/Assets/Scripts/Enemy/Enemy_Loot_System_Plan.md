# Enemy Loot System Plan

This document defines the first-pass enemy loot system for Toris.

It is a planning document, not a changelog.

## Purpose

Enemies currently have combat, death, despawn, pooling, world integration, and encounter logic.

What is still missing is a proper shared reward pipeline for:

- item drops
- gold
- experience

The goal of this plan is to add loot without inventing a second inventory or pickup system.

## Key Design Decisions

### Physical vs Instant Rewards

The reward split for first pass should be:

- `items`
  - physically drop into the world
  - player picks them up manually
- `gold`
  - granted instantly on enemy death
- `xp`
  - granted instantly on enemy death

So the “plop out of the enemy” feeling is reserved for item loot.

Gold and XP are still enemy rewards, but they are not world pickups.

### Loot Roll Behavior

Each enemy death should roll its own table independently.

That means:

- Wolf A and Wolf B can produce different outcomes
- each item entry is rolled at death time
- multiple different items can drop from the same enemy if multiple entries succeed

### Enemy Ownership

For first pass, loot should be defined per enemy type / prefab via a dedicated loot table asset.

This gives us:

- `Leader Wolf` loot table
- `Minion Wolf` loot table
- `Necromancer` loot table
- `Blood Mage` loot table

That is simple to test and distinct enough for the current enemy roster.

### Blood Mage and Minion Wolf Policy

Blood Mages are valid enemies and should have their own loot tables.

They are not excluded just because they are summoned.

The same for Minion Wolves.

### Special Encounter Rewards

For now, focus only on enemy loot.

Site rewards such as:

- grave reward
- chest reward
- post-encounter interactable reward

should remain an open future extension, but not block first-pass enemy loot.

### Inventory Full Behavior

If the player inventory is full:

- dropped items remain in the world
- player can free space and return to pick them up
- if the player leaves the area and the world unloads, those items can be wiped

First pass does not need cross-area persistence for dropped world loot.

## What Already Exists And Should Be Reused

The project already contains the critical building blocks we should reuse.

### Enemy Death Hook

- `Enemy.Died`
- file: `Assets/Scripts/Enemy/Base/Enemy.cs`

This is the clean event point for spawning loot and granting instant rewards.

### World Pickup Object

- `WorldItem`
- file: `Assets/Scripts/Items/WorldItem.cs`

This already represents a physical item in the world and already supports player interaction.

### Pickup To Inventory Flow

- `ItemPicker`
- file: `Assets/Scripts/Player/Player/Inventory/ItemPicker.cs`
- `InventoryManager.AddItem`
- file: `Assets/Scripts/Player/Player/Inventory/InventoryManager.cs`

This already handles:

- selecting a world item
- attempting pickup
- respecting inventory fullness

So we should not build a second pickup or inventory path.

### Gold / XP Runtime

- `PlayerProgression`
- file: `Assets/Scripts/Player/Player/Status/PlayerProgression.cs`
- `PlayerProgressionAnchorSO`
- file: `Assets/Scripts/Player/Player/Anchors/PlayerProgressionAnchorSO.cs`

This already supports:

- `AddGold`
- `AddExperience`

So gold and XP rewards should flow through that system directly.

## Important Existing Constraint

`WorldItem` currently stores:

- `InventoryItemSO`
- `quantity`

It does not currently carry a fully authored `ItemInstance`.

That means first-pass world drops naturally support:

- dropping base items
- stack quantities

This is fine for first pass.

It does mean that first pass should avoid trying to drop:

- custom runtime item states
- special upgraded item instances
- randomized per-instance equipment data

That can be a future extension if needed.

## Recommended First-Pass Architecture

## 1. Loot Table Asset

Introduce a dedicated ScriptableObject, something like:

- `EnemyLootTableSO`

It should contain:

- item drop entries
- gold range
- XP range

### Item Drop Entry

Each item entry should support:

- `InventoryItemSO item`
- `float dropChance`
- `int minQuantity`
- `int maxQuantity`

Each item entry is rolled independently.

If the roll succeeds:

- choose quantity from the configured range
- spawn that many via one world drop stack

### Immediate Reward Fields

The same table should also support:

- `int minGold`
- `int maxGold`
- `int minXp`
- `int maxXp`

These are rolled once per enemy death and granted immediately.

This keeps all reward configuration in one place.

## 2. Enemy Loot Driver

Introduce one shared component or helper, something like:

- `EnemyLootDropper`

This should:

- subscribe to `Enemy.Died`
- roll the assigned `EnemyLootTableSO`
- spawn item drops around the dead enemy
- grant gold/xp immediately

This is better than hardcoding rewards inside each enemy type.

## 3. World Drop Spawn

Use `WorldItem` as the actual world pickup object.

Recommended first pass:

- one pooled or instantiated `WorldItem` per successful item entry
- set:
  - item type
  - quantity
- spawn it near the enemy death position with a small random scatter

Optional polish later:

- toss arc
- short pop-out force
- pickup magnet tuning

But those are not required for the system to work.

## 4. Instant Gold / XP Grant

Gold and XP should be granted on death through `PlayerProgression`, using the progression anchor rather than touching enemy-specific logic.

That means:

- no physical gold coins in world for first pass
- no physical XP orbs for first pass

This keeps the first implementation simpler and cleaner.

## Runtime Flow

The intended runtime flow should be:

1. enemy dies
2. enemy `Died` event fires
3. loot driver resolves the enemy’s assigned loot table
4. item entries are rolled independently
5. successful item entries spawn `WorldItem` drops around the corpse
6. gold is rolled and added instantly
7. XP is rolled and added instantly
8. enemy death animation / despawn proceeds normally

This flow should work for:

- Leader Wolf
- Minion Wolf
- Necromancer
- Blood Mage

## First-Pass Runtime Notes

The implemented first pass should stay deliberately light:

- loot resolves directly from `Enemy.Die()`
- gold and XP are granted immediately through `PlayerProgression`
- successful item rolls create runtime `WorldItem` drops
- those drops reuse the existing pickup path:
  - `WorldItem`
  - `ItemPicker`
  - `InventoryManager`
- those drops should behave like the working `MainArea` test items:
  - stable world pickups
  - no auto-float magnet behavior
- dropped items are not persisted across area unloads

For now, the runtime drop object is created in code rather than through a dedicated pooled loot prefab.

That keeps the first version small and easy to validate while still leaving room for:

- pooled loot objects
- custom drop VFX
- site rewards
- persisted world loot

## Data Ownership Recommendation

For first pass, assign loot tables directly on enemy prefabs or enemy controller components.

That is simpler than trying to infer loot from encounter sites or biome rules.

This also matches the current testing goal:

- each enemy type should feel obviously distinct

## What Should Be Removed / Replaced

There is already at least one direct reward shortcut in enemy code:

- `Wolf.cs` currently adds gold directly on death

That behavior should be replaced by the shared loot system.

Reason:

- direct per-enemy reward logic does not scale
- it makes Blood Mage / Necromancer / Wolf rewards inconsistent
- it bypasses the idea of authored loot tables

So once the loot system is implemented, bespoke reward code in enemy scripts should be migrated into loot tables.

## Suggested First-Pass Data Model

### EnemyLootTableSO

Fields:

- `List<EnemyLootItemEntry> itemDrops`
- `int minGold`
- `int maxGold`
- `int minXp`
- `int maxXp`

### EnemyLootItemEntry

Fields:

- `InventoryItemSO item`
- `float dropChance`
- `int minQuantity`
- `int maxQuantity`

This is enough for the current game state.

## Why This Is The Right First Pass

This design intentionally avoids overbuilding.

It does not add:

- site reward system
- chest reward system
- persisted dropped-loot chunks
- custom runtime item-instance drops
- weighted nested loot groups
- rarity tiers
- biome-dependent reward rules

Those are all valid later, but unnecessary for the first working enemy loot system.

The first pass only needs:

- enemy-specific authored tables
- multi-drop support
- quantity ranges
- instant gold/xp
- world item pickup reuse

## Open Future Extensions

These should stay open for later, but should not block first pass:

### Site Rewards

Examples:

- grave reward after Necromancer death
- Wolf Den clear reward
- shrine/chest/corpse interactables

### Advanced Drop Logic

Examples:

- guaranteed + chance entries
- weighted one-of-many groups
- rare bonus rolls
- difficulty-tier multipliers

### Custom Item Instances

If later we want enemies to drop things like:

- special upgraded weapons
- generated equipment states
- unique item variants

then `WorldItem` would need to be extended to hold a full `ItemInstance`, not just `InventoryItemSO`.

### Persistent World Drops

If later we want loot to survive unloading/reloading a chunk, that should integrate with:

- `ChunkStateStore`

But that is not needed for first pass.

## Recommended Implementation Order

1. Create `EnemyLootTableSO`
2. Create `EnemyLootItemEntry`
3. Create shared enemy loot driver / spawner
4. Hook it to `Enemy.Died`
5. Use `WorldItem` for item drops
6. Use `PlayerProgression` for gold/xp
7. Remove direct bespoke reward code from enemy scripts
8. Author distinct loot tables for:
   - Leader Wolf
   - Minion Wolf
   - Necromancer
   - Blood Mage
9. Validate:
   - items drop physically
   - multiple entries can drop
   - quantity ranges work
   - gold/xp grant instantly
   - inventory-full behavior leaves items in world

## Immediate Next Step

The next implementation step should be:

- build the first-pass shared enemy loot table system using existing `WorldItem`, `ItemPicker`, `InventoryManager`, and `PlayerProgression`

That gives us a real enemy reward pipeline without duplicating systems the project already has.
