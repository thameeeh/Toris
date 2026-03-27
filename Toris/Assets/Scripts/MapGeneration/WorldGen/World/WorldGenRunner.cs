using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public sealed class WorldGenRunner : MonoBehaviour
{
    #region Inspector

    [Header("Assets")]
    [SerializeField] private WorldProfile profile;
    [SerializeField] private Grid grid;
    [SerializeField] private BiomeDatabase biomeDb;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundMap;
    [SerializeField] private Tilemap waterMap;
    [SerializeField] private Tilemap decorMap;

    [Header("Streaming")]
    [SerializeField] private Transform followTarget;

    [SerializeField] private Camera streamCamera;

    [SerializeField] private int unloadHysteresisChunks = 1;
    [SerializeField] private int maxChunksPerFrame = 2;
    [SerializeField] private int preloadChunks = 1;
    //[SerializeField] private int anchorShiftThreshold = 1; // currently unused kept for future/compat
    [SerializeField] private float genBudgetMs = 1f;

    [SerializeField] private bool clearOnDisable = false;

    [Header("Gate")]
    [SerializeField] private float gateCooldownSeconds = 1f;

    [Header("Pool")]
    [SerializeField] private WorldPoiPoolManager poiPool;

    #endregion

    #region Runtime State
    private int biomeIndex = 0;
    private float lastGateTime = -999f;

    private WorldContext ctx;
    private WorldRuntimeState runtimeState;
    private ChunkGenerator generator;
    private TilemapApplier applier;
    private ChunkStreamingSystem chunkStreamingSystem;

    private GameObject runGateInstance;
    private Transform runGateRoot;

    public System.Action<Vector2Int, ChunkStateStore.ChunkState> OnChunkLoaded;
    public System.Action<Vector2Int, ChunkStateStore.ChunkState> OnChunkUnloading;

    // refactor
    private WorldFeatureLifecycle worldFeatureLifecycle;
    //

    private const uint GateSpawnSalt = 0x6A7E1234u;
    private const uint WolfDenSpawnSalt = 0xA11CE5EDu;

    public static uint GateSpawnSaltValue => GateSpawnSalt;
    public static uint WolfDenSpawnSaltValue => WolfDenSpawnSalt;

    public Grid Grid => grid;
    #endregion

    #region Public API
    public int PreloadChunkCount => preloadChunks;
    public int UnloadHysteresisChunkCount => unloadHysteresisChunks;
    public Camera StreamingCamera => streamCamera != null ? streamCamera : Camera.main;
    public WorldProfile Profile => profile;
    public WorldContext Context => ctx;
    public WorldRuntimeState RuntimeState => runtimeState;

    public bool IsChunkLoaded(Vector2Int chunk)
    {
        return chunkStreamingSystem != null && chunkStreamingSystem.IsChunkLoaded(chunk);
    }
    public IReadOnlyCollection<Vector2Int> LoadedChunks =>
    chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunks : null;

    public int LoadedChunkCount =>
        chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunkCount : 0;
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (profile == null)
        {
            Debug.LogError($"{nameof(WorldGenRunner)} missing profile.", this);
            enabled = false;
            return;
        }

        if (grid == null)
        {
            Debug.LogError($"{nameof(WorldGenRunner)} missing grid.", this);
            enabled = false;
            return;
        }

        if (poiPool == null)
        {
            poiPool = gameObject.GetComponent<WorldPoiPoolManager>();
            if (poiPool == null)
                poiPool = gameObject.AddComponent<WorldPoiPoolManager>();
        }

        ctx = new WorldContext(profile);
        runtimeState = new WorldRuntimeState();
        generator = new ChunkGenerator(ctx);
        applier = new TilemapApplier(groundMap, waterMap, decorMap);

        EnsurePoiPool();
        EnsureNavWorld();

        chunkStreamingSystem = new ChunkStreamingSystem();
        worldFeatureLifecycle = new WorldFeatureLifecycle(this, ctx, runtimeState, poiPool);

        Vector2Int spawnTile = WorldToTile(profile.spawnPosTiles);

        StartBiome(biomeIndex, spawnTile);

        Vector2Int spawnChunk = TileToChunk(spawnTile, profile.chunkSize);
        chunkStreamingSystem.InitializeAnchor(spawnChunk);
    }

    private void Update()
    {
        if (profile == null || groundMap == null || waterMap == null || decorMap == null)
            return;

        Vector2 focusWorld = followTarget != null
            ? (Vector2)followTarget.position
            : profile.spawnPosTiles;

        Camera cam = streamCamera != null ? streamCamera : Camera.main;
        if (cam == null)
            return;

        GetCameraChunk(cam, out Vector2Int loadMinChunk, out Vector2Int loadMaxChunk);

        int paddingChunks = Mathf.Max(0, profile.viewDistanceChunks) + Mathf.Max(0, preloadChunks);

        loadMinChunk -= new Vector2Int(paddingChunks, paddingChunks);
        loadMaxChunk += new Vector2Int(paddingChunks, paddingChunks);

        Vector2Int unloadMinChunk = loadMinChunk - new Vector2Int(unloadHysteresisChunks, unloadHysteresisChunks);
        Vector2Int unloadMaxChunk = loadMaxChunk + new Vector2Int(unloadHysteresisChunks, unloadHysteresisChunks);

        chunkStreamingSystem.EnqueueNeededChunks(loadMinChunk, loadMaxChunk);

        double budgetMs = Mathf.Max(0.1f, genBudgetMs);

        long frameStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        long stopwatchFrequency = System.Diagnostics.Stopwatch.Frequency;

        double ElapsedMs()
            => (System.Diagnostics.Stopwatch.GetTimestamp() - frameStartTicks) * 1000.0 / stopwatchFrequency;

        int hardChunkCap = Mathf.Max(0, maxChunksPerFrame);
        int generatedChunkCount = 0;

        long totalGenerationTicks = 0;
        long totalApplyTicks = 0;

        double estimatedChunkMs = 6.0;

        while (generatedChunkCount < hardChunkCap)
        {
            double elapsedMs = ElapsedMs();
            double remainingMs = budgetMs - elapsedMs;
            if (remainingMs <= 0.0)
                break;

            if (generatedChunkCount > 0)
            {
                double generationMsSoFar = totalGenerationTicks * 1000.0 / stopwatchFrequency;
                double applyMsSoFar = totalApplyTicks * 1000.0 / stopwatchFrequency;
                estimatedChunkMs = (generationMsSoFar + applyMsSoFar) / generatedChunkCount;
            }

            const double safetyMs = 0.25;
            if (generatedChunkCount > 0 && remainingMs < (estimatedChunkMs + safetyMs))
                break;

            if (!chunkStreamingSystem.TryDequeueNextChunk(loadMinChunk, loadMaxChunk, out Vector2Int chunkCoord))
                break;

            long generationStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            ChunkResult chunk = generator.GenerateChunk(chunkCoord);
            long generationEndTicks = System.Diagnostics.Stopwatch.GetTimestamp();

            applier.Apply(chunk);

            TileNavWorld.Instance?.BuildNavChunk(chunkCoord, profile.chunkSize);
            worldFeatureLifecycle?.ActivateChunk(chunkCoord);

            long applyEndTicks = System.Diagnostics.Stopwatch.GetTimestamp();

            chunkStreamingSystem.MarkChunkLoaded(chunkCoord);
            NotifyChunkLoaded(chunkCoord);

            totalGenerationTicks += (generationEndTicks - generationStartTicks);
            totalApplyTicks += (applyEndTicks - generationEndTicks);
            generatedChunkCount++;
        }

        long unloadStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        UnloadOutside(unloadMinChunk, unloadMaxChunk);
        long unloadEndTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        double unloadMs = (unloadEndTicks - unloadStartTicks) * 1000.0 / stopwatchFrequency;

        double generationMsTotal = totalGenerationTicks * 1000.0 / stopwatchFrequency;
        double applyMsTotal = totalApplyTicks * 1000.0 / stopwatchFrequency;

        const double WarnUnloadMs = 2.0;
        const double WarnGenerationMs = 10.0;
        const double WarnApplyMs = 2.0;

        if (unloadMs >= WarnUnloadMs || generationMsTotal >= WarnGenerationMs || applyMsTotal >= WarnApplyMs)
        {
            Debug.Log(
                $"[WorldGen] unload={(int)unloadMs}ms, " +
                $"genChunks={generatedChunkCount}/{hardChunkCap} budget={budgetMs:F1}ms " +
                $"gen={generationMsTotal:F2}ms apply={applyMsTotal:F2}ms, " +
                $"queue={(chunkStreamingSystem != null ? chunkStreamingSystem.GenerationQueueCount : 0)} " +
                $"loaded={(chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunkCount : 0)} " +
                $"chunkSize={profile.chunkSize}"
            );
        }
    }
    private void OnDisable()
    {
        if (!clearOnDisable || profile == null)
            return;

        worldFeatureLifecycle?.ClearAll();
        DespawnRunGate();

        if (chunkStreamingSystem != null && chunkStreamingSystem.LoadedChunks != null)
        {
            foreach (Vector2Int chunkCoord in chunkStreamingSystem.LoadedChunks)
                applier.ClearChunk(chunkCoord, profile.chunkSize);
        }

        chunkStreamingSystem?.Reset();
    }
    #endregion

    #region Biome Control

    private void StartBiome(int nextBiomeIndex, Vector2Int originTile)
    {
        biomeIndex = nextBiomeIndex;

        BiomeDefinition def = biomeDb != null ? biomeDb.Get(biomeIndex) : null;
        if (def == null)
        {
            Debug.LogError($"Missing biome definition for index {biomeIndex}", this);
            return;
        }

        int biomeSeed = ComputeBiomeSeed(profile.seed, biomeIndex);

        var biomeInstance = new BiomeInstance(
            biomeIndex,
            biomeSeed,
            originTile,
            profile.worldRadiusTiles
        );

        ctx.BindBiome(def, biomeInstance);
        runtimeState?.Clear();

        if (TileNavWorld.Instance != null)
            TileNavWorld.Instance.SetSiteBlockers(ctx.SiteBlockers);

        applier.ClearAll();
        worldFeatureLifecycle?.ClearAll();
        worldFeatureLifecycle?.RebuildPlacements();
        SpawnRunGateForBiome();

        chunkStreamingSystem?.Reset();

        Vector2Int spawnChunk = TileToChunk(originTile, profile.chunkSize);
        chunkStreamingSystem?.InitializeAnchor(spawnChunk);
    }
    private int ComputeBiomeSeed(int runSeed, int biomeIdx)
    {
        uint h = DeterministicHash.Hash((uint)runSeed, biomeIdx, 0, 0xC0FFEEu);
        return (int)(h & 0x7FFFFFFF);
    }

    #endregion

    #region Streaming - Queue/Unload

    private void UnloadOutside(Vector2Int keepMinChunk, Vector2Int keepMaxChunk)
    {
        if (chunkStreamingSystem == null)
            return;

        const int maxRemovalsPerFrame = 1;

        List<Vector2Int> chunksToUnload = chunkStreamingSystem.CollectChunksToUnload(
            keepMinChunk,
            keepMaxChunk,
            maxRemovalsPerFrame);

        if (chunksToUnload == null || chunksToUnload.Count == 0)
            return;

        for (int i = 0; i < chunksToUnload.Count; i++)
        {
            Vector2Int chunkCoord = chunksToUnload[i];

            NotifyChunkUnloading(chunkCoord);
            worldFeatureLifecycle?.DeactivateChunk(chunkCoord);
            applier.ClearChunk(chunkCoord, profile.chunkSize);
            TileNavWorld.Instance?.ClearNavChunk(chunkCoord);

            chunkStreamingSystem.MarkChunkUnloaded(chunkCoord);
        }
    }
    private static bool IsChunkIn(Vector2Int c, Vector2Int minChunk, Vector2Int maxChunk)
    {
        return c.x >= minChunk.x && c.x <= maxChunk.x &&
               c.y >= minChunk.y && c.y <= maxChunk.y;
    }
    #endregion

    #region Camera Rect -> Chunk Rect

    private void GetCameraChunk(Camera cam, out Vector2Int minChunk, out Vector2Int maxChunk)
    {
        float zPlane = 0f;
        float dist = DistanceAlongCameraForwardToZPlane(cam, zPlane);

        Vector3 w0 = cam.ViewportToWorldPoint(new Vector3(0f, 0f, dist));
        Vector3 w1 = cam.ViewportToWorldPoint(new Vector3(1f, 0f, dist));
        Vector3 w2 = cam.ViewportToWorldPoint(new Vector3(0f, 1f, dist));
        Vector3 w3 = cam.ViewportToWorldPoint(new Vector3(1f, 1f, dist));

        Vector3Int c0 = grid.WorldToCell(w0);
        Vector3Int c1 = grid.WorldToCell(w1);
        Vector3Int c2 = grid.WorldToCell(w2);
        Vector3Int c3 = grid.WorldToCell(w3);

        int minX = Mathf.Min(c0.x, c1.x, c2.x, c3.x);
        int maxX = Mathf.Max(c0.x, c1.x, c2.x, c3.x);
        int minY = Mathf.Min(c0.y, c1.y, c2.y, c3.y);
        int maxY = Mathf.Max(c0.y, c1.y, c2.y, c3.y);

        minX -= 1; minY -= 1;
        maxX += 1; maxY += 1;

        Vector2Int minTile = new Vector2Int(minX, minY);
        Vector2Int maxTile = new Vector2Int(maxX, maxY);

        minChunk = TileToChunk(minTile, profile.chunkSize);
        maxChunk = TileToChunk(maxTile, profile.chunkSize);
    }

    private static float DistanceAlongCameraForwardToZPlane(Camera cam, float zPlane)
    {
        Vector3 camPos = cam.transform.position;
        Vector3 fwd = cam.transform.forward;

        float denom = fwd.z;
        if (Mathf.Abs(denom) < 0.00001f)
            return cam.nearClipPlane;

        float t = (zPlane - camPos.z) / denom;
        if (t < 0f) t = -t;

        return Mathf.Max(cam.nearClipPlane, t);
    }

    #endregion

    #region Coordinates / Math

    private Vector2Int WorldToTile(Vector2 world)
    {
        if (grid != null)
        {
            Vector3Int cell = grid.WorldToCell((Vector3)world);
            return new Vector2Int(cell.x, cell.y);
        }

        return new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
    }

    private static Vector2Int TileToChunk(Vector2Int tile, int chunkSize)
    {
        int cx = FloorDiv(tile.x, chunkSize);
        int cy = FloorDiv(tile.y, chunkSize);
        return new Vector2Int(cx, cy);
    }

    private static int FloorDiv(int a, int b)
    {
        if (b == 0) return 0;

        int q = a / b;
        int r = a % b;

        if (r != 0 && ((r > 0) != (b > 0)))
            q--;

        return q;
    }

    #endregion
    #region Helpers
    // Chunk persistance helpers
    private void NotifyChunkLoaded(Vector2Int chunkCoord)
    {
        if (runtimeState == null)
            return;

        ChunkStateStore.ChunkState chunkState = runtimeState.ChunkStates.GetChunkState(chunkCoord);
        OnChunkLoaded?.Invoke(chunkCoord, chunkState);
    }

    private void NotifyChunkUnloading(Vector2Int chunkCoord)
    {
        if (runtimeState == null)
            return;

        ChunkStateStore.ChunkState chunkState = runtimeState.ChunkStates.GetChunkState(chunkCoord);
        OnChunkUnloading?.Invoke(chunkCoord, chunkState);
    }

    private void EnsurePoiPool()
    {
        if (poiPool == null)
            poiPool = GetComponent<WorldPoiPoolManager>();

        if (poiPool == null)
            poiPool = gameObject.AddComponent<WorldPoiPoolManager>();
    }

    public void UseGate(Vector2Int gateTile)
    {
        if (Time.time - lastGateTime <= gateCooldownSeconds)
            return;

        lastGateTime = Time.time;

        int next = biomeIndex + 1;
        if (biomeDb != null && biomeDb.Count > 0)
            next %= biomeDb.Count;

        StartBiome(next, gateTile);
    }

    private void EnsureRunGateRoot()
    {
        if (poiPool == null) return;

        var active = poiPool.GetActiveRoot();
        if (runGateRoot == null)
        {
            var go = new GameObject("RunGate");
            go.transform.SetParent(active, false);
            runGateRoot = go.transform;
        }
    }

    private void DespawnRunGate()
    {
        if (runGateInstance != null)
        {
            poiPool.Release(runGateInstance);
            runGateInstance = null;
        }
    }

    private void SpawnRunGateForBiome()
    {
        DespawnRunGate();

        var prefab = ctx.Biome != null ? ctx.Biome.endGatePrefab : null;
        if (prefab == null || grid == null || poiPool == null || ctx == null)
            return;

        EnsureRunGateRoot();

        Vector2Int tile = ctx.ActiveBiome.OriginTile + ctx.Biome.RunGateOffsetTiles;
        Vector3 worldPos = grid.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));

        runGateInstance = poiPool.Spawn(prefab, worldPos, Quaternion.identity, runGateRoot);
    }

    #endregion
    // diagnostics
    public WorldGenDiagnosticsSnapshot CreateDiagnosticsSnapshot()
    {
        int activeSiteChunkCount = worldFeatureLifecycle != null
            ? worldFeatureLifecycle.GetActiveSiteChunkCount()
            : 0;

        int activeSiteCount = worldFeatureLifecycle != null
            ? worldFeatureLifecycle.GetActiveSiteCount()
            : 0;

        int totalPlacedSiteCount = worldFeatureLifecycle != null
            ? worldFeatureLifecycle.GetTotalPlacedSiteCount()
            : 0;

        return new WorldGenDiagnosticsSnapshot(
            LoadedChunks,
            LoadedChunkCount,
            preloadChunks,
            unloadHysteresisChunks,
            activeSiteChunkCount,
            activeSiteCount,
            totalPlacedSiteCount,
            streamCamera != null ? streamCamera : Camera.main,
            profile);
    }
    // Tile nav
    private void EnsureNavWorld()
    {
        var nav = TileNavWorld.Instance;
        if (nav == null)
        {
            var go = new GameObject("TileNavWorld");
            nav = go.AddComponent<TileNavWorld>();
            DontDestroyOnLoad(go);
        }

        nav.Initialize(groundMap, waterMap);
        nav.SetSiteBlockers(ctx.SiteBlockers);
    }
}
