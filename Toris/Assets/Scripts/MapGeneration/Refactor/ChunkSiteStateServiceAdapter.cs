using UnityEngine;

public sealed class ChunkSiteStateServiceAdapter : IChunkSiteStateService
{
    private readonly WorldRuntimeState worldRuntimeState;

    public ChunkSiteStateServiceAdapter(WorldRuntimeState worldRuntimeState)
    {
        this.worldRuntimeState = worldRuntimeState;
    }

    public bool IsConsumed(Vector2Int chunkCoord, int spawnId)
    {
        if (worldRuntimeState == null)
            return false;

        return worldRuntimeState.ChunkStates
            .GetChunkState(chunkCoord)
            .consumedIds
            .Contains(spawnId);
    }

    public void MarkConsumed(Vector2Int chunkCoord, int spawnId)
    {
        if (worldRuntimeState == null)
            return;

        worldRuntimeState.ChunkStates.MarkConsumed(chunkCoord, spawnId);
    }
}