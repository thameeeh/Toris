# Grid Layering And Tree Blocking Plan

## Summary
This plan describes how to expand the `ProceduralTiles` grid into a clearer layered world setup that supports:
- ground tiles
- water tiles
- non-blocking decorations
- blocking obstacle visuals such as tree stumps and rocks
- optional canopy visuals above actors
- a future path for choppable trees without rewriting the layering model later

The goal is to keep rendering, collision, and navigation responsibilities separated so the system remains easy to reason about as the world grows.

## Current Scene Baseline
The `ProceduralTiles` scene currently uses one Unity `Grid` with these child Tilemaps:
- `Terrain`
- `Interactible`
- `Water`

Current runtime wiring:
- `Terrain` is the world ground tilemap
- `Water` is the world water tilemap and has a `TilemapCollider2D`
- `Interactible` is currently used as the generated decor layer

Current runtime behavior:
- generation writes only three tilemap arrays: ground, water, and decor
- navigation reads ground and water, then applies explicit blocker contributions
- decor visuals do not automatically block navigation

This means the project already has the right conceptual split for visuals versus movement blocking. The next step is to make that split more explicit and more extensible.

## Recommended Grid Layer Model
Keep a single shared `Grid` GameObject. Do not introduce multiple world grids.

Recommended child Tilemaps under the same `Grid`:
- `Terrain`
- `Water`
- `Decoration`
- `Obstacle`
- `Canopy`

Layer intent:
- `Terrain`
  - base walkable ground only
  - grass, dirt, road, forest floor, mud, roots patches
- `Water`
  - water visuals
  - water collision if needed
  - never walkable
- `Decoration`
  - non-blocking visual dressing only
  - grass tufts, flowers, pebbles, debris, tiny mushrooms
- `Obstacle`
  - base visuals of objects that occupy space
  - tree stumps, rock bases, logs, ruins, wall bases, trunks
- `Canopy`
  - high visual overlays that should appear above actors
  - tree leaves, roof tops, hanging foliage, tall upper silhouettes

## Core Responsibility Rules
These rules should stay fixed as the system evolves:

- Tilemap layer decides visual placement.
- Collision and navigation blocking are separate concerns.
- Do not treat a visible obstacle tile as automatically blocking unless a blocker path also marks it as blocked.
- Decorations never block.
- Water blocks by layer and by nav.
- Obstacle visuals may block, but only when paired with explicit blocker data.
- Canopy is visual only.

This separation keeps the rendering model clean and avoids coupling art choices directly to movement logic.

## Rendering Plan
Use the grid to establish visual layers, not gameplay rules.

Recommended rendering intent:
- `Terrain`
  - lowest visible ground layer
- `Water`
  - still on the ground stack
  - visually above or blended against terrain depending on art need
- `Decoration`
  - above terrain and water
  - still below large obstacle tops and actor overlays
- `Obstacle`
  - above ground visuals
  - contains trunks and bases that visually cover the ground beneath them
- `Canopy`
  - highest world-tilemap visual layer
  - intended to overlap actors visually when needed

For trees specifically:
- keep the forest floor on `Terrain`
- place stump or trunk visuals on `Obstacle`
- place leaves on `Canopy` if the tree art is tall enough to need separation

Do not remove the ground under a tree unless the art truly requires an empty patch. Most of the time the correct visual result is:
- ground remains on `Terrain`
- trunk sits above it on `Obstacle`
- canopy sits higher on `Canopy`

If the tree should feel grounded visually, stamp a special terrain tile beneath it first, such as:
- darker dirt
- roots patch
- shadowed forest floor

## Blocking Plan For Trees And Obstacles
For procedural gameplay blocking, continue using explicit blocker data rather than making the visual tilemap itself the navigation source of truth.

Use the existing blocker path:
- `WorldBuildOutput.SiteBlockers`
- `SiteStamping.AddSquareBlocker(...)`
- `TileNavWorld` navigation contributions

Recommended rule:
- a tree stump that should block movement must stamp both:
  - an obstacle visual on the visual tilemap side
  - a blocker footprint on the navigation side

That means the tree system should always be authored as two parts:
- visual footprint
- blocker footprint

This keeps AI and player movement aligned with the same blocking data and avoids relying on tilemap collider side effects for procedural content.

## Decoration Plan
Decorations should stay cheap and purely visual.

Use `Decoration` for:
- grass clumps
- flowers
- pebbles
- ground litter
- small mushrooms
- tiny plants that should not affect movement

Rules:
- no collider
- no nav blocker
- no site state
- no runtime interaction

If a decoration later becomes interactable, promote it from decoration to either:
- a blocking obstacle visual plus blocker footprint
- a world site prefab if it needs state or interaction

## Tree Structure Model
Treat each tree as two conceptual parts:
- stump or trunk base
- canopy top

Recommended default tree authoring:
- `Terrain`
  - optional dirt or roots stamp beneath the tree
- `Obstacle`
  - stump or trunk base
- `Canopy`
  - leaf mass or top silhouette
- blocker data
  - footprint on the stump or trunk only

Movement rule:
- the stump blocks
- the canopy does not block

This gives the player and enemies a clear physical obstacle while still allowing tall foliage to visually overlap the character.

## Future Choppable Trees Plan
If trees may become chop-down interactables later, do not build the first layered system in a way that prevents that evolution.

The clean future model is:
- decorative non-interactive trees remain tilemap-authored visuals plus blocker data
- choppable trees become site-like interactive objects with runtime state

Recommended upgrade path for a choppable tree:
1. Keep the environment layering model the same.
2. Replace the static obstacle tree instance with a tree site or pooled world object.
3. Give the tree runtime state such as:
   - intact
   - damaged
   - felled
   - stump only
4. On chop-down:
   - remove or swap the canopy visual
   - remove or swap the trunk visual
   - remove or reduce the blocker footprint
   - optionally spawn logs, drops, or stump remains
5. Persist that state through the same world-site state system used by other procedural sites if the tree must stay felled across reloads.

Important design rule:
- do not make all trees full GameObjects just because some may become choppable later
- keep the cheap decorative version for most trees
- reserve runtime site objects for special trees that actually need interaction or persistence

## Recommended Technical Expansion Path
Implement the grid expansion in this order:

### 1. Clarify the visual tilemap naming
Rename or repurpose the current visual layers so their intent is obvious:
- keep `Terrain`
- keep `Water`
- rename `Interactible` to `Decoration` or `Obstacle`

Preferred result:
- current `Interactible` becomes `Decoration`
- add new `Obstacle`
- add new `Canopy`

### 2. Extend generation output only when needed
Right now generation writes:
- ground
- water
- decor

If the procedural generator should author more than one visual overlay layer, extend:
- `TileResult`
- `ChunkResult`
- `TileResolver`
- `TilemapApplier`
- `WorldGenRunner` tilemap references

Suggested future generated outputs:
- ground
- water
- decoration
- obstacle
- canopy

Do not extend these until there is a real generator use case. For manual scene-only experimentation, extra tilemaps can be added without changing generation code yet.

### 3. Keep blockers separate from visuals
Continue to drive blocking through blocker contributions rather than tying walkability directly to obstacle visuals.

For generated trees and large props:
- stamp terrain if needed
- stamp obstacle visuals
- stamp blocker footprint

### 4. Use canopy only for tall overlap cases
Do not put every bush or plant into a canopy layer.

Use `Canopy` only for art that must visually sit above actors, such as:
- large trees
- roof tops
- hanging vines above the player

## Practical Usage Rules
Use tilemaps when the content is:
- static
- numerous
- cheap to render
- visually repeated
- not individually stateful

Use world objects or site prefabs when the content needs:
- health
- interaction
- loot
- damage states
- persistence
- destruction
- AI attachment

That means:
- most trees should stay tilemap-based plus blocker data
- special trees should become site or prefab-driven

## Example Tree Cases

### Decorative Tree
- `Terrain`: forest floor
- `Obstacle`: trunk
- `Canopy`: leaves
- blocker footprint: yes
- runtime state: none

### Decorative Bush
- `Terrain`: grass
- `Decoration`: bush tile
- blocker footprint: no
- runtime state: none

### Rock Obstacle
- `Terrain`: dirt or grass
- `Obstacle`: rock tile
- blocker footprint: yes
- runtime state: none

### Choppable Tree Later
- `Terrain`: roots patch
- visual runtime object or site for trunk and canopy
- blocker footprint: enabled while standing
- blocker footprint: removed or reduced when chopped
- runtime state: intact or felled

## Acceptance Criteria For The Layered Grid System
The design is successful when:
- ground remains visible and consistent beneath obstacles
- tree trunks visually sit above ground tiles
- optional canopies can overlap actors visually without affecting movement
- water remains clearly separated and non-walkable
- decorations can be added freely without blocking movement
- obstacle blocking is driven by explicit blocker data rather than accidental art setup
- future choppable trees can be introduced without converting every decorative tree into a GameObject

## Recommended Next Step
If this plan is implemented later, the first practical milestone should be:
- add `Decoration`, `Obstacle`, and `Canopy` tilemaps under the existing `Grid`
- keep the current generator writing to the existing three channels for now
- manually validate the visual stack in `ProceduralTiles`
- then decide whether the procedural generation output should be extended to write obstacle and canopy layers directly

## Concrete Implementation Plan

### Locked Decisions
- Keep one shared `Grid` in `ProceduralTiles`.
- Separate visuals from movement blocking.
- Keep most trees tilemap-based.
- Reserve runtime objects or sites for trees that need state or interaction.
- Treat stump or trunk blocking as explicit blocker data, not as an implied property of a visible tile.
- Treat canopy as visual-only.

### Phase 1 - Scene Layer Expansion
Goal:
Create a clearer tilemap stack in the scene before changing procedural generation.

Steps:
- Under the existing `Grid`, keep:
  - `Terrain`
  - `Water`
- Rename or repurpose `Interactible` into a clearer visual layer:
  - preferred: `Decoration`
- Add:
  - `Obstacle`
  - `Canopy`
- Keep all of these under the same Unity `Grid` so cell/world conversion remains shared.
- Set initial rendering intent:
  - `Terrain` lowest
  - `Water` on the ground stack
  - `Decoration` above ground visuals
  - `Obstacle` above decoration
  - `Canopy` highest

Validation:
- The new tilemaps exist and render in the intended order.
- Ground remains visible beneath obstacle visuals.
- Canopy can visually overlap the player without affecting movement.

### Phase 2 - Clarify Layer Ownership In Code
Goal:
Make the runtime explicit about what each tilemap is for before adding new generated outputs.

Steps:
- Keep current generation behavior working with the existing output channels first.
- Update naming and references so the code reads clearly when referring to:
  - ground
  - water
  - decoration
- Do not yet overload obstacle or canopy into unrelated references.
- Document that:
  - `Decoration` is visual-only
  - `Obstacle` is a visual support layer
  - movement blocking still comes from blocker contributions

Validation:
- Existing world generation still targets the same visible layers it used before.
- No code path assumes that obstacle visuals automatically block navigation.

### Phase 3 - Add Procedural Obstacle And Canopy Output Only When Needed
Goal:
Extend the procedural output model only when there is a real use case for authored tree and obstacle layering.

Steps:
- When procedural obstacle visuals become necessary, extend:
  - `TileResult`
  - `ChunkResult`
  - `TileResolver`
  - `TilemapApplier`
  - `WorldGenRunner` tilemap references
- Add explicit visual output channels for:
  - `Decoration`
  - `Obstacle`
  - `Canopy`
- Keep water and terrain rules unchanged.

Rules:
- Do not extend generation output just because the scene can support more tilemaps.
- Only add procedural obstacle and canopy channels once the content genuinely needs them.

Validation:
- Generated chunks can write obstacle visuals and optional canopy visuals independently.
- Existing terrain and water generation still match previous behavior.

### Phase 4 - Blocking Authoring For Trees And Obstacles
Goal:
Make tree stumps and other solid props block movement through explicit blocker data.

Steps:
- Continue using the existing blocker path:
  - `WorldBuildOutput.SiteBlockers`
  - `SiteStamping`
  - nav contribution application through navigation lifecycle
- For any procedural tree or rock that should block:
  - stamp terrain if needed
  - stamp the obstacle visual
  - stamp a blocker footprint for the stump or trunk area
- Keep canopy out of the blocker footprint.

Rules:
- Decorations never block.
- Canopy never blocks.
- Only the base footprint blocks.

Validation:
- The player cannot walk through tree trunks or rock bases.
- Enemies cannot path through the blocked footprint.
- Actors can still move underneath pure canopy visuals where intended.

### Phase 5 - Decorative Trees Versus Special Trees
Goal:
Keep the cheap path for common trees while leaving room for richer interaction later.

Decorative trees:
- tilemap visuals
- explicit blocker footprint
- no runtime state
- no prefab

Special trees:
- runtime object or site
- stateful
- can take damage
- can drop resources
- can persist chopped or intact state

Rule:
- Do not promote all trees to runtime objects.
- Only special trees should pay the runtime cost.

Validation:
- The world can contain many decorative trees cheaply.
- A smaller set of special trees can exist without changing how normal trees are authored.

### Phase 6 - Future Choppable Tree Upgrade Path
Goal:
Support chopping without invalidating the layered grid model.

Recommended structure for a choppable tree:
- keep the terrain context under it
- use a stateful world object or site for the standing tree
- represent its blocking footprint explicitly
- remove or reduce that footprint when chopped
- swap visuals from standing tree to stump or felled state
- persist the state if the tree must stay chopped across reloads

Recommended runtime states:
- intact
- damaged
- felled
- stump-only
- optional regrown later

Recommended behavior on chop:
- remove or swap canopy visuals
- remove or swap trunk visuals
- update blocker footprint
- optionally spawn drops

Validation:
- Standing tree blocks movement.
- Chopped tree no longer blocks in the same way.
- Visual state and blocker state stay in sync.
- Persistent trees restore the correct state after reload.

## Practical Build Order
Use this order when implementation begins:

1. Expand the `Grid` hierarchy in `ProceduralTiles`.
2. Validate rendering order manually with test tiles.
3. Rename or repurpose the current decor layer into `Decoration`.
4. Add `Obstacle` and `Canopy` as scene layers only.
5. Keep generation unchanged until the visual stack is stable.
6. Add blocker-authoring rules for tree stumps and rocks.
7. Extend procedural outputs for `Obstacle` and `Canopy` only when needed.
8. Add one special tree prototype only after the decorative path is proven.
9. If chopping is needed, build it as a stateful special-tree path, not as a rewrite of all trees.

## Implementation Progress

### Verified Slice 1 - Grid Foundation And Naming Cleanup
Status:
- verified

What was completed:
- expanded the `ProceduralTiles` grid hierarchy to include:
  - `Terrain`
  - `Decoration`
  - `Obstacle`
  - `Canopy`
  - `Water`
- renamed the old `Interactible` tilemap to `Decoration`
- updated `WorldGenRunner` to use `decorationMap` as the real runtime name
- added serialized scene references for:
  - `obstacleMap`
  - `canopyMap`
- renamed the current generated visual output path from `decor` to `decoration` in the runtime code

What remains intentionally unchanged:
- generation still writes only to:
  - ground
  - water
  - decoration
- `Obstacle` and `Canopy` are present in the scene but are not generator-driven yet
- blocker behavior is unchanged in this slice
- choppable trees remain deferred and are not part of the current implementation scope

Why this slice matters:
- it establishes the readable tilemap stack in the scene first
- it aligns runtime naming with the intended layer model
- it keeps the first implementation step low risk before any blocker or generator expansion begins

## Acceptance Criteria For The Plan
This plan is ready to implement when:
- the scene hierarchy is clear and stable
- visual layers are understandable by name
- blocker rules are explicit
- decorative and special trees are treated as different cost tiers
- the future choppable-tree path fits the same grid model instead of fighting it
