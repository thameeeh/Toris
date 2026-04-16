using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class WorldNavigationLifecycle
{
    private readonly TileNavWorld tileNavWorld;
    private readonly Tilemap groundMap;
    private readonly Tilemap waterMap;
    private readonly Tilemap obstacleMap;

    public int LoadedNavChunkCount => tileNavWorld != null ? tileNavWorld.LoadedNavChunkCount : 0;
    public bool HasNavigationContributions => tileNavWorld != null && tileNavWorld.HasNavigationContributions;

    public WorldNavigationLifecycle(TileNavWorld tileNavWorld, Tilemap groundMap, Tilemap waterMap, Tilemap obstacleMap)
    {
        this.tileNavWorld = tileNavWorld;
        this.groundMap = groundMap;
        this.waterMap = waterMap;
        this.obstacleMap = obstacleMap;
    }

    public void Initialize(ITileNavigationContributionSource navigationContributions)
    {
        if (tileNavWorld == null)
            return;

        tileNavWorld.Initialize(groundMap, waterMap, obstacleMap);
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

    public NavigationDiagnosticsSnapshot CreateDiagnosticsSnapshot()
    {
        return new NavigationDiagnosticsSnapshot(
            LoadedNavChunkCount,
            HasNavigationContributions);
    }
}
