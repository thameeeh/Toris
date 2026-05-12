# Procedural World Extras Integration Roadmap

## Purpose
This roadmap defines a practical content order for making the procedural world feel more authored while keeping the current map-generation architecture intact.

The world already supports:
- five visual tile layers: `Ground`, `Water`, `Decoration`, `Obstacle`, and `Canopy`
- painted layout baking through `SiteLayoutAuthoringRoot`
- reusable `SiteTileLayoutDefinition` assets
- deterministic placement rules
- runtime world sites for stateful or interactive objects

The next work should focus on adding better authored content and wiring it through existing placement paths, not creating a second world feature system.

## Core Rules
- Paint visuals on the correct layer in the layout authoring tool.
- Use `Decoration` for non-blocking visual detail.
- Use `Obstacle` for physical base visuals that should occupy space.
- Use `Canopy` only for tall overlap visuals.
- Use `SiteStampDefinition` and `SiteTileLayoutDefinition` for authored tile layouts.
- Use `WorldSiteDefinition` prefabs only when the feature needs interaction, state, enemies, transitions, or runtime behavior.
- Keep broad natural shapes procedural, then add authored pockets on top.

## 1. Author Wolf Den Layouts First
Wolf dens should be the first expanded world extra because they already exist as real runtime sites and already have a placement rule.

Implementation direction:
- bake several wolf den `SiteTileLayoutDefinition` variants
- create or assign a `SiteStampDefinition` for wolf dens
- assign the stamp to `WolfDenSitePlacementRuleDefinition`
- keep the `WolfDen` prefab responsible for health, collapsed state, and encounter logic
- let the painted layout handle ground, den surroundings, clutter, rocks, bones, and canopy framing

Design goals:
- dens should feel like intentional clearings rather than a simple stamped square
- each den should have a readable anchor for the prefab
- layouts should leave enough navigable space for wolves to spawn and move

## 2. Add More Wolf Den Variants
After the first wolf den stamp works, build variety through data instead of new code.

Suggested variants:
- open clearing den
- forest-edge den with canopy framing
- rocky den with obstacle clusters
- bone-litter den
- overgrown den with heavy decoration

Tune:
- `minWolfDenCount` and `maxWolfDenCount`
- `wolfDenMinSpacingTiles`
- visual clear zone size
- blocker footprint, only if the authored den needs extra blocked space

## 3. Expand Roadside Vignettes
Roadside layouts should come after wolf dens because the road surface and roadside placement rule already exist.

Implementation direction:
- bake small roadside `SiteTileLayoutDefinition` variants
- assign them to `RoadsideVignettePlacementRuleDefinition`
- use counted layouts for memorable details
- use fill mode for tiny repeated roadside dressing

Suggested content:
- broken signpost
- abandoned cart marks
- campfire remains
- flower or grass shoulder
- small stone cluster
- warning stakes or bones near dangerous biomes

## 4. Build Roadside Density In Layers
Roads should not become noisy all at once. Use layered placement.

Recommended setup:
- rare roadside features with high spacing
- common roadside details with medium spacing
- repeatable filler with low spacing and conservative chance

This keeps the road readable while making long stretches less empty.

## 5. Continue Lake Edge Content
Lake-edge vignettes are already supported, so the main work here is content volume.

Implementation direction:
- keep generic lakes procedural
- add more authored edge layouts
- use rare/common/repeatable shoreline rule layers
- clip land-side content against generated lake water as the current shoreline rule already does

Suggested content:
- reed banks
- flower banks
- fallen log banks
- fishing nook
- small shrine edge
- muddy stone edge
- bone or danger-bank variant for hostile biomes

## 6. Add Small Non-Site Wilderness Dressing
Once roads and lakes have better authored pockets, add lightweight wilderness layouts that do not spawn prefabs.

Implementation direction:
- create visual-only placement rules only when existing shoreline/roadside rules do not fit
- prefer `SiteTileLayoutDefinition` stamps for clusters of decoration, rocks, logs, and canopy pockets
- avoid runtime site prefabs unless the feature needs state

Suggested content:
- small ruin fragments
- flower patches
- mushroom patches
- tree clusters
- rock gardens
- abandoned supply scraps

## 7. Add Rare Special World Sites
After the world has enough visual texture, add a small number of rare interactive or stateful sites.

Good candidates:
- shrine
- ruined camp
- mini boss grave
- treasure cache
- cursed tree
- hermit hut

Integration direction:
- create a `WorldSiteDefinition`
- create a runtime config only if the site needs tunable behavior
- author a layout stamp for the surrounding tiles
- register the site through a placement rule
- use `WorldSiteContext` for runtime services and state

## 8. Add Biome-Specific Identity Passes
When the general extras feel good, split content by biome personality.

Examples:
- Plains: flowers, low grass, open road shoulders, small camps
- Forest: heavier canopy, roots, trunks, grave-like clearings, dense lake edges
- Later hostile biomes: bones, corrupted plants, broken ruins, harsher blockers

The goal is not just more decoration, but different silhouettes and composition rules per biome.

## 9. Review Navigation And Encounter Space
After authored extras are placed, review them in play mode or through static scene inspection.

Check:
- wolf dens have enough walkable space around the prefab
- roads remain readable and traversable
- lake edges do not trap the player
- obstacle clusters do not block required routes
- wildlife spawn rules still avoid obstacles and stamped sites correctly

If a layout looks good but hurts movement, fix the painted layout first before changing placement code.

## 10. Tune Counts Last
Only tune density after the content families exist.

Recommended order:
1. make a few good layouts
2. wire them into placement rules
3. verify they appear
4. add variants
5. tune counts, spacing, radius, and fill chance

This avoids over-tuning sparse placeholder content and keeps the world direction content-led.

## Suggested Immediate Next Task
Create the first authored wolf den stamp:
- paint one den layout
- bake it into a `SiteTileLayoutDefinition`
- create a `SiteStampDefinition` that uses the layout
- assign it to `WolfDenSitePlacementRuleDefinition`
- tune the wolf den min/max count range
- verify that wolf den placement still spawns the runtime prefab at the intended anchor
