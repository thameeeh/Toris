public sealed class WorldBuildOutput
{
    private readonly FeatureStamps terrainOverrides = new();
    private readonly SitePlacementIndex sitePlacements = new();
    private readonly SiteBlockerMap siteBlockers = new();
    private readonly SiteVisualClearMap siteVisualClears = new();
    private readonly RoadAnchorMap roadAnchors = new();
    private readonly WildlifeSpawnPlacementIndex wildlifeSpawns = new();

    public FeatureStamps TerrainOverrides => terrainOverrides;
    public SitePlacementIndex SitePlacements => sitePlacements;
    public SiteBlockerMap SiteBlockers => siteBlockers;
    public SiteVisualClearMap SiteVisualClears => siteVisualClears;
    public ITileNavigationContributionSource NavigationContributions => siteBlockers;
    public RoadAnchorMap RoadAnchors => roadAnchors;
    public WildlifeSpawnPlacementIndex WildlifeSpawns => wildlifeSpawns;

    public void Clear()
    {
        terrainOverrides.Clear();
        sitePlacements.Clear();
        siteBlockers.Clear();
        siteVisualClears.Clear();
        roadAnchors.Clear();
        wildlifeSpawns.Clear();
    }

    public void RegisterSite(
        WorldSiteDefinition siteDefinition,
        UnityEngine.Vector2Int centerTile,
        int chunkSize,
        SitePlacementLifecycleScope lifecycleScope = SitePlacementLifecycleScope.Chunk)
    {
        if (siteDefinition == null || !siteDefinition.IsValid)
            return;

        sitePlacements.Add(SitePlacement.Create(
            siteDefinition,
            centerTile,
            chunkSize,
            lifecycleScope));
    }

    public void RegisterWildlifeSpawn(
        WorldWildlifeSpawnDefinition spawnDefinition,
        UnityEngine.Vector2Int centerTile,
        int chunkSize)
    {
        if (spawnDefinition == null || !spawnDefinition.IsValid)
            return;

        wildlifeSpawns.Add(WildlifeSpawnPlacement.Create(
            spawnDefinition,
            centerTile,
            chunkSize));
    }

    public BuildOutputDiagnosticsSnapshot CreateDiagnosticsSnapshot()
    {
        return new BuildOutputDiagnosticsSnapshot(
            terrainOverrides.OverrideCount,
            sitePlacements.All.Count,
            sitePlacements.ChunkPlacementCount,
            sitePlacements.PersistentPlacementCount,
            siteBlockers.BlockedTileCount,
            roadAnchors.GateAnchorCount);
    }
}
