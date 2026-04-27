# MainArea / ProceduralTiles State Transfer Analysis

This document analyzes how `MainArea` and `ProceduralTiles` currently hand off player state, UI, and scene-specific systems.

It is intentionally focused on practical scene-pipeline decisions, not on enemy combat logic.

## Goal

Define a clean rule for:

- what should transfer between `MainArea` and `ProceduralTiles`
- what should stay scene-local
- which UI screens belong in both scenes
- which screens should only exist where they are actually usable

## Current Scene Pipeline

### Scene Loading

Both scene transition paths currently perform a normal single-scene load:

- [SceneTransitionService.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/MapGeneration/Runtime/Transitions/SceneTransitionService.cs)
- [SceneLoader.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/SceneTesting/SceneLoader.cs)

That means:

- old scene objects are destroyed
- new scene objects are freshly created
- only `DontDestroyOnLoad` objects and shared ScriptableObject state remain alive

So there is no automatic object continuity between `MainArea` and `ProceduralTiles`.

## What Actually Survives Today

### 1. Persistent Services

These survive scene changes because they are marked persistent:

- `SceneTransitionService`
- `SceneLoader`

These are orchestration/services only. They do not carry player inventory data.

### 2. Shared ScriptableObjects

These remain loaded as shared assets across scene changes:

- [GameSessionSO.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/ScriptableObjects/GameSessionSO.cs)
- [PlayerProgressionAnchorSO.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Anchors/PlayerProgressionAnchorSO.cs)
- [PlayerStatsAnchorSO.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Anchors/PlayerStatsAnchorSO.cs)
- shop/crafting/salvage manager SOs

Important detail:

- `GameSessionSO.PlayerInventory` is **not** stored as data
- it is only a runtime pointer to the currently active scene `InventoryManager`

So `GameSessionSO` is currently a reference bridge, not a real transfer container for inventory contents.

### 3. Skill Tracker

`GameSessionSO` already contains:

- `_playerSkills`

Unlike `PlayerInventory`, this is actual data on the ScriptableObject, so it is already much closer to transferable state.

## What Resets Today

### 1. Player Inventory Containers

The backpack and equipment containers are currently scene objects:

- [InventoryManager.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryManager.cs)

Behavior today:

- when the player inventory scene object enables, it sets `GlobalSession.PlayerInventory = this`
- when it disables, it clears that reference

This means:

- `MainArea` has its own scene inventory object
- `ProceduralTiles` has its own scene inventory object
- switching scenes swaps the pointer to a different scene container

So inventory contents are not truly being transferred by system design right now.

### 2. Equipment Inventory

Equipment is also scene-local:

- `PlayerEquipments` exists separately in both scenes

Like the backpack, this is currently a fresh scene object, not a shared runtime data store.

### 3. Player Progression Runtime

- [PlayerProgression.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Status/PlayerProgression.cs)

The anchor pattern here only exposes the current scene instance.

It does **not** preserve runtime gold/xp/level by itself.

`PlayerProgression` creates fresh runtime progression in `Awake()` and initializes from config if enabled.

So without an explicit transfer/apply step, progression is also scene-bound in practice.

### 4. Player Stats Runtime

- [PlayerStats.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Status/PlayerStats.cs)

Same pattern:

- anchor gives access to the current scene instance
- runtime stats are rebuilt in `Awake()`
- health/stamina are initialized there again

So current HP/stamina are not automatically preserved between scenes either.

### 5. Consumable Cooldowns / Active Timed Effects

These are currently runtime-only:

- [PlayerConsumableController.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/PlayerConsumableController.cs)

That means scene changes currently reset:

- per-item cooldown tracking
- timed consumable expiration tracking

This is probably acceptable for now unless scene transitions are meant to feel fully continuous.

## What The Existing Managers Actually Expect

These systems already assume there is one authoritative player inventory pointer:

- [ShopManagerSO.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/ScriptableObjects/ShopManagerSO.cs)
- [CraftingManagerSO.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/ScriptableObjects/CraftingManagerSO.cs)
- [SalvageManagerSO.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/ScriptableObjects/SalvageManagerSO.cs)

They all consume:

- `SessionData.PlayerInventory`
- progression anchor data

So from their point of view, the clean contract is:

- there should be one currently valid player inventory
- it should already contain the correct runtime items for the current run

That reinforces the idea that scene transfer should happen **before** those systems become active in the new scene.

## Important Clarification

- [InventoryTransferManagerSO.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryTransferManagerSO.cs)

Despite the name, this is not scene transfer.

It only handles:

- drag/drop moves
- stacking
- swapping between containers inside the active scene/UI

So it should not be treated as the MainArea <-> ProceduralTiles handoff system.

## Recommended Split: Transferable Vs Scene-Local

## Transferable Player Runtime State

These should be preserved when moving between `MainArea` and `ProceduralTiles`:

- backpack inventory contents
- equipment inventory contents
- gold
- xp
- level
- skills
- equipped ability slots
- optionally current HP/stamina

### Optional Later Transfer State

These can be deferred until later if needed:

- active buff timers
- consumable cooldown timers
- temporary world-only effect sources

## Scene-Local State

These should be rebuilt fresh per scene:

- `UIManager`
- screen controllers
- prompt UI
- HUD bridge
- world/site runtime
- vendor/shop inventories
- biome/world generation state
- any NPC-specific interaction screens

This separation is important:

- player runtime state should transfer
- scene presentation and interaction wiring should not

## Recommended UI Screen Split

## Screens That Should Exist In Both Scenes

If they are meant to be globally available to the player, these belong in both `MainArea` and `ProceduralTiles`:

- HUD
- Inventory
- Skills

Reason:

- they are player-owned views
- they do not depend on one specific hub/vendor context

## Screens That Should Be Scene-Specific

These should only exist in scenes where they are actually usable:

- Smith
- Mage
- any vendor/crafting/salvage-specific screen

Reason:

- they are interaction-driven
- they depend on scene NPCs or scene-specific shop containers
- they should not be part of the generic world-scene baseline

So if `ProceduralTiles` does not contain a smith or mage interaction, those screen controllers should simply not be present there.

## Practical Recommendation

### MainArea

Keep:

- HUD
- Inventory
- Skills
- Smith
- Mage
- any hub-only crafting/vendor screens

### ProceduralTiles

Keep:

- HUD
- Inventory
- Skills

Leave out by default:

- Smith
- Mage
- hub/vendor-only screens

Add those only if the world scene later gets a real in-world interaction that can open them.

## Recommended Input Policy Split

The two scenes should not treat gameplay input the same way.

### MainArea

`MainArea` is a hub.

That means:

- movement should stay active
- NPC/world interaction should stay active
- combat-style gameplay input should be blocked

Blocked in hub:

- shooting
- active abilities

Still allowed in hub:

- dash

### ProceduralTiles

`ProceduralTiles` is the active exploration/combat scene.

That means:

- movement should stay active
- NPC/world interaction should stay active
- combat-style gameplay input should stay active

### Blocking UI Rule

Whenever a blocking screen is open, gameplay input should be suppressed regardless of scene.

That means when screens such as:

- Inventory
- Smith
- Mage
- Skills

are open, the player should not:

- move
- interact with the world
- shoot
- dash
- trigger abilities

This keeps hub UI and overworld UI from fighting with live gameplay input.

## Recommended Transfer Architecture

The cleanest architecture is:

1. use scene objects for the active player/inventory/components in each scene
2. use one shared runtime data container for the transferable player state
3. capture before scene load
4. apply after scene load
5. let scene-local UI/controllers bind to the newly created scene objects

## Recommended Data Container

Use one shared runtime session container for transferable player data.

This can be either:

- an expanded `GameSessionSO`

or

- a new dedicated runtime player-state SO

Recommended contents:

- backpack slot snapshot data
- equipment slot snapshot data
- progression snapshot
- skill snapshot
- optional current HP/stamina snapshot

Important:

this should store **data**, not scene component references.

That is the core problem with the current inventory setup.

## Recommended Transition Flow

### Before Leaving Scene A

Capture from the current scene player:

- backpack contents
- equipment contents
- gold/xp/level
- skills if needed
- optional HP/stamina

### Load Scene B

Use normal single-scene load as you already do.

### After Scene B Loads

Resolve the new scene instances:

- player inventory manager
- equipment inventory manager
- player progression
- player stats

Then apply the transferred snapshot into them.

After that:

- `GameSessionSO.PlayerInventory` points at the correct new scene container
- anchors point at the correct new scene progression/stats components
- UI controllers bind normally

## Why This Is Better Than Carrying Scene Objects Across Scenes

Because it keeps the project consistent with how it already works:

- scenes build their own local presentation/runtime objects
- ScriptableObjects act as bridges and shared data holders
- scene transition remains simple

If you instead try to carry the old player inventory objects or UI objects directly across scenes, you will end up mixing:

- scene-local references
- persistent objects
- stale controller hookups

and the whole setup becomes harder to reason about.

## First Recommended Scope

To keep this manageable, the first real scene-transfer implementation should only preserve:

- backpack inventory
- equipment inventory
- gold
- xp
- level

Then decide separately whether to also preserve:

- current HP/stamina
- temporary buffs/cooldowns

That gives a clean first milestone without overcomplicating the handoff.

## Bottom Line

The right model is:

- transfer **player runtime data**
- rebuild **scene UI and scene services**

Not:

- transfer whole scene objects
- keep every screen/controller alive everywhere

So for `MainArea` <-> `ProceduralTiles`:

- keep `HUD`, `Inventory`, and likely `Skills` in both
- leave `Smith`, `Mage`, and similar screens out of `ProceduralTiles` unless that scene really supports them
- move backpack/equipment/progression through a dedicated runtime data snapshot, not through scene references

## Suggested Next Step

When you want to implement this, the next clean task would be:

- design the exact runtime snapshot format for:
  - backpack
  - equipment
  - progression

and decide whether that data should live in:

- `GameSessionSO`

or

- a separate dedicated runtime player-state ScriptableObject

## First-Pass Implementation Note

The current first-pass implementation direction is:

1. keep scene-owned player/runtime objects in both scenes
2. store transferable runtime snapshots inside `GameSessionSO`
3. let scene components capture themselves on `OnDisable()`
4. let new scene components restore themselves on `OnEnable()`

Current first-pass transfer scope:

- backpack inventory
- equipment inventory
- player progression (`level`, `xp`, `gold`)
- player runtime resources (`health`, `stamina`)

## Runtime Re-entry Notes

Two practical pitfalls showed up immediately once the first pass was wired:

- `PlayerStats` must restore against a valid resolved-effects baseline
  - if player resource state is applied before the effect pipeline has produced valid `maxHealth` / `maxStamina`, UI and damage behavior can drift after a scene swap
- enemy trigger checks should not depend on one cached player `GameObject`
  - when scenes rebuild the player, aggro and striking checks should identify the player through the active collider/tag/runtime receiver instead of a stale cached object reference

These are not separate systems from persistence.
They are part of making scene re-entry actually stable in play.

## Editor Testing Note

Because the first-pass transfer uses runtime snapshots stored on `GameSessionSO`, editor play-mode testing can become misleading if stale in-memory snapshots survive between play sessions.

The current implementation now clears those runtime-only snapshots at play start before the first scene loads.

That means:

- a real in-session scene transition still transfers correctly
- a fresh editor play session should not inherit old inventory / hp / stamina / progression handoff state
- actual serialized session data such as unlocked skills on `GameSessionSO` still remains until you reset that asset manually

This keeps the handoff lightweight while preserving the most important run continuity.
