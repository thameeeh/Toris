using UnityEngine;

public readonly struct WorldSiteContext
{
    public readonly SitePlacement Placement;
    public readonly int SpawnId;
    public readonly IGateTransitionService GateTransitionService;
    public readonly IChunkSiteStateService ChunkSiteStateService;
    public readonly WorldEncounterServices EncounterServices;

    public WorldSiteContext(
        SitePlacement placement,
        int spawnId,
        IGateTransitionService gateTransitionService,
        IChunkSiteStateService chunkSiteStateService,
        WorldEncounterServices encounterServices)
    {
        Placement = placement;
        SpawnId = spawnId;
        GateTransitionService = gateTransitionService;
        ChunkSiteStateService = chunkSiteStateService;
        EncounterServices = encounterServices;
    }
}