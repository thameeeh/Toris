using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class ChunkResult
{
    public readonly Vector2Int chunkCoord;
    public readonly int chunkSize;

    public readonly TileBase[] ground;
    public readonly TileBase[] water;
    public readonly TileBase[] decor;

    public ChunkResult(Vector2Int chunkCoord, int chunkSize)
    {
        this.chunkCoord = chunkCoord;
        this.chunkSize = chunkSize;

        int n = chunkSize * chunkSize;
        ground = new TileBase[n];
        water = new TileBase[n];
        decor = new TileBase[n];
    }

    public static int Index(int localX, int localY, int chunkSize) => localX + localY * chunkSize;
}
