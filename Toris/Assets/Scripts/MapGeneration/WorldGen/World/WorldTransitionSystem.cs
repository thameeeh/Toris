using UnityEngine;

public sealed class WorldTransitionSystem : IGateTransitionService
{
    private readonly WorldProfile worldProfile;
    private readonly BiomeDatabase biomeDatabase;
    private readonly WorldSceneServices worldSceneServices;
    private readonly WorldContext worldContext;
    private readonly WorldRuntimeState worldRuntimeState;
    private readonly TilemapApplier tilemapApplier;
    private readonly ChunkStreamingSystem chunkStreamingSystem;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly float gateCooldownSeconds;

    private WorldFeatureLifecycle worldFeatureLifecycle;
    private PersistentWorldFeatureLifecycle persistentWorldFeatureLifecycle;

    private int biomeIndex;
    private float lastGateTime = -999f;

    public int CurrentBiomeIndex => biomeIndex;

    public WorldTransitionSystem(
        WorldProfile worldProfile,
        BiomeDatabase biomeDatabase,
        WorldSceneServices worldSceneServices,
        WorldContext worldContext,
        WorldRuntimeState worldRuntimeState,
        TilemapApplier tilemapApplier,
        ChunkStreamingSystem chunkStreamingSystem,
        WorldPoiPoolManager poiPoolManager,
        float gateCooldownSeconds)
    {
        this.worldProfile = worldProfile;
        this.biomeDatabase = biomeDatabase;
        this.worldSceneServices = worldSceneServices;
        this.worldContext = worldContext;
        this.worldRuntimeState = worldRuntimeState;
        this.tilemapApplier = tilemapApplier;
        this.chunkStreamingSystem = chunkStreamingSystem;
        this.poiPoolManager = poiPoolManager;
        this.gateCooldownSeconds = gateCooldownSeconds;
    }

    public void AttachLifecycles(
        WorldFeatureLifecycle worldFeatureLifecycle,
        PersistentWorldFeatureLifecycle persistentWorldFeatureLifecycle)
    {
        this.worldFeatureLifecycle = worldFeatureLifecycle;
        this.persistentWorldFeatureLifecycle = persistentWorldFeatureLifecycle;
    }

    public void StartInitialBiome(int startingBiomeIndex, Vector2Int originTile)
    {
        StartBiome(startingBiomeIndex, originTile);
    }

    public void UseGate(Vector2Int gateTile)
    {
        if (Time.time - lastGateTime <= gateCooldownSeconds)
            return;

        lastGateTime = Time.time;

        int nextBiomeIndex = biomeIndex + 1;
        if (biomeDatabase != null && biomeDatabase.Count > 0)
            nextBiomeIndex %= biomeDatabase.Count;

        StartBiome(nextBiomeIndex, gateTile);
    }

    public void ResetTransitionArtifacts()
    {
        lastGateTime = -999f;
        persistentWorldFeatureLifecycle?.ClearAll();
    }

    private void StartBiome(int nextBiomeIndex, Vector2Int originTile)
    {
        biomeIndex = nextBiomeIndex;

        BiomeDefinition biomeDefinition = biomeDatabase != null ? biomeDatabase.Get(biomeIndex) : null;
        if (biomeDefinition == null)
        {
            Debug.LogError($"Missing biome definition for index {biomeIndex}");
            return;
        }

        int biomeSeed = ComputeBiomeSeed(worldProfile.seed, biomeIndex);

        BiomeInstance biomeInstance = new BiomeInstance(
            biomeIndex,
            biomeSeed,
            originTile,
            worldProfile.worldRadiusTiles
        );

        worldContext.BindBiome(biomeDefinition, biomeInstance);
        worldRuntimeState?.Clear();

        worldSceneServices?.SetSiteBlockers(worldContext.SiteBlockers);

        worldFeatureLifecycle?.ClearAll();
        worldFeatureLifecycle?.RebuildPlacements();
        persistentWorldFeatureLifecycle?.ClearAll();
        ActivateRunGateForBiome();

        chunkStreamingSystem?.Reset();

        Vector2Int spawnChunk = TileToChunk(originTile, worldProfile.chunkSize);
        chunkStreamingSystem?.InitializeAnchor(spawnChunk);
    }

    private int ComputeBiomeSeed(int runSeed, int biomeIndex)
    {
        uint hash = DeterministicHash.Hash((uint)runSeed, biomeIndex, 0, 0xC0FFEEu);
        return (int)(hash & 0x7FFFFFFF);
    }

    private void ActivateRunGateForBiome()
    {
        if (worldContext?.Biome == null)
            return;

        WorldSiteDefinition runGateSiteDefinition = worldContext.Biome.RunGateSiteDefinition;
        if (runGateSiteDefinition == null || !runGateSiteDefinition.IsValid)
            return;

        Vector2Int tile = worldContext.ActiveBiome.OriginTile + worldContext.Biome.RunGateOffsetTiles;
        persistentWorldFeatureLifecycle?.ActivatePersistentSite(runGateSiteDefinition, tile);
    }

    private static Vector2Int TileToChunk(Vector2Int tile, int chunkSize)
    {
        int chunkX = FloorDiv(tile.x, chunkSize);
        int chunkY = FloorDiv(tile.y, chunkSize);
        return new Vector2Int(chunkX, chunkY);
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