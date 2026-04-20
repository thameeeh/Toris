# World Feature Layout Authoring Tool Plan

This document plans a design-first workflow for building hand-authored world feature layouts that can still be spawned inside the procedural map.

It is not a changelog.

## Problem

The current layout system is technically useful, but the authoring experience is weak for anything beyond very small patterns.

Right now, larger features require:

- imagining the layout in your head
- manually typing `x/y` offsets
- assigning tiles cell by cell in an inspector

That is workable for tiny grave adjustments, but it becomes painful for bigger features such as:

- grave variants
- lakes
- ruined camps
- shrines
- small forest clearings
- decorated points of interest

The data format is acceptable.
The workflow is the real problem.

## Goal

Move from a code-first and inspector-first workflow to a paint-first workflow.

The intended experience should be:

1. create a small authoring grid
2. paint the feature by hand using tilemaps
3. optionally place prefab sockets / markers
4. press `Bake Layout`
5. generate a reusable layout asset
6. let procedural worldgen place that layout in valid locations

That means the world stays procedural in:

- where a feature appears
- how many can appear
- which variant gets chosen

But the feature itself can still look handcrafted.

## Core Idea

We already have the beginning of the runtime data model:

- `SiteStampDefinition`
- `SiteTileLayoutDefinition`
- deterministic variant selection in `SiteStamping`

The next step is to add a proper authoring layer on top of that runtime format.

The tool should author layouts visually and then bake into the current layout assets instead of replacing the current worldgen architecture.

## Planned Workflow

### Authoring

The designer creates a small authoring object or scene with:

- a `Grid`
- child `Tilemap`s for different layers
- optional child marker objects for prefab sockets

Suggested tilemap layers:

- `Ground`
- `Water`
- `Decoration`
- `Obstacle`
- `Canopy`

Optional marker layers:

- `PrefabSocket`
- `EncounterAnchor`
- `LootSocket`
- `VFXSocket`

### Baking

The tool reads the painted tilemaps and converts them into a layout asset.

For each painted tile cell, it stores:

- offset from the chosen layout origin
- assigned tile per supported layer

For each placed socket/marker, it stores:

- local offset
- marker type
- optional prefab or category id

### Runtime

Worldgen chooses a placement location.

Then:

1. site stamp reserves / clears the footprint
2. one authored layout variant is chosen
3. baked tiles are applied
4. any prefab sockets are resolved and spawned
5. the site prefab or encounter runtime is placed on top

## What This Tool Should Support

### First-Pass Required

- bake painted tilemaps into `SiteTileLayoutDefinition`
- support all existing tile layers:
  - ground
  - water
  - decoration
  - obstacle
  - canopy
- choose a pivot / origin tile for offsets
- rebake an existing layout asset without manual offset editing

### Strong Second-Pass Support

- prefab sockets / marker baking
- previewing the footprint bounds
- quick duplicate-as-new-variant flow
- layout validation warnings

### Nice Later Support

- randomization tags on sockets
- weighted variant metadata
- auto-thumbnail preview
- scene gizmos for footprint and origin

## Recommended Authoring Asset Structure

### Runtime Assets

Keep using:

- `SiteStampDefinition`
- `SiteTileLayoutDefinition`

Add later if needed:

- `SitePrefabSocketLayoutDefinition`
- or extend the current layout system to include socket entries

### Authoring Asset / Prefab

Introduce something like:

- `SiteLayoutAuthoringRoot`

This should contain:

- `Grid`
- the tilemap layers
- a chosen origin marker
- optional socket markers

### Editor Tooling

Introduce an editor-only workflow such as:

- `SiteLayoutBakerWindow`
- or a custom inspector on `SiteLayoutAuthoringRoot`

Main actions:

- `Bake New Layout`
- `Rebake Existing Layout`
- `Validate Layout`
- `Clear Empty Cells`

## Recommended Data Separation

Keep these responsibilities separate:

### Stamp

`SiteStampDefinition`

Owns:

- footprint size
- clutter clear zone
- navigation blocker footprint
- shared area shaping

### Layout

`SiteTileLayoutDefinition`

Owns:

- exact painted tile arrangement
- decorative variation
- hand-authored local pattern

### Site Prefab / Runtime

Owns:

- interaction
- encounter logic
- special visuals
- animation
- one-off authored objects that are not just tiles

This separation matters because it lets one feature type have:

- one shared footprint
- many visual variants
- one gameplay runtime

## Example Use Cases

### Necromancer Grave

Shared:

- one grave site stamp
- one grave encounter runtime

Variants:

- grave layout A
- grave layout B
- grave layout C

Possible socket later:

- candle prop
- bones pile
- broken cart piece

### Plains Lake

Shared:

- one lake stamp / reserved footprint

Variants:

- circular lake
- crescent lake
- narrow pond

Possible sockets later:

- fallen log
- reeds cluster
- tree marker
- flower patch

This is the kind of feature that becomes much easier once the authoring tool exists.

## Why This Is Better Than Inspector-Only Layout Editing

This tool would let us:

- design visually instead of mentally simulating offsets
- make more variants faster
- produce more convincing handcrafted spaces inside procedural biomes
- reuse one worldgen placement rule with many painted results

Most importantly, it shifts effort from:

- coding placement details

to:

- designing actual world features

## Implementation Phases

## Phase 1

Goal:

- visual baking for tile layouts only

Deliverables:

- authoring root component
- editor bake action
- bake into `SiteTileLayoutDefinition`
- support rebaking

This phase is enough to make grave layout authoring dramatically better.

## Phase 2

Goal:

- prefab socket support

Deliverables:

- marker objects
- baked socket entries
- runtime resolver for sockets

This phase unlocks richer features like lakes with logs, trees, props, and edge set dressing.

## Phase 3

Goal:

- workflow polish

Deliverables:

- validation
- previews
- duplicate variant flow
- optional thumbnails / metadata

## Important Constraints

The tool should:

- reuse the current runtime worldgen path
- not introduce a separate bespoke spawn system
- stay deterministic at runtime
- remain editor-first and designer-friendly

The tool should not try to solve everything immediately.

For first pass, it only needs to make layout creation sane.

## First Recommended Milestone

The first implementation target should be:

- an editor authoring prefab with layered tilemaps
- a baker that converts that painted content into `SiteTileLayoutDefinition`

That alone would immediately improve:

- Necromancer grave layouts
- future forest sites
- lakes and other shaped features later

## Suggested Next Step

After this plan, the next practical task should be:

1. define the authoring root component
2. define how the origin tile is chosen
3. build the first bake path for tile layers only
4. test it by creating `NecromancerGraveLayout_A`

That gives us one real end-to-end proof that the workflow is worth expanding.
