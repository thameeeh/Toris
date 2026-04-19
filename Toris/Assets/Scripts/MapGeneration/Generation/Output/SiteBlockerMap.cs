using System.Collections.Generic;
using UnityEngine;

public sealed class SiteBlockerMap : ITileNavigationContributionSource
{
    private readonly HashSet<Vector2Int> blockedTiles = new HashSet<Vector2Int>();
    public int BlockedTileCount => blockedTiles.Count;

    public void Clear()
    {
        blockedTiles.Clear();
    }

    public void AddSquareFootprint(Vector2Int centerTile, int size)
    {
        int clampedSize = Mathf.Max(1, size);
        AddRectFootprint(centerTile, clampedSize, clampedSize);
    }

    public void AddRectFootprint(Vector2Int centerTile, int width, int height)
    {
        int clampedWidth = Mathf.Max(1, width);
        int clampedHeight = Mathf.Max(1, height);
        int halfWidth = Mathf.Max(0, clampedWidth / 2);
        int halfHeight = Mathf.Max(0, clampedHeight / 2);

        for (int y = -halfHeight; y <= halfHeight; y++)
        {
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                blockedTiles.Add(centerTile + new Vector2Int(x, y));
            }
        }
    }

    public TileNavigationContribution GetNavigationContribution(Vector2Int tile)
    {
        return blockedTiles.Contains(tile)
            ? TileNavigationContribution.Blocked
            : TileNavigationContribution.None;
    }
}
