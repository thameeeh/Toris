using UnityEngine;
using UnityEngine.Tilemaps;

public static class SiteStamping
{
    public static void ApplyStampDefinition(
        WorldContext worldContext,
        Vector2Int centerTile,
        SiteStampDefinition stampDefinition)
    {
        if (worldContext == null || stampDefinition == null)
            return;

        if (stampDefinition.HasGroundStamp)
        {
            StampRectGround(
                worldContext,
                centerTile + stampDefinition.GroundOffset,
                stampDefinition.GroundWidth,
                stampDefinition.GroundHeight,
                stampDefinition.GroundTile);
        }

        if (stampDefinition.HasNavigationBlockerStamp)
        {
            AddRectBlocker(
                worldContext,
                centerTile + stampDefinition.BlockerOffset,
                stampDefinition.BlockerWidth,
                stampDefinition.BlockerHeight);
        }
    }

    public static void StampSquareGround(
        WorldContext worldContext,
        Vector2Int centerTile,
        int size,
        TileBase groundTile)
    {
        int resolvedSize = Mathf.Max(1, size);
        StampRectGround(worldContext, centerTile, resolvedSize, resolvedSize, groundTile);
    }

    public static void StampRectGround(
        WorldContext worldContext,
        Vector2Int centerTile,
        int width,
        int height,
        TileBase groundTile)
    {
        if (worldContext == null || groundTile == null)
            return;

        int resolvedWidth = Mathf.Max(1, width);
        int resolvedHeight = Mathf.Max(1, height);
        WorldBuildOutput buildOutput = worldContext.BuildOutput;
        if (buildOutput == null)
            return;

        buildOutput.TerrainOverrides.StampRectGround(centerTile, resolvedWidth, resolvedHeight, groundTile);
    }

    public static void AddSquareBlocker(
        WorldContext worldContext,
        Vector2Int centerTile,
        int size)
    {
        int resolvedSize = Mathf.Max(1, size);
        AddRectBlocker(worldContext, centerTile, resolvedSize, resolvedSize);
    }

    public static void AddRectBlocker(
        WorldContext worldContext,
        Vector2Int centerTile,
        int width,
        int height)
    {
        if (worldContext == null)
            return;

        int resolvedWidth = Mathf.Max(1, width);
        int resolvedHeight = Mathf.Max(1, height);
        WorldBuildOutput buildOutput = worldContext.BuildOutput;
        if (buildOutput == null)
            return;

        buildOutput.SiteBlockers.AddRectFootprint(centerTile, resolvedWidth, resolvedHeight);
    }
}
