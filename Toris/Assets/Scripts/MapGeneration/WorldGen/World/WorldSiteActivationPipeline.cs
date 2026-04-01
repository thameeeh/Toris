using UnityEngine;

public sealed class WorldSiteActivationPipeline
{
    private readonly WorldSceneServices worldSceneServices;
    private readonly WorldEncounterServices worldEncounterServices;
    private readonly WorldRuntimeState worldRuntimeState;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly IGateTransitionService gateTransitionService;
    private readonly IRunGateTransitionService runGateTransitionService;
    private readonly IWorldSiteStateService worldSiteStateService;

    public WorldSiteActivationPipeline(
        WorldSceneServices worldSceneServices,
        WorldEncounterServices worldEncounterServices,
        WorldRuntimeState worldRuntimeState,
        WorldPoiPoolManager poiPoolManager,
        IGateTransitionService gateTransitionService,
        IRunGateTransitionService runGateTransitionService)
    {
        this.worldSceneServices = worldSceneServices;
        this.worldEncounterServices = worldEncounterServices;
        this.worldRuntimeState = worldRuntimeState;
        this.poiPoolManager = poiPoolManager;
        this.gateTransitionService = gateTransitionService;
        this.runGateTransitionService = runGateTransitionService;

        worldSiteStateService = new WorldSiteStateServiceAdapter(worldRuntimeState);
    }

    public GameObject ActivateSite(
        SitePlacement placement,
        Transform parent,
        int biomeSeed)
    {
        WorldSiteDefinition siteDefinition = placement.SiteDefinition;
        if (siteDefinition == null || !siteDefinition.IsValid)
            return null;

        GameObject prefab = siteDefinition.Prefab;
        if (prefab == null || worldSceneServices == null || poiPoolManager == null)
            return null;

        int spawnId = worldRuntimeState.ChunkStates.MakeSpawnId(
            biomeSeed,
            placement.ChunkCoord,
            placement.LocalIndex,
            siteDefinition.SpawnSalt);

        if (siteDefinition.SkipIfConsumed)
        {
            WorldSiteStateHandle siteState = worldSiteStateService.GetSiteState(placement.ChunkCoord, spawnId);
            if (siteState.IsConsumed)
                return null;
        }

        WorldSiteContext siteContext = new WorldSiteContext(
            placement,
            spawnId,
            gateTransitionService,
            runGateTransitionService,
            worldSiteStateService,
            worldEncounterServices,
            siteDefinition.RuntimeConfig);

        Vector3 worldPosition = worldSceneServices.GetCellCenterWorld(placement.CenterTile);
        GameObject siteObject = poiPoolManager.SpawnPrepared(
            prefab,
            worldPosition,
            Quaternion.identity,
            parent,
            siteObject =>
            {
                IWorldSiteContextConsumer[] siteContextConsumers =
                    siteObject.GetComponentsInChildren<IWorldSiteContextConsumer>(true);

                if (siteContextConsumers == null || siteContextConsumers.Length == 0)
                {
                    Debug.LogWarning(
                        $"Site prefab '{prefab.name}' has no IWorldSiteContextConsumer.",
                        siteObject);
                    return false;
                }

                for (int i = 0; i < siteContextConsumers.Length; i++)
                {
                    siteContextConsumers[i].Initialize(siteContext);
                }

                return true;
            });

        if (siteObject == null)
            return null;

        IWorldSiteActivationListener[] siteActivationListeners =
            siteObject.GetComponentsInChildren<IWorldSiteActivationListener>(true);

        for (int i = 0; i < siteActivationListeners.Length; i++)
        {
            siteActivationListeners[i].OnSiteActivated();
        }

        return siteObject;
    }
}
