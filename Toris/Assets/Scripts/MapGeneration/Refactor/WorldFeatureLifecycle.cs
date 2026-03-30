using System.Collections.Generic;
using UnityEngine;

public sealed class WorldFeatureLifecycle
{
    private readonly WorldContext worldContext;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly WorldSiteActivationPipeline worldSiteActivationPipeline;
    private readonly WorldFeatureOwnershipCollection<Vector2Int> chunkOwnership;

    private SitePlacementIndex sitePlacementIndex;

    private readonly Dictionary<Vector2Int, List<ActiveSiteHandle>> activeSitesByChunk =
        new Dictionary<Vector2Int, List<ActiveSiteHandle>>();

    public WorldFeatureLifecycle(
        WorldContext worldContext,
        WorldPoiPoolManager poiPoolManager,
        WorldSiteActivationPipeline worldSiteActivationPipeline)
    {
        this.worldContext = worldContext;
        this.poiPoolManager = poiPoolManager;
        this.worldSiteActivationPipeline = worldSiteActivationPipeline;

        chunkOwnership = new WorldFeatureOwnershipCollection<Vector2Int>(
            poiPoolManager,
            "WorldFeatureLifecycle_Root",
            chunkCoord => $"ChunkSites_{chunkCoord.x}_{chunkCoord.y}");
    }

    public void RebuildPlacements()
    {
        sitePlacementIndex = worldContext != null ? worldContext.SitePlacements : null;
    }

    public void ClearAll()
    {
        activeSitesByChunk.Clear();
        chunkOwnership.ClearAll();
        sitePlacementIndex = null;
    }

    public void ActivateChunk(Vector2Int chunkCoord)
    {
        if (sitePlacementIndex == null)
        {
            return;
        }

        if (activeSitesByChunk.ContainsKey(chunkCoord))
        {
            return;
        }

        if (!sitePlacementIndex.TryGetChunk(chunkCoord, out List<SitePlacement> placements) || placements == null || placements.Count == 0)
        {
            return;
        }

        WorldFeatureOwnershipGroup ownershipGroup = chunkOwnership.GetOrCreateGroup(chunkCoord);
        Transform chunkRoot = ownershipGroup.Root;
        List<ActiveSiteHandle> activeSites = new List<ActiveSiteHandle>(placements.Count);

        for (int i = 0; i < placements.Count; i++)
        {
            SitePlacement placement = placements[i];
            GameObject instance = SpawnSiteInstance(placement, chunkRoot);

            if (instance != null)
            {
                ownershipGroup.AddInstance(instance);
                activeSites.Add(new ActiveSiteHandle(placement, instance));
            }
        }

        if (activeSites.Count > 0)
        {
            activeSitesByChunk.Add(chunkCoord, activeSites);
        }
    }

    public void DeactivateChunk(Vector2Int chunkCoord)
    {
        if (!activeSitesByChunk.ContainsKey(chunkCoord))
        {
            return;
        }

        activeSitesByChunk.Remove(chunkCoord);
        chunkOwnership.RemoveAndClearGroup(chunkCoord);
    }
    private GameObject SpawnSiteInstance(SitePlacement placement, Transform parent)
    {
        if (worldSiteActivationPipeline == null || worldContext == null)
            return null;

        return worldSiteActivationPipeline.ActivateSite(
            placement,
            parent,
            worldContext.ActiveBiome.Seed);
    }

    public int GetActiveSiteChunkCount()
    {
        return activeSitesByChunk.Count;
    }

    public int GetActiveSiteCount()
    {
        int totalCount = 0;

        foreach (KeyValuePair<Vector2Int, List<ActiveSiteHandle>> pair in activeSitesByChunk)
        {
            totalCount += pair.Value.Count;
        }

        return totalCount;
    }

    public int GetTotalPlacedSiteCount()
    {
        return sitePlacementIndex != null ? sitePlacementIndex.All.Count : 0;
    }
}
