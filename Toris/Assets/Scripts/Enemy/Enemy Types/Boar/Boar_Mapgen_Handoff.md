# Boar Map Generation Handoff

This is the short handoff for wiring Boar into map generation.

Boar is a wildlife hazard, not a normal hostile camp enemy. It should be placed like wildlife, but tuned more sparsely than Deer because one Boar can damage and displace the player.

## First Pass Decision

Use Boar Oasis sites for the first Plains Boar pass, not free-scattered wildlife.

The Boar should read as a territorial animal living around a small authored tree pocket. MapGeneration places the pocket and spawns the Boar from that site. The Boar should not be a camp enemy, den enemy, or random wildlife herd.

Use the site/stamp lane:

- create a Plains `BoarOasisSitePlacementRuleDefinition`
- create `MapGeneration/Generation/Data/ScriptableObjects/BiomeData/Plains/SiteDefinitions/BoarOasisSiteDefinition.asset`
- create Common and Rare Boar Oasis stamp assets under `MapGeneration/Generation/Data/ScriptableObjects/BiomeData/Plains/SiteStamps/BoarOasis/`
- create a Boar Oasis runtime config that references `Enemy/Enemy Types/Boar/Boar.prefab`
- add Rare and Common Boar Oasis rules to `MapGeneration/Generation/Data/ScriptableObjects/BiomeData/Plains/BuildSteps/SitePlacementRuleBuildStepDefinition.asset`
- keep the normal Plains wildlife build step for Deer only
- use an invisible spawner-like site prefab; all visible trees, ground, decoration, and obstacle content should come from the painted layout
- treat the painted layout origin as the invisible Boar spawner point

Initial Plains Common Boar Oasis rule:

- `Min Site Count = 4`
- `Max Site Count = 8`
- `Min Spacing Tiles = 56`
- `Placement Radius Factor = 0.85`
- `Avoid Origin Radius Tiles = 50`
- `Avoid Existing Stamps = true`
- `Avoid Existing Sites = true`
- `Avoid Road Tiles = true`
- `Road Spacing Tiles = 0`
- `Avoid Terrain Overrides = true`
- `Avoid Navigation Blockers = true`
- `Avoid Obstacles = true`

Initial Plains Rare Boar Oasis rule:

- `Min Site Count = 1`
- `Max Site Count = 2`
- `Min Spacing Tiles = 84`
- `Placement Radius Factor = 0.85`
- `Avoid Origin Radius Tiles = 50`
- `Avoid Existing Stamps = true`
- `Avoid Existing Sites = true`
- `Avoid Road Tiles = true`
- `Road Spacing Tiles = 0`
- `Avoid Terrain Overrides = true`
- `Avoid Navigation Blockers = true`
- `Avoid Obstacles = true`

The count should stay configurable. Current Plains tuning targets `5-10` Boar Oasis sites total, making Boar the more common Plains danger pressure while wolves stay lighter.

Common uses `4-8`; Rare uses `1-2`, so rare layouts can appear reliably without competing with the common oasis set.

Boar Oasis may be near roads, lakes, and shorelines, but it must not stamp on top of roads or intersect roads. Lakes and shoreline proximity are allowed.

Initial Boar Oasis occupant rule:

- `Min Boar Count = 2`
- `Max Boar Count = 2`
- `Spawn Radius = 3`
- `Home Radius = 8`
- `Respawn Delay = 60`
- `Keep Chasing On Unload = true`
- `Keep Chase If Within Player Range = 40`

Boars should spawn on walkable tiles picked inside the spawn radius around the painted layout origin, not blindly at the visual center of the oasis.

The Oasis is a persistent habitat marker. It should not become consumed/cleared when a Boar dies. If a Boar dies, the site should respawn a replacement Boar after the configured respawn delay.

The existing site encounter infrastructure already has a `WorldEncounterOccupantPolicy` with spawn radius, home radius, unload behavior, and respawn delay. Boar Oasis should reuse that shape, but the Boar AI still needs explicit home-return behavior if it should always drift back after charge/flee. MapGeneration can provide the site origin and home radius; Boar behavior must consume that home anchor.

Cross-wildlife spacing is less urgent if Boar uses site placement instead of the wildlife list, but the Oasis placement rule should still avoid existing Deer wildlife placements if Deer are generated earlier than Boar in the future. With the current Plains build order, sites are placed before wildlife, so Deer will already avoid the Oasis if the Oasis stamps terrain/blockers and wildlife keeps `Avoid Terrain Overrides`, `Avoid Navigation Blockers`, and `Avoid Obstacles` enabled.

## MapGeneration Implementation Status

Implemented on the MapGeneration side:

- added dedicated `BoarOasisSitePlacementRuleDefinition`
- added invisible `BoarOasisSite` spawner prefab
- added `BoarOasisEncounterConfig` with configurable Boar count and occupant policy
- added Plains `BoarOasisSiteDefinition`
- added Plains `BoarOasisSiteStampCommon` and `BoarOasisSiteStampRare` stamp assets
- added Plains `BoarOasisSiteCommon` and `BoarOasisSiteRare` placement rule assets
- wired Rare before Common in the Plains `SitePlacementRuleBuildStepDefinition`
- guarded Boar Oasis placement so empty stamp assets do not spawn invisible Boar sites

Authoring handoff:

- paint Boar Oasis layouts with the existing layout tool
- add ordinary Boar Oasis layouts to `BoarOasisSiteStampCommon`
- add more memorable or unusual Boar Oasis layouts to `BoarOasisSiteStampRare`
- keep layout offset `(0,0)` as the invisible Boar spawner point
- keep layout offset `(0,0)` walkable, because the invisible site spawner sits there
- roads may be nearby, but the painted footprint should not overlap road tiles

Runtime behavior now handled by MapGeneration:

- spawns Boars from the invisible site object
- picks nearby walkable spawn tiles inside the configured spawn radius
- respawns dead Boars after the configured delay
- adds/updates `HomeAnchor` on spawned Boars with the site origin and home radius
- releases or detaches spawned Boars when the site unloads using the occupant policy

## Implementation Shape Note

Final implementation shape: create a dedicated `BoarOasisSitePlacementRuleDefinition`.

Reason: the current need is specific and small. A dedicated rule can keep the serialized fields obvious in the Inspector: site count, road avoidance, origin avoidance, stamp/layout reference, and Boar Oasis site definition. If future habitats need the same shape, extract a reusable `AuthoredHabitatSitePlacementRuleDefinition` later.

## Enemy Team Handoff Note

MapGeneration can provide a site origin and a home radius through the Boar Oasis runtime site/spawner. The Boar behavior still needs Enemy-side home behavior.

Reuse the existing `HomeAnchor` pattern from wolf den occupants:

- add or assign a `HomeAnchor` to spawned Boars
- set `HomeAnchor.Center` to the Boar Oasis site origin
- set `HomeAnchor.Radius` from the Boar Oasis occupant policy
- make Boar wander prefer the home area while calm
- after charge/flee resolves, make Boar return toward the home area instead of drifting indefinitely
- do not make MapGeneration own the AI decision; MapGeneration should only pass the site/home data

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
