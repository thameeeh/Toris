# First Two Biomes Map Generation Audit

## Scope

This audit is the MapGeneration-side companion to `First_Two_Biomes_Handoff.md`.

It documents current biome build steps, spawn/site data, and procedural handoff tasks. It does not change or define enemy AI, enemy behavior, enemy authoring, or spawn-table decisions outside `MapGeneration/`.

Do not update `CHANGELOG.md` for this handoff work.

## Current Biome Build Steps

### Plains

`Generation/Data/ScriptableObjects/BiomeData/Plains/BBD_Plains.asset`

- `RoadSurfaceBuildStepDefinition.asset`
- `SitePlacementRuleBuildStepDefinition.asset`
- `PersistentSitePlacementBuildStepDefinition.asset`
- `WildlifeSpawnBuildStepDefinition.asset`

Current Plains site-placement rules:

- `PlacementRules/GateSitePlacementRuleDefinition.asset`
- `PlacementRules/WolfDenSiteRare.asset`
- `PlacementRules/WolfDenSiteCommon.asset`
- `PlacementRules/ShorelineVignetteCommonRule.asset`
- `PlacementRules/ShorelineVignetteRareRule.asset`
- `PlacementRules/ShorelineVignetteRepeatableRule.asset`

Current Plains wildlife:

- `BuildSteps/WildlifeSpawnBuildStepDefinition.asset` spawns `Wildlife/DeerWildlifeSpawnDefinition.asset`.
- `Wildlife/DeerWildlifeSpawnDefinition.asset` references `Enemy/Animations/Deer/Deer.prefab`.
- Current rule budget is 4 to 16 groups, 2 to 5 members per cluster, 40-tile spacing, 50-tile origin avoidance.

### Forest

`Generation/Data/ScriptableObjects/BiomeData/Forest/BBD_Forest.asset`

- `RoadSurfaceBuildStepDefinition.asset`
- `SitePlacementRuleBuildStepDefinition.asset`
- `PersistentSitePlacementBuildStepDefinition.asset`

Current Forest site-placement rules:

- `GateSitePlacementRuleDefinition.asset`
- `NecromancerGraveSitePlacementRuleDefinition.asset`
- `WolfDenSiteRare.asset`
- `WolfDenSiteCommon.asset`

Current Forest wildlife:

- No `WildlifeSpawnBuildStepDefinition.asset` is wired into `BBD_Forest.asset`.
- Forest wolf dens are wired as site placements, not generic wildlife spawns.

## Content Matrix

| Content | Current Biome | Current Source | Intended Biome | Intended Role | MapGeneration Notes |
| --- | --- | --- | --- | --- | --- |
| Deer | Plains | `Plains/BuildSteps/WildlifeSpawnBuildStepDefinition.asset` -> `Plains/Wildlife/DeerWildlifeSpawnDefinition.asset` | Plains primary, Forest optional background | Calm wildlife and food-chain texture | This is the only direct wildlife spawn definition currently found in Plains/Forest MapGeneration data. |
| Badger | Not wired in MapGeneration | No `Badger` references found under `MapGeneration/` | Plains and possibly Forest later | Passive/reactive wildlife | Do not add to MapGeneration spawn configs until the separate enemy/content handoff provides a stable prefab/behavior decision. |
| Boar | Not wired in MapGeneration | No `Boar` references found under `MapGeneration/` | Possible Plains light danger, maybe passive fallback | Experimental light-danger or wildlife role | Keep out of spawn configs until the separate behavior prototype is proven readable. |
| Regular wolves | Encounter-bound only | `Sites/WolfDen/WolfDenEncounterConfig.asset` references leader/minion wolf prefabs | Forest | Main hostile territorial pressure | There is no standalone Forest wildlife/patrol spawn rule for wolves yet. Keep that as a later handoff point instead of changing enemy AI here. |
| Wolf dens | Plains and Forest | `Plains/BuildSteps/SitePlacementRuleBuildStepDefinition.asset` includes `Plains/PlacementRules/WolfDenSiteRare.asset` and `Plains/PlacementRules/WolfDenSiteCommon.asset`; `Forest/BuildSteps/SitePlacementRuleBuildStepDefinition.asset` includes `Forest/PlacementRules/WolfDenSiteRare.asset` and `Forest/PlacementRules/WolfDenSiteCommon.asset` | Forest main, Plains rare or absent | Territorial sites and pack identity | Plains common wolf-den budget is 4 to 6; rare budget is 0 to 1. Forest common budget is 8 to 12; rare budget is 0 to 2. |
| Necromancer graves | Forest | `Forest/PlacementRules/NecromancerGraveSitePlacementRuleDefinition.asset` | Forest or later, rare | Optional supernatural danger | Current Forest budget is 5 to 15 graves with 40-tile spacing, 0.9 placement radius, and 32-tile origin avoidance. Do not add to Plains. |
| Blood Mages | Not directly wired in MapGeneration | No `Blood` or `BloodMage` MapGeneration spawn config found | Encounter-owned only | Necromancer-owned summon content | Keep them out of generic wildlife and biome site-placement rules. |

## MapGeneration Work Items

1. Decide the active Forest wolf-den data home.
   - First pass created `BiomeData/Forest/PlacementRules/WolfDenSiteCommon.asset` and `BiomeData/Forest/PlacementRules/WolfDenSiteRare.asset`.
   - First pass wired both into `Forest/BuildSteps/SitePlacementRuleBuildStepDefinition.asset`.
   - Rare runs before Common so rare authored sites get first placement chance.
   - Reuse `Shared/SiteDefinitions/WolfDenSiteDefinition.asset` and `Sites/WolfDen/WolfDenEncounterConfig.asset` unless separate enemy/content work asks for new runtime data.

2. Reduce Plains wolf-den pressure.
   - First pass changed the Plains wolf-den budget from 10-to-10 to 4-to-7.
   - Revisit after Boar/Badger authoring and seed sampling.

3. Keep deer as the first safe wildlife baseline.
   - Plains already has deer configured.
   - If Forest should get background deer, create a Forest wildlife build step that references the existing deer spawn definition or a Forest-specific deer spawn definition.

4. Leave Badger and Boar out of procedural configs for now.
   - Their intended roles depend on separate enemy/content behavior work.
   - MapGeneration can add spawn definitions later once those prefabs and behaviors are ready.

5. Preserve Necromancer graves as rare Forest content.
   - Do not move them into Plains.
   - After wolf dens are added to Forest, seed-sample whether 1 to 3 graves competes too much with wolf identity.

6. Resolve wolf-den stamp data before layout polish.
   - Common and rare wolf-den stamp assets now live in `Shared/SiteStamps/WolfDens/`.
   - Plains and Forest wolf-den rules both point at those shared common/rare stamp assets.
   - Add new painted layout variants to `WolfSiteStampCommon.asset` or `WolfSiteStampRare.asset`.

7. Add seed-sampling checks after the data changes.
   - Plains should show deer and quiet world texture before danger.
   - Forest should clearly read as wolf territory before rare supernatural content appears.

## Wolf Den Authoring Workflow

1. Paint each wolf-den layout into a `SiteTileLayoutDefinition`.
2. Create or select a `SiteStampDefinition` for the common den set.
3. Put common layout variants into that stamp's `Tile Layout Variants` list.
4. Create or select a separate `SiteStampDefinition` for the rare den set.
5. Put rare layout variants into that stamp's `Tile Layout Variants` list.
6. Assign or verify the common stamp on `Forest/PlacementRules/WolfDenSiteCommon.asset` and `Plains/PlacementRules/WolfDenSiteCommon.asset`.
7. Assign or verify the rare stamp on `Forest/PlacementRules/WolfDenSiteRare.asset` and `Plains/PlacementRules/WolfDenSiteRare.asset`.
8. Keep `Wolf Den Site Definition` the same on both rules so both still spawn the wolf-den runtime site.

## Authoring Folder Map

- `Plains/BuildSteps/` and `Forest/BuildSteps/`: biome pipeline entries referenced by each `BBD_*` asset.
- `Plains/PlacementRules/` and `Forest/PlacementRules/`: author-facing spawn/placement budgets such as gates, shoreline vignettes, wolf dens, and necromancer graves.
- `Shared/SiteDefinitions/`: runtime site definitions reused across biomes, including gates and wolf dens.
- `Shared/SiteStamps/WolfDens/`: common and rare wolf-den stamp assets where painted wolf layout variants should be assigned.
- `Forest/SiteStamps/`: Forest-only stamp assets, currently the necromancer grave stamp.
- `Forest/EncounterConfigs/`: Forest site runtime configs, currently the necromancer grave config.
- `Plains/Wildlife/`: Plains wildlife spawn definitions, currently deer.

## Open Questions

- Should Forest get background deer immediately using the existing deer spawn definition, or should it wait until wolf-den placement is moved first?
