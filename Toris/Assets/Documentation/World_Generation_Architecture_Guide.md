# World Generation Architecture Guide

## Purpose

This document explains how the current world-generation stack works in `Assets/Scripts/MapGeneration`, how its major systems fit together, and how to extend it safely.

It is intended to answer three practical questions:

1. What owns world generation at runtime?
2. How does generated data flow from authored biome assets into loaded chunks and active site prefabs?
3. How do you add a new structure or world system without reopening the entire architecture?

## Folder Layout

The MapGeneration script tree is now organized into six top-level folders:

- Diagnostics: HUD and diagnostics read models.
- Generation: biome definitions, build steps, generated output types, shared generation helpers, tile generation, and generation-side data assets.
- Navigation: navigation contracts and navigation lifecycle/runtime.
- Runtime: shared runtime infrastructure, world composition, transitions, services, and generic site runtime plumbing.
- Sites: concrete world content such as gates and wolf dens.
- Streaming: chunk streaming state, processing, and runtime ownership.

This layout is meant to stay readable without collapsing unlike systems into the same bucket.
## High-Level Mental Model

The world stack is built around a simple flow:

1. `WorldGenRunner` composes the runtime systems.
2. `WorldTransitionSystem` selects and binds the active biome.
3. The biome's authored build steps write neutral generated data into `WorldBuildOutput`.
4. `WorldStreamingRuntime` decides which chunks should exist around the player.
5. `ChunkProcessingPipeline` generates chunk tiles, applies them, builds navigation, and activates chunk-scoped sites.
6. `WorldFeatureLifecycleSystem` owns site activation and deactivation.
7. `WorldSiteActivationPipeline` spawns pooled site prefabs and injects `WorldSiteContext`.
8. Site-specific runtime logic lives on the prefab side instead of in the runner.

This means the system is no longer structured around a single monolith. The build layer produces neutral data, the streaming layer decides what should be loaded, the lifecycle layer owns active world features, and site runtimes consume narrow services.

## Core Runtime Ownership

### `WorldGenRunner`

File: `Assets/Scripts/MapGeneration/Runtime/World/WorldGenRunner.cs`

`WorldGenRunner` is the composition root. It wires together:

- `WorldContext`
- `ChunkStateStore`
- `ChunkStreamingSystem`
- `ChunkProcessingPipeline`
- `ChunkStreamingCoordinator`
- `WorldStreamingRuntime`
- `WorldNavigationLifecycle`
- `WorldFeatureLifecycleSystem`
- `WorldTransitionSystem`
- `WorldPoiPoolManager`

What it should do:

- own serialized references and startup settings
- build the runtime object graph in `Awake()`
- delegate per-frame chunk streaming through `WorldStreamingRuntime`
- expose diagnostics through `IWorldDiagnosticsSource`

What it should not do:

- contain feature-specific spawning logic
- contain biome-specific branches for gates, dens, or future structures
- act as the place where site or encounter behavior lives

### `WorldContext`

File: `Assets/Scripts/MapGeneration/Runtime/World/WorldContext.cs`

`WorldContext` is the active-biome runtime state. It owns:

- `WorldProfile`
- active biome definition and profile
- active biome instance
- mask and noise context
- authoritative generated output through `WorldBuildOutput`

When a biome is rebound, `WorldContext.BindBiome(...)` clears the current build output and runs the active biome's build steps to generate a fresh set of neutral world data.

## Authoritative Build Output

### `WorldBuildOutput`

File: `Assets/Scripts/MapGeneration/Generation/Output/WorldBuildOutput.cs`

`WorldBuildOutput` is the main generated-world artifact. It owns:

- `FeatureStamps TerrainOverrides`
- `SitePlacementIndex SitePlacements`
- `SiteBlockerMap SiteBlockers`
- `RoadAnchorMap RoadAnchors`

This is the key architectural rule for the current world stack:

- build code writes to `WorldBuildOutput`
- runtime systems read from `WorldBuildOutput`
- feature-specific generated data should not bypass this layer

### `SitePlacementIndex`

File: `Assets/Scripts/MapGeneration/Generation/Output/SitePlacementIndex.cs`

`SitePlacementIndex` stores generated sites in two categories:

- chunk-scoped placements
- persistent biome placements

A `SitePlacement` contains:

- `WorldSiteDefinition`
- center tile
- owning chunk coordinate
- deterministic local index
- lifecycle scope

This is how the system unifies chunk sites and persistent sites under one placement model.

## Biome Build Layer

### `BiomeDefinition` and `BasicBiomeDefinition`

Files:
- `Assets/Scripts/MapGeneration/Generation/Biome/BiomeDefinition.cs`
- `Assets/Scripts/MapGeneration/Generation/Biome/BasicBiomeDefinition.cs`

A biome is authored as a definition asset. `BasicBiomeDefinition` runs an ordered list of `BiomeBuildStepDefinition` assets.

### `BiomeBuildStepDefinition`

File: `Assets/Scripts/MapGeneration/Generation/BuildSteps/BiomeBuildStepDefinition.cs`

Each build step receives `WorldContext` and contributes generated data.

Current important build steps:

- `RoadSurfaceBuildStepDefinition`
- `SitePlacementRuleBuildStepDefinition`
- `PersistentSitePlacementBuildStepDefinition`

### Current Content Path

The active biome set now uses authored build steps for:

- road layout and anchors
- chunk-scoped sites such as gates and wolf dens
- persistent biome sites

That means adding content should usually mean editing biome assets and build-step assets, not adding special behavior to runtime managers.

## Terrain Generation

### `ChunkGenerator`

File: `Assets/Scripts/MapGeneration/Streaming/Chunk/ChunkGenerator.cs`

`ChunkGenerator` iterates over each tile in a chunk and asks `TileResolver` to resolve it.

### `TileResolver`

File: `Assets/Scripts/MapGeneration/Generation/Tile/TileResolver.cs`

The tile-resolution order is:

1. stamped terrain overrides from `WorldBuildOutput`
2. optional biome-specific tile override hook
3. land/water mask
4. sampled world signals
5. lake rule
6. ground tile selection
7. decor selection

This is why authored structures can carve roads, platforms, or foundations into the world without bypassing the normal chunk generator.

### `FeatureStamps`

File: `Assets/Scripts/MapGeneration/Generation/Output/FeatureStamps.cs`

`FeatureStamps` is the current terrain-override representation. It is intentionally simple: deterministic terrain resolution still handles most of the world, and explicit structure-driven stamps override tiles only where needed.

## Streaming Layer

### `WorldStreamingRuntime`

File: `Assets/Scripts/MapGeneration/Streaming/WorldStreamingRuntime.cs`

`WorldStreamingRuntime` owns per-frame streaming policy:

- camera resolution
- streaming frame settings
- coordinator invocation
- warning logging
- last processed frame cache
- reset behavior

### `ChunkStreamingSystem`

File: `Assets/Scripts/MapGeneration/Streaming/Chunk/ChunkStreamingSystem.cs`

This holds streaming state:

- loaded chunks
- queued chunks
- generation queue
- streaming anchor chunk

### `ChunkStreamingCoordinator`

File: `Assets/Scripts/MapGeneration/Streaming/Chunk/ChunkStreamingCoordinator.cs`

This converts the current camera view into chunk work:

- calculate load and unload bounds
- update streaming anchor
- enqueue needed chunks
- invoke chunk processing

### `ChunkProcessingPipeline`

File: `Assets/Scripts/MapGeneration/Streaming/Chunk/ChunkProcessingPipeline.cs`

This is where chunk work becomes real runtime state.

On load it:

- generates chunk tiles
- applies tilemaps
- builds navigation
- activates chunk sites
- marks the chunk as loaded

On unload it:

- deactivates chunk sites
- clears tilemaps
- clears nav
- marks the chunk as unloaded

## Navigation Layer

### `TileNavWorld`

File: `Assets/Scripts/MapGeneration/Navigation/TileNavWorld.cs`

`TileNavWorld` builds per-chunk navigation data from tilemaps and then applies generic navigation contributions.

### `WorldNavigationLifecycle`

File: `Assets/Scripts/MapGeneration/Navigation/WorldNavigationLifecycle.cs`

This is the lifecycle boundary for nav. It owns:

- nav initialization
- contribution rebinding
- chunk nav build
- chunk nav clear

### `SiteBlockerMap`

File: `Assets/Scripts/MapGeneration/Generation/Output/SiteBlockerMap.cs`

Right now the main navigation contribution producer is `SiteBlockerMap`, which means structures affect navigation through neutral tile contribution data rather than direct special cases in pathfinding code.

## Site Lifecycle And Activation

### `WorldFeatureLifecycleSystem`

File: `Assets/Scripts/MapGeneration/Runtime/Sites/WorldFeatureLifecycleSystem.cs`

This coordinates:

- `WorldFeatureLifecycle` for chunk-scoped sites
- `PersistentWorldFeatureLifecycle` for persistent biome sites

It is the entry point used by streaming and transitions.

### `WorldFeatureLifecycle`

File: `Assets/Scripts/MapGeneration/Runtime/Sites/WorldFeatureLifecycle.cs`

This activates and deactivates chunk sites based on `SitePlacementIndex`.

### `PersistentWorldFeatureLifecycle`

File: `Assets/Scripts/MapGeneration/Runtime/Sites/PersistentWorldFeatureLifecycle.cs`

This activates persistent biome placements from the same shared placement data.

### `WorldSiteActivationPipeline`

File: `Assets/Scripts/MapGeneration/Runtime/Sites/WorldSiteActivationPipeline.cs`

This is the only place that should turn a `SitePlacement` into a live prefab instance.

It handles:

- deterministic spawn id creation
- consumed-state checks
- `WorldSiteContext` construction
- pooled prefab spawn
- context injection into `IWorldSiteContextConsumer`
- activation callbacks through `IWorldSiteActivationListener`

This is one of the most important seams in the architecture. If a feature bypasses it, it is probably going in the wrong direction.

### `WorldSiteContext`

File: `Assets/Scripts/MapGeneration/Runtime/Sites/WorldSiteContext.cs`

`WorldSiteContext` is what a world site receives at runtime.

It includes:

- placement
- deterministic spawn id
- gate transition service
- run gate transition service
- site state service
- encounter services
- runtime config

It also supports `TryGetEncounterPackage(...)` for sites whose runtime config also defines an encounter package.

## Site Definitions And Runtime Config

### `WorldSiteDefinition`

File: `Assets/Scripts/MapGeneration/Runtime/Sites/WorldSiteDefinition.cs`

A site definition asset contains:

- `siteId`
- prefab
- `skipIfConsumed`
- `spawnSalt`
- runtime config asset

This is the authored bridge between generation and runtime.

### `WorldSiteRuntimeConfig`

File: `Assets/Scripts/MapGeneration/Runtime/Sites/WorldSiteRuntimeConfig.cs`

This is the base config type for site-specific runtime authoring data.

## Persistence Layer

### `ChunkStateStore`

File: `Assets/Scripts/MapGeneration/Streaming/Chunk/ChunkStateStore.cs`

This is runtime persistence for the current run/biome flow. It provides:

- deterministic spawn ids
- claimed/consumed state
- per-site key/value state via `WorldSiteStateRecord`

### `WorldSiteStateHandle`

File: `Assets/Scripts/MapGeneration/Runtime/Sites/WorldSiteStateHandle.cs`

Site code reads and writes its state through `WorldSiteStateHandle`, not through direct knowledge of the chunk store.

This keeps the persistence seam small and reusable.

## Transition Layer

### `WorldTransitionSystem`

File: `Assets/Scripts/MapGeneration/Runtime/World/WorldTransitionSystem.cs`

This owns biome progression inside the procedural world.

It handles:

- current biome index
- gate cooldown
- biome instance creation and seeding
- `WorldContext.BindBiome(...)`
- chunk-state reset
- nav contribution rebinding
- lifecycle rebuild
- streaming reset and anchor reset

### `SceneTransitionService`

File: `Assets/Scripts/MapGeneration/Runtime/Transitions/SceneTransitionService.cs`

This owns scene-to-scene transitions for run gates.

### Intentional Bootstrap Exceptions

The current system still intentionally uses bootstrap fallbacks in a few places:

- `SceneTransitionService.Instance`
- `FindFirstObjectByType<SceneTransitionService>()`
- `TileNavWorld.Instance`

These are considered acceptable bootstrap exceptions in the current architecture. They are not the general pattern for new runtime feature code.

## Diagnostics Layer

### Grouped Diagnostics Snapshots

Files:
- `Assets/Scripts/MapGeneration/Diagnostics/WorldGenDiagnosticsSnapshot.cs`
- `Assets/Scripts/MapGeneration/Diagnostics/StreamingDiagnosticsSnapshot.cs`
- `Assets/Scripts/MapGeneration/Diagnostics/LifecycleDiagnosticsSnapshot.cs`
- `Assets/Scripts/MapGeneration/Diagnostics/NavigationDiagnosticsSnapshot.cs`
- `Assets/Scripts/MapGeneration/Diagnostics/TransitionDiagnosticsSnapshot.cs`
- `Assets/Scripts/MapGeneration/Diagnostics/BuildOutputDiagnosticsSnapshot.cs`

Diagnostics are now organized by subsystem instead of one flat field bag.

### `WorldGenDebugHUD`

File: `Assets/Scripts/MapGeneration/Diagnostics/WorldGenDebugHUD.cs`

The HUD reads through `IWorldDiagnosticsSource`, which keeps diagnostics consumers bound to a narrow read-only contract instead of directly to `WorldGenRunner`.

The default HUD is intentionally compact, with advanced stats hidden behind a toggle.

## Wolf Den As The Reference Structure

The wolf den is the reference implementation for a full site-plus-encounter structure.

### Site Runtime

File: `Assets/Scripts/MapGeneration/Sites/WolfDen/WolfDen.cs`

`WolfDen` owns:

- den HP
- cleared state
- collapse visuals
- colliders
- consumed persistence
- site events (`Initialized`, `Cleared`, `DamagedAlert`)

It is both:

- a world site bridge
- an encounter site

### Encounter Runtime

File: `Assets/Scripts/MapGeneration/Sites/WolfDen/WolfDenSpawner.cs`

`WolfDenSpawner` owns:

- leader spawn
- occupant tracking
- respawn timing
- unload behavior
- binding to the den's encounter site events

### Wolf-Specific Command Layer

File: `Assets/Scripts/MapGeneration/Sites/WolfDen/WolfEncounterCommandController.cs`

This owns wolf-specific encounter responses such as:

- alert escalation
- investigation targeting
- max-alert howl behavior

### Shared Encounter Building Blocks

Files:
- `Assets/Scripts/MapGeneration/Runtime/Sites/WorldEncounterPackage.cs`
- `Assets/Scripts/MapGeneration/Runtime/Sites/WorldEncounterPackageBinding.cs`
- `Assets/Scripts/MapGeneration/Runtime/Sites/WorldEncounterOccupantCollection.cs`
- `Assets/Scripts/MapGeneration/Runtime/Sites/WorldEncounterOccupantPolicy.cs`
- `Assets/Scripts/MapGeneration/Runtime/Sites/WorldEncounterAlertRuntime.cs`
- `Assets/Scripts/MapGeneration/Runtime/Sites/IWorldEncounterPackageConfig.cs`
- `Assets/Scripts/MapGeneration/Runtime/Sites/IWorldEncounterSite.cs`

This split is the preferred model for any future structure that mixes world-site state with occupant-based encounter behavior.

## How To Add A New Structure

Use the smallest path that matches the feature.

### Case 1: Passive Chunk-Scoped Structure

Examples:
- shrine
- campsite
- loot structure
- decorative site with simple interaction

Implementation path:

1. Create a prefab.
2. Add a site runtime component that implements `IWorldSiteBridge`.
3. If needed, add `IPoolable` so the prefab resets itself on spawn/despawn.
4. If needed, create a config asset extending `WorldSiteRuntimeConfig`.
5. Create a `WorldSiteDefinition` asset and assign prefab, site id, spawn salt, and config.
6. Create a `SitePlacementRuleDefinition` asset/class to place the structure.
7. In `BuildSites(WorldContext ctx)`:
   - choose deterministic center tiles
   - stamp terrain if needed
   - add blockers if needed
   - call `ctx.BuildOutput.RegisterSite(...)`
8. Add that rule to the biome's site-placement build-step asset.

What not to do:

- do not instantiate prefabs in build code
- do not call the pool directly from generation code
- do not patch nav directly at runtime for the structure footprint
- do not reach into `WorldGenRunner`

### Case 2: Persistent Biome Structure

Examples:
- one biome shrine
- persistent gate
- biome-specific hub structure

Implementation path:

1. Reuse the same prefab, runtime, config, and `WorldSiteDefinition` flow.
2. Add it through `PersistentSitePlacementBuildStepDefinition` instead of a chunk-scoped site rule.
3. Register it with `SitePlacementLifecycleScope.PersistentBiome`.
4. Let `PersistentWorldFeatureLifecycle` own activation.

What not to do:

- do not manually instantiate persistent structures during transitions
- do not construct ad hoc `SitePlacement` instances in lifecycle code

### Case 3: Encounter Structure

Examples:
- monster nest
- bandit camp
- ritual site with spawned enemies

Preferred split:

1. Site runtime component:
   - own site HP/state/visuals
   - persist cleared state
   - expose site events
   - implement `IWorldEncounterSite` if the encounter layer should listen to it
2. Encounter config asset:
   - extend `WorldSiteRuntimeConfig`
   - implement `IWorldEncounterPackageConfig` if it should use the shared encounter package path
3. Encounter runtime component:
   - implement `IWorldSiteContextConsumer`
   - bind to `WorldSiteContext.TryGetEncounterPackage(...)`
   - use `WorldEncounterServices`
   - spawn/despawn occupants
4. Add a feature-specific command/controller layer only if the encounter behavior is meaningfully different from plain occupant tracking.

Shared pieces to reuse first:

- `WorldEncounterPackageBinding`
- `WorldEncounterOccupantCollection<TEnemy>`
- `WorldEncounterOccupantPolicy`
- `WorldEncounterAlertRuntime`

Do not generalize further unless there is a second real consumer that needs the same abstraction.

## How To Add A New World System

If you need a new supporting subsystem, follow this order:

1. Define the narrowest responsibility first.
2. Compose it in `WorldGenRunner`.
3. Pass it through the correct seam:
   - `WorldSiteContext` for site runtime dependencies
   - `WorldEncounterServices` for encounter dependencies
   - `WorldSceneServices` for world-space and navigation queries
   - `WorldTransitionSystem` for biome progression
   - `WorldNavigationLifecycle` for nav lifecycle ownership
4. Add diagnostics only if the system is important enough to inspect at runtime.
5. Keep the subsystem read/write ownership clear.

Do not make the new system depend on `WorldGenRunner` after composition.

## Practical Rules For Safe Extension

- Author world content through assets first, code second.
- Write generated content into `WorldBuildOutput`.
- Let lifecycle spawn world sites.
- Keep site state, encounter state, and command behavior separate.
- Use deterministic sampling and named salts.
- Persist site state through `WorldSiteStateHandle` instead of bespoke globals.
- Affect movement through navigation contributions, not custom runtime pathing hacks.
- Reuse the existing encounter and lifecycle seams before creating new abstraction layers.

## Quick Checklists

### New Passive Structure Checklist

- prefab created
- runtime component implements `IWorldSiteBridge`
- optional `IPoolable` reset added
- optional runtime config asset created
- `WorldSiteDefinition` asset created
- placement rule created
- build output stamps/blockers added if needed
- site registered through `ctx.BuildOutput.RegisterSite(...)`
- biome build-step asset updated
- verified in world startup, chunk unload/reload, and interaction flow

### New Persistent Structure Checklist

- all passive-structure steps completed
- authored through `PersistentSitePlacementBuildStepDefinition`
- lifecycle scope set to `PersistentBiome`
- verified on biome start and biome switch
- verified no duplicate persistent instances appear

### New Encounter Structure Checklist

- site runtime owns site state and visuals
- site exposes encounter-facing events if needed
- config implements `IWorldEncounterPackageConfig` if package flow is desired
- encounter runtime uses `WorldSiteContext`
- occupant tracking uses shared collection helpers where possible
- structure verified for spawn, clear, unload/reload, and transition behavior

## Final Guidance

If adding a new structure feels like it requires editing multiple unrelated managers, that is a warning sign.

The intended extension path is:

- authored assets
- one placement rule or persistent build step
- one site definition
- one prefab runtime
- optional encounter runtime

That is the current architectural standard for world content in this project.

