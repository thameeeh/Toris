using UnityEngine;

public sealed class ChunkSiteStateServiceAdapter : IChunkSiteStateService
{
    private readonly WorldContext worldContext;

    public ChunkSiteStateServiceAdapter(WorldContext worldContext)
    {
        this.worldContext = worldContext;
    }

    public bool IsConsumed(Vector2Int chunkCoord, int spawnId)
    {
        if (worldContext == null)
            return false;

        return worldContext.ChunkStates
            .GetChunkState(chunkCoord)
            .consumedIds
            .Contains(spawnId);
    }

    public void MarkConsumed(Vector2Int chunkCoord, int spawnId)
    {
        if (worldContext == null)
            return;

        worldContext.ChunkStates.MarkConsumed(chunkCoord, spawnId);
    }
}