using UnityEngine;

public sealed class WorldContext
{
    public readonly WorldProfile World;

    //public int RunSeed { get; private set; } KEEP FOR RUN MODIFIERS LATER

    public BiomeInstance ActiveBiome { get; private set; }
    public BiomeDefinition ActiveDef { get; private set; }
    public BiomeProfile Biome => ActiveDef != null ? ActiveDef.profile : null;

    public NoiseContext Noise { get; private set; }
    public BiomeMask Mask { get; private set; }
    public FeatureStamps Stamps { get; private set; }
    public SiteBlockerMap SiteBlockers { get; private set; }

    public SitePlacementIndex SitePlacements { get; private set; }

    public GateRegistry Gates { get; private set; }
    public ChunkStateStore ChunkStates { get; private set; }
    public DenRegistry Dens { get; private set; }

    public AnimationCurve DangerCurve => World.dangerCurve;

    public WorldContext(WorldProfile world)
    {
        World = world;
        //RunSeed = world.seed;

        Mask = new BiomeMask();
        Stamps = new FeatureStamps();
        SitePlacements = new SitePlacementIndex();
        SiteBlockers = new SiteBlockerMap();

        Gates = new GateRegistry();
        ChunkStates = new ChunkStateStore();
        Dens = new DenRegistry();

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

        Gates.Clear();
        ChunkStates.Clear();
        Dens.Clear();

        ActiveDef?.BuildFeatures(this);
    }

    public void AddGateSite(Vector2Int centerTile)
    {
        AddSite(WorldSiteType.Gate, centerTile);
    }

    public void AddWolfDenSite(Vector2Int centerTile)
    {
        AddSite(WorldSiteType.WolfDen, centerTile);
    }

    private void AddSite(WorldSiteType siteType, Vector2Int centerTile)
    {
        int chunkSize = Mathf.Max(1, World.chunkSize);

        Vector2Int chunkCoord = TileToChunk(centerTile, chunkSize);
        int localIndex = ToLocalIndex(centerTile, chunkCoord, chunkSize);

        SitePlacements.Add(new SitePlacement(
            siteType,
            centerTile,
            chunkCoord,
            localIndex));
    }

    private static Vector2Int TileToChunk(Vector2Int tile, int chunkSize)
    {
        int cx = FloorDiv(tile.x, chunkSize);
        int cy = FloorDiv(tile.y, chunkSize);
        return new Vector2Int(cx, cy);
    }

    private static int ToLocalIndex(Vector2Int centerTile, Vector2Int chunkCoord, int chunkSize)
    {
        int baseX = chunkCoord.x * chunkSize;
        int baseY = chunkCoord.y * chunkSize;

        int localX = centerTile.x - baseX;
        int localY = centerTile.y - baseY;

        return localX + localY * chunkSize;
    }

    private static int FloorDiv(int a, int b)
    {
        if (b == 0)
            return 0;

        int q = a / b;
        int r = a % b;

        if (r != 0 && ((r > 0) != (b > 0)))
            q--;

        return q;
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