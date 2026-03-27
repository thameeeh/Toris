using UnityEngine;

public sealed class WorldTransitionSystem : IGateTransitionService
{
    private readonly WorldProfile worldProfile;
    private readonly BiomeDatabase biomeDatabase;
    private readonly WorldContext worldContext;
    private readonly WorldRuntimeState worldRuntimeState;
    private readonly TilemapApplier tilemapApplier;
    private readonly ChunkStreamingSystem chunkStreamingSystem;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly Grid grid;
    private readonly float gateCooldownSeconds;

    private WorldFeatureLifecycle worldFeatureLifecycle;

    private int biomeIndex;
    private float lastGateTime = -999f;

    private GameObject runGateInstance;
    private Transform runGateRoot;

    public int CurrentBiomeIndex => biomeIndex;

    public WorldTransitionSystem(
        WorldProfile worldProfile,
        BiomeDatabase biomeDatabase,
        WorldContext worldContext,
        WorldRuntimeState worldRuntimeState,
        TilemapApplier tilemapApplier,
        ChunkStreamingSystem chunkStreamingSystem,
        WorldPoiPoolManager poiPoolManager,
        Grid grid,
        float gateCooldownSeconds)
    {
        this.worldProfile = worldProfile;
        this.biomeDatabase = biomeDatabase;
        this.worldContext = worldContext;
        this.worldRuntimeState = worldRuntimeState;
        this.tilemapApplier = tilemapApplier;
        this.chunkStreamingSystem = chunkStreamingSystem;
        this.poiPoolManager = poiPoolManager;
        this.grid = grid;
        this.gateCooldownSeconds = gateCooldownSeconds;
    }

    public void AttachLifecycle(WorldFeatureLifecycle worldFeatureLifecycle)
    {
        this.worldFeatureLifecycle = worldFeatureLifecycle;
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
        DespawnRunGate();
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

        if (TileNavWorld.Instance != null)
            TileNavWorld.Instance.SetSiteBlockers(worldContext.SiteBlockers);

        tilemapApplier.ClearAll();
        worldFeatureLifecycle?.ClearAll();
        worldFeatureLifecycle?.RebuildPlacements();
        SpawnRunGateForBiome();

        chunkStreamingSystem?.Reset();

        Vector2Int spawnChunk = TileToChunk(originTile, worldProfile.chunkSize);
        chunkStreamingSystem?.InitializeAnchor(spawnChunk);
    }

    private int ComputeBiomeSeed(int runSeed, int biomeIndex)
    {
        uint hash = DeterministicHash.Hash((uint)runSeed, biomeIndex, 0, 0xC0FFEEu);
        return (int)(hash & 0x7FFFFFFF);
    }

    private void EnsureRunGateRoot()
    {
        if (poiPoolManager == null)
            return;

        Transform activeRoot = poiPoolManager.GetActiveRoot();
        if (runGateRoot == null)
        {
            GameObject rootObject = new GameObject("RunGate");
            rootObject.transform.SetParent(activeRoot, false);
            runGateRoot = rootObject.transform;
        }
    }

    private void DespawnRunGate()
    {
        if (runGateInstance != null)
        {
            poiPoolManager.Release(runGateInstance);
            runGateInstance = null;
        }
    }

    private void SpawnRunGateForBiome()
    {
        DespawnRunGate();

        GameObject prefab = worldContext.Biome != null ? worldContext.Biome.endGatePrefab : null;
        if (prefab == null || grid == null || poiPoolManager == null)
            return;

        EnsureRunGateRoot();

        Vector2Int tile = worldContext.ActiveBiome.OriginTile + worldContext.Biome.RunGateOffsetTiles;
        Vector3 worldPosition = grid.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));

        runGateInstance = poiPoolManager.Spawn(prefab, worldPosition, Quaternion.identity, runGateRoot);
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