public readonly struct BuildOutputDiagnosticsSnapshot
{
    public readonly int TerrainOverrideCount;
    public readonly int TotalPlacementCount;
    public readonly int ChunkPlacementCount;
    public readonly int PersistentPlacementCount;
    public readonly int NavigationContributionCount;
    public readonly int RoadAnchorCount;

    public BuildOutputDiagnosticsSnapshot(
        int terrainOverrideCount,
        int totalPlacementCount,
        int chunkPlacementCount,
        int persistentPlacementCount,
        int navigationContributionCount,
        int roadAnchorCount)
    {
        TerrainOverrideCount = terrainOverrideCount;
        TotalPlacementCount = totalPlacementCount;
        ChunkPlacementCount = chunkPlacementCount;
        PersistentPlacementCount = persistentPlacementCount;
        NavigationContributionCount = navigationContributionCount;
        RoadAnchorCount = roadAnchorCount;
    }
}
