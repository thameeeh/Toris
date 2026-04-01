using UnityEngine;
using UnityEngine.Tilemaps;

public static class SiteStamping
{
    public static void StampSquareGround(
        WorldContext worldContext,
        Vector2Int centerTile,
        int size,
        TileBase groundTile)
    {
        if (worldContext == null || groundTile == null)
            return;

        int resolvedSize = Mathf.Max(1, size);
        WorldBuildOutput buildOutput = worldContext.BuildOutput;
        if (buildOutput == null)
            return;

        buildOutput.TerrainOverrides.StampRectGround(centerTile, resolvedSize, resolvedSize, groundTile);
    }

    public static void AddSquareBlocker(
        WorldContext worldContext,
        Vector2Int centerTile,
        int size)
    {
        if (worldContext == null)
            return;

        int resolvedSize = Mathf.Max(1, size);
        WorldBuildOutput buildOutput = worldContext.BuildOutput;
        if (buildOutput == null)
            return;

        buildOutput.SiteBlockers.AddSquareFootprint(centerTile, resolvedSize);
    }
}
