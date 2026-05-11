# Procedural World Dressing And Lake Vignette Plan

This document defines a practical direction for making the procedural world feel less empty without requiring handcrafted variants for every natural feature.

It belongs under `MapGeneration/QoL Tool` because this is primarily a world-generation quality and authoring workflow problem.

It is a planning/design document, not a changelog.

Scope boundary:
- this is for the procedural world, wilderness, roads, lakes, and generated sites
- this is not for Main Area / Safe Haven beautification
- authored vignettes in this document are reusable procedural-world feature layouts, not hand-placed Main Area decorations

## Current Generator Baseline

The procedural world already has a five-layer visual output path:

- ground
- water
- decoration
- obstacle
- canopy

This means world dressing work can focus on rules and content rather than adding more tilemap plumbing.

Generated roads now also support deterministic biome-authored visual variants. This improves the base path feel before adding separate road-edge decoration passes.

Useful existing layers for this plan:

- `Decoration` for flowers, reeds, pebbles, small plants, and non-blocking shoreline clutter
- `Obstacle` for rocks, logs, trunks, and other base visuals that may need explicit blocker data
- `Canopy` for tall trees or upper silhouettes that should remain visual-only

Important rule:
- placing an `Obstacle` tile still does not automatically block movement
- any generated solid object must also provide explicit blocker data if it should affect navigation

## Problem

The world currently has the large shapes it needs:

- terrain
- roads
- lakes
- sites

But it can still feel visually dull because many areas only contain the primary shape and not enough secondary dressing.

The grave layouts immediately improved the feel of the world because they added:

- local composition
- intentional asymmetry
- edge detail
- a stronger visual identity

The same improvement is needed for general world spaces, especially around procedural lakes.

## Important Constraint

We should **not** try to hand-author every lake.

That would create too much content work and would scale badly.

Instead, the world should use a hybrid model:

1. procedural natural shapes for broad coverage
2. procedural dressing passes for general richness
3. authored vignettes for memorable small spaces

That gives the world more life without requiring handcrafted replacements for everything.

## Goal

Make the world feel more handcrafted by adding detail at the right layer.

Do this by combining:

- generic procedural lake generation
- shoreline decoration passes
- occasional authored shoreline vignettes
- rare special feature locations

## Core Design

## 1. Keep Generic Lakes Procedural

The normal biome lake generation should remain the source of most water shapes.

That system already gives the world breadth.

It should continue handling:

- general lake footprint
- random natural variation
- broad biome coverage

Do not replace this with a handcrafted-layout-only approach.

## 2. Add A Generic Shoreline Dressing Pass

After a lake is generated, run a dressing pass around shoreline-adjacent land tiles.

This pass should add light detail such as:

- flowers
- reeds
- shoreline rocks
- logs / driftwood
- small vegetation clusters
- occasional nearby tree grouping

This is the cheapest and highest-value way to make ordinary lakes feel less empty.

This pass should be:

- lightweight
- rule-based
- deterministic
- broad enough to affect most lakes

## 3. Add Authored Lake-Edge Vignettes

The existing layout authoring workflow should be used for **small handcrafted scenes**, not for every whole lake.

Good examples:

- flower bank
- fallen log bank
- fishing spot
- small campfire nook
- shrine edge
- bone pile / ruin edge

These should be attached to suitable positions on lake edges.

This is the best place to use the layout tool for natural features because it gives handcrafted feel without requiring full handcrafted lakes.

## 4. Reserve Full Feature Layouts For Special Spots

Full handcrafted feature spaces should be used sparingly for more intentional locations such as:

- shrine ponds
- ritual ponds
- campsite ponds
- unique biome landmarks

These should be rarer than normal lakes.

They are not the baseline solution for all lake variety.

## Three-Tier Model

The lake system should be thought of in three tiers:

## Tier 1 - Generic Lake

Fully procedural lake shape.

Enhancement:

- shoreline dressing rules only

Use this for:

- most lakes in the world

## Tier 2 - Lake-Edge Vignette

Procedural lake plus one or more small authored scenes at the edge.

Enhancement:

- vignette layout asset
- optional prop markers later

Use this for:

- giving normal lakes small moments of identity

## Tier 3 - Feature Lake

A rarer, more intentionally-authored special location.

Enhancement:

- stronger authored footprint
- stronger visual identity
- possible runtime interaction or site logic

Use this for:

- memorable exploration spots
- encounter-adjacent natural landmarks

## Why This Is Better Than Handcrafting Every Lake

This hybrid model gives:

- broad coverage from procedural generation
- visual richness from dressing
- memorable moments from authored vignettes

without requiring:

- dozens of fully-authored lake variants
- a handcrafted replacement for every random lake blob

It is the scalable middle ground.

## Relationship To The Layout Authoring Tool

The layout authoring tool should be used for:

- lake-edge vignettes
- shrine spots
- campfire spots
- small handcrafted natural pockets
- special encounter spaces

It should not be treated as the solution for every large natural terrain shape.

That tool is strongest when authoring:

- small to medium local compositions
- repeated feature families
- memorable edge scenes

not when replacing broad natural world generation wholesale.

## Suggested First Practical Scope

The first world dressing implementation should focus on ordinary lakes.

Start with:

1. identify shoreline-adjacent land tiles
2. add a deterministic decoration pass
3. decorate with a small palette of shoreline content

Example first palette:

- flowers
- reeds
- small rocks
- logs

This alone should already improve lake readability and reduce visual emptiness.

Roads can follow the same philosophy after the base road variant palette is tuned:

1. keep the main road surface procedural
2. use biome road variants for surface texture
3. add separate road-edge dressing later for flowers, stones, grass, ruins, or signpost clutter

## Suggested Second Scope

After the generic shoreline dressing pass works, add authored vignettes.

Start with a tiny set:

- `LakeVignette_FlowerBank_A`
- `LakeVignette_FlowerBank_B`
- `LakeVignette_FallenLog_A`
- `LakeVignette_CampNook_A`

These can use the same paint-and-bake layout workflow already proven by the grave system.

Current implementation note:
- authored shoreline vignettes are supported through `ShorelineVignettePlacementRuleDefinition`
- the rule owns a list of `SiteTileLayoutDefinition` variants
- each rule declares the authored water-facing direction in cell/grid space
- each rule can run as a counted feature placement or as a repeatable shoreline filler
- runtime placement finds lake-adjacent land tiles and rotates the chosen layout toward the real water direction
- candidate anchors are collected from the actual shoreline instead of only random disk samples
- layout land-side cells that would land on generated lake water are skipped so imperfect shoreline art still blends safely
- repeatable filler rules can avoid existing stamps so rare/common features remain visible and filler appears in the gaps

Unity setup:
1. Bake one or more lake-edge layouts from the layout authoring tool.
2. Create a `WorldGen/Biomes/Site Rules/Shoreline Vignette Rule` asset.
3. Add the baked layouts to the rule's shoreline layout variant list.
4. Set the authored water direction to match the layout's origin-to-water direction in cell space.
5. Use `FeatureCount` for rare/common anchors or `FillAvailableShoreline` for repeatable grass/reed filler.
6. Tune count, spacing, fill chance, and avoid-existing behavior.
7. Add the shoreline rule to the biome's `SitePlacementRuleBuildStepDefinition` rule list.

Recommended layered shoreline setup:
1. `PlacementRare`
   - mode: `FeatureCount`
   - content: rich/hero shoreline layouts
   - count: low
   - spacing: high
2. `PlacementCommon`
   - mode: `FeatureCount`
   - content: logs, rocks, visible local details
   - count: medium
   - spacing: medium
3. `PlacementRepeatable`
   - mode: `FillAvailableShoreline`
   - content: reeds, grass, tiny shoreline clutter
   - spacing: low
   - avoid existing stamps: enabled

Run repeatable filler after rare/common rules so it wraps the remaining shoreline gaps.

## Suggested Third Scope

Only after the first two steps feel good should we move into special feature lakes.

Examples:

- shrine pond
- ritual pond
- ruined camp pond

These should be deliberately rare so they stay memorable.

## Future Extension

Once the layout tool supports markers/sockets, lake vignettes can become much richer.

Examples:

- log prop socket
- tree socket
- VFX socket
- loot socket
- interaction socket

That will allow the same vignette layout to mix:

- painted tiles
- placed props
- optional runtime interactions

## Main Design Decision

The correct next move is **not** to hand-author full replacements for generic procedural lakes.

The correct next move is:

1. procedural shoreline dressing everywhere
2. authored vignettes sometimes
3. feature lakes rarely

That gives the strongest visual improvement for the least content burden.

## Recommended Next Step

The next implementation/design task should be:

- design a `shoreline decoration pass` for generic lakes

That should define:

- how shoreline tiles are detected
- what decoration categories exist
- how densities are rolled
- how to avoid ugly overlap and repetition

Once that exists, authored lake-edge vignettes can layer on top cleanly.

## Current Best Implementation Slice

Recommended first procedural-world slice:

1. Detect land tiles adjacent to generated lake water.
2. Roll deterministic shoreline vignette anchors using biome seed and tile position.
3. Pick one authored layout from the configured shoreline layout list.
4. Rotate the layout so its authored water-facing side points toward the real lake water.
5. Place non-blocking details on `Decoration`.
6. Place occasional rock/log base visuals on `Obstacle` only when the art needs that layer.
7. Do not add blocker footprints until a generated object truly needs to block movement.

This keeps the work broad, cheap, and directly useful for making procedural wilderness feel more intentional.
