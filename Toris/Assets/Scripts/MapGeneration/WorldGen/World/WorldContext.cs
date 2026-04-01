using UnityEngine;

public sealed class WorldContext
{
    public readonly WorldProfile World;

    public BiomeInstance ActiveBiome { get; private set; }
    public BiomeDefinition ActiveDef { get; private set; }
    public BiomeProfile Biome => ActiveDef != null ? ActiveDef.profile : null;

    public NoiseContext Noise { get; private set; }
    public BiomeMask Mask { get; private set; }
    public WorldBuildOutput BuildOutput { get; private set; }
    public FeatureStamps Stamps => BuildOutput != null ? BuildOutput.TerrainOverrides : null;
    public SiteBlockerMap SiteBlockers => BuildOutput != null ? BuildOutput.SiteBlockers : null;
    public ITileNavigationContributionSource NavigationContributions => BuildOutput != null ? BuildOutput.NavigationContributions : null;
    public RoadAnchorMap RoadAnchors => BuildOutput != null ? BuildOutput.RoadAnchors : null;
    public SitePlacementIndex SitePlacements => BuildOutput != null ? BuildOutput.SitePlacements : null;

    public AnimationCurve DangerCurve => World.dangerCurve;

    public WorldContext(WorldProfile world)
    {
        World = world;

        Mask = new BiomeMask();
        BuildOutput = new WorldBuildOutput();

        ActiveBiome = new BiomeInstance(0, World.seed, Vector2Int.zero, world.worldRadiusTiles);
        Noise = new NoiseContext(ActiveBiome.Seed);
    }

    public void BindBiome(BiomeDefinition def, BiomeInstance biome)
    {
        ActiveDef = def;
        ActiveBiome = biome;
        Noise = new NoiseContext(biome.Seed);

        BuildOutput.Clear();

        ActiveDef?.BuildFeatures(this);
    }

    public void RegisterSite(
        WorldSiteDefinition siteDefinition,
        Vector2Int centerTile,
        SitePlacementLifecycleScope lifecycleScope = SitePlacementLifecycleScope.Chunk)
    {
        if (BuildOutput == null)
            return;

        int chunkSize = Mathf.Max(1, World.chunkSize);
        BuildOutput.RegisterSite(siteDefinition, centerTile, chunkSize, lifecycleScope);
    }
}

public struct WorldSignals
{
    public float dist01;
    public float danger01;

    public float vegetation01;
    public float variation01;
    public float lake01;

    public float road01;
}
