using UnityEngine;


public readonly struct SitePlacement
{
    public readonly WorldSiteType SiteType;
    public readonly Vector2Int CenterTile;
    public readonly Vector2Int ChunkCoord;
    public readonly int LocalIndex;

    public SitePlacement(
        WorldSiteType siteType,
        Vector2Int centerTile,
        Vector2Int chunkcoord,
        int localIndex)
    {
        SiteType = siteType;
        CenterTile = centerTile;
        ChunkCoord = chunkcoord;
        LocalIndex = localIndex;
    }
}