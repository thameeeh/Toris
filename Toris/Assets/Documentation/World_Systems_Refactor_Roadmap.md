# World Systems Refactor Roadmap

Detailed execution roadmap for finishing the world systems refactor in a controlled, observable order.

This document is the implementation companion to the higher-level blueprint in `world_systems_refactorBP.docx`.
It translates the target architecture into ordered close-out phases based on the current codebase state in `Assets/Scripts/MapGeneration`.

## 1. Purpose

The world systems refactor is no longer at the "what should the architecture be?" stage.
The codebase already contains several extracted systems, but some are only partially migrated and some boundaries are not yet enforced.

This roadmap exists to:

- define the best execution order for completing the refactor
- describe what each phase is responsible for finishing
- state clear entry conditions, work items, forbidden shortcuts, and exit criteria
- prevent random cleanup from creating more temporary architecture
- keep the refactor shippable while gradually deleting old pressure-point behavior

## 2. How To Use This Roadmap

For every implementation pass:

1. Identify the current phase being closed out.
2. Write or confirm the boundary contract before moving implementation.
3. Move one vertical slice at a time.
4. Verify behavior through diagnostics and static inspection.
5. Delete the old path only after the new path is observable and understandable.

Do not skip ahead to later phases if an earlier phase still forces exceptions or temporary glue.

## 3. Current Codebase Snapshot

The codebase already contains meaningful refactor progress.
### 3.0 Progress Update (2026-04-01)

- Phase 1 close-out is complete for the current active-biome migration scope.
- Phase 2 close-out is complete for the current chunk streaming extraction scope.
- Phase 3 close-out is complete for unified site lifecycle ownership.
- Phase 4 and Phase 7 now cover explicit `IRunGateTransitionService` and `IGateTransitionService` boundaries, the old `ISceneTransitionService` migration path has been deleted, and the current transition cleanup scope is complete through explicit bootstrap fallback for static-scene run gates.
- Phase 5 close-out is complete for the current site-versus-encounter split scope through `IWorldEncounterSite`, `WorldEncounterOccupantCollection`, `WorldEncounterOccupantPolicy`, `WorldEncounterPackage`, `WorldEncounterPackageBinding`, `WorldEncounterPackageState`, `WorldEncounterAlertRuntime`, and `WolfEncounterCommandController`.
- Phase 6 now has `ITileNavigationContributionSource` and `WorldNavigationLifecycle`, which move navigation input and nav chunk ownership onto explicit neutral boundaries while keeping `SiteBlockerMap` as the current navigation-contribution producer.
- Phase 8 has started with expanded nav lifecycle and transition diagnostics in `WorldGenDiagnosticsSnapshot` and `WorldGenDebugHUD`.
- Phase 8 cleanup has already removed stale transition and blocker compatibility layers, trimmed smaller redundant nav and run-gate scaffolding, localized startup-only runner composition state, narrowed site activation and biome-transition persistence ownership to `ChunkStateStore`, removed dead public runner surface, deleted the obsolete chunk callback plus `WorldRuntimeState` wrapper plumbing, trimmed dead helper and redundant alias surface from the streaming bounds, system, frame-result, and diagnostics snapshot types, moved per-frame streaming ownership into `WorldStreamingRuntime`, and replaced the debug HUD's direct `WorldGenRunner` dependency with the narrow `IWorldDiagnosticsSource` boundary.

### 3.1 Boundaries That Already Exist In Code

- Build steps and authored feature placement rules exist through `BiomeBuildStepDefinition`, `RoadSurfaceBuildStepDefinition`, `SitePlacementRuleBuildStepDefinition`, and concrete site rules.
- Neutral site placement and blocker data exist through `SitePlacement`, `SitePlacementIndex`, `SiteBlockerMap`, and `WorldSiteDefinition`.
- A chunk site lifecycle exists through `WorldFeatureLifecycle`.
- A persistent feature lifecycle exists through `PersistentWorldFeatureLifecycle`.
- Site activation is routed through `WorldSiteActivationPipeline`.
- Site runtimes consume a `WorldSiteContext` and narrow services instead of directly receiving `WorldGenRunner`.
- Chunk streaming and frame processing are partially extracted through `ChunkStreamingSystem` and `ChunkProcessingPipeline`.
- Transitions are routed through explicit services via `WorldTransitionSystem`, `SceneTransitionService`, `IRunGateTransitionService`, and `IGateTransitionService`.
- Navigation consumes generic contribution data through `TileNavWorld.SetNavigationContributions`.
- Diagnostics now expose intentional subsystem read models for streaming, lifecycle, navigation, transition, and build output through `WorldGenDiagnosticsSnapshot` and `WorldGenDebugHUD`.

### 3.2 Main Remaining Pressure Points

- `WorldGenRunner` is now functioning as a thin composition root plus diagnostics provider for the world stack; the main remaining work is verifying whether any further extraction is still structurally justified.
- The next close-out pressure is a final audit for any remaining dead-path deletions or boundary leaks rather than another known major subsystem extraction.
- Build output is now authoritative for the active content set, but WorldGenRunner still owns too much streaming policy and diagnostics aggregation to count as a thin shell.
- Encounter boundaries are now explicit for the current wolf-den scope, but future multi-encounter reuse remains a later expansion rather than a blocking refactor pressure point.
- Major service boundaries are now in place; the main remaining pressure is cleanup and deletion of leftover migration scaffolding rather than missing transition or nav architecture.
- Asset migration is incomplete; not all biome assets are wired into the new build-step pipeline.

### 3.3 Refactor Status Summary

- Architecture definition: established
- Core system extraction: partially established
- Service-bound runtime features: mostly established
- Content migration and enforcement: incomplete
- Old pathway deletion: incomplete

## 4. Refactor Strategy

The best order is to complete earlier dependency-shaping phases before deepening later gameplay abstractions.

Rationale:

- Build output must become authoritative before streaming and lifecycle can become cleanly generic.
- Streaming policy must be explicit before `WorldGenRunner` can shrink into a thin bootstrap shell.
- Lifecycle must be unified before sites and encounters can be cleanly generalized.
- Runtime services must be complete before site and encounter logic can stop depending on singleton escape hatches.
- Diagnostics must expand alongside each phase so old paths can be removed safely.

## 5. Global Rules Of Engagement

These rules apply to every phase:

- Do not introduce new feature-specific branches into `WorldGenRunner`.
- Do not allow runtime features to pull world state by reaching into orchestrators or singletons.
- Do not add temporary registries that duplicate `SitePlacementIndex`, `SiteBlockerMap`, or lifecycle ownership.
- Do not delete an old path until the replacement has diagnostics and a parity check.
- Prefer moving ownership before polishing naming.
- Prefer a narrow service interface over passing a larger world object.
- If a change touches more than one phase, split the work unless the dependency is unavoidable.

## 6. Phase Overview

The refactor should be closed out in this order:

1. Phase 1: Normalize build output and finish content migration
2. Phase 2: Finish extraction of chunk streaming policy
3. Phase 3: Unify world feature lifecycle ownership
4. Phase 4: Complete service-based runtime dependencies
5. Phase 5: Split site runtime from encounter runtime
6. Phase 6: Harden navigation around generic blocker and modifier inputs
7. Phase 7: Unify all world and run transitions behind services
8. Phase 8: Expand diagnostics and delete remaining legacy paths

## 7. Detailed Phase Plan

## Phase 1 - Normalize Build Output And Finish Content Migration

### Goal

Make build output the single authoritative source of generated world content so later systems consume neutral placement data rather than feature-specific knowledge.

### Why This Phase Comes First

If authored content is not consistently flowing through build steps, placements, blockers, and deterministic ids, later phases will keep needing exceptions and migration glue.

### Current State

- Plains biome is using build-step assets.
- Forest biome asset migration is incomplete.
- Site placement and blocker outputs already exist.
- Terrain output is still represented mostly by stamps and per-chunk tile results rather than a broader neutral world build model.

### Scope

- audit all biome definition assets for build-step adoption
- migrate remaining biomes and persistent features onto the build-step and site-definition pathway
- ensure all site-producing world content is registered through `WorldContext.RegisterSite`
- ensure all blocker-producing world content goes through `SiteBlockerMap` or a future generic blocker contribution path
- document any world content that still bypasses the neutral build layer

### Key Work Items

- inventory every authored biome and confirm whether it uses `BasicBiomeDefinition` plus concrete build steps
- migrate null or partially configured biome assets
- confirm that road, gate, wolf den, and persistent feature placement all originate from authored definitions
- define whether terrain stamps remain an accepted build-output representation or whether they need a more explicit build artifact
- identify any remaining runtime-prefab assumptions living in build code

### Forbidden Shortcuts

- no feature-specific spawn code in `WorldGenRunner`
- no new build registries for individual feature types
- no direct runtime object references stored as generation output

### Exit Criteria

- every active biome is configured through the build-step system
- every generated site is represented through `SitePlacement`
- every site blocker contribution is represented through neutral blocker data
- no new content requires editing a central monolith to appear in the world
- the build layer can be explained entirely in terms of authored inputs and neutral outputs

### Verification

- inspect biome assets for complete build-step wiring
- log or snapshot placed site counts by biome
- verify blocker counts and active site counts through diagnostics
- verify that a biome switch rebuilds all expected placements without feature-specific special cases

## Phase 2 - Finish Extraction Of Chunk Streaming Policy

### Goal

Move chunk selection, queueing, camera bounds, preload policy, unload hysteresis, and frame-budget policy behind explicit streaming services so `WorldGenRunner` stops acting as a streaming manager.

### Why This Phase Comes Next

Streaming is already partially extracted. Finishing it early reduces churn in every later phase because lifecycle, transitions, and diagnostics all depend on stable chunk lifecycle semantics.

### Current State

- `ChunkStreamingSystem` owns loaded and queued chunk sets.
- `ChunkProcessingPipeline` owns generation and unload processing.
- `WorldGenRunner` still computes camera chunk ranges, preload extents, unload extents, and passes policy values each frame.

### Scope

- move camera-to-chunk rect calculation into a dedicated service or streaming coordinator
- move preload and unload rectangle policy out of `WorldGenRunner`
- formalize chunk lifecycle events as explicit streaming outputs
- decide whether `ChunkProcessingPipeline` remains separate from the streaming policy object or is wrapped by a higher-level streaming coordinator

### Key Work Items

- create an explicit streaming input model for focus position, camera, and policy settings
- create an explicit streaming output model for desired loads and unloads
- remove direct policy math from `WorldGenRunner.Update`
- ensure initial biome spawn and rebuild use the same streaming entry path as normal runtime operation

### Forbidden Shortcuts

- no new per-feature logic inside streaming
- no runner-owned queue prioritization branches
- no hidden streaming policy duplicated in debug code

### Exit Criteria

- `WorldGenRunner` no longer computes chunk load and unload rectangles directly
- streaming policy can be tested or inspected independently of the runner
- chunk lifecycle semantics are described by one explicit system boundary
- the frame update path reads as orchestration of systems rather than embedded streaming logic

### Verification

- compare loaded chunk counts and chunk borders before and after extraction
- verify queue behavior, preload behavior, and unload hysteresis visually in `WorldGenDebugHUD`
- confirm chunk generation cadence still respects frame budget and hard caps

## Phase 3 - Unify World Feature Lifecycle Ownership

### Goal

Make site activation and deactivation a single named subsystem with clear ownership over both chunk-bound and persistent world features.

### Why This Phase Follows Streaming

Lifecycle is driven by chunk load state and biome activation state. Streaming semantics should be stable first so lifecycle can consume clean signals instead of mixed runner behavior.

### Current State

- `WorldFeatureLifecycle` activates chunk sites.
- `PersistentWorldFeatureLifecycle` activates persistent sites.
- `WorldSiteActivationPipeline` is shared by both.
- Ownership is conceptually correct but still split across two lifecycle classes plus transition wiring.

### Scope

- define whether chunk and persistent sites should remain separate classes or become policy variants of one lifecycle
- make active instance tracking, ownership roots, and deactivation behavior explicit and inspectable
- ensure all world site activation goes through the same activation pipeline contract

### Key Work Items

- define a common lifecycle contract for activate, deactivate, rebuild, and clear
- decide where persistent site placement records should live
- ensure transition code does not need to know lifecycle internals beyond a clear service boundary
- make active-site diagnostics include persistent and chunk-scoped views

### Forbidden Shortcuts

- no direct pooling calls from transitions or runners that bypass lifecycle
- no feature-specific activation branches outside the lifecycle and activation pipeline

### Exit Criteria

- all world site spawning routes through one explicit lifecycle boundary
- chunk and persistent feature ownership rules are documented and inspectable
- transitions request lifecycle resets through services rather than manipulating internal structures

### Verification

- inspect active site counts by chunk and by persistent group
- verify unload, reload, and biome-switch behavior for both chunk-bound and persistent sites
- confirm pooled instances are always released through lifecycle ownership groups

## Phase 4 - Complete Service-Based Runtime Dependencies

### Goal

Remove the remaining runtime dependencies on orchestration classes and singleton services so feature runtimes consume only narrow injected interfaces.

### Why This Phase Comes Before Encounter Generalization

Reusable encounters are harder to define cleanly if runtimes can still bypass service boundaries and fetch global state directly.

### Current State

- biome gates use `IGateTransitionService`
- wolf dens use `IWorldSiteStateService`
- encounter consumers use `IWorldNavigationService`, `IEnemySpawnService`, and `IPlayerLocator`
- run-gate transitions now use `ISceneTransitionService` through `WorldSiteContext` in procedural-world usage, while static-scene return gates resolve the same service locally until those scenes gain a dedicated composition path

### Scope

- identify every runtime feature and bridge component that still uses direct singletons or broad world objects
- replace them with injected interfaces through `WorldSiteContext` or adjacent service composition
- keep site prefabs initialization-driven rather than discovery-driven

### Key Work Items

- create a transition interface for run-scene transitions
- route `RunGateInteractable` through a service boundary
- audit all feature-related MonoBehaviours for direct singleton usage
- keep `WorldSiteContext` narrow and intentionally scoped

### Forbidden Shortcuts

- no new singleton usage in site or encounter runtime scripts
- no runtime object should receive `WorldGenRunner` directly
- no service locator fallback inside feature code

### Exit Criteria

- all site runtimes rely on injected interfaces only
- there are no remaining direct runtime dependencies on `WorldGenRunner`
- scene and biome gate interactions both request transitions through services

### Verification

- inspect cross-references for singleton and orchestrator usage
- verify pooled site reactivation still restores the correct service context
- verify scene and biome transition requests still behave correctly after interface extraction

## Phase 5 - Split Site Runtime From Encounter Runtime

### Goal

Separate "what this place is" from "what enemies or occupants do here" so encounters can be reused by other site types.

### Why This Phase Comes Here

The den and gate site runtimes are already on service boundaries. That makes this the right time to generalize occupant logic without dragging orchestration concerns back in.

### Current State

- `WolfDen` mainly represents site state and interaction state
- `WolfDenSpawner` represents wolf encounter behavior
- the first encounter-site seam now exists through `IWorldEncounterSite`
- tracked occupant bookkeeping, despawn callback ownership, and bulk unload release now sit behind `WorldEncounterOccupantCollection`
- respawn timing, spawn radius, home radius, and chase-on-unload rules now sit behind `WorldEncounterOccupantPolicy`
- encounter-package selection now exists through `WorldEncounterPackage` and `TryGetEncounterPackage(...)`
- package activation and namespaced package persistence now exist through `WorldEncounterPackageBinding` and `WorldEncounterPackageState`
- alert accumulation, decay, and max-alert gating now exist through `WorldEncounterAlertRuntime`
- wolf-specific command behavior now sits behind `WolfEncounterCommandController`, which completes the current site-versus-encounter split for the wolf-den scope

### Scope
- define an encounter package model or service boundary
- separate site-owned state from occupant-owned state
- identify which parts of `WolfDenSpawner` are generic encounter concerns versus wolf-specific behavior

### Key Work Items

- validate that the current wolf-den encounter package boundary is stable enough to stand as the Phase 5 reference implementation
- document which parts of the current encounter package are intentionally reusable versus still wolf-specific by design
- leave broader multi-encounter reuse and future host expansion as post-close-out follow-up instead of reopening the current refactor phase

### Forbidden Shortcuts

- no new site scripts that directly own group AI logic by convenience
- no new encounter orchestration hidden inside pooled prefabs without a service boundary

### Exit Criteria

- a site can request an encounter package through an explicit API
- encounter orchestration no longer depends on a single concrete site type
- site code can be described without describing enemy-group internals

### Verification

- verify den clear, reload, unload, and leader respawn behavior still work
- validate that encounter logic can be reasoned about separately from site interaction logic
- confirm site-owned persistence and occupant-owned persistence are not mixed accidentally

## Phase 6 - Harden Navigation Around Generic Blocker And Modifier Inputs

### Goal

Keep navigation permanently feature-agnostic by formalizing how blockers and future modifiers are contributed.

### Why This Phase Is Not First

Navigation is already closer to the target than streaming and lifecycle are. It should now be hardened after the systems feeding it are more explicit.

### Current State

- `TileNavWorld` consumes generic site blockers
- navigation does not appear to reference den or gate registries anymore
- blocker contributions are currently simple and binary

### Scope

- define whether navigation only needs blockers or will also need traversal modifiers later
- formalize the input contract for nav contributions from build output and runtime systems
- keep path queries isolated from feature identity

### Key Work Items

- audit nav inputs and ensure they come from generic data structures
- decide how runtime state changes would affect nav if future features alter traversability dynamically
- keep nav chunk rebuild triggers explicit and observable

### Forbidden Shortcuts

- no feature-name checks in navigation
- no site-specific special cases in walkability logic
- no runtime site objects mutating nav state without an explicit service path

### Exit Criteria

- navigation accepts only terrain plus generic contribution data
- future features can change traversability without introducing feature identity into nav code
- nav update triggers are explicit and inspectable

### Verification

- inspect nav chunk build and clear flows
- verify site blockers consistently affect walkability
- verify no feature-type references exist in navigation code

## Phase 7 - Unify All World And Run Transitions Behind Services

### Goal

Turn transitions into a complete service boundary so biome progression and run-scene transitions share the same architectural rule: interactive objects request transitions, they do not execute orchestration directly.

### Why This Phase Comes After Runtime Service Cleanup

Both site runtime and transition callers need stable service injection first. Otherwise transition cleanup can regress into singleton access patterns again.

### Current State

- biome transitions flow through `WorldTransitionSystem`
- run-scene transitions still bypass the world transition boundary through `SceneTransitionService`

### Scope

- define transition service responsibilities clearly
- decide whether scene transition and biome transition live under one interface family or adjacent service interfaces
- move all interactables to transition-request mode

### Key Work Items

- create a run transition interface
- inject it where needed instead of using `SceneTransitionService.Instance`
- document what belongs to transition policy versus scene-loading implementation
- ensure cooldowns, safety checks, and gating rules live with transition services

### Forbidden Shortcuts

- no direct scene-loading calls from feature interactables
- no transition-side manipulation of lifecycle internals outside the transition service contract

### Exit Criteria

- all transitions are requested through explicit service boundaries
- site runtime and transition callers do not know scene management details
- transition behavior is inspectable and documented

### Verification

- verify biome gate transitions and run gate transitions both still work
- verify transition cooldown and reset behavior after biome changes
- inspect all interactables for transition calls and confirm they use service interfaces

## Phase 8 - Expand Diagnostics And Delete Remaining Legacy Paths

### Goal

Finish the refactor by making the pipeline intentionally inspectable and removing temporary scaffolding and obsolete paths.

### Why This Is Last

Deletion should happen only after the replacement systems are stable enough to observe directly.

### Current State

- `WorldGenDebugHUD` and `WorldGenDiagnosticsSnapshot` provide useful high-level diagnostics
- not every major subsystem exposes a dedicated read-only snapshot
- some classes still contain scaffold behavior or partially used state

### Scope

- add read-only diagnostics to build output, streaming, lifecycle, transitions, persistence, and navigation
- remove leftover code paths that were only kept during migration
- remove dead fields, duplicated policy math, and incomplete adapter scaffolding that is no longer needed

### Key Work Items

- define per-system diagnostic snapshots
- expose chunk queue, active instance, persistent feature, and transition state in stable forms
- remove stale fields or partially used members once replacements are verified
- archive or update related documentation once final system names are settled

### Forbidden Shortcuts

- no reflection-based diagnostics as a permanent fallback
- no keeping duplicate old and new paths "just in case" after the new path is proven

### Exit Criteria

- each major subsystem exposes intentional read-only diagnostics
- no legacy or scaffold path remains as the real source of behavior
- the world stack can be explained by named systems without code archaeology

### Verification

- verify the debug HUD and snapshots match real runtime behavior
- run static cross-reference audits for deleted legacy concepts
- confirm the old pressure-point responsibilities are no longer concentrated in one class

## 8. Phase Entry And Exit Checklist Template

Each phase should use the same checklist before being marked complete.

### Before Starting A Phase

- write the boundary contract in plain language
- identify all files currently sharing ownership of that concern
- identify all diagnostics needed to compare old and new behavior
- define what will count as "old path deleted"

### Before Finishing A Phase

- confirm all in-scope behavior uses the new boundary
- confirm no new exceptions were added to central orchestrators
- confirm diagnostics expose the new pathway clearly
- update documentation and `K_CHANGELOG.md`

## 9. Recommended Execution Cadence

Use small, reviewable passes inside each phase.

Recommended rhythm:

1. Contract pass: write the ownership and interface rules
2. Migration pass: move one vertical slice
3. Verification pass: compare behavior and inspect diagnostics
4. Deletion pass: remove old code and update docs

Do not merge all four passes into one large opaque change unless the scope is genuinely tiny.

## 10. Immediate Next Move

The next implementation step should stay inside Phase 8 cleanup and target one more small dead-path deletion.

Specifically:

- audit `WorldGenRunner`, `WorldSceneServices`, and `TileNavWorld` for any remaining scaffolding-only comments, redundant fallback lookups, or dead helper surfaces
- prefer no-behavior deletions that can be verified through the existing HUD parity checks
- keep using the expanded nav and transition diagnostics as the safety net for each cleanup slice
- leave the deferred wolf bugs in the post-refactor queue unless one starts blocking Phase 8 verification

That keeps the program in close-out mode instead of reopening already-verified phase work.

## 11. Deferred Post-Refactor Wolf Bug Queue

These two issues are intentionally recorded for investigation immediately after the map refactor sequence is complete unless one starts blocking current phase verification.

### Bug 1 - Intermittent Dead Wolf Fails To Despawn

- symptom: a wolf can remain visually dead on screen while the game still treats it as alive, which then causes wolf den behavior to stop working correctly
- current clue: the wolf appears to hold on the last frame of its death animation instead of finishing the despawn path
- repro status: intermittent, not 100 percent reproducible

### Bug 2 - Dying Leader Can Resurrect Into A Howl Loop At Max Aggro

- symptom: while a wolf leader is playing its dying animation, pushing the den to max aggro can cause the dying leader to resurrect and get stuck in a howl loop
- associated messages:

```text
'Animator' AnimationEvent 'Howl' on animation 'Wolf_Howl_SW' has no receiver! Are you missing a component?
'Animator' AnimationEvent has no function name specified!
```

## 12. Definition Of Done For The Refactor Program
The refactor is finished when all of the following are true:

- new world content can be added without editing a central monolith
- generated site and blocker data exist independently of live Unity objects
- chunk streaming, persistence, lifecycle, runtime sites, encounters, navigation, transitions, and diagnostics all have named ownership
- runtime features consume narrow services only
- navigation remains feature-agnostic
- diagnostics are intentional enough that future refactors do not require code archaeology
- `WorldGenRunner` is reduced to a thin bootstrap and orchestration shell rather than a pressure point







































