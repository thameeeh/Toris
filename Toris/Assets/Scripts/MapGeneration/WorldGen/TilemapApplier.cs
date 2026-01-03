using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TilemapApplier
{
    private readonly Tilemap groundMap;
    private readonly Tilemap waterMap;
    private readonly Tilemap decorMap;

    public TilemapApplier(Tilemap groundMap, Tilemap waterMap, Tilemap decorMap)
    {
        this.groundMap = groundMap;
        this.waterMap = waterMap;
        this.decorMap = decorMap;
    }

    public void Apply(ChunkResult chunk)
    {
        int size = chunk.chunkSize;
        int baseX = chunk.chunkCoord.x * size;
        int baseY = chunk.chunkCoord.y * size;

        // One SetTilesBlock per map keeps it fast.
        BoundsInt bounds = new BoundsInt(baseX, baseY, 0, size, size, 1);

        groundMap.SetTilesBlock(bounds, chunk.ground);
        waterMap.SetTilesBlock(bounds, chunk.water);
        decorMap.SetTilesBlock(bounds, chunk.decor);
    }

    public void ClearChunk(Vector2Int chunkCoord, int chunkSize)
    {
        int baseX = chunkCoord.x * chunkSize;
        int baseY = chunkCoord.y * chunkSize;

        BoundsInt bounds = new BoundsInt(baseX, baseY, 0, chunkSize, chunkSize, 1);

        int n = chunkSize * chunkSize;
        var empty = new TileBase[n]; // all nulls clears tiles

        groundMap.SetTilesBlock(bounds, empty);
        waterMap.SetTilesBlock(bounds, empty);
        decorMap.SetTilesBlock(bounds, empty);
    }
}
