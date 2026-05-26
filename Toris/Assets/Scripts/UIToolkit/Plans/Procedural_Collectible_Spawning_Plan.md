# Implementation Plan: Procedural Collectible Spawning (Flowers & Herbs)

This plan details how to procedurally spawn interactive, collectible objects (like flowers or herbs) across the procedurally generated world, leveraging the existing map-generation architecture (`WorldSiteDefinition`), `WorldItem` component, and chunk persistence system.

---

## 1. Architectural Strategy

To make flowers interactive, collectable, and stateful (so they do not respawn once picked up), they will be modeled as **Lightweight World Sites**:
1. **Model as World Sites**: Create a `WorldSiteDefinition` for collectibles (e.g., `Flower_GoldDrop`, `Flower_BloodRose`).
2. **State Persistence**: Enable `skipIfConsumed = true` on the `WorldSiteDefinition`. This hooks into the existing `ChunkStateStore` automatically: once a player collects the flower, its `SpawnId` is marked as consumed, preventing it from ever respawning when returning to the chunk.
3. **Procedural Distribution**: Register the collectible sites under a new or existing **Site Placement Rule** in the biome profiles (e.g., `WildernessCollectiblePlacementRule`), distributing them deterministically based on noise samplers.

---

## 2. Proposed Changes

### Component: Collectible Prefab Assets

We will create simple Prefabs to represent these flowers in the world.

#### [NEW] `Prefabs/World/Collectibles/Collectible_GoldFlower.prefab`
- Contains a `SpriteRenderer` component (starts empty, gets assigned by `WorldItem` on spawn).
- Contains a `Collider2D` (marked as `Is Trigger` with a small radius).
- Contains a child GameObject with `InteractableProximity` to trigger prompt detection.
- Contains the **`WorldItem`** component:
  - `_itemData`: Assign your custom `GoldFlower_ItemSO` asset.
  - `_quantity`: `1` (or random range if preferred).
  - `_reportQuestPickUpFact`: `true` (so picking it up progresses gather quests).
  - `_questItemFactType`: `QuestFactType.Collect`.

---

### Component: Procedural Generation Configurations

#### [NEW] [Assets/WorldGen/Sites/Collectible_GoldFlower_Definition.asset](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/WorldGen/Sites/Collectible_GoldFlower_Definition.asset)
- A new ScriptableObject of type `WorldSiteDefinition`:
  - `SiteId`: `"Collectible_GoldFlower"`
  - `Prefab`: Drag the `Collectible_GoldFlower` prefab.
  - `SkipIfConsumed`: `true` (CRITICAL: enables persistent pickup memory!).
  - `SpawnSalt`: `12345` (or any random seed salt).

#### [NEW] [Assets/WorldGen/Rules/WildernessCollectiblePlacementRule.asset](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/WorldGen/Rules/WildernessCollectiblePlacementRule.asset)
- A new ScriptableObject representing the procedural placement rule for collectibles:
  - Define placement constraints (e.g., must spawn on `Ground` tiles, cannot spawn on `Water` tiles, must be at least 4 tiles away from roads/wolf dens).
  - Noise density thresholds: controls how densely flowers are clustered.
  - Assign `Collectible_GoldFlower_Definition` as a potential placement outcome.

---

### Component: Biome Registration

#### [MODIFY] [PlainsBiomeProfile.asset](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/WorldGen/Biomes/PlainsBiomeProfile.asset)
- Append `WildernessCollectiblePlacementRule` to the active placement rules list so that the procedural generation pipeline evaluates and places flowers during Plains biome streaming.

---

## 3. Verification Plan

### Manual Verification Steps
1. Play the game and walk into the procedurally generated wilderness.
2. Locate a spawned flower; observe the correct sprite and a hovering `"E"` interaction prompt when close.
3. Press `E` to pick up the flower.
4. Verify that:
   - The flower disappears from the world scene.
   - The item is successfully added to the player's active inventory.
   - If an active quest requires gathering flowers, quest progress increments.
5. Walk far away to force the chunk to stream out (unload), then return to stream the chunk back in.
6. Verify that the collected flower **does not respawn** (confirming `SkipIfConsumed` persistence).
