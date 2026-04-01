using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class WorldNavigationLifecycle
{
    private readonly TileNavWorld tileNavWorld;
    private readonly Tilemap groundMap;
    private readonly Tilemap waterMap;

    public WorldNavigationLifecycle(TileNavWorld tileNavWorld, Tilemap groundMap, Tilemap waterMap)
    {
        this.tileNavWorld = tileNavWorld;
        this.groundMap = groundMap;
        this.waterMap = waterMap;
    }

    public void Initialize(ITileNavigationContributionSource navigationContributions)
    {
        if (tileNavWorld == null)
            return;

        tileNavWorld.Initialize(groundMap, waterMap);
        tileNavWorld.SetNavigationContributions(navigationContributions);
    }

    public void SetNavigationContributions(ITileNavigationContributionSource navigationContributions)
    {
        tileNavWorld?.SetNavigationContributions(navigationContributions);
    }

    public void BuildChunk(Vector2Int chunkCoord, int chunkSize)
    {
        tileNavWorld?.BuildNavChunk(chunkCoord, chunkSize);
    }

    public void ClearChunk(Vector2Int chunkCoord)
    {
        tileNavWorld?.ClearNavChunk(chunkCoord);
    }
}
