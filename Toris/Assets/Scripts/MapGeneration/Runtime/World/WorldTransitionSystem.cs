using UnityEngine;

public sealed class WorldTransitionSystem : IGateTransitionService
{
    private readonly WorldProfile worldProfile;
    private readonly BiomeDatabase biomeDatabase;
    private readonly WorldNavigationLifecycle worldNavigationLifecycle;
    private readonly WorldContext worldContext;
    private readonly ChunkStateStore chunkStateStore;
    private readonly ChunkStreamingSystem chunkStreamingSystem;
    private readonly int runSeed;
    private readonly float gateCooldownSeconds;

    private WorldFeatureLifecycleSystem worldFeatureLifecycleSystem;
    private WorldStreamingRuntime worldStreamingRuntime;

    private int biomeIndex;
    private float lastGateTime = -999f;

    public int CurrentBiomeIndex => biomeIndex;
    public float GateCooldownRemainingSeconds
    {
        get
        {
            float elapsed = Time.time - lastGateTime;
            float remaining = gateCooldownSeconds - elapsed;
            return Mathf.Max(0f, remaining);
        }
    }

    public WorldTransitionSystem(
        WorldProfile worldProfile,
        BiomeDatabase biomeDatabase,
        WorldNavigationLifecycle worldNavigationLifecycle,
        WorldContext worldContext,
        ChunkStateStore chunkStateStore,
        ChunkStreamingSystem chunkStreamingSystem,
        float gateCooldownSeconds)
        : this(
            worldProfile,
            biomeDatabase,
            worldNavigationLifecycle,
            worldContext,
            chunkStateStore,
            chunkStreamingSystem,
            worldProfile != null ? worldProfile.seed : 0,
            gateCooldownSeconds)
    {
    }

    public WorldTransitionSystem(
        WorldProfile worldProfile,
        BiomeDatabase biomeDatabase,
        WorldNavigationLifecycle worldNavigationLifecycle,
        WorldContext worldContext,
        ChunkStateStore chunkStateStore,
        ChunkStreamingSystem chunkStreamingSystem,
        int runSeed,
        float gateCooldownSeconds)
    {
        this.worldProfile = worldProfile;
        this.biomeDatabase = biomeDatabase;
        this.worldNavigationLifecycle = worldNavigationLifecycle;
        this.worldContext = worldContext;
        this.chunkStateStore = chunkStateStore;
        this.chunkStreamingSystem = chunkStreamingSystem;
        this.runSeed = runSeed;
        this.gateCooldownSeconds = gateCooldownSeconds;
    }

    public void AttachLifecycleSystem(WorldFeatureLifecycleSystem worldFeatureLifecycleSystem)
    {
        this.worldFeatureLifecycleSystem = worldFeatureLifecycleSystem;
    }

    public void AttachStreamingRuntime(WorldStreamingRuntime worldStreamingRuntime)
    {
        this.worldStreamingRuntime = worldStreamingRuntime;
    }

    public void StartInitialBiome(int startingBiomeIndex, Vector2Int originTile)
    {
        TryStartBiome(startingBiomeIndex, originTile);
    }

    public bool CanUseGate()
    {
        if (Time.time - lastGateTime <= gateCooldownSeconds)
            return false;

        return TryResolveNextBiomeIndex(out _);
    }

    public bool UseGate(Vector2Int gateTile)
    {
        if (Time.time - lastGateTime <= gateCooldownSeconds)
            return false;

        if (!TryResolveNextBiomeIndex(out int nextBiomeIndex))
            return false;

        if (!TryStartBiome(nextBiomeIndex, gateTile))
            return false;

        lastGateTime = Time.time;
        return true;
    }

    public void ResetTransitionArtifacts()
    {
        lastGateTime = -999f;
    }

    public TransitionDiagnosticsSnapshot CreateDiagnosticsSnapshot(bool sceneTransitionLoading)
    {
        return new TransitionDiagnosticsSnapshot(
            biomeIndex,
            GateCooldownRemainingSeconds,
            sceneTransitionLoading);
    }

    private bool TryStartBiome(int nextBiomeIndex, Vector2Int originTile)
    {
        BiomeDefinition biomeDefinition = biomeDatabase != null ? biomeDatabase.Get(nextBiomeIndex) : null;
        if (biomeDefinition == null)
        {
            Debug.LogError($"Missing biome definition for index {nextBiomeIndex}");
            return false;
        }

        ClearCurrentBiomeRuntime();

        biomeIndex = nextBiomeIndex;

        int biomeSeed = ComputeBiomeSeed(runSeed, biomeIndex);

        BiomeInstance biomeInstance = new BiomeInstance(
            biomeIndex,
            biomeSeed,
            originTile,
            worldProfile.worldRadiusTiles
        );

        worldContext.BindBiome(biomeDefinition, biomeInstance);
        chunkStateStore?.Clear();

        worldNavigationLifecycle?.Initialize(worldContext.NavigationContributions);
        worldFeatureLifecycleSystem?.RebuildForCurrentBiome();

        Vector2Int spawnChunk = TileToChunk(originTile, worldProfile.chunkSize);
        chunkStreamingSystem?.SetStreamingAnchor(spawnChunk);
        return true;
    }

    private bool TryResolveNextBiomeIndex(out int nextBiomeIndex)
    {
        nextBiomeIndex = biomeIndex + 1;

        if (biomeDatabase == null || biomeDatabase.Count <= 0)
            return false;

        nextBiomeIndex %= biomeDatabase.Count;
        return biomeDatabase.Get(nextBiomeIndex) != null;
    }

    private void ClearCurrentBiomeRuntime()
    {
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy != null)
                enemy.RequestDespawn();
        }

        worldStreamingRuntime?.Reset();
        worldFeatureLifecycleSystem?.ClearAll();
    }

    private int ComputeBiomeSeed(int runSeed, int biomeIndex)
    {
        uint hash = DeterministicHash.Hash((uint)runSeed, biomeIndex, 0, 0xC0FFEEu);
        return (int)(hash & 0x7FFFFFFF);
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
