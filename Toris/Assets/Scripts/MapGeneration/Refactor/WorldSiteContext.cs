using UnityEngine;

public readonly struct WorldSiteContext
{
    public readonly SitePlacement Placement;
    public readonly int SpawnId;
    public readonly IGateTransitionService GateTransitionService;
    public readonly IWorldSiteStateService WorldSiteStateService;
    public readonly WorldEncounterServices EncounterServices;

    public WorldSiteContext(
        SitePlacement placement,
        int spawnId,
        IGateTransitionService gateTransitionService,
        IWorldSiteStateService worldSiteStateService,
        WorldEncounterServices encounterServices)
    {
        Placement = placement;
        SpawnId = spawnId;
        GateTransitionService = gateTransitionService;
        WorldSiteStateService = worldSiteStateService;
        EncounterServices = encounterServices;
    }
}