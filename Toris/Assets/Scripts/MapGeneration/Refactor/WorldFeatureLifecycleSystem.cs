using UnityEngine;

public sealed class WorldFeatureLifecycleSystem
{
    private readonly WorldContext worldContext;
    private readonly WorldFeatureLifecycle chunkFeatureLifecycle;
    private readonly PersistentWorldFeatureLifecycle persistentFeatureLifecycle;

    public WorldFeatureLifecycleSystem(
        WorldContext worldContext,
        WorldFeatureLifecycle chunkFeatureLifecycle,
        PersistentWorldFeatureLifecycle persistentFeatureLifecycle)
    {
        this.worldContext = worldContext;
        this.chunkFeatureLifecycle = chunkFeatureLifecycle;
        this.persistentFeatureLifecycle = persistentFeatureLifecycle;
    }

    public void RebuildForCurrentBiome()
    {
        chunkFeatureLifecycle?.ClearAll();
        chunkFeatureLifecycle?.RebuildPlacements();
        persistentFeatureLifecycle?.ClearAll();
        ActivatePersistentFeaturesForCurrentBiome();
    }

    public void ClearAll()
    {
        chunkFeatureLifecycle?.ClearAll();
        persistentFeatureLifecycle?.ClearAll();
    }

    public void ActivateChunk(Vector2Int chunkCoord)
    {
        chunkFeatureLifecycle?.ActivateChunk(chunkCoord);
    }

    public void DeactivateChunk(Vector2Int chunkCoord)
    {
        chunkFeatureLifecycle?.DeactivateChunk(chunkCoord);
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

    private void ActivatePersistentFeaturesForCurrentBiome()
    {
        if (worldContext?.Biome == null || persistentFeatureLifecycle == null)
            return;

        PersistentBiomeFeatureDefinition[] persistentFeatures = worldContext.Biome.PersistentFeatures;
        if (persistentFeatures == null || persistentFeatures.Length == 0)
            return;

        Vector2Int biomeOriginTile = worldContext.ActiveBiome.OriginTile;

        for (int i = 0; i < persistentFeatures.Length; i++)
        {
            PersistentBiomeFeatureDefinition persistentFeature = persistentFeatures[i];
            if (!persistentFeature.IsValid)
                continue;

            Vector2Int centerTile = biomeOriginTile + persistentFeature.TileOffsetFromBiomeOrigin;
            persistentFeatureLifecycle.ActivatePersistentSite(
                persistentFeature.SiteDefinition,
                centerTile);
        }
    }
}
