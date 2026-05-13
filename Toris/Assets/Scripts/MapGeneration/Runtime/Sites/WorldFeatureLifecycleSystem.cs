using UnityEngine;

public sealed class WorldFeatureLifecycleSystem
{
    private readonly WorldFeatureLifecycle chunkFeatureLifecycle;
    private readonly PersistentWorldFeatureLifecycle persistentFeatureLifecycle;
    private readonly WorldWildlifeLifecycle wildlifeLifecycle;

    public WorldFeatureLifecycleSystem(
        WorldFeatureLifecycle chunkFeatureLifecycle,
        PersistentWorldFeatureLifecycle persistentFeatureLifecycle,
        WorldWildlifeLifecycle wildlifeLifecycle)
    {
        this.chunkFeatureLifecycle = chunkFeatureLifecycle;
        this.persistentFeatureLifecycle = persistentFeatureLifecycle;
        this.wildlifeLifecycle = wildlifeLifecycle;
    }

    public void RebuildForCurrentBiome()
    {
        chunkFeatureLifecycle?.ClearAll();
        persistentFeatureLifecycle?.ClearAll();
        wildlifeLifecycle?.ClearAll();

        chunkFeatureLifecycle?.RebuildPlacements();
        persistentFeatureLifecycle?.RebuildPlacements();
        persistentFeatureLifecycle?.ActivatePersistentSites();
        wildlifeLifecycle?.RebuildPlacements();
    }

    public void ClearAll()
    {
        chunkFeatureLifecycle?.ClearAll();
        persistentFeatureLifecycle?.ClearAll();
        wildlifeLifecycle?.ClearAll();
    }

    public void ActivateChunk(Vector2Int chunkCoord)
    {
        chunkFeatureLifecycle?.ActivateChunk(chunkCoord);
        wildlifeLifecycle?.ActivateChunk(chunkCoord);
    }

    public void DeactivateChunk(Vector2Int chunkCoord)
    {
        wildlifeLifecycle?.DeactivateChunk(chunkCoord);
        chunkFeatureLifecycle?.DeactivateChunk(chunkCoord);
    }

    public void Tick(float deltaTime)
    {
        wildlifeLifecycle?.Tick(deltaTime);
    }

    public int GetActiveSiteChunkCount()
    {
        return chunkFeatureLifecycle != null ? chunkFeatureLifecycle.GetActiveSiteChunkCount() : 0;
    }

    public int GetActiveChunkSiteCount()
    {
        return chunkFeatureLifecycle != null ? chunkFeatureLifecycle.GetActiveSiteCount() : 0;
    }

    public int GetActivePersistentSiteCount()
    {
        return persistentFeatureLifecycle != null ? persistentFeatureLifecycle.GetActiveSiteCount() : 0;
    }

    public int GetActiveSiteCount()
    {
        return GetActiveChunkSiteCount() + GetActivePersistentSiteCount();
    }

    public int GetTotalPlacedSiteCount()
    {
        return chunkFeatureLifecycle != null ? chunkFeatureLifecycle.GetTotalPlacedSiteCount() : 0;
    }

    public LifecycleDiagnosticsSnapshot CreateDiagnosticsSnapshot()
    {
        return new LifecycleDiagnosticsSnapshot(
            GetActiveSiteChunkCount(),
            GetActivePersistentSiteCount(),
            GetActiveSiteCount(),
            GetTotalPlacedSiteCount());
    }
}
