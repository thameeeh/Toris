public readonly struct LifecycleDiagnosticsSnapshot
{
    public readonly int ActiveSiteChunkCount;
    public readonly int ActivePersistentSiteCount;
    public readonly int ActiveSiteCount;
    public readonly int TotalPlacedSiteCount;

    public LifecycleDiagnosticsSnapshot(
        int activeSiteChunkCount,
        int activePersistentSiteCount,
        int activeSiteCount,
        int totalPlacedSiteCount)
    {
        ActiveSiteChunkCount = activeSiteChunkCount;
        ActivePersistentSiteCount = activePersistentSiteCount;
        ActiveSiteCount = activeSiteCount;
        TotalPlacedSiteCount = totalPlacedSiteCount;
    }
}
