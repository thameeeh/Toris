using System.Collections.Generic;
using UnityEngine;

public sealed class WorldFeatureLifecycle
{
    private readonly WorldContext worldContext;
    private readonly WorldSiteActivationPipeline worldSiteActivationPipeline;
    private readonly WorldFeatureOwnershipCollection<Vector2Int> chunkOwnership;

    private SitePlacementIndex sitePlacementIndex;

    private readonly Dictionary<Vector2Int, int> activeSiteCountsByChunk =
        new Dictionary<Vector2Int, int>();

    public WorldFeatureLifecycle(
        WorldContext worldContext,
        WorldPoiPoolManager poiPoolManager,
        WorldSiteActivationPipeline worldSiteActivationPipeline)
    {
        this.worldContext = worldContext;
        this.worldSiteActivationPipeline = worldSiteActivationPipeline;

        chunkOwnership = new WorldFeatureOwnershipCollection<Vector2Int>(
            poiPoolManager,
            "WorldFeatureLifecycle_Root",
            chunkCoord => $"ChunkSites_{chunkCoord.x}_{chunkCoord.y}");
    }

    public void RebuildPlacements()
    {
        sitePlacementIndex = worldContext != null && worldContext.BuildOutput != null
            ? worldContext.BuildOutput.SitePlacements
            : null;
    }

    public void ClearAll()
    {
        activeSiteCountsByChunk.Clear();
        chunkOwnership.ClearAll();
        sitePlacementIndex = null;
    }

    public void ActivateChunk(Vector2Int chunkCoord)
    {
        if (sitePlacementIndex == null)
        {
            return;
        }

        if (activeSiteCountsByChunk.ContainsKey(chunkCoord))
        {
            return;
        }

        if (!sitePlacementIndex.TryGetChunk(chunkCoord, out List<SitePlacement> placements) || placements == null || placements.Count == 0)
        {
            return;
        }

        WorldFeatureOwnershipGroup ownershipGroup = chunkOwnership.GetOrCreateGroup(chunkCoord);
        Transform chunkRoot = ownershipGroup.Root;
        int activeSiteCount = 0;

        for (int i = 0; i < placements.Count; i++)
        {
            SitePlacement placement = placements[i];
            GameObject instance = SpawnSiteInstance(placement, chunkRoot);

            if (instance != null)
            {
                ownershipGroup.AddInstance(instance);
                activeSiteCount++;
            }
        }

        if (activeSiteCount > 0)
        {
            activeSiteCountsByChunk.Add(chunkCoord, activeSiteCount);
        }
    }

    public void DeactivateChunk(Vector2Int chunkCoord)
    {
        if (!activeSiteCountsByChunk.ContainsKey(chunkCoord))
        {
            return;
        }

        activeSiteCountsByChunk.Remove(chunkCoord);
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
        return activeSiteCountsByChunk.Count;
    }

    public int GetActiveSiteCount()
    {
        int totalCount = 0;

        foreach (KeyValuePair<Vector2Int, int> pair in activeSiteCountsByChunk)
        {
            totalCount += pair.Value;
        }

        return totalCount;
    }

    public int GetTotalPlacedSiteCount()
    {
        return sitePlacementIndex != null ? sitePlacementIndex.All.Count : 0;
    }
}
