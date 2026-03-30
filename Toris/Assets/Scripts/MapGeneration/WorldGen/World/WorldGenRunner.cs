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

    [Header("Gameplay")]
    [SerializeField] private GameplayPoolManager gameplayPoolManager;

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

    private WorldContext ctx;
    private WorldRuntimeState runtimeState;
    private ChunkGenerator generator;
    private TilemapApplier applier;
    private ChunkStreamingSystem chunkStreamingSystem;
    private ChunkProcessingPipeline chunkProcessingPipeline;
    private WorldTransitionSystem worldTransitionSystem;
    private WorldSceneServices worldSceneServices;
    private WorldEncounterServices worldEncounterServices;
    private PersistentWorldFeatureLifecycle persistentWorldFeatureLifecycle;
    private WorldSiteActivationPipeline worldSiteActivationPipeline;

    public System.Action<Vector2Int, ChunkStateStore.ChunkState> OnChunkLoaded;
    public System.Action<Vector2Int, ChunkStateStore.ChunkState> OnChunkUnloading;

    private WorldFeatureLifecycle worldFeatureLifecycle;

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

        if (followTarget == null)
        {
            Debug.LogWarning($"{nameof(WorldGenRunner)} has no followTarget assigned. PlayerLocatorService will return null.", this);
        }

        if (gameplayPoolManager == null)
        {
            Debug.LogWarning($"{nameof(WorldGenRunner)} has no GameplayPoolManager assigned. Enemy spawning through encounter services will fail.", this);
        }

        ctx = new WorldContext(profile);
        runtimeState = new WorldRuntimeState();
        generator = new ChunkGenerator(ctx);
        applier = new TilemapApplier(groundMap, waterMap, decorMap);

        EnsurePoiPool();
        EnsureNavWorld();

        worldSceneServices = new WorldSceneServices(grid, TileNavWorld.Instance);
        chunkStreamingSystem = new ChunkStreamingSystem();

        worldEncounterServices = new WorldEncounterServices(
            worldSceneServices,
            new PlayerLocatorService(followTarget),
            new EnemySpawnService(gameplayPoolManager));

        worldTransitionSystem = new WorldTransitionSystem(
            profile,
            biomeDb,
            worldSceneServices,
            ctx,
            runtimeState,
            applier,
            chunkStreamingSystem,
            poiPool,
            gateCooldownSeconds);

        worldSiteActivationPipeline = new WorldSiteActivationPipeline(
            worldSceneServices,
            worldEncounterServices,
            runtimeState,
            poiPool,
            worldTransitionSystem);

        worldFeatureLifecycle = new WorldFeatureLifecycle(
            ctx,
            poiPool,
            worldSiteActivationPipeline);

        persistentWorldFeatureLifecycle = new PersistentWorldFeatureLifecycle(
            ctx,
            poiPool,
            worldSiteActivationPipeline);

        chunkProcessingPipeline = new ChunkProcessingPipeline(
            profile,
            worldSceneServices,
            generator,
            applier,
            worldFeatureLifecycle,
            runtimeState,
            chunkStreamingSystem);

        worldTransitionSystem.AttachLifecycles(
            worldFeatureLifecycle,
            persistentWorldFeatureLifecycle);

        Vector2Int spawnTile = WorldToTile(profile.spawnPosTiles);
        worldTransitionSystem.StartInitialBiome(0, spawnTile);
    }

    private void Update()
    {
        if (profile == null || groundMap == null || waterMap == null || decorMap == null)
            return;

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

        ChunkProcessingFrameStats processingFrameStats = chunkProcessingPipeline.ProcessFrame(
            loadMinChunk,
            loadMaxChunk,
            unloadMinChunk,
            unloadMaxChunk,
            Mathf.Max(0.1f, genBudgetMs),
            Mathf.Max(0, maxChunksPerFrame),
            maxUnloadRemovalsPerFrame: 1,
            onChunkLoaded: HandleChunkLoaded,
            onChunkUnloading: HandleChunkUnloading);

        const double WarnUnloadMs = 2.0;
        const double WarnGenerationMs = 10.0;
        const double WarnApplyMs = 2.0;

        if (processingFrameStats.UnloadMs >= WarnUnloadMs ||
            processingFrameStats.GenerationMsTotal >= WarnGenerationMs ||
            processingFrameStats.ApplyMsTotal >= WarnApplyMs)
        {
            Debug.Log(
                $"[WorldGen] unload={(int)processingFrameStats.UnloadMs}ms, " +
                $"genChunks={processingFrameStats.GeneratedChunkCount}/{Mathf.Max(0, maxChunksPerFrame)} " +
                $"budget={Mathf.Max(0.1f, genBudgetMs):F1}ms " +
                $"gen={processingFrameStats.GenerationMsTotal:F2}ms " +
                $"apply={processingFrameStats.ApplyMsTotal:F2}ms, " +
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
        persistentWorldFeatureLifecycle?.ClearAll();
        worldTransitionSystem?.ResetTransitionArtifacts();
        chunkProcessingPipeline?.ClearLoadedChunks();
        chunkStreamingSystem?.Reset();
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
    private void HandleChunkLoaded(Vector2Int chunkCoord, ChunkStateStore.ChunkState chunkState)
    {
        OnChunkLoaded?.Invoke(chunkCoord, chunkState);
    }

    private void HandleChunkUnloading(Vector2Int chunkCoord, ChunkStateStore.ChunkState chunkState)
    {
        OnChunkUnloading?.Invoke(chunkCoord, chunkState);
    }

    private void EnsurePoiPool()
    {
        if (poiPool == null)
            poiPool = GetComponent<WorldPoiPoolManager>();

        if (poiPool == null)
            poiPool = gameObject.AddComponent<WorldPoiPoolManager>();
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
