using UnityEngine;

public sealed class WorldSiteStateServiceAdapter : IWorldSiteStateService
{
    private readonly ChunkStateStore chunkStateStore;

    public WorldSiteStateServiceAdapter(ChunkStateStore chunkStateStore)
    {
        this.chunkStateStore = chunkStateStore;
    }

    public WorldSiteStateHandle GetSiteState(Vector2Int chunkCoord, int spawnId)
    {
        if (chunkStateStore == null)
            return default;

        return chunkStateStore.GetSiteState(chunkCoord, spawnId);
    }
}
