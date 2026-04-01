public readonly struct NavigationDiagnosticsSnapshot
{
    public readonly int LoadedNavChunkCount;
    public readonly bool NavigationContributionsBound;

    public NavigationDiagnosticsSnapshot(
        int loadedNavChunkCount,
        bool navigationContributionsBound)
    {
        LoadedNavChunkCount = loadedNavChunkCount;
        NavigationContributionsBound = navigationContributionsBound;
    }
}
