using UnityEngine;

public interface IChunkSiteStateService
{
    bool IsConsumed(Vector2Int chunkCoord, int spawnId);
    void MarkConsumed(Vector2Int chunkCoord, int spawnId);
}