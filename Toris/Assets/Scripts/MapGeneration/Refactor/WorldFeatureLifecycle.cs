using System.Collections.Generic;
using UnityEngine;

public sealed class WorldFeatureLifecycle
{
    private readonly WorldSceneServices worldSceneServices;
    private readonly WorldEncounterServices worldEncounterServices;
    private readonly WorldContext worldContext;
    private readonly WorldRuntimeState worldRuntimeState;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly IGateTransitionService gateTransitionService;
    private readonly IWorldSiteStateService worldSiteStateService;

    private SitePlacementIndex sitePlacementIndex;

    private readonly Dictionary<Vector2Int, List<ActiveSiteHandle>> activeSitesByChunk =
        new Dictionary<Vector2Int, List<ActiveSiteHandle>>();

    private readonly Dictionary<Vector2Int, Transform> chunkSiteRoots =
        new Dictionary<Vector2Int, Transform>();

    private Transform siteRootContainer;

    public WorldFeatureLifecycle(
        WorldSceneServices worldSceneServices,
        WorldContext worldContext,
        WorldRuntimeState worldRuntimeState,
        WorldPoiPoolManager poiPoolManager,
        IGateTransitionService gateTransitionService,
        WorldEncounterServices worldEncounterServices)
    {
        this.worldSceneServices = worldSceneServices;
        this.worldContext = worldContext;
        this.worldRuntimeState = worldRuntimeState;
        this.poiPoolManager = poiPoolManager;
        this.gateTransitionService = gateTransitionService;
        this.worldEncounterServices = worldEncounterServices;

        worldSiteStateService = new WorldSiteStateServiceAdapter(worldRuntimeState);
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
        WorldSiteDefinition siteDefinition = placement.SiteDefinition;
        if (siteDefinition == null || !siteDefinition.IsValid)
            return null;

        GameObject prefab = siteDefinition.Prefab;
        if (prefab == null || worldSceneServices == null || worldSceneServices.Grid == null || poiPoolManager == null)
            return null;

        int spawnId = ComputeSpawnId(placement, siteDefinition);

        if (siteDefinition.SkipIfConsumed)
        {
            WorldSiteStateHandle siteState = worldSiteStateService.GetSiteState(placement.ChunkCoord, spawnId);
            if (siteState.IsConsumed)
                return null;
        }

        Vector3 worldPosition = worldSceneServices.GetCellCenterWorld(placement.CenterTile);

        GameObject siteObject = poiPoolManager.Spawn(prefab, worldPosition, Quaternion.identity, parent);
        if (siteObject == null)
            return null;

        IWorldSiteContextConsumer[] siteContextConsumers =
            siteObject.GetComponentsInChildren<IWorldSiteContextConsumer>(true);

        if (siteContextConsumers == null || siteContextConsumers.Length == 0)
        {
            Debug.LogWarning(
                $"Site prefab '{prefab.name}' for site type '{placement.SiteDefinition}' has no IWorldSiteContextConsumer.",
                siteObject);

            ReleaseSiteInstance(siteObject);
            return null;
        }

        WorldSiteContext siteContext = new WorldSiteContext(
            placement,
            spawnId,
            gateTransitionService,
            worldSiteStateService,
            worldEncounterServices,
            placement.SiteDefinition != null ? placement.SiteDefinition.RuntimeConfig : null);

        for (int i = 0; i < siteContextConsumers.Length; i++)
        {
            siteContextConsumers[i].Initialize(siteContext);
        }

        IWorldSiteActivationListener[] siteActivationListeners =
    siteObject.GetComponentsInChildren<IWorldSiteActivationListener>(true);

        for (int i = 0; i < siteActivationListeners.Length; i++)
        {
            siteActivationListeners[i].OnSiteActivated();
        }

        return siteObject;
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

    private int ComputeSpawnId(SitePlacement placement, WorldSiteDefinition siteDefinition)
    {
        return worldRuntimeState.ChunkStates.MakeSpawnId(
            worldContext.ActiveBiome.Seed,
            placement.ChunkCoord,
            placement.LocalIndex,
            siteDefinition.SpawnSalt);
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