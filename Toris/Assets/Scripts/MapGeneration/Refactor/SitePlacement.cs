using UnityEngine;

public readonly struct SitePlacement
{
    public readonly WorldSiteDefinition SiteDefinition;
    public readonly Vector2Int CenterTile;
    public readonly Vector2Int ChunkCoord;
    public readonly int LocalIndex;

    public SitePlacement(
        WorldSiteDefinition siteDefinition,
        Vector2Int centerTile,
        Vector2Int chunkCoord,
        int localIndex)
    {
        SiteDefinition = siteDefinition;
        CenterTile = centerTile;
        ChunkCoord = chunkCoord;
        LocalIndex = localIndex;
    }
}