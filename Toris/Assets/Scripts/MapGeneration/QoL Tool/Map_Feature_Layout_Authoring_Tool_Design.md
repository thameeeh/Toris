# Map Feature Layout Authoring Tool Design

This document defines the first real design for the world feature layout authoring tool.

It belongs under `MapGeneration/QoL Tool` because this is a map-generation workflow improvement, not a player-system feature.

It is a planning/design document, not a changelog.

## Purpose

The procedural world already supports:

- site stamps
- site tile layouts
- layout variants
- deterministic placement

What is currently missing is a good **designer workflow** for creating those layouts.

Right now, the weak point is authoring:

- manually typing `x/y` offsets
- imagining patterns in your head
- assigning tiles cell-by-cell in inspectors

That is acceptable for tiny tests.
It is not acceptable long-term for:

- Necromancer grave variants
- lakes
- camps
- shrines
- ritual sites
- ruins
- encounter clearings

## Goal

Move from:

- inspector-first layout authoring

to:

- paint-first layout authoring

The intended workflow should be:

1. create an authoring root
2. paint tiles visually on layered tilemaps
3. optionally place markers later
4. bake the result into a reusable layout asset
5. let procedural placement use that baked layout at runtime

This keeps the world procedural in:

- where a feature appears
- how many appear
- which variant is chosen

while making the feature itself feel hand-authored.

## Why This Tool Matters

This is a strong leverage tool because it improves every future handcrafted procedural feature.

Immediate uses:

- Necromancer grave layouts
- future lake layouts
- ruined camp layouts
- shrine layouts

Long-term value:

- designers spend time shaping actual spaces
- less time mentally simulating coordinates
- more layout variants become practical

## Existing Runtime Pieces To Reuse

Do not replace the current runtime architecture.

Build on top of:

- [SiteStampDefinition.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/MapGeneration/Generation/Output/SiteStampDefinition.cs)
- [SiteTileLayoutDefinition.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/MapGeneration/Generation/Output/SiteTileLayoutDefinition.cs)
- [SiteStamping.cs](/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/MapGeneration/Generation/Output/SiteStamping.cs)

The tool should produce data for those systems, not invent a second feature-placement path.

## Core Design

## 1. Authoring Root

Introduce an editor-facing authoring object, something like:

- `SiteLayoutAuthoringRoot`

Its job is:

- define a local mini-grid for authoring a feature
- expose tilemaps for supported layers
- define an origin/pivot cell
- provide bake actions

This is not the runtime site object.
It is an editor authoring helper.

## 2. Layered Tilemap Authoring

The authoring root should contain a `Grid` with child `Tilemap`s for:

- `Ground`
- `Water`
- `Decoration`
- `Obstacle`
- `Canopy`

These match the world-layer model already being established in map generation docs.

Meaning:

- `Ground` = base terrain painting
- `Water` = ponds/lake pockets if needed
- `Decoration` = non-blocking scene dressing
- `Obstacle` = blocking base visuals
- `Canopy` = high overlay visuals

## 3. Bake Output

The bake step should convert painted cells into one `SiteTileLayoutDefinition`.

Each baked cell should store:

- local offset from the chosen origin
- whichever tile layers are painted at that cell

This means the runtime still consumes the same kind of layout asset it already understands.

## 4. Runtime Placement Model

The runtime placement flow should remain:

1. placement rule chooses the site location
2. stamp reserves / clears the footprint
3. one layout variant is selected
4. baked tiles are applied
5. runtime site prefab / encounter is spawned

The new tool only improves step 3 authoring.

## Scope For Phase 1

Phase 1 should stay deliberately small.

Deliver only:

- authoring root
- layered tilemaps
- origin selection
- bake into `SiteTileLayoutDefinition`
- rebake existing layout assets

This is enough to unlock:

- `NecromancerGraveLayout_A`
- `NecromancerGraveLayout_B`
- `NecromancerGraveLayout_C`

without building a bigger tool suite first.

## Explicit Non-Goals For Phase 1

Do not build these yet:

- prefab sockets
- weighted layout metadata
- thumbnail generation
- random marker sets
- runtime preview systems
- custom in-world placement gizmo suite

Those are all valid later, but they should not delay the first useful tool.

## Phase 2 Direction

Once Phase 1 is proven useful, add marker support.

That means support for things like:

- prop socket
- loot socket
- VFX socket
- encounter anchor

This is what will make features like lakes and camps much richer later.

Examples:

- lake with fallen log
- grave with candles
- ritual site with prop markers

But this should come only after tile baking is working well.

## Recommended Authoring Workflow

The intended workflow should be:

1. create a `SiteLayoutAuthoringRoot`
2. choose the origin tile
3. paint the feature visually
4. click `Bake New Layout` or `Rebake Existing Layout`
5. assign the resulting layout asset into a stamp variant list
6. run the world and verify the feature in procedural placement

That is the entire value proposition:

- visually author once
- reuse procedurally many times

## Recommended First Test Case

The first proof should be the Necromancer grave.

Why:

- it already exists as a real site
- it already uses a layout system
- it has clear variants in mind
- it is small enough to validate quickly

First test target:

- bake one real grave layout from painted tiles
- use it in the existing grave stamp
- confirm it appears correctly in the Forest

If that works, the tool has already paid for itself.

## Recommended Editor Surface

Best first UI shape:

- custom inspector on `SiteLayoutAuthoringRoot`

Buttons:

- `Bake New Layout`
- `Rebake Existing Layout`
- `Validate Layout`

Reason:

- simpler than starting with a full editor window
- keeps the workflow attached to the authored object
- lower implementation overhead

If later the tool grows, it can move into a more elaborate editor window.

## Validation Rules

The tool should warn about:

- missing origin
- no painted cells
- duplicate/misaligned tilemap setup
- unsupported tilemap layers

It should not try to be clever beyond that in phase 1.

## Naming Recommendation

Suggested runtime/editor names:

- `SiteLayoutAuthoringRoot`
- `SiteLayoutAuthoringBaker`
- `SiteTileLayoutDefinition`

This keeps the distinction clear:

- authoring object
- bake logic
- runtime asset

## Design Decision

The tool should stay **mapgen-owned**, not player-owned.

Reason:

- it is for procedural site authoring
- it serves biome/site/encounter layout creation
- it should live beside map generation systems and docs

So future related docs and tooling should also prefer this folder area:

- `Assets/Scripts/MapGeneration/QoL Tool/`

## Immediate Next Implementation Step

When we start building this, the first concrete task should be:

1. create `SiteLayoutAuthoringRoot`
2. give it the layered tilemap references
3. let it choose an origin
4. bake into `SiteTileLayoutDefinition`

That is the smallest useful version of the tool.
