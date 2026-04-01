using UnityEngine;

public interface IWorldSiteStateService
{
    WorldSiteStateHandle GetSiteState(Vector2Int chunkCoord, int spawnId);
}