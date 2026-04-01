public sealed class WorldBuildOutput
{
    private readonly FeatureStamps terrainOverrides = new();
    private readonly SitePlacementIndex sitePlacements = new();
    private readonly SiteBlockerMap siteBlockers = new();
    private readonly RoadAnchorMap roadAnchors = new();

    public FeatureStamps TerrainOverrides => terrainOverrides;
    public SitePlacementIndex SitePlacements => sitePlacements;
    public SiteBlockerMap SiteBlockers => siteBlockers;
    public ITileNavigationContributionSource NavigationContributions => siteBlockers;
    public RoadAnchorMap RoadAnchors => roadAnchors;

    public void Clear()
    {
        terrainOverrides.Clear();
        sitePlacements.Clear();
        siteBlockers.Clear();
        roadAnchors.Clear();
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
