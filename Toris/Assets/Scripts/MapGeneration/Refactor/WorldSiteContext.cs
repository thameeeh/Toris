using UnityEngine;

public readonly struct WorldSiteContext
{
    public readonly SitePlacement Placement;
    public readonly int SpawnId;
    public readonly IGateTransitionService GateTransitionService;
    public readonly IRunGateTransitionService RunGateTransitionService;
    public readonly IWorldSiteStateService WorldSiteStateService;
    public readonly WorldEncounterServices EncounterServices;
    public readonly WorldSiteRuntimeConfig RuntimeConfig;

    public WorldSiteContext(
        SitePlacement placement,
        int spawnId,
        IGateTransitionService gateTransitionService,
        IRunGateTransitionService runGateTransitionService,
        IWorldSiteStateService worldSiteStateService,
        WorldEncounterServices encounterServices,
        WorldSiteRuntimeConfig runtimeConfig)
    {
        Placement = placement;
        SpawnId = spawnId;
        GateTransitionService = gateTransitionService;
        RunGateTransitionService = runGateTransitionService;
        WorldSiteStateService = worldSiteStateService;
        EncounterServices = encounterServices;
        RuntimeConfig = runtimeConfig;
    }
    public T GetRuntimeConfig<T>() where T : WorldSiteRuntimeConfig
    {
        return RuntimeConfig as T;
    }

    public bool TryGetEncounterPackage(out WorldEncounterPackage encounterPackage)
    {
        if (RuntimeConfig is IWorldEncounterPackageConfig encounterPackageConfig)
        {
            WorldSiteStateHandle siteState = WorldSiteStateService != null
                ? WorldSiteStateService.GetSiteState(Placement.ChunkCoord, SpawnId)
                : default;

            encounterPackage = new WorldEncounterPackage(
                encounterPackageConfig.PackageId,
                EncounterServices,
                encounterPackageConfig.OccupantPolicy,
                new WorldEncounterPackageState(encounterPackageConfig.PackageId, siteState),
                RuntimeConfig);

            return encounterPackage.IsValid;
        }

        encounterPackage = default;
        return false;
    }
}
