# Boar Map Generation Handoff

This is the short handoff for wiring Boar into map generation.

Boar is a wildlife hazard, not a normal hostile camp enemy. It should be placed like wildlife, but tuned more sparsely than Deer because one Boar can damage and displace the player.

## Intended Spawn Role

- Plains: rare solo danger inside otherwise quiet wildlife space.
- Forest: optional uncommon wildlife danger if wolves are not already making the area too busy.
- Do not spawn Boar as a den/camp encounter.
- Prefer solo Boar first. Add pairs only after the single-Boar behavior feels stable.

The player should read the Boar as part of the living world: mostly calm, briefly dangerous when approached, then gone from immediate combat.

## Assets To Wire

Use the existing wildlife spawn lane:

- create a `WorldGen/Wildlife/Wildlife Spawn Definition` asset for Boar
- assign the Boar prefab to `Enemy Prefab`
- add the Boar prefab to the relevant `GameplayPoolConfiguration` enemy pool
- add a Boar wildlife rule to the target biome wildlife build step
- keep the wildlife build step after site, road, blocker, and obstacle placement

The Boar does not currently need `IWildlifeGroupMember`. If a spawn definition uses group ids anyway, treat them only as placement grouping, not herd behavior.

## Conservative Starting Values

Start low and open it up after testing:

- `Min Group Count = 0`
- `Max Group Count = 1`
- `Min Cluster Size = 1`
- `Max Cluster Size = 1`
- `Cluster Radius Tiles = 2`
- `Min Cluster Member Spacing Tiles = 3`
- `Min Spacing Tiles = 24`
- `Placement Radius Factor = 0.85`
- `Avoid Origin Radius Tiles = 20`
- `Avoid Terrain Overrides = true`
- `Avoid Navigation Blockers = true`
- `Avoid Obstacles = true`

If pairs become desirable later:

- keep `Min Cluster Size = 1`
- raise `Max Cluster Size` only to `2`
- raise `Min Cluster Member Spacing Tiles` to `4` or more
- keep `Min Spacing Tiles` high so charges do not overlap too often

## Placement Requirements

Boar behavior assumes the spawn tile is valid for navigation.

Check that generated Boar placements:

- are on walkable tiles
- are not on water unless the biome explicitly wants that
- are not inside road tiles
- are not inside POI stamp blockers
- are not inside obstacle colliders
- have enough nearby walkable space for wander, charge, and flee

If a Boar spawns on a bad tile, `GridPathAgent` and the Boar behavior can look broken even when the enemy code is fine.

## Test Checklist

After wiring the rule, test a few generated seeds:

- Boar spawns in the expected biome only.
- Boar does not spawn in view at world origin.
- Boar does not stack with Deer or other wildlife clusters.
- Boar does not spawn on water, blockers, roads, or POIs.
- Boar wanders normally after chunk load.
- Boar charges only when the player enters its aggro range.
- Boar continues through the player and flees along the same direction.
- Chunk unload despawns and returns the Boar through the pool.
- Reused pooled Boar has fresh health, fresh sprite alpha, and no stale charge/flee state.

## Tuning Notes

Use spawn density to control pressure before touching combat values.

If the biome feels too dangerous, reduce Boar count or spacing first. If the Boar feels individually weak, tune the Boar prefab/SO values second.

For Plains, Boar should be an occasional surprise. For Forest, it can be more common only if wolves are rare enough that the biome still has breathing room.
