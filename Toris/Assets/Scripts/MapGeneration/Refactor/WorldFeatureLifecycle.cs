using System.Collections.Generic;
using UnityEngine;

public sealed class WorldFeatureLifecycle
{
    private readonly WorldContext worldContext;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly WorldSiteActivationPipeline worldSiteActivationPipeline;

    private SitePlacementIndex sitePlacementIndex;

    private readonly Dictionary<Vector2Int, List<ActiveSiteHandle>> activeSitesByChunk =
        new Dictionary<Vector2Int, List<ActiveSiteHandle>>();

    private readonly Dictionary<Vector2Int, Transform> chunkSiteRoots =
        new Dictionary<Vector2Int, Transform>();

    private Transform siteRootContainer;

    public WorldFeatureLifecycle(
        WorldContext worldContext,
        WorldPoiPoolManager poiPoolManager,
        WorldSiteActivationPipeline worldSiteActivationPipeline)
    {
        this.worldContext = worldContext;
        this.poiPoolManager = poiPoolManager;
        this.worldSiteActivationPipeline = worldSiteActivationPipeline;
    }

    public void RebuildPlacements()
    {
        sitePlacementIndex = worldContext != null ? worldContext.SitePlacements : null;
    }

    public void ClearAll()
    {
        foreach (var pair in activeSitesByChunk)
        {
            List<ActiveSiteHandle> activeSites = pair.Value;
            for (int i = 0; i < activeSites.Count; i++)
            {
                ReleaseSiteInstance(activeSites[i].Instance);
            }
        }

        activeSitesByChunk.Clear();

        foreach (var pair in chunkSiteRoots)
        {
            Transform chunkRoot = pair.Value;
            if (chunkRoot != null)
            {
                Object.Destroy(chunkRoot.gameObject);
            }
        }

        chunkSiteRoots.Clear();
        sitePlacementIndex = null;

        if (siteRootContainer != null)
        {
            Object.Destroy(siteRootContainer.gameObject);
            siteRootContainer = null;
        }
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

        Transform chunkRoot = GetOrCreateChunkSiteRoot(chunkCoord);
        List<ActiveSiteHandle> activeSites = new List<ActiveSiteHandle>(placements.Count);

        for (int i = 0; i < placements.Count; i++)
        {
            SitePlacement placement = placements[i];
            GameObject instance = SpawnSiteInstance(placement, chunkRoot);

            if (instance != null)
            {
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
        if (!activeSitesByChunk.TryGetValue(chunkCoord, out List<ActiveSiteHandle> activeSites))
        {
            return;
        }

        for (int i = 0; i < activeSites.Count; i++)
        {
            ReleaseSiteInstance(activeSites[i].Instance);
        }

        activeSitesByChunk.Remove(chunkCoord);

        if (chunkSiteRoots.TryGetValue(chunkCoord, out Transform chunkRoot))
        {
            if (chunkRoot != null)
            {
                Object.Destroy(chunkRoot.gameObject);
            }

            chunkSiteRoots.Remove(chunkCoord);
        }
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
    private void ReleaseSiteInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (poiPoolManager != null)
        {
            poiPoolManager.Release(instance);
            return;
        }

        Object.Destroy(instance);
    }

    private Transform GetOrCreateChunkSiteRoot(Vector2Int chunkCoord)
    {
        if (chunkSiteRoots.TryGetValue(chunkCoord, out Transform existingRoot) && existingRoot != null)
        {
            return existingRoot;
        }

        if (siteRootContainer == null)
        {
            GameObject rootObject = new GameObject("WorldFeatureLifecycle_Root");
            siteRootContainer = rootObject.transform;
        }

        GameObject chunkRootObject = new GameObject($"ChunkSites_{chunkCoord.x}_{chunkCoord.y}");
        Transform chunkRoot = chunkRootObject.transform;
        chunkRoot.SetParent(siteRootContainer, false);

        chunkSiteRoots.Add(chunkCoord, chunkRoot);
        return chunkRoot;
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