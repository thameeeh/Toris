using UnityEngine;

public sealed class WorldContext
{
    public readonly WorldProfile World;

    public BiomeInstance ActiveBiome { get; private set; }
    public BiomeDefinition ActiveDef { get; private set; }
    public BiomeProfile Biome => ActiveDef != null ? ActiveDef.profile : null;

    public NoiseContext Noise { get; private set; }
    public BiomeMask Mask { get; private set; }
    public FeatureStamps Stamps { get; private set; }
    public SiteBlockerMap SiteBlockers { get; private set; }
    public RoadAnchorMap RoadAnchors { get; private set; }
    public SitePlacementIndex SitePlacements { get; private set; }

    public AnimationCurve DangerCurve => World.dangerCurve;

    public WorldContext(WorldProfile world)
    {
        World = world;

        Mask = new BiomeMask();
        Stamps = new FeatureStamps();
        SitePlacements = new SitePlacementIndex();
        SiteBlockers = new SiteBlockerMap();
        RoadAnchors = new RoadAnchorMap();

        ActiveBiome = new BiomeInstance(0, World.seed, Vector2Int.zero, world.worldRadiusTiles);
        Noise = new NoiseContext(ActiveBiome.Seed);
    }

    public void BindBiome(BiomeDefinition def, BiomeInstance biome)
    {
        ActiveDef = def;
        ActiveBiome = biome;
        Noise = new NoiseContext(biome.Seed);

        Stamps.Clear();
        SitePlacements.Clear();
        SiteBlockers.Clear();
        RoadAnchors.Clear();

        ActiveDef?.BuildFeatures(this);
    }

    public void RegisterSite(WorldSiteDefinition siteDefinition, Vector2Int centerTile)
    {
        if (siteDefinition == null || !siteDefinition.IsValid)
            return;

        int chunkSize = Mathf.Max(1, World.chunkSize);

        Vector2Int chunkCoord = TileToChunk(centerTile, chunkSize);
        int localIndex = ToLocalIndex(centerTile, chunkCoord, chunkSize);

        SitePlacements.Add(new SitePlacement(
            siteDefinition,
            centerTile,
            chunkCoord,
            localIndex));
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

public struct WorldSignals
{
    public float dist01;
    public float danger01;

    public float vegetation01;
    public float variation01;
    public float lake01;

    public float road01;
}