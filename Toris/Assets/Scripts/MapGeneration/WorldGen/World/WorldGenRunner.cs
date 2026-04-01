using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class WorldGenRunner : MonoBehaviour
{
    private const double WarnUnloadMs = 2.0;
    private const double WarnGenerationMs = 10.0;
    private const double WarnApplyMs = 2.0;

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
    [SerializeField] private float genBudgetMs = 1f;

    [SerializeField] private bool clearOnDisable = false;

    [Header("Gate")]
    [SerializeField] private float gateCooldownSeconds = 1f;

    [Header("Pool")]
    [SerializeField] private WorldPoiPoolManager poiPool;

    #endregion

    #region Runtime State

    private WorldContext ctx;
    private ChunkStateStore chunkStateStore;
    private ChunkStreamingSystem chunkStreamingSystem;
    private ChunkProcessingPipeline chunkProcessingPipeline;
    private ChunkStreamingCoordinator chunkStreamingCoordinator;
    private WorldTransitionSystem worldTransitionSystem;
    private WorldNavigationLifecycle worldNavigationLifecycle;
    private WorldFeatureLifecycleSystem worldFeatureLifecycleSystem;
    private ChunkStreamingFrameResult lastStreamingFrameResult;
    private bool hasLastStreamingFrameResult;

    #endregion

    #region Public API
    public WorldContext Context => ctx;
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
        chunkStateStore = new ChunkStateStore();
        ChunkGenerator generator = new ChunkGenerator(ctx);
        TilemapApplier applier = new TilemapApplier(groundMap, waterMap, decorMap);

        EnsurePoiPool();
        EnsureNavWorld();

        worldNavigationLifecycle = new WorldNavigationLifecycle(TileNavWorld.Instance, groundMap, waterMap);
        worldNavigationLifecycle.Initialize(ctx.NavigationContributions);
        WorldSceneServices worldSceneServices = new WorldSceneServices(grid, TileNavWorld.Instance);
        chunkStreamingSystem = new ChunkStreamingSystem();

        WorldEncounterServices worldEncounterServices = new WorldEncounterServices(
            worldSceneServices,
            new PlayerLocatorService(followTarget),
            new EnemySpawnService(gameplayPoolManager));

        worldTransitionSystem = new WorldTransitionSystem(
            profile,
            biomeDb,
            worldNavigationLifecycle,
            ctx,
            chunkStateStore,
            chunkStreamingSystem,
            gateCooldownSeconds);

        WorldSiteActivationPipeline worldSiteActivationPipeline = new WorldSiteActivationPipeline(
            worldSceneServices,
            worldEncounterServices,
            chunkStateStore,
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
            worldNavigationLifecycle,
            generator,
            applier,
            worldFeatureLifecycleSystem,
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

        Camera cam = streamCamera != null ? streamCamera : Camera.main;
        if (cam == null)
            return;

        ChunkStreamingFrameSettings streamingSettings = new ChunkStreamingFrameSettings(
            preloadChunks,
            unloadHysteresisChunks,
            genBudgetMs,
            maxChunksPerFrame,
            maxUnloadRemovalsPerFrame: 1);

        ChunkStreamingRequest streamingRequest = new ChunkStreamingRequest(cam, streamingSettings);
        ChunkStreamingFrameResult streamingFrameResult = chunkStreamingCoordinator.ProcessFrame(streamingRequest);

        if (!streamingFrameResult.ProcessedFrame)
            return;

        lastStreamingFrameResult = streamingFrameResult;
        hasLastStreamingFrameResult = true;

        ChunkProcessingFrameStats processingFrameStats = streamingFrameResult.ProcessingStats;

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

        BuildOutputDiagnosticsSnapshot buildOutputDiagnostics =
            ctx != null && ctx.BuildOutput != null
                ? ctx.BuildOutput.CreateDiagnosticsSnapshot()
                : default;

        int loadedNavChunkCount = worldNavigationLifecycle != null
            ? worldNavigationLifecycle.LoadedNavChunkCount
            : 0;

        bool navigationContributionsBound = worldNavigationLifecycle != null &&
            worldNavigationLifecycle.HasNavigationContributions;

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

        bool hasStreamingBounds = hasLastStreamingFrameResult && lastStreamingFrameResult.ProcessedFrame;
        ChunkStreamingBounds streamingBounds = hasStreamingBounds
            ? lastStreamingFrameResult.View.Bounds
            : default;

        if (!streamingAnchorInitialized && hasLastStreamingFrameResult && lastStreamingFrameResult.ProcessedFrame)
        {
            streamingAnchorInitialized = true;
            streamingAnchorChunk = lastStreamingFrameResult.View.FocusChunk;
        }

        int currentBiomeIndex = worldTransitionSystem != null
            ? worldTransitionSystem.CurrentBiomeIndex
            : -1;

        float gateCooldownRemainingSeconds = worldTransitionSystem != null
            ? worldTransitionSystem.GateCooldownRemainingSeconds
            : 0f;

        bool sceneTransitionLoading = sceneTransitionService != null && sceneTransitionService.IsLoading;

        return new WorldGenDiagnosticsSnapshot(
            chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunks : null,
            chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunkCount : 0,
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
            buildOutputDiagnostics,
            loadedNavChunkCount,
            navigationContributionsBound,
            currentBiomeIndex,
            gateCooldownRemainingSeconds,
            sceneTransitionLoading,
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
    }
}
