using UnityEngine;

public readonly struct SitePlacement
{
    public readonly WorldSiteDefinition SiteDefinition;
    public readonly Vector2Int CenterTile;
    public readonly Vector2Int ChunkCoord;
    public readonly int LocalIndex;
    public readonly SitePlacementLifecycleScope LifecycleScope;

    public SitePlacement(
        WorldSiteDefinition siteDefinition,
        Vector2Int centerTile,
        Vector2Int chunkCoord,
        int localIndex,
        SitePlacementLifecycleScope lifecycleScope = SitePlacementLifecycleScope.Chunk)
    {
        SiteDefinition = siteDefinition;
        CenterTile = centerTile;
        ChunkCoord = chunkCoord;
        LocalIndex = localIndex;
        LifecycleScope = lifecycleScope;
    }

    public static SitePlacement Create(
        WorldSiteDefinition siteDefinition,
        Vector2Int centerTile,
        int chunkSize,
        SitePlacementLifecycleScope lifecycleScope = SitePlacementLifecycleScope.Chunk)
    {
        int resolvedChunkSize = Mathf.Max(1, chunkSize);
        Vector2Int chunkCoord = TileToChunk(centerTile, resolvedChunkSize);
        int localIndex = ToLocalIndex(centerTile, chunkCoord, resolvedChunkSize);

        return new SitePlacement(
            siteDefinition,
            centerTile,
            chunkCoord,
            localIndex,
            lifecycleScope);
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
