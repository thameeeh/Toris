using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SiteStamping
{
    private const uint LayoutVariantSelectionHashSalt = 0x51A7E011u;

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

        if (stampDefinition.HasVisualClearZone)
        {
            AddRectVisualClear(
                worldContext,
                centerTile + stampDefinition.ClearVisualsOffset,
                stampDefinition.ClearVisualsWidth,
                stampDefinition.ClearVisualsHeight);
        }

        if (stampDefinition.HasNavigationBlockerStamp)
        {
            AddRectBlocker(
                worldContext,
                centerTile + stampDefinition.BlockerOffset,
                stampDefinition.BlockerWidth,
                stampDefinition.BlockerHeight);
        }

        ApplyLayoutDefinition(
            worldContext,
            centerTile,
            ResolveLayoutDefinition(worldContext, centerTile, stampDefinition));
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

    public static void AddRectVisualClear(
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

        buildOutput.SiteVisualClears.AddRectFootprint(centerTile, resolvedWidth, resolvedHeight);
    }

    public static void ApplyLayoutDefinition(
        WorldContext worldContext,
        Vector2Int centerTile,
        SiteTileLayoutDefinition layoutDefinition)
    {
        if (worldContext == null || layoutDefinition == null)
            return;

        FeatureStamps terrainOverrides = worldContext.BuildOutput != null
            ? worldContext.BuildOutput.TerrainOverrides
            : null;

        if (terrainOverrides == null)
            return;

        IReadOnlyList<SiteTileLayoutCell> cells = layoutDefinition.Cells;
        if (cells == null)
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            SiteTileLayoutCell cell = cells[i];
            Vector2Int worldTile = centerTile + cell.offset;

            if (cell.ground != null)
                terrainOverrides.SetGround(worldTile, cell.ground);

            if (cell.water != null)
                terrainOverrides.SetWater(worldTile, cell.water);

            if (cell.decoration != null)
                terrainOverrides.SetDecoration(worldTile, cell.decoration);

            if (cell.obstacle != null)
                terrainOverrides.SetObstacle(worldTile, cell.obstacle);

            if (cell.canopy != null)
                terrainOverrides.SetCanopy(worldTile, cell.canopy);
        }
    }

    private static SiteTileLayoutDefinition ResolveLayoutDefinition(
        WorldContext worldContext,
        Vector2Int centerTile,
        SiteStampDefinition stampDefinition)
    {
        if (worldContext == null || stampDefinition == null)
            return null;

        IReadOnlyList<SiteTileLayoutDefinition> variants = stampDefinition.TileLayoutVariants;
        int validVariantCount = CountValidLayoutVariants(variants);

        if (validVariantCount <= 0)
            return stampDefinition.TileLayoutDefinition;

        if (validVariantCount == 1)
            return GetValidLayoutVariantAt(variants, 0) ?? stampDefinition.TileLayoutDefinition;

        uint variantHash = DeterministicHash.Hash(
            (uint)worldContext.ActiveBiome.Seed,
            centerTile.x,
            centerTile.y,
            LayoutVariantSelectionHashSalt);
        int variantIndex = Mathf.Min(
            validVariantCount - 1,
            Mathf.FloorToInt(DeterministicHash.Hash01(variantHash) * validVariantCount));

        return GetValidLayoutVariantAt(variants, variantIndex) ?? stampDefinition.TileLayoutDefinition;
    }

    private static int CountValidLayoutVariants(IReadOnlyList<SiteTileLayoutDefinition> variants)
    {
        if (variants == null)
            return 0;

        int count = 0;
        for (int i = 0; i < variants.Count; i++)
        {
            if (variants[i] != null)
                count++;
        }

        return count;
    }

    private static SiteTileLayoutDefinition GetValidLayoutVariantAt(
        IReadOnlyList<SiteTileLayoutDefinition> variants,
        int validIndex)
    {
        if (variants == null || validIndex < 0)
            return null;

        int currentValidIndex = 0;
        for (int i = 0; i < variants.Count; i++)
        {
            SiteTileLayoutDefinition variant = variants[i];
            if (variant == null)
                continue;

            if (currentValidIndex == validIndex)
                return variant;

            currentValidIndex++;
        }

        return null;
    }
}
