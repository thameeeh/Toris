# Procedural World Dressing And Lake Vignette Plan

This document defines a practical direction for making the procedural world feel less empty without requiring handcrafted variants for every natural feature.

It belongs under `MapGeneration/QoL Tool` because this is primarily a world-generation quality and authoring workflow problem.

It is a planning/design document, not a changelog.

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

## Suggested Second Scope

After the generic shoreline dressing pass works, add authored vignettes.

Start with a tiny set:

- `LakeVignette_FlowerBank_A`
- `LakeVignette_FlowerBank_B`
- `LakeVignette_FallenLog_A`
- `LakeVignette_CampNook_A`

These can use the same paint-and-bake layout workflow already proven by the grave system.

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
