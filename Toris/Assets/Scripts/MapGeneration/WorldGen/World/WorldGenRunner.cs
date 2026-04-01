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
    [SerializeField] private SceneTransitionService sceneTransitionService;

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
    private ChunkStreamingCoordinator chunkStreamingCoordinator;
    private WorldTransitionSystem worldTransitionSystem;
    private WorldSceneServices worldSceneServices;
    private WorldEncounterServices worldEncounterServices;
    private WorldFeatureLifecycleSystem worldFeatureLifecycleSystem;
    private WorldSiteActivationPipeline worldSiteActivationPipeline;

    public System.Action<Vector2Int, ChunkStateStore.ChunkState> OnChunkLoaded;
    public System.Action<Vector2Int, ChunkStateStore.ChunkState> OnChunkUnloading;

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

        if (sceneTransitionService == null)
        {
            sceneTransitionService = SceneTransitionService.Instance;
            if (sceneTransitionService == null)
                sceneTransitionService = FindFirstObjectByType<SceneTransitionService>();
        }

        if (sceneTransitionService == null)
        {
            Debug.LogWarning($"{nameof(WorldGenRunner)} has no SceneTransitionService assigned. Run gate site transitions will be unavailable.", this);
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
            chunkStreamingSystem,
            gateCooldownSeconds);

        worldSiteActivationPipeline = new WorldSiteActivationPipeline(
            worldSceneServices,
            worldEncounterServices,
            runtimeState,
            poiPool,
            worldTransitionSystem,
            sceneTransitionService);

        WorldFeatureLifecycle chunkFeatureLifecycle = new WorldFeatureLifecycle(
            ctx,
            poiPool,
            worldSiteActivationPipeline);

        PersistentWorldFeatureLifecycle persistentFeatureLifecycle = new PersistentWorldFeatureLifecycle(
            ctx,
            poiPool,
            worldSiteActivationPipeline);

        worldFeatureLifecycleSystem = new WorldFeatureLifecycleSystem(
            ctx,
            chunkFeatureLifecycle,
            persistentFeatureLifecycle);

        chunkProcessingPipeline = new ChunkProcessingPipeline(
            profile,
            worldSceneServices,
            generator,
            applier,
            worldFeatureLifecycleSystem,
            runtimeState,
            chunkStreamingSystem);

        chunkStreamingCoordinator = new ChunkStreamingCoordinator(
            profile,
            grid,
            chunkStreamingSystem,
            chunkProcessingPipeline);

        worldTransitionSystem.AttachLifecycleSystem(worldFeatureLifecycleSystem);

        Vector2Int spawnTile = WorldToTile(profile.spawnPosTiles);
        worldTransitionSystem.StartInitialBiome(0, spawnTile);
    }

    private void Update()
    {
        if (profile == null || groundMap == null || waterMap == null || decorMap == null)
            return;

        Camera cam = StreamingCamera;
        if (cam == null)
            return;

        ChunkStreamingFrameSettings streamingSettings = new ChunkStreamingFrameSettings(
            preloadChunks,
            unloadHysteresisChunks,
            genBudgetMs,
            maxChunksPerFrame,
            maxUnloadRemovalsPerFrame: 1);

        ChunkStreamingRequest streamingRequest = new ChunkStreamingRequest(cam, streamingSettings);
        ChunkStreamingFrameResult streamingFrameResult = chunkStreamingCoordinator.ProcessFrame(
            streamingRequest,
            HandleChunkLoaded,
            HandleChunkUnloading);

        if (!streamingFrameResult.ProcessedFrame)
            return;

        ChunkProcessingFrameStats processingFrameStats = streamingFrameResult.ProcessingStats;

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

        worldFeatureLifecycleSystem?.ClearAll();
        worldTransitionSystem?.ResetTransitionArtifacts();
        chunkProcessingPipeline?.ClearLoadedChunks();
        chunkStreamingSystem?.Reset();
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
        int activeSiteChunkCount = worldFeatureLifecycleSystem != null
            ? worldFeatureLifecycleSystem.GetActiveSiteChunkCount()
            : 0;

        int activePersistentSiteCount = worldFeatureLifecycleSystem != null
            ? worldFeatureLifecycleSystem.GetActivePersistentSiteCount()
            : 0;

        int activeSiteCount = worldFeatureLifecycleSystem != null
            ? worldFeatureLifecycleSystem.GetActiveSiteCount()
            : 0;

        int totalPlacedSiteCount = worldFeatureLifecycleSystem != null
            ? worldFeatureLifecycleSystem.GetTotalPlacedSiteCount()
            : 0;

        int generationQueueCount = chunkStreamingSystem != null
            ? chunkStreamingSystem.GenerationQueueCount
            : 0;

        int queuedChunkCount = chunkStreamingSystem != null
            ? chunkStreamingSystem.QueuedChunkCount
            : 0;

        bool streamingAnchorInitialized = chunkStreamingSystem != null && chunkStreamingSystem.AnchorInitialized;

        Vector2Int streamingAnchorChunk = chunkStreamingSystem != null
            ? chunkStreamingSystem.StreamingAnchorChunk
            : default;

        ChunkStreamingFrameResult lastStreamingFrameResult = default;
        bool hasLastStreamingFrameResult = chunkStreamingCoordinator != null &&
            chunkStreamingCoordinator.TryGetLastFrameResult(out lastStreamingFrameResult);

        bool hasStreamingBounds = hasLastStreamingFrameResult && lastStreamingFrameResult.HasView;
        ChunkStreamingBounds streamingBounds = hasStreamingBounds
            ? lastStreamingFrameResult.View.Bounds
            : default;

        if (!streamingAnchorInitialized && hasLastStreamingFrameResult && lastStreamingFrameResult.HasView)
        {
            streamingAnchorInitialized = true;
            streamingAnchorChunk = lastStreamingFrameResult.View.FocusChunk;
        }

        return new WorldGenDiagnosticsSnapshot(
            LoadedChunks,
            LoadedChunkCount,
            generationQueueCount,
            queuedChunkCount,
            preloadChunks,
            unloadHysteresisChunks,
            hasStreamingBounds,
            streamingBounds,
            streamingAnchorInitialized,
            streamingAnchorChunk,
            activeSiteChunkCount,
            activePersistentSiteCount,
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
        nav.SetNavigationBlockers(ctx.NavigationBlockers);
    }
}
