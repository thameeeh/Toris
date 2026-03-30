using System.Collections.Generic;
using UnityEngine;

public sealed class PersistentWorldFeatureLifecycle
{
    private readonly WorldContext worldContext;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly WorldSiteActivationPipeline worldSiteActivationPipeline;

    private const string PersistentOwnershipKey = "persistent";
    private readonly WorldFeatureOwnershipCollection<string> persistentOwnership;

    public PersistentWorldFeatureLifecycle(
        WorldContext worldContext,
        WorldPoiPoolManager poiPoolManager,
        WorldSiteActivationPipeline worldSiteActivationPipeline)
    {
        this.worldContext = worldContext;
        this.poiPoolManager = poiPoolManager;
        this.worldSiteActivationPipeline = worldSiteActivationPipeline;

        persistentOwnership = new WorldFeatureOwnershipCollection<string>(
            poiPoolManager,
            "PersistentWorldFeatures_Root",
            _ => "PersistentWorldFeatures");
    }

    public void ActivatePersistentSite(WorldSiteDefinition siteDefinition, Vector2Int centerTile)
    {
        if (siteDefinition == null || !siteDefinition.IsValid || worldContext == null)
            return;

        WorldFeatureOwnershipGroup ownershipGroup =
            persistentOwnership.GetOrCreateGroup(PersistentOwnershipKey);

        SitePlacement placement = BuildPlacement(siteDefinition, centerTile);
        GameObject siteObject = worldSiteActivationPipeline != null
            ? worldSiteActivationPipeline.ActivateSite(
                placement,
                ownershipGroup.Root,
                worldContext.ActiveBiome.Seed)
            : null;

        if (siteObject != null)
        {
            ownershipGroup.AddInstance(siteObject);
        }
    }

    public void ClearAll()
    {
        persistentOwnership.ClearAll();
    }

    public int GetActiveSiteCount()
    {
        if (!persistentOwnership.TryGetGroup(PersistentOwnershipKey, out WorldFeatureOwnershipGroup ownershipGroup))
            return 0;

        return ownershipGroup.InstanceCount;
    }

    private SitePlacement BuildPlacement(WorldSiteDefinition siteDefinition, Vector2Int centerTile)
    {
        int chunkSize = Mathf.Max(1, worldContext.World.chunkSize);
        Vector2Int chunkCoord = TileToChunk(centerTile, chunkSize);
        int localIndex = ToLocalIndex(centerTile, chunkCoord, chunkSize);

        return new SitePlacement(
            siteDefinition,
            centerTile,
            chunkCoord,
            localIndex);
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
