using System.Collections.Generic;
using UnityEngine;

public sealed class PersistentWorldFeatureLifecycle
{
    private readonly WorldSceneServices worldSceneServices;
    private readonly WorldEncounterServices worldEncounterServices;
    private readonly WorldContext worldContext;
    private readonly WorldRuntimeState worldRuntimeState;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly IGateTransitionService gateTransitionService;
    private readonly IWorldSiteStateService worldSiteStateService;

    private readonly List<GameObject> activePersistentInstances = new List<GameObject>();
    private Transform persistentRoot;

    public PersistentWorldFeatureLifecycle(
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

    public void ActivatePersistentSite(WorldSiteDefinition siteDefinition, Vector2Int centerTile)
    {
        if (siteDefinition == null || !siteDefinition.IsValid)
            return;

        GameObject prefab = siteDefinition.Prefab;
        if (prefab == null || worldSceneServices == null || worldSceneServices.Grid == null || poiPoolManager == null)
            return;

        SitePlacement placement = BuildPlacement(siteDefinition, centerTile);
        int spawnId = ComputeSpawnId(placement, siteDefinition);

        if (siteDefinition.SkipIfConsumed)
        {
            WorldSiteStateHandle siteState = worldSiteStateService.GetSiteState(placement.ChunkCoord, spawnId);
            if (siteState.IsConsumed)
                return;
        }

        EnsureRoot();

        Vector3 worldPosition = worldSceneServices.GetCellCenterWorld(centerTile);
        GameObject siteObject = poiPoolManager.Spawn(prefab, worldPosition, Quaternion.identity, persistentRoot);
        if (siteObject == null)
            return;

        IWorldSiteContextConsumer[] siteContextConsumers =
            siteObject.GetComponentsInChildren<IWorldSiteContextConsumer>(true);

        if (siteContextConsumers == null || siteContextConsumers.Length == 0)
        {
            Debug.LogWarning(
                $"Persistent site prefab '{prefab.name}' has no IWorldSiteContextConsumer.",
                siteObject);

            poiPoolManager.Release(siteObject);
            return;
        }

        WorldSiteContext siteContext = new WorldSiteContext(
            placement,
            spawnId,
            gateTransitionService,
            worldSiteStateService,
            worldEncounterServices,
            siteDefinition.RuntimeConfig);

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

        activePersistentInstances.Add(siteObject);
    }

    public void ClearAll()
    {
        for (int i = 0; i < activePersistentInstances.Count; i++)
        {
            GameObject instance = activePersistentInstances[i];
            if (instance != null && poiPoolManager != null)
                poiPoolManager.Release(instance);
        }

        activePersistentInstances.Clear();

        if (persistentRoot != null)
        {
            Object.Destroy(persistentRoot.gameObject);
            persistentRoot = null;
        }
    }

    private void EnsureRoot()
    {
        if (persistentRoot != null)
            return;

        Transform activeRoot = poiPoolManager.GetActiveRoot();
        GameObject rootObject = new GameObject("PersistentWorldFeatures");
        persistentRoot = rootObject.transform;
        persistentRoot.SetParent(activeRoot, false);
    }

    private SitePlacement BuildPlacement(WorldSiteDefinition siteDefinition, Vector2Int centerTile)
    {
        int chunkSize = Mathf.Max(1, worldContext.World.chunkSize);
        Vector2Int chunkCoord = TileToChunk(centerTile, chunkSize);
        int localIndex = ToLocalIndex(centerTile, chunkCoord, chunkSize);

        return new SitePlacement(
            siteDefinition,
            centerTile,
            chunkCoord,
            localIndex);
    }

    private int ComputeSpawnId(SitePlacement placement, WorldSiteDefinition siteDefinition)
    {
        return worldRuntimeState.ChunkStates.MakeSpawnId(
            worldContext.ActiveBiome.Seed,
            placement.ChunkCoord,
            placement.LocalIndex,
            siteDefinition.SpawnSalt);
    }

    private static Vector2Int TileToChunk(Vector2Int tile, int chunkSize)
    {
        int chunkX = FloorDiv(tile.x, chunkSize);
        int chunkY = FloorDiv(tile.y, chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    private static int ToLocalIndex(Vector2Int centerTile, Vector2Int chunkCoord, int chunkSize)
    {
        int baseX = chunkCoord.x * chunkSize;
        int baseY = chunkCoord.y * chunkSize;

        int localX = centerTile.x - baseX;
        int localY = centerTile.y - baseY;

        return localX + localY * chunkSize;
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (divisor == 0)
            return 0;

        int quotient = value / divisor;
        int remainder = value % divisor;

        if (remainder != 0 && ((remainder > 0) != (divisor > 0)))
            quotient--;

        return quotient;
    }
}