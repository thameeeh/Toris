using UnityEngine;

public readonly struct WildlifeSpawnPlacement
{
    public readonly WorldWildlifeSpawnDefinition SpawnDefinition;
    public readonly Vector2Int CenterTile;
    public readonly Vector2Int ChunkCoord;
    public readonly int LocalIndex;
    public readonly int GroupId;

    public WildlifeSpawnPlacement(
        WorldWildlifeSpawnDefinition spawnDefinition,
        Vector2Int centerTile,
        Vector2Int chunkCoord,
        int localIndex,
        int groupId)
    {
        SpawnDefinition = spawnDefinition;
        CenterTile = centerTile;
        ChunkCoord = chunkCoord;
        LocalIndex = localIndex;
        GroupId = groupId;
    }

    public static WildlifeSpawnPlacement Create(
        WorldWildlifeSpawnDefinition spawnDefinition,
        Vector2Int centerTile,
        int chunkSize,
        int groupId)
    {
        int resolvedChunkSize = Mathf.Max(1, chunkSize);
        Vector2Int chunkCoord = TileToChunk(centerTile, resolvedChunkSize);
        int localIndex = ToLocalIndex(centerTile, chunkCoord, resolvedChunkSize);

        return new WildlifeSpawnPlacement(
            spawnDefinition,
            centerTile,
            chunkCoord,
            localIndex,
            groupId);
    }

    private static Vector2Int TileToChunk(Vector2Int tile, int chunkSize)
    {
        int chunkX = FloorDiv(tile.x, chunkSize);
        int chunkY = FloorDiv(tile.y, chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    private static int ToLocalIndex(Vector2Int centerTile, Vector2Int chunkCoord, int chunkSize)
    {
        int baseX = chunkCoord.x * chunkSize;
        int baseY = chunkCoord.y * chunkSize;

        int localX = centerTile.x - baseX;
        int localY = centerTile.y - baseY;

        return localX + localY * chunkSize;
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (divisor == 0)
            return 0;

        int quotient = value / divisor;
        int remainder = value % divisor;

        if (remainder != 0 && ((remainder > 0) != (divisor > 0)))
            quotient--;

        return quotient;
    }
}
