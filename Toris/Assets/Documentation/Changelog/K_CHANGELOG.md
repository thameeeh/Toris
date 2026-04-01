## [Current/Recent] - Debug HUD Readability Trim
This update keeps the new grouped diagnostics model in code while simplifying the default F3 surface so it stays readable at larger font sizes and remains focused on quick world and chunk-debug visibility.

### 1. Reduced The Default HUD To High-Signal World Info
* Kept visuals toggles, biome, tile position, and the core signal readouts visible by default.
* This keeps the HUD usable as a quick chunk-visualization and world-sampling tool instead of trying to display every subsystem stat at once.

### 2. Hid Verbose Diagnostics Behind An Explicit Toggle
* Kept grouped streaming, lifecycle, build-output, navigation, and transition sections in code behind showAdvancedStats instead of deleting them.
* Added an in-HUD toggle for advanced stats so the deeper diagnostics remain available when needed without crowding the default view.

### 3. Preserved Existing Debug Functionality While Improving Readability
* Verified the compact HUD layout at larger font sizes and confirmed chunk-border and streaming-rect toggles still work.
* This keeps the diagnostics cleanup practical without throwing away the newer snapshot boundaries.

---## [Current/Recent] - Grouped World Diagnostics Snapshots
This update turns the world diagnostics model into grouped subsystem read models so build output, streaming, lifecycle, navigation, and transition state each expose intentional snapshot data instead of one flat aggregate field bag.

### 1. Added Subsystem Diagnostics Snapshots
* Added StreamingDiagnosticsSnapshot, LifecycleDiagnosticsSnapshot, NavigationDiagnosticsSnapshot, and TransitionDiagnosticsSnapshot.
* Each major subsystem now has a clearer read-only diagnostics shape instead of contributing raw fields directly to one large flat snapshot.

### 2. Reduced The Aggregate Snapshot To A Wrapper
* Updated WorldGenDiagnosticsSnapshot to become an aggregate wrapper over subsystem snapshots rather than a flat all-fields payload.
* Updated WorldStreamingRuntime, WorldFeatureLifecycleSystem, WorldNavigationLifecycle, and WorldTransitionSystem to create their own diagnostics snapshots intentionally.

### 3. Preserved Existing Debug Visibility And Runtime Behavior
* Updated WorldGenDebugHUD to read the grouped diagnostics model and verified startup, chunk streaming, lifecycle counts, build-output counters, biome transitions, run gates, and den behavior after the read-model refactor.
* This keeps the diagnostics cleanup structural rather than gameplay-facing.

---## [Current/Recent] - Streaming Runtime Ownership Extraction
This update moves the frame-by-frame streaming manager responsibilities out of the runner and into a dedicated runtime object, so streaming camera resolution, frame settings, last-frame caching, reset, and warning logging all live behind one explicit boundary.

### 1. Added A Dedicated Streaming Runtime
* Added WorldStreamingRuntime to own camera resolution, frame settings, streaming coordinator invocation, warning logging, last processed frame caching, and runtime reset behavior.
* This reduces WorldGenRunner.Update() to a thin delegation point instead of a streaming manager.

### 2. Moved Streaming Diagnostics Reads Onto The Runtime Boundary
* Updated WorldGenRunner.CreateDiagnosticsSnapshot() to pull loaded chunks, queue counts, streaming anchor state, and last-frame bounds from the streaming runtime.
* This keeps the HUD on the same explicit runtime boundary that now owns per-frame streaming behavior.

### 3. Preserved Existing Streaming And Gameplay Behavior
* Verified startup, chunk streaming, HUD streaming diagnostics, wolf den behavior, biome transitions, and run gates after the extraction.
* This keeps the change focused on ownership and structure rather than gameplay changes.

---
## [Current/Recent] - Persistent Site Authoring Through Shared Placements
This update moves persistent biome sites for the active content set onto the same authored placement pipeline as chunk sites, so persistent site activation now consumes shared generated placement data instead of inventing placements later at runtime.

### 1. Added A Persistent Site Build Step
* Added PersistentSitePlacementBuildStepDefinition and wired Plains and Forest to use it during biome build.
* Persistent biome site authoring now happens through build-step assets instead of only through BiomeProfile runtime reads.

### 2. Moved Persistent Lifecycle Activation Onto Shared Placement Data
* Updated PersistentWorldFeatureLifecycle and WorldFeatureLifecycleSystem so persistent activation rebuilds from SitePlacementIndex.PersistentBiomePlacements.
* This removes the ad hoc placement-construction path and makes persistent sites obey the same placement-model rule as chunk sites.

### 3. Preserved Persistent Site Behavior While Making It Observable
* Cleared the old active-biome profile arrays for Plains and Forest after migrating that authored data into build-step assets.
* Verified startup, biome switching, persistent site appearance, and the Persistent placements HUD counter after the migration.

---
## [Current/Recent] - Build Output Ownership And Diagnostics
This update introduces a named generated-world artifact for terrain overrides, authored site placements, navigation contributions, and road anchors, so world build data is visible and centralized before persistent feature migration continues.

### 1. Introduced A Named World Build Output Artifact
* Added WorldBuildOutput, BuildOutputDiagnosticsSnapshot, and SitePlacementLifecycleScope.
* WorldContext now owns a single BuildOutput object that groups terrain overrides, site placements, navigation contributions, and road anchors under one explicit build-data surface.

### 2. Moved Current Build Writers Onto The Build Output Path
* Updated road generation, site stamping, gate placement, wolf den placement, and tile resolution to write and read through WorldBuildOutput instead of treating the old world-context buckets as the authored path.
* This keeps the current stamp-based terrain model intact while making the generated-world artifact explicit and centralized.

### 3. Exposed Build Output Through Runtime Diagnostics
* Extended WorldGenDiagnosticsSnapshot, WorldGenRunner, and WorldGenDebugHUD to report build overrides, total placements, chunk placements, persistent placements, navigation contributions, and road anchors.
* Verified startup, biome transitions, gate and den placement, nav-affecting den blockers, and the new HUD counters after the ownership shift.

---
## [Current/Recent] - Phase 8 Slice: Streaming Frame Result Alias Cleanup
This update continues the Phase 8 deletion pass by removing one more redundant alias from the streaming diagnostics path, so the frame-result type exposes only the state that still has distinct meaning.

### 1. Removed A Redundant Frame-Result Alias
* Deleted the HasView field from ChunkStreamingFrameResult.
* That field was only mirroring ProcessedFrame, so it carried no separate state and only preserved dead compatibility surface.

### 2. Tightened The Runner HUD Snapshot Path To The Real Field
* Updated WorldGenRunner to use ProcessedFrame directly when building streaming bounds for diagnostics.
* This keeps the HUD path aligned with the actual frame-result contract instead of relying on an unnecessary alias.

### 3. Verified No-Behavior Change After The Cleanup
* Verified startup, chunk loading and unloading, wolf den behavior, den cleared-state persistence, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps the Phase 8 deletion work focused on proven redundant surface instead of speculative refactoring.

---
## [Current/Recent] - Phase 8 Slice: Diagnostics Snapshot Surface Cleanup
This update continues the Phase 8 deletion pass by trimming one more dead field from the diagnostics snapshot so the HUD-facing read model only carries values that still have live consumers.

### 1. Removed A Dead Snapshot Field
* Deleted the unused StreamCamera field from WorldGenDiagnosticsSnapshot.
* This keeps the debug snapshot aligned with the actual HUD and runtime readers instead of preserving stale payload.

### 2. Trimmed The Matching Runner Snapshot Write Path
* Updated WorldGenRunner so it no longer passes an unused camera reference into the diagnostics snapshot constructor.
* This keeps the diagnostics composition path honest without changing how streaming camera selection works at runtime.

### 3. Verified No-Behavior Change After The Cleanup
* Verified startup, chunk loading and unloading, wolf den behavior, den cleared-state persistence, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps the Phase 8 deletion work focused on proven dead diagnostics surface instead of speculative refactors.

---
## [Current/Recent] - Phase 8 Slice: Streaming Diagnostics Cache Cleanup
This update continues the Phase 8 deletion pass by moving last-frame streaming diagnostics ownership into the runner, so the streaming coordinator no longer keeps extra cached frame state just for the HUD path.

### 1. Moved Last-Frame Streaming Diagnostics Into WorldGenRunner
* WorldGenRunner now caches the most recent processed streaming frame result directly after coordinator processing.
* This keeps the diagnostics snapshot owned by the same top-level runtime that already builds the HUD snapshot.

### 2. Deleted Extra Cached Coordinator Surface
* Removed the coordinator-owned last-frame cache and its TryGetLastFrameResult(...) helper.
* This trims one more dead read-back surface from the streaming layer without changing the runtime processing flow.

### 3. Verified No-Behavior Change After The Cleanup
* Verified startup, chunk loading and unloading, wolf den behavior, den cleared-state persistence, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps the Phase 8 deletion work grounded in verified parity checks instead of preserving unused coordinator state.

---
## [Current/Recent] - Phase 8 Slice: Chunk-State And Streaming Plumbing Cleanup
This update continues the Phase 8 deletion pass by removing an empty runtime-state wrapper and deleting the last dead chunk callback plumbing from the streaming path, so chunk persistence and streaming flow reflect the real runtime ownership more directly.

### 1. Deleted The Empty WorldRuntimeState Wrapper
* Deleted WorldRuntimeState and moved ChunkStateStore ownership directly into WorldGenRunner.
* Updated WorldTransitionSystem to clear ChunkStateStore directly instead of going through an empty wrapper object.

### 2. Deleted Dead Chunk Callback Plumbing
* Removed chunk loaded/unloading callback parameters from ChunkStreamingCoordinator and ChunkProcessingPipeline.
* This also removed the last dead ChunkProcessingPipeline dependency that only existed to support those callbacks.

### 3. Verified No-Behavior Change After The Deletion
* Verified startup, chunk streaming, wolf den behavior, den cleared-state persistence, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps the Phase 8 close-out grounded in deleting proven dead plumbing instead of preserving stale extension points.

---
## [Current/Recent] - Phase 8 Slice: Runner Public Surface Cleanup
This update continues the Phase 8 deletion pass by trimming dead public runner surface and empty callback wrappers so WorldGenRunner exposes only the runtime entry points that still have real consumers.

### 1. Removed Dead Public Runner Surface
* Deleted unused public getters for preload and hysteresis settings, streaming camera, world profile, loaded chunk state, and chunk-loaded queries.
* This keeps the runner from advertising read access patterns that the current diagnostics and gameplay code no longer use.

### 2. Deleted Empty Chunk Callback Wrappers
* Removed the unused chunk loaded and chunk unloading callback actions along with their empty wrapper methods.
* This clears out one more leftover extension path after the streaming pipeline stopped using runner-owned callback plumbing.

### 3. Verified No-Behavior Change After Surface Cleanup
* Verified startup, chunk streaming, wolf den behavior, den cleared-state persistence, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps the runner cleanup grounded in verified parity instead of preserving dead public scaffolding.
---
## [Current/Recent] - Phase 8 Slice: Site Activation Persistence Boundary Cleanup
This update continues the Phase 8 deletion pass by narrowing site activation down to the exact persistence boundary it actually needs, so the activation path no longer receives a broader runtime-state object just to reach chunk-site state.

### 1. Narrowed Site Activation To Chunk-State Persistence
* Updated WorldSiteActivationPipeline to depend on ChunkStateStore instead of the broader WorldRuntimeState.
* This makes the activation boundary reflect its real responsibility: deterministic site spawn ids and per-site chunk persistence.

### 2. Narrowed The Site-State Adapter To Match
* Updated WorldSiteStateServiceAdapter to wrap ChunkStateStore directly.
* This keeps the adapter aligned with the smaller persistence surface instead of preserving a broader dependency than it needs.

### 3. Verified No-Behavior Change After The Boundary Trim
* Verified startup, chunk streaming, wolf den behavior, den cleared-state persistence, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps the Phase 8 cleanup work focused on honest dependency boundaries without changing the authored runtime behavior.

---

## [Current/Recent] - Phase 8 Slice: Service Boundary Surface Cleanup
This update continues the Phase 8 deletion pass by removing one more unnecessary peek-through on the world-scene service boundary, so callers rely on the service behavior they actually need instead of reaching through it for internal state.

### 1. Removed Public Grid Peek-Through From WorldSceneServices
* Kept Grid private inside WorldSceneServices instead of exposing it as a public property.
* This narrows the service back toward its intended job: providing scene and navigation behavior, not leaking implementation details.

### 2. Tightened Site Activation To Use The Service Boundary Directly
* Updated WorldSiteActivationPipeline so it no longer null-checks WorldSceneServices.Grid before requesting a world-space cell center.
* The activation path now depends on GetCellCenterWorld(...) directly instead of peeking through the service boundary.

### 3. Verified No-Behavior Change After Narrowing The Surface
* Verified startup, chunk streaming, wolf den behavior, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps the cleanup work focused on deleting unnecessary surfaces while preserving the already-verified runtime behavior.

---
## [Current/Recent] - Phase 8 Slice: Runner Composition State Cleanup
This update continues the Phase 8 deletion pass by shrinking WorldGenRunner down to the runtime state it actually needs after startup, so one-shot composition helpers no longer live as long-lived runner fields.

### 1. Localized Startup-Only Composition Helpers
* Moved ChunkGenerator, TilemapApplier, WorldSceneServices, WorldEncounterServices, and WorldSiteActivationPipeline to local variables inside Awake().
* This keeps the runner focused on long-lived runtime systems instead of retaining startup-only composition objects as member state.

### 2. Preserved The Existing Runtime Ownership Boundaries
* The same systems are still composed in the same order and passed into the same lifecycle, streaming, transition, and encounter boundaries.
* This is a pure ownership cleanup, not a change to world-generation or gameplay behavior.

### 3. Verified No-Behavior Change After Localization
* Verified startup, chunk streaming, wolf den behavior, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps the Phase 8 deletion work grounded in parity checks rather than speculative cleanup.

---
## [Current/Recent] - Phase 8 Slice: Runner Scaffold Cleanup
This update continues the Phase 8 deletion pass by removing a pair of no-behavior leftovers from WorldGenRunner so the composition root keeps one clear startup path instead of carrying duplicate bootstrap code and dead migration comments.

### 1. Removed Duplicate POI Pool Bootstrap
* Deleted the inline poiPool get-or-add block from Awake() and left EnsurePoiPool() as the single startup owner of POI pool creation.
* This keeps pool bootstrap logic in one place instead of maintaining two equivalent setup paths in the runner.

### 2. Deleted Dead Streaming Migration Scaffold
* Removed the commented nchorShiftThreshold field that was left behind from the earlier streaming extraction work.
* This trims one more stale migration artifact from the runner without affecting runtime behavior.

### 3. Verified No-Behavior Change After Cleanup
* Verified startup, chunk streaming, wolf den behavior, biome gates, run gates, and the Phase 8 diagnostics HUD after the cleanup.
* This keeps Phase 8 moving through small deletions with parity checks instead of broad cleanup guesses.

---
## [Current/Recent] - Phase 8 Slice: Transition And Navigation Scaffold Cleanup
This update continues the Phase 8 deletion pass by removing dead transition and navigation compatibility layers, trimming a few no-behavior helper surfaces, and keeping the current diagnostics as the verification safety net.

### 1. Deleted Dead Transition Compatibility Paths
* Removed the stale scene-transition service path from `WorldSiteContext` and `WorldSiteActivationPipeline`.
* Deleted `ISceneTransitionService` and removed that obsolete interface from `SceneTransitionService`.
* This leaves run gates and biome gates on the transition contracts that are still actually consumed: `IRunGateTransitionService` and `IGateTransitionService`.

### 2. Deleted Dead Navigation Compatibility Surfaces
* Deleted `ITileNavigationBlockerSource` and moved `SiteBlockerMap` fully onto `ITileNavigationContributionSource`.
* Removed the old blocker-specific helper surface from `SiteBlockerMap` and the unused public `TileNavWorld` exposure from `WorldSceneServices`.
* This keeps navigation input aligned with the generic contribution model instead of preserving blocker-era shims.

### 3. Trimmed Redundant Run-Gate Bootstrap
* Removed the eager `Awake()`-time service resolution from `RunGateInteractable` while keeping the verified initialize-time and interact-time fallback chain intact.
* Verified biome gates, run gates, return gates, nav-dependent den behavior, and the Phase 8 HUD diagnostics after the cleanup pass.

---
## [Current/Recent] - Phase 8 Slice: Transition Dead-Path Cleanup
This update starts the deletion side of Phase 8 by removing transition scaffolding that no longer participates in any real site-runtime behavior.

### 1. Removed The Stale Scene Transition Site-Context Path
* Removed the unused SceneTransitionService field from WorldSiteContext and stopped threading it through WorldSiteActivationPipeline.
* This deletes a migration path that no longer had any real consumers after run gates moved onto IRunGateTransitionService.

### 2. Deleted The Dead ISceneTransitionService Compatibility Layer
* Deleted ISceneTransitionService and removed that unused interface from SceneTransitionService.
* This keeps the transition surface aligned with the contracts that are still actually consumed in runtime code.

### 3. Verified Transition Behavior After Deletion
* Verified world startup, biome gates, run gates, return gates, and the Phase 8 transition diagnostics after the dead-path cleanup.
* This confirms the deletion was behavioral no-op cleanup rather than a hidden dependency break.

---
## [Current/Recent] - Phase 8 Slice: Nav And Transition Diagnostics
This update expands the debug snapshot and HUD so the newer navigation and transition boundaries are directly observable before the remaining legacy cleanup work begins.

### 1. Expanded Diagnostics To Cover Nav Lifecycle State
* Added nav chunk count and nav-contribution binding state to WorldGenDiagnosticsSnapshot and WorldGenDebugHUD.
* This makes the newer WorldNavigationLifecycle and contribution-bound navigation path observable instead of relying on code inspection alone.

### 2. Expanded Diagnostics To Cover Transition Runtime State
* Added current biome index, gate cooldown remaining, and scene-transition loading state to the diagnostics snapshot and HUD.
* This gives transition boundaries a clearer read-only debug surface before cleanup work starts removing migration scaffolding.

### 3. Verified The New Diagnostics Against Runtime Behavior
* Verified HUD output during normal play, chunk movement, biome transitions, and run-gate transitions.
* This starts Phase 8 with a visibility pass instead of deleting paths blindly.

---
## [Current/Recent] - Phase 7 Slice: Explicit Static-Scene Run Gate Bootstrap
This update finishes the current transition cleanup scope by replacing the last broad runtime discovery path for static-scene run gates with an explicit fallback chain.

### 1. Removed Broad Runtime Discovery From Static-Scene Run Gates
* Updated RunGateInteractable so it no longer scans arbitrary scene behaviours to find any IRunGateTransitionService implementation.
* This removes hidden ownership and keeps static-scene fallback bounded and understandable.

### 2. Kept Static-Scene Bootstrap Explicit And Local
* Static-scene run gates now resolve transition capability through a clear order: serialized override, SceneTransitionService.Instance, then a local SceneTransitionService.
* This preserves the existing authoring path while making the bootstrap rule much clearer than the old broad discovery approach.

### 3. Verified The Current Transition Boundary Scope
* Verified paired-scene run gates, return gates, and normal working-case fallback behavior after the bootstrap cleanup.
* This closes the current Phase 7 transition cleanup scope and points the roadmap at Phase 8 diagnostics and legacy cleanup.

---
## [Current/Recent] - Phase 7 Slice: Run Gate Transition Service Boundary
This update moves run-gate scene-pair routing out of RunGateInteractable and into an explicit transition service so run gates now request transitions the same way biome gates already do.

### 1. Added An Explicit Run Gate Transition Service Contract
* Added IRunGateTransitionService and made SceneTransitionService implement it.
* This moves paired-scene routing logic out of the interactable and into a named transition-service boundary.

### 2. Moved RunGateInteractable Off Caller-Owned Transition Orchestration
* Updated RunGateInteractable to request UseRunGate(sceneA, sceneB) instead of reading the active scene and choosing scene loads itself.
* This keeps the interactable focused on interaction while the service owns transition selection behavior.

### 3. Threaded The Run Gate Service Through World Site Composition
* Updated WorldSiteContext, WorldSiteActivationPipeline, and WorldGenRunner to provide the explicit run-gate transition service to site-spawned run gates.
* Verified paired-scene run gates, return gates, biome gates, and existing inspector override wiring after the change.

---
## [Current/Recent] - Phase 6 Slice: Navigation Lifecycle Ownership
This update moves nav initialization, chunk rebuild, chunk clear, and contribution rebinding behind an explicit navigation lifecycle boundary so runtime query services no longer also own nav lifecycle control.

### 1. Added An Explicit Navigation Lifecycle Boundary
* Added WorldNavigationLifecycle to own nav initialization, chunk build/clear, and contribution rebinding.
* This gives navigation a named lifecycle owner instead of spreading that responsibility across chunk processing, transition code, and a query-facing scene service.

### 2. Returned WorldSceneServices To Query-Focused Responsibility
* Updated ChunkProcessingPipeline, WorldTransitionSystem, and WorldGenRunner to use the new nav lifecycle for nav control work.
* WorldSceneServices now stays focused on runtime cell/world queries and walkability checks instead of also acting as a nav lifecycle controller.

### 3. Preserved Verified Nav-Dependent Runtime Behavior
* Verified startup, chunk loading, den spawn placement, den investigation/pathing, unload/reload flow, and biome transition behavior after the nav ownership split.
* This makes the current Phase 6 nav boundary much clearer without introducing new gameplay-facing behavior.

---
## [Current/Recent] - Phase 6 Slice: Navigation Contribution Model
This update widens the new navigation boundary from a blocker-only contract into a generic contribution model so future nav-affecting systems can plug into the same seam without reopening TileNavWorld again.

### 1. Added A Generic Navigation Contribution Contract
* Added ITileNavigationContributionSource and TileNavigationContribution.
* This creates a neutral nav input model that can represent today's blocker-only behavior while leaving room for future traversal modifiers if they ever become real requirements.

### 2. Moved TileNavWorld Onto The Contribution Path
* Updated TileNavWorld, WorldSceneServices, WorldContext, WorldGenRunner, and WorldTransitionSystem to consume navigation contributions rather than a blocker-only API.
* ITileNavigationBlockerSource remains as the current specialized producer contract so the codebase can keep its blocker semantics clear without constraining the nav boundary itself.

### 3. Preserved Verified Nav-Dependent Encounter Behavior
* Verified world startup, chunk loading, den spawn placement, den investigation/pathing behavior, unload/reload flow, and biome transition behavior after the contribution-model change.
* This gives Phase 6 a stable generic nav seam before deciding whether nav rebuild trigger ownership also needs further extraction.

---
## [Current/Recent] - Phase 6 Slice: Generic Navigation Blocker Source
This update starts the navigation hardening phase by moving the nav-facing blocker boundary onto a neutral input contract so navigation no longer names sites directly at its public seam.

### 1. Added A Generic Navigation Blocker Source Contract
* Added ITileNavigationBlockerSource as the public blocker-input surface for tile navigation.
* This keeps navigation feature-agnostic at its boundary and makes future non-site blocker producers possible without renaming the nav API again.

### 2. Moved TileNavWorld And Scene Services Off Site-Named Blocker Wiring
* Updated TileNavWorld, WorldSceneServices, WorldGenRunner, WorldTransitionSystem, and WorldContext to pass blocker input through the neutral navigation contract.
* SiteBlockerMap remains the current producer, but it now plugs into nav through a generic seam instead of a site-specific one.

### 3. Preserved Verified Nav-Dependent Encounter Behavior
* Verified world startup, chunk loading, den spawn placement, den investigation/pathing behavior, unload/reload flow, and biome transition behavior after the nav boundary change.
* This gives Phase 6 a stable first step before deciding whether navigation needs a formal modifier layer alongside blockers.

---
## [Current/Recent] - Phase 5 Slice: Wolf Encounter Command Controller Boundary
This update moves the remaining wolf-specific command layer out of `WolfDenSpawner` and into a dedicated controller so the den spawner now reads primarily as encounter package lifecycle and occupant ownership orchestration.

### 1. Added A Dedicated Wolf Encounter Command Controller
* Added `WolfEncounterCommandController` to own wolf-specific command behavior such as investigation targeting and howl execution.
* This separates reusable encounter package layers from the species-specific response layer without inventing a fake generic abstraction.

### 2. Reduced WolfDenSpawner To Package And Occupant Ownership
* Updated `WolfDenSpawner` to delegate wolf command behavior to the new controller while keeping package lifecycle, respawn, and occupant tracking in place.
* The spawner now reads much more clearly as encounter orchestration rather than a mixed state-and-command object.

### 3. Verified The Remaining Den Encounter Command Layer
* Verified den investigation response, max-alert howl behavior, alert recovery, leader respawn, and cleared-state persistence after the controller extraction.
* This closes the current Phase 5 site-versus-encounter split scope without changing the authored den experience.

---
## [Current/Recent] - Phase 5 Slice: Encounter Alert Runtime Boundary
This update extracts reusable encounter alert-state bookkeeping out of `WolfDenSpawner` so alert accumulation, decay, and max-alert gating no longer live as den-local runtime state.

### 1. Added A Reusable Encounter Alert Runtime
* Added `WorldEncounterAlertRuntime` to own alert level, decay timing, and max-alert trigger gating.
* This creates a reusable package-level state machine for encounter alert pressure without assuming wolf-specific responses.

### 2. Moved WolfDenSpawner Off Direct Alert Bookkeeping
* Updated `WolfDenSpawner` to use the alert runtime for raising alert, ticking decay, and consuming max-alert transitions.
* The spawner now keeps the wolf-specific reactions while the generic alert-state flow lives behind a reusable helper.

### 3. Preserved Verified Den Alert And Respawn Behavior
* Verified den investigation response, max-alert howl flow, alert recovery, leader respawn, and cleared-state behavior after the extraction.
* This keeps the refactor moving toward a reusable encounter controller layer without changing the authored den experience.

---
## [Current/Recent] - Phase 5 Slice: Encounter Package Activation And Persistence Hooks
This update moves encounter package activation and persistence ownership onto a reusable binding and namespaced package state so den encounter lifecycles no longer rely on spawner-local event wiring alone.

### 1. Added Namespaced Encounter Package State
* Added `WorldEncounterPackageState` to store encounter-package state under package-scoped keys inside site state.
* This creates a reusable persistence surface for encounter packages instead of forcing each encounter runtime to improvise its own state keys.

### 2. Added A Reusable Encounter Package Binding
* Added `WorldEncounterPackageBinding` to own site-event subscription, package active-state updates, and activation callback flow.
* This moves package activation ownership behind a reusable helper instead of leaving it embedded in `WolfDenSpawner`.

### 3. Moved WolfDenSpawner Onto The Package Binding Path
* Updated `WorldEncounterPackage` and `WorldSiteContext` to carry package state and updated `WolfDenSpawner` to bind through the reusable package binding.
* Verified den spawn, respawn, clear persistence, and chase-on-unload behavior after the extraction.

---
## [Current/Recent] - Phase 5 Slice: Encounter Package Boundary
This update introduces an explicit encounter-package API so encounter consumers can request package data through `WorldSiteContext` instead of inferring everything from a raw runtime config.

### 1. Added An Explicit Encounter Package Contract
* Added `IWorldEncounterPackageConfig` and `WorldEncounterPackage`.
* This gives site runtime config a named way to expose encounter-package identity, services, and occupant policy.

### 2. Moved WolfDen Encounter Initialization Onto The Package Path
* Updated `WorldSiteContext` to expose `TryGetEncounterPackage(...)` and updated `WolfDenSpawner` to initialize through that path first.
* The den encounter now reads as consuming an explicit package surface instead of reaching straight into a wolf-specific runtime config assumption.

### 3. Preserved Verified Den Behavior While Formalizing The API
* Updated `WolfDenEncounterConfig` to implement the encounter-package contract while preserving the already verified occupant policy and alert configuration behavior.
* This widens the Phase 5 boundary without changing the authored den experience.

---

## Deferred Investigation Queue - Wolf Bugs
These are intentionally deferred until the current map refactor sequence is complete unless one of them starts blocking Phase 5 or later verification.

### 1. Intermittent Dead Wolf Fails To Despawn
* Symptom: a wolf can remain visually dead on screen while the game still treats it as alive, which then causes wolf den behavior to stop working correctly.
* Current clue: the wolf appears to hold on the last frame of its death animation instead of finishing the despawn path.
* Repro status: intermittent, not 100 percent reproducible.

### 2. Dying Leader Can Resurrect Into A Howl Loop At Max Aggro
* Symptom: while a wolf leader is playing its dying animation, pushing the den to max aggro can cause the dying leader to resurrect and get stuck in a howl loop.
* Associated messages:
```text
'Animator' AnimationEvent 'Howl' on animation 'Wolf_Howl_SW' has no receiver! Are you missing a component?
'Animator' AnimationEvent has no function name specified!
```

---
## [Current/Recent] - Phase 5 Slice: Encounter Occupant Policy Boundary
This update moves reusable encounter lifecycle tuning out of the wolf-specific den config and into a dedicated occupant policy object so respawn, spawn area, home radius, and chase-on-unload rules read like encounter-package behavior instead of den-only behavior.

### 1. Added A Reusable Encounter Occupant Policy Object
* Added `WorldEncounterOccupantPolicy` to represent respawn timing, spawn radius, home radius, and chase-on-unload rules.
* This creates a clearer config boundary for future encounter hosts that need the same occupant lifecycle behavior without inheriting wolf-specific alert settings.

### 2. Moved WolfDenSpawner Onto The Reusable Policy Surface
* Updated `WolfDenSpawner` to read respawn, unload, spawn, and home rules through `OccupantPolicy` instead of directly from `WolfDenEncounterConfig`.
* The spawner now depends on a more reusable encounter lifecycle surface while keeping wolf-specific alert and howl behavior separate.

### 3. Preserved Existing Asset Tuning Through In-Place Migration
* Updated `WolfDenEncounterConfig` to expose the new policy object and migrate legacy serialized values into it.
* This keeps the existing authored den behavior intact while widening the config seam for later encounter generalization.

---
## [Current/Recent] - Phase 5 Slice: Encounter Occupant Boundary
This update extracts reusable encounter occupant bookkeeping out of WolfDenSpawner so the den encounter can keep wolf-specific alert behavior while shared spawn, despawn, and unload tracking move behind a generic helper boundary.

### 1. Added A Reusable Occupant Tracking Helper
* Added `WorldEncounterOccupantCollection` to own tracked occupant lists, despawn unsubscription, snapshots, and bulk release behavior.
* This creates a reusable seam for future encounter hosts that need the same occupant lifecycle policy without copying wolf-den plumbing.

### 2. Moved WolfDenSpawner Off Direct Occupant Bookkeeping
* Updated `WolfDenSpawner` to use the helper for tracked wolves, unload release flow, and despawn callback handling.
* The spawner now reads more clearly as encounter behavior instead of collection-management code.

### 3. Preserved Verified Den Behavior While Widening The Seam
* Verified leader respawn, den clear, unload/reload, and encounter behavior after the extraction.
* This keeps the refactor moving toward reusable encounter packaging without changing the authored den experience.

---
## [Current/Recent] - Phase 5 Slice: Encounter-Site Boundary
This update starts the den-versus-encounter split in earnest by introducing an encounter-facing site contract so wolf encounter orchestration no longer depends directly on the concrete WolfDen runtime.

### 1. Introduced An Encounter-Facing Site Contract
* Added IWorldEncounterSite to represent the encounter-relevant site lifecycle and position surface.
* This creates a cleaner seam between site runtime responsibilities and encounter runtime responsibilities.

### 2. Moved Wolf Encounter Runtime Onto The Interface
* Updated WolfDen to implement IWorldEncounterSite and updated WolfDenSpawner to depend on the interface instead of the concrete WolfDen type.
* The encounter layer now reads as depending on a host site contract rather than on one specific site implementation.

### 3. Tightened The Existing Encounter Runtime Slice
* Kept wolf-den behavior intact while cleaning one unload chase-range check to use squared distance.
* This keeps the slice aligned with the project performance rules while preserving verified runtime behavior.

---

## [Current/Recent] - Phase 4 Slice: Run Gate Transition Service Boundary
This update removes the direct singleton transition call from run-gate runtime behavior by introducing a narrow scene-transition service interface and threading it through world-site activation, while preserving the static-scene return-gate path used outside the procedural world.

### 1. Introduced A Narrow Scene Transition Service Boundary
* Added ISceneTransitionService and made SceneTransitionService implement it.
* This gives run-gate runtime behavior the same kind of service-bound transition contract that biome gates already use.

### 2. Routed World-Site Activation Through Scene Transition Context
* Updated WorldSiteContext, WorldSiteActivationPipeline, and WorldGenRunner to pass scene-transition access through the world-site composition path.
* Procedural-world run gates now receive transition capability through initialization instead of reaching directly into a singleton.

### 3. Preserved Static Scene Gate Behavior Without Reopening The Old Coupling
* Updated RunGateInteractable to prefer injected transition context and to safely resolve the shared scene-transition service when the same prefab is used as a regular scene object.
* This keeps the return gate functional in non-procedural scenes while still moving the site-spawned runtime onto the intended service boundary.

---

## [Current/Recent] - Phase 3 Slice: Unified Site Lifecycle Ownership
This update unifies chunk and persistent site ownership behind a single lifecycle system, moves site activation onto a prepared hidden-before-show path, and fixes wolf den pooled restore visuals by correcting prefab visual wiring and hardening the runtime against miswired child references.

### 1. Unified Chunk And Persistent Site Ownership
* Added `WorldFeatureLifecycleSystem` as the single lifecycle boundary for chunk sites and persistent biome sites.
* Updated `WorldGenRunner`, `WorldTransitionSystem`, and `ChunkProcessingPipeline` to route lifecycle work through the unified system instead of coordinating chunk and persistent lifecycles separately.

### 2. Expanded Lifecycle Diagnostics
* Updated `WorldGenDiagnosticsSnapshot`, `WorldGenDebugHUD`, and `PersistentWorldFeatureLifecycle` to expose persistent site counts alongside total active site counts.
* This makes the Phase 3 lifecycle extraction observable during streaming and biome transitions.

### 3. Hardened Pooled Site Activation And Wolf Den Restore Behavior
* Updated `WorldPoiPoolManager` and `WorldSiteActivationPipeline` so pooled sites are initialized while hidden and only activated after site context is applied.
* Updated `WolfDen` to cache colliders, restore collapsed state without replaying collapse visuals, and self-heal visual child references in the editor if they become miswired.
* Fixed `WolfDenRoot.prefab` so `activeVisual` points at the den's local `ActiveVisual` child instead of an unrelated external prefab reference.

---

# K Changelog

Use this file for world systems refactor planning and execution updates.
Archive older entries downward and add new entries at the top.

---

## [Current/Recent] - Phase 2 Close-Out: Explicit Streaming Boundary
This update finishes the current chunk streaming extraction pass by introducing explicit streaming request and result models, making the streaming anchor a live runtime value, and removing the short-lived compatibility path in favor of direct API usage.

### 1. Introduced Explicit Streaming Request And Result Models
* Added ChunkStreamingRequest, ChunkStreamingView, and ChunkStreamingFrameResult.
* This replaces ad hoc streaming out-parameter flow with a clearer system boundary that the runner can orchestrate without owning streaming semantics.

### 2. Made Streaming Anchor Part Of The Real Runtime Path
* Updated streaming bounds calculation to derive a focus chunk and updated the coordinator to push that anchor into ChunkStreamingSystem each frame.
* This turns the streaming anchor from mostly diagnostic scaffolding into actual runtime state.

### 3. Removed The Temporary Compatibility Path
* Updated WorldTransitionSystem to call SetStreamingAnchor directly and removed the old alias from ChunkStreamingSystem.
* This keeps the codebase on the intended long-term API instead of preserving a migration shortcut.

---
## [Current/Recent] - Phase 2 Slice: Streaming Runtime Diagnostics
This update makes chunk streaming diagnostics report the exact runtime state processed by the streaming coordinator, so the debug HUD becomes a readout of live system state instead of recomputing its own bounds.

### 1. Persisted Last Processed Streaming Bounds
* Updated ChunkStreamingCoordinator to retain the last load and unload chunk bounds it successfully processed.
* This gives runtime diagnostics a stable source of truth for the exact streaming rectangles used during the last frame.

### 2. Expanded Streaming Diagnostics Snapshot
* Updated WorldGenDiagnosticsSnapshot and WorldGenRunner.CreateDiagnosticsSnapshot() to expose queue counts, anchor state, and the last processed streaming bounds.
* This turns the diagnostics snapshot into a fuller description of streaming state rather than a partial summary.

### 3. Converted HUD To Snapshot-Only Streaming Data
* Updated WorldGenDebugHUD to render streaming rectangles from the diagnostics snapshot and added queue and anchor readouts.
* This removes duplicate bounds calculation from the HUD and keeps debug visuals aligned with runtime state.

---
## [Current/Recent] - Phase 2 Slice: Streaming Bounds Extraction
This update moves chunk streaming bounds calculation and per-frame streaming orchestration out of `WorldGenRunner` into dedicated chunk streaming helpers, while making the debug HUD consume the same bounds logic as runtime.

### 1. Added Dedicated Streaming Helpers
* Added `ChunkStreamingBounds`, `ChunkStreamingFrameSettings`, `ChunkStreamingBoundsCalculator`, and `ChunkStreamingCoordinator`.
* These types centralize chunk-range policy and frame-level streaming orchestration instead of leaving that logic embedded in the runner.

### 2. Reduced WorldGenRunner Streaming Ownership
* Updated `WorldGenRunner` to delegate per-frame streaming work through `ChunkStreamingCoordinator`.
* This removes runner-owned camera-to-chunk math and keeps the runner closer to a thin orchestration shell.

### 3. Unified Runtime And Debug Bounds Logic
* Updated `WorldGenDebugHUD` to draw load and unload streaming rectangles through `ChunkStreamingBoundsCalculator`.
* This keeps debug visualization aligned with the exact chunk bounds used by runtime streaming.

---
## [Current/Recent] - Phase 1 Close-Out: Forest Build-Step Migration
This update completes the immediate Phase 1 authoring gap by migrating the Forest biome onto the build-step pipeline and adding safety feedback for null-only biome step configuration.

### 1. Migrated Forest Biome Authoring Onto Build Steps
* Updated `BBD_Forest.asset` so Forest no longer carries null build-step slots.
* Added Forest-local build-step assets for road generation and site placement so the biome now participates in the same authored build pipeline as Plains.

### 2. Added Forest Site Rule Assets
* Added a Forest-local gate placement rule asset and kept Forest wolf dens disabled for now.
* This keeps Forest site generation authored through neutral placement rules instead of silently inheriting an incomplete setup, while matching the current content decision for Forest.

### 3. Hardened Biome Build-Step Validation
* Updated `BasicBiomeDefinition` to warn when a biome has build-step slots configured but every assigned entry is null.
* This closes the silent failure mode that let partially migrated biome assets behave like valid configurations.

---
## [Current/Recent] - World Systems Refactor Roadmap
This update adds a dedicated roadmap document for finishing the world systems refactor in a controlled, phased order before further implementation work continues.

### 1. Added Refactor Roadmap Document
* Added `World_Systems_Refactor_Roadmap.md` to central project documentation.
* The roadmap translates the existing blueprint into implementation-oriented close-out phases based on the current `MapGeneration` codebase state.

### 2. Defined Ordered Close-Out Phases
* Documented the recommended execution order for Phase 1 through Phase 8.
* Each phase now includes goals, scope, key work items, forbidden shortcuts, exit criteria, and verification guidance.

### 3. Captured Current Refactor State
* Recorded which boundaries are already established in code and which pressure points still remain.
* This creates a shared reference for future refactor work so changes can be evaluated against the intended end state instead of ad hoc cleanup.







































