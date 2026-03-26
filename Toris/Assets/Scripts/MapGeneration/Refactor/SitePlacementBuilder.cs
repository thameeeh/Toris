using UnityEngine;

public static class SitePlacementBuilder
{
    public static SitePlacementIndex Build(WorldContext ctx)
    {
        var index = new SitePlacementIndex();
        if (ctx == null || ctx.World == null)
            return index;

        int chunkSize = Mathf.Max(1, ctx.World.chunkSize);

        foreach (var center in ctx.Gates.GateCenters)
        {
            var chunkCoord = TileToChunk(center, chunkSize);
            int localIndex = ToLocalIndex(center, chunkCoord, chunkSize);

            index.Add(new SitePlacement(
                WorldSiteType.Gate,
                center,
                chunkCoord,
                localIndex));
        }

        foreach (var center in ctx.Dens.DenCenters)
        {
            var chunkCoord = TileToChunk(center, chunkSize);
            int localIndex = ToLocalIndex(center, chunkCoord, chunkSize);

            index.Add(new SitePlacement(
                WorldSiteType.WolfDen,
                center,
                chunkCoord,
                localIndex));
        }

        return index;
    }

    private static Vector2Int TileToChunk(Vector2Int tile, int chunkSize)
    {
        return new Vector2Int(
            Mathf.FloorToInt((float)tile.x / chunkSize),
            Mathf.FloorToInt((float)tile.y / chunkSize));
    }

    private static int ToLocalIndex(Vector2Int center, Vector2Int chunkCoord, int chunkSize)
    {
        int baseX = chunkCoord.x * chunkSize;
        int baseY = chunkCoord.y * chunkSize;

        int lx = center.x - baseX;
        int ly = center.y - baseY;

        return lx + ly * chunkSize;
    }
}