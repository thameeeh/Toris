using UnityEngine;

public readonly struct WorldSiteContext
{
    public readonly SitePlacement Placement;
    public readonly int SpawnId;
    public readonly IGateTransitionService GateTransitionService;
    public readonly IWorldSiteStateService WorldSiteStateService;
    public readonly WorldEncounterServices EncounterServices;
    public readonly WorldSiteRuntimeConfig RuntimeConfig;

    public WorldSiteContext(
        SitePlacement placement,
        int spawnId,
        IGateTransitionService gateTransitionService,
        IWorldSiteStateService worldSiteStateService,
        WorldEncounterServices encounterServices,
        WorldSiteRuntimeConfig runtimeConfig)
    {
        Placement = placement;
        SpawnId = spawnId;
        GateTransitionService = gateTransitionService;
        WorldSiteStateService = worldSiteStateService;
        EncounterServices = encounterServices;
        RuntimeConfig = runtimeConfig;
    }
    public T GetRuntimeConfig<T>() where T : WorldSiteRuntimeConfig
    {
        return RuntimeConfig as T;
    }
}