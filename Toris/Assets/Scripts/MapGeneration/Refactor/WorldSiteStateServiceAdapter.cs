using UnityEngine;

public sealed class WorldSiteStateServiceAdapter : IWorldSiteStateService
{
    private readonly WorldRuntimeState worldRuntimeState;

    public WorldSiteStateServiceAdapter(WorldRuntimeState worldRuntimeState)
    {
        this.worldRuntimeState = worldRuntimeState;
    }

    public WorldSiteStateHandle GetSiteState(Vector2Int chunkCoord, int spawnId)
    {
        if (worldRuntimeState == null)
            return default;

        return worldRuntimeState.ChunkStates.GetSiteState(chunkCoord, spawnId);
    }
}