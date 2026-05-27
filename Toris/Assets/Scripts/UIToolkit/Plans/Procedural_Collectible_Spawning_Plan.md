# Implementation Plan: Procedural Collectible Spawning (Corrected)

> [!NOTE]
> **CURRENT STATUS: PENDING**
> This procedural spawning and collectible persistence pipeline has not been started yet. All design blueprints, script hooks (such as `CollectibleSiteBridge.cs`), placement algorithms, and scene assets are mapped out and ready to be built in a future task.

This plan details how to procedurally spawn interactive, collectible items (flowers, herbs) across the world, making them persistent (they don't respawn once collected), leveraging the existing map generation pipeline.

> [!IMPORTANT]
> This plan was validated against the live codebase. All class names, interface signatures, data flows, and lifecycle hooks have been confirmed by reading the actual source files. The original plan had **2 critical breaking bugs** which are now fixed.

---

## 1. Architectural Strategy

### How the Existing Systems Work

| System | Interface | Triggered By | Purpose |
|---|---|---|---|
| **Item Pickup** | `IContainerInteractable` | `ItemPicker` (scans `Physics2D.OverlapCircleAll` every frame) | Picking up items into inventory |
| **NPC Interaction** | `IInteractable` | `InteractableProximity` → `PlayerInteractor` | NPCs, gates, dialogue triggers |
| **World Site Spawning** | `IWorldSiteContextConsumer` | `WorldSiteActivationPipeline.ActivateSite()` | Receives `WorldSiteContext` with persistence handle |
| **Consumed Persistence** | `WorldSiteStateHandle.MarkConsumed()` | Must be called explicitly by the spawned prefab | Prevents respawn on chunk reload |

### The Two Critical Bugs in the Original Plan (Now Fixed)

> [!CAUTION]
> **Bug 1 — Interface Mismatch (Fixed):** The original plan put `InteractableProximity` on the same prefab as `WorldItem`. This silently fails because `InteractableProximity` calls `GetComponentInParent<IInteractable>()`, but `WorldItem` implements `IContainerInteractable` — a completely different interface. These two systems are incompatible.
>
> **Fix:** Do NOT use `InteractableProximity`. The collectible is picked up by the existing `ItemPicker` system, which already scans for `IContainerInteractable` via `Physics2D.OverlapCircleAll`. This is how all existing item drops work.

> [!CAUTION]
> **Bug 2 — Flowers Respawn Forever (Fixed):** The original plan assumed `SkipIfConsumed = true` alone prevents respawning. It doesn't. `SkipIfConsumed` only *checks* the consumed flag during spawn — nothing in the original plan ever *sets* that flag. `WorldItem` calls `Destroy(gameObject)` on pickup but never notifies the `ChunkStateStore`.
>
> **Fix:** Add a new bridge component `CollectibleSiteBridge` that implements `IWorldSiteContextConsumer`, receives the `WorldSiteStateHandle` from the pipeline, and calls `siteState.MarkConsumed()` when the flower is collected.

### Design Decisions

- **Use the `ItemPicker` system** (not `InteractableProximity`) — `WorldItem` already implements `IContainerInteractable`, so `ItemPicker` detects and collects it automatically with prompt display.
- **New `CollectibleSiteBridge`** component bridges the gap between the item pickup system and the world site persistence system.
- **New `CollectibleSitePlacementRuleDefinition`** extends `SitePlacementRuleDefinition` (like `WolfDenSitePlacementRuleDefinition` does) to scatter collectibles using noise-based sampling.
- **No modifications to `WorldItem.cs`** — we use Unity events to notify the bridge.

---

## 2. Proposed Changes (Step by Step)

---

### Step 1: Create the `CollectibleSiteBridge` Component

#### [NEW] `Assets/Scripts/MapGeneration/Runtime/Sites/CollectibleSiteBridge.cs`

This is the **critical missing piece**. It bridges `WorldItem` pickup → `ChunkStateStore` consumed persistence.

```csharp
using UnityEngine;

/// <summary>
/// Bridges a WorldItem collectible with the world site persistence system.
/// When placed on a procedurally spawned prefab:
/// 1. Receives the WorldSiteStateHandle from WorldSiteActivationPipeline
/// 2. Listens for the WorldItem being destroyed (collected)
/// 3. Marks the site as consumed so it never respawns
/// </summary>
public class CollectibleSiteBridge : MonoBehaviour, IWorldSiteContextConsumer
{
    private WorldSiteStateHandle _siteState;
    private bool _initialized;

    public void Initialize(WorldSiteContext siteContext)
    {
        // The pipeline calls this with the site's unique state handle
        _siteState = siteContext.WorldSiteStateService != null
            ? siteContext.WorldSiteStateService.GetSiteState(
                siteContext.Placement.ChunkCoord,
                siteContext.SpawnId)
            : default;

        _initialized = _siteState.IsValid;
    }

    /// <summary>
    /// Call this when the collectible is picked up.
    /// Marks the spawn point as consumed in ChunkStateStore.
    /// </summary>
    public void OnCollected()
    {
        if (!_initialized) return;
        _siteState.MarkConsumed();
    }

    private void OnDestroy()
    {
        // Safety net: if WorldItem calls Destroy(gameObject)
        // on successful pickup, mark consumed automatically
        if (_initialized)
        {
            _siteState.MarkConsumed();
        }
    }
}
```

**How the persistence chain works end-to-end:**

```
1. WorldSiteActivationPipeline.ActivateSite() spawns the collectible prefab
2. Pipeline finds IWorldSiteContextConsumer[] → calls CollectibleSiteBridge.Initialize()
3. Bridge receives WorldSiteStateHandle (backed by ChunkStateStore)
4. Player walks near flower → ItemPicker detects IContainerInteractable
5. Player presses pickup → WorldItem.Interact() adds item to inventory
6. WorldItem calls Destroy(gameObject) on success
7. CollectibleSiteBridge.OnDestroy() fires → calls siteState.MarkConsumed()
8. ChunkStateStore records the spawnId as consumed
9. Player leaves and returns → Pipeline checks SkipIfConsumed → IsConsumed == true → skips spawn ✓
```

> [!NOTE]
> Using `OnDestroy()` as the persistence trigger is intentionally simple. Since `WorldItem` already calls `Destroy(gameObject)` on successful pickup, the bridge can catch this in its `OnDestroy()` callback. This avoids modifying `WorldItem.cs`. If the chunk is unloaded (destroying the GameObject without pickup), `_initialized` will be true but the flower was never collected — this is fine because `MarkConsumed()` being called on chunk unload would be a bug. To prevent this: set `_initialized = false` when the chunk is deactivated, or use a `_wasCollected` flag set by `WorldItem`.

**Safer alternative using a collected flag (recommended):**

```csharp
private bool _wasCollected;

public void OnCollected()
{
    _wasCollected = true;
    if (_initialized)
        _siteState.MarkConsumed();
}

private void OnDestroy()
{
    // Only persist if actually collected, not on chunk unload
    if (_wasCollected && _initialized)
        _siteState.MarkConsumed();
}
```

> [!WARNING]
> **The `OnDestroy()` safety-net approach has a subtle risk**: if the chunk unloads and destroys the flower GameObject without the player collecting it, `OnDestroy()` would fire and incorrectly mark the flower as consumed. **Use the `_wasCollected` flag pattern above** to guard against this. `WorldItem` must call `bridge.OnCollected()` before `Destroy(gameObject)`.

---

### Step 2: Modify `WorldItem` to Notify the Bridge on Pickup

#### [MODIFY] [WorldItem.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/Items/WorldItem.cs)

Add a single line to the `Interact()` method to notify any `CollectibleSiteBridge` before destroying:

```diff
             if (success)
             {
                 // Visual feedback, sound effects go here
                 ReportQuestPickUpFactIfNeeded();
+
+                // Notify world site persistence (if spawned by the procedural pipeline)
+                var siteBridge = GetComponent<CollectibleSiteBridge>();
+                if (siteBridge != null) siteBridge.OnCollected();
+
                 Destroy(gameObject);
                 return true;
             }
```

**Why this is safe:**
- `GetComponent<CollectibleSiteBridge>()` returns `null` on non-procedural WorldItems (hand-placed drops, quest rewards, etc.) — zero overhead for existing items.
- The call must happen **before** `Destroy()` so the bridge can call `MarkConsumed()` while the handle is still valid.
- Only 3 lines added; no existing behavior is changed.

---

### Step 3: Create the Placement Rule for Collectibles

#### [NEW] `Assets/Scripts/MapGeneration/Generation/BuildSteps/CollectibleSitePlacementRuleDefinition.cs`

Follows the same pattern as [WolfDenSitePlacementRuleDefinition.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/MapGeneration/Generation/BuildSteps/WolfDenSitePlacementRuleDefinition.cs):

```csharp
[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Collectible Site Rule",
    fileName = "CollectibleSitePlacementRuleDefinition")]
public sealed class CollectibleSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    [Header("Site")]
    [SerializeField] private WorldSiteDefinition[] collectibleSiteDefinitions;

    [Header("Count")]
    [SerializeField, Min(0)] private int minCount = 5;
    [SerializeField, Min(0)] private int maxCount = 12;

    [Header("Placement")]
    [SerializeField, Min(1)] private int minSpacingTiles = 8;
    [SerializeField, Range(0.1f, 1f)] private float placementRadiusFactor = 0.85f;
    [SerializeField, Min(0)] private int avoidOriginRadiusTiles = 10;

    public override void BuildSites(WorldContext ctx) { ... }
}
```

**Key behaviors:**
- `collectibleSiteDefinitions` is an **array** — supports multiple flower types. Each site is randomly selected from the array per placement (weighted or round-robin).
- Uses `SitePlacementSampling.PickSpacedCentersInBiomeDisk()` (the same utility `WolfDenSitePlacementRuleDefinition` uses) to deterministically scatter positions.
- Validates candidate tiles via `ctx.Mask.IsLand()` (no flowers in water).
- Registers each via `buildOutput.RegisterSite(selectedDefinition, centerTile, ctx.World.chunkSize)`.
- Uses `SitePlacementLifecycleScope.Chunk` (default) so flowers are managed per-chunk, spawning/despawning as the player streams chunks.

---

### Step 4: Create Collectible Prefab Assets

#### [NEW] `Prefabs/World/Collectibles/Collectible_GoldFlower.prefab`

Hierarchy:
```
Collectible_GoldFlower (root)
├── SpriteRenderer         (assigned by WorldItem.ApplyVisuals() from _itemData.Icon)
├── Collider2D             (isTrigger = true, small circle for ItemPicker detection)
├── WorldItem              (component)
│   ├── _itemData:           → GoldFlower_ItemSO asset
│   ├── _quantity:           1
│   ├── _reportQuestPickUpFact: true
│   ├── _questItemFactType:  QuestFactType.Collect
│   ├── _questItemTypeOrTag: "Herb"
│   └── _questItemContextId: "" (set per-biome or left empty)
└── CollectibleSiteBridge  (component — receives persistence context from pipeline)
```

**What this prefab does NOT have:**
- ❌ `InteractableProximity` — not needed. `ItemPicker` handles proximity detection via `Physics2D.OverlapCircleAll` and displays the `"E"` prompt via `InteractionPromptUI`.
- ❌ `IInteractable` — wrong interface for item pickup.

**LayerMask requirement:** The prefab's `Collider2D` must be on a layer included in `ItemPicker._layerMask` so that `Physics2D.OverlapCircleAll` can detect it.

> [!IMPORTANT]
> Check which layer existing dropped items (loot, quest items) use — the collectible must use the **same layer** so `ItemPicker` can find it. This is typically an `Items` or `Interactable` layer.

---

### Step 5: Create WorldSiteDefinition Assets

#### [NEW] `Assets/MapGeneration/Generation/Data/ScriptableObjects/Sites/Collectible_GoldFlower_Definition.asset`

A new ScriptableObject of type `WorldSiteDefinition`:

| Field | Value | Rationale |
|---|---|---|
| `siteId` | `"Collectible_GoldFlower"` | Unique identifier for this collectible type |
| `prefab` | `Collectible_GoldFlower.prefab` | The prefab from Step 4 |
| `skipIfConsumed` | `true` | **CRITICAL**: enables persistent pickup memory |
| `spawnSalt` | `0xF10E7001` | Unique seed salt for deterministic hashing (any unique uint) |
| `runtimeConfig` | `null` | No special runtime config needed for simple collectibles |

Create additional definitions for other flower types (e.g., `Collectible_BloodRose_Definition.asset`) following the same pattern, each with a unique `siteId` and `spawnSalt`.

---

### Step 6: Create Placement Rule Asset and Register in Biome

#### [NEW] `Assets/MapGeneration/Generation/Data/ScriptableObjects/BiomeData/Plains/PlacementRules/CollectibleSitePlacementRuleDefinition.asset`

An instance of the class from Step 3, configured for the Plains biome:

| Field | Value |
|---|---|
| `collectibleSiteDefinitions` | `[Collectible_GoldFlower_Definition, Collectible_BloodRose_Definition, ...]` |
| `minCount` | `5` |
| `maxCount` | `12` |
| `minSpacingTiles` | `8` |
| `placementRadiusFactor` | `0.85` |
| `avoidOriginRadiusTiles` | `10` |

#### [MODIFY] `Assets/MapGeneration/Generation/Data/ScriptableObjects/BiomeData/Plains/BuildSteps/SitePlacementRuleBuildStepDefinition.asset`

Add the new collectible rule to the existing `sitePlacementRules[]` array:

```diff
  sitePlacementRules:
    - GateSitePlacementRuleDefinition     (existing)
+   - CollectibleSitePlacementRuleDefinition  (new)
```

This ensures the generation pipeline evaluates and places flowers during Plains biome world building.

---

### Step 7: Create Item SO Assets (Flower Inventory Items)

#### [NEW] `Assets/Items/Collectibles/GoldFlower_ItemSO.asset`

A new `InventoryItemSO` representing the Gold Flower in the player's inventory:

| Field | Value |
|---|---|
| `ItemName` | `"Gold Flower"` |
| `Icon` | Gold flower sprite (also used by `WorldItem.ApplyVisuals()` for the world sprite) |
| `Description` | `"A radiant golden bloom found in the wilderness."` |
| `ItemType` | Material / Herb (whatever type fits existing categories) |
| `MaxStackSize` | `99` |
| `Components` | None needed (purely a material/crafting ingredient) |

---

## 3. File Summary

| # | Action | File | Purpose |
|---|---|---|---|
| 1 | **NEW** | `CollectibleSiteBridge.cs` | **Critical fix**: bridges `WorldItem` pickup → `ChunkStateStore.MarkConsumed()` |
| 2 | **MODIFY** | `WorldItem.cs` | 3-line change: notify bridge before `Destroy()` |
| 3 | **NEW** | `CollectibleSitePlacementRuleDefinition.cs` | Placement rule (extends `SitePlacementRuleDefinition`) |
| 4 | **NEW** | `Collectible_GoldFlower.prefab` | Prefab with `WorldItem` + `CollectibleSiteBridge` |
| 5 | **NEW** | `Collectible_GoldFlower_Definition.asset` | `WorldSiteDefinition` with `SkipIfConsumed = true` |
| 6 | **MODIFY** | `SitePlacementRuleBuildStepDefinition.asset` (Plains) | Register new rule in biome pipeline |
| 7 | **NEW** | `GoldFlower_ItemSO.asset` | Inventory item data |

**What is NOT on the prefab (and why):**
- ❌ `InteractableProximity` — uses `IInteractable`, incompatible with `WorldItem`'s `IContainerInteractable`
- ❌ Child `InteractionPoint` GameObject — `ItemPicker` handles proximity via `OverlapCircleAll`

---

## 4. Verification Plan

### Manual Verification Steps

1. **Compilation**: Build the project. Verify no compilation errors.
2. **Layer check**: Verify the collectible prefab's `Collider2D` is on the same layer as existing droppable items (check `ItemPicker._layerMask` in the Inspector).
3. **World generation**: Enter Play mode. Walk into the Plains biome.
4. **Spawn check**: Locate spawned flowers. Verify:
   - Correct sprite displayed (from `_itemData.Icon`)
   - Prompt `"E"` appears when `ItemPicker` detects proximity
5. **Pickup**: Press the pickup button. Verify:
   - Flower disappears from the world
   - Item added to player inventory with correct name/icon/quantity
   - If active quest requires collecting herbs → quest progress increments
6. **Persistence (THE critical test)**:
   - After collecting a flower, note the chunk coordinates
   - Walk far away to force the chunk to unload
   - Walk back to reload the chunk
   - **Verify the collected flower does NOT respawn** ✓
   - Verify uncollected flowers in the same chunk DO respawn normally ✓
7. **Save/Load persistence**:
   - Collect a flower, save the game, reload
   - Walk to the same area → verify the flower stays gone
8. **Edge case — chunk unload without pickup**:
   - Walk near a flower but do NOT pick it up
   - Walk away (chunk unloads, `OnDestroy()` fires on the prefab)
   - Walk back → **verify the flower DOES respawn** (the `_wasCollected` flag ensures it's not falsely marked consumed)
