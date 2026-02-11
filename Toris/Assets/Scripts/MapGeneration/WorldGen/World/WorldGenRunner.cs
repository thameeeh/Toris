using System.Collections.Generic;
using UnityEngine;
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
    private ChunkGenerator generator;
    private TilemapApplier applier;

    private Vector2Int streamingAnchorChunk;
    private bool anchorInitialized;

    private GameObject runGateInstance;
    private Transform runGateRoot;

    private readonly Queue<Vector2Int> generateQueue = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> queued = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> loaded = new HashSet<Vector2Int>();

    public System.Action<Vector2Int, ChunkStateStore.ChunkState> OnChunkLoaded;
    public System.Action<Vector2Int, ChunkStateStore.ChunkState> OnChunkUnloading;

    private readonly Dictionary<Vector2Int, List<GameObject>> spawnedByChunk
    = new Dictionary<Vector2Int, List<GameObject>>();

    private readonly Dictionary<Vector2Int, List<Vector2Int>> gateCentersByChunk = new();
    private readonly Dictionary<Vector2Int, List<Vector2Int>> denCentersByChunk = new();

    private readonly Dictionary<Vector2Int, Transform> activeChunkRoots = new();
    private readonly Dictionary<Vector2Int, Transform> cachedChunkRoots = new();

    private Transform poiChunkRootsActive;
    private Transform poiChunkRootsCached;

    private const uint GateSpawnSalt = 0x6A7E1234u;
    private const uint WolfDenSpawnSalt = 0xA11CE5EDu;
    #endregion

    #region Public API

    public WorldContext Context => ctx;

    public bool IsChunkLoaded(Vector2Int chunk)
    {
        return loaded.Contains(chunk);
    }

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
        generator = new ChunkGenerator(ctx);
        applier = new TilemapApplier(groundMap, waterMap, decorMap);

        EnsurePoiPool();
        EnsureNavWorld();

        Vector2Int spawnTile = WorldToTile(profile.spawnPosTiles);

        StartBiome(biomeIndex, spawnTile);
        streamingAnchorChunk = TileToChunk(spawnTile, profile.chunkSize);
        anchorInitialized = true;
    }

    private void Update()
    {
        if (profile == null || groundMap == null || waterMap == null || decorMap == null)
            return;

        Vector2 focusWorld = followTarget != null
            ? (Vector2)followTarget.position
            : profile.spawnPosTiles;

        Vector2Int focusTile;
        if (grid != null)
        {
            Vector3Int cell = grid.WorldToCell((Vector3)focusWorld);
            focusTile = new Vector2Int(cell.x, cell.y);
        }
        else
        {
            focusTile = new Vector2Int(
                Mathf.FloorToInt(focusWorld.x),
                Mathf.FloorToInt(focusWorld.y)
            );
        }

        Camera cam = streamCamera != null ? streamCamera : Camera.main;
        if (cam == null)
            return;

        GetCameraChunk(cam, out Vector2Int loadMinChunk, out Vector2Int loadMaxChunk);

        int pad = Mathf.Max(0, profile.viewDistanceChunks) + Mathf.Max(0, preloadChunks);

        loadMinChunk -= new Vector2Int(pad, pad);
        loadMaxChunk += new Vector2Int(pad, pad);

        Vector2Int unloadMinChunk = loadMinChunk - new Vector2Int(unloadHysteresisChunks, unloadHysteresisChunks);
        Vector2Int unloadMaxChunk = loadMaxChunk + new Vector2Int(unloadHysteresisChunks, unloadHysteresisChunks);

        EnqueueNeededChunks(loadMinChunk, loadMaxChunk);

        double budgetMs = Mathf.Max(0.1f, genBudgetMs);

        long frameStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        long freq = System.Diagnostics.Stopwatch.Frequency;

        double ElapsedMs()
            => (System.Diagnostics.Stopwatch.GetTimestamp() - frameStartTicks) * 1000.0 / freq;

        int hardCap = Mathf.Max(0, maxChunksPerFrame);
        int genCount = 0;

        long genTicksTotal = 0;
        long applyTicksTotal = 0;

        double estChunkMs = 6.0;

        while (generateQueue.Count > 0 && genCount < hardCap)
        {
            double elapsedMs = ElapsedMs();
            double remainingMs = budgetMs - elapsedMs;
            if (remainingMs <= 0.0)
                break;

            if (genCount > 0)
            {
                double genMsSoFar = genTicksTotal * 1000.0 / freq;
                double applyMsSoFar = applyTicksTotal * 1000.0 / freq;
                estChunkMs = (genMsSoFar + applyMsSoFar) / genCount;
            }

            const double safetyMs = 0.25;
            if (genCount > 0 && remainingMs < (estChunkMs + safetyMs))
                break;

            Vector2Int c = generateQueue.Dequeue();
            queued.Remove(c);

            if (loaded.Contains(c))
                continue;

            if (!IsChunkIn(c, loadMinChunk, loadMaxChunk))
                continue;

            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            ChunkResult chunk = generator.GenerateChunk(c);
            long t1 = System.Diagnostics.Stopwatch.GetTimestamp();

            applier.Apply(chunk);

            TileNavWorld.Instance?.BuildNavChunk(c, profile.chunkSize);

            SpawnGatesForChunk(c);
            SpawnDensForChunk(c);
            long t2 = System.Diagnostics.Stopwatch.GetTimestamp();

            loaded.Add(c);
            NotifyChunkLoaded(c);

            genTicksTotal += (t1 - t0);
            applyTicksTotal += (t2 - t1);
            genCount++;
        }

        long unloadT0 = System.Diagnostics.Stopwatch.GetTimestamp();
        UnloadOutside(unloadMinChunk, unloadMaxChunk);
        long unloadT1 = System.Diagnostics.Stopwatch.GetTimestamp();
        double unloadMs = (unloadT1 - unloadT0) * 1000.0 / freq;

        double genMsTotal = genTicksTotal * 1000.0 / freq;
        double applyMsTotal = applyTicksTotal * 1000.0 / freq;

        if (unloadMs >= 2.0 || genMsTotal >= 10.0 || applyMsTotal >= 2.0)
        {
            Debug.Log(
                $"[WorldGen] unload={(int)unloadMs}ms, " +
                $"genChunks={genCount}/{hardCap} budget={budgetMs:F1}ms " +
                $"gen={genMsTotal:F2}ms apply={applyMsTotal:F2}ms, " +
                $"queue={generateQueue.Count} loaded={loaded.Count} chunkSize={profile.chunkSize}"
            );
        }
    }

    private void OnDisable()
    {
        if (!clearOnDisable || profile == null)
            return;

        DespawnAllSpawned();

        foreach (var c in loaded)
            applier.ClearChunk(c, profile.chunkSize);

        loaded.Clear();
        queued.Clear();
        generateQueue.Clear();
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

        RebuildPoiLookup();
        applier.ClearAll();
        DespawnAllSpawned();
        ClearCachedChunkRoots();
        SpawnRunGateForBiome();

        loaded.Clear();
        queued.Clear();
        generateQueue.Clear();

        anchorInitialized = false;
    }

    private int ComputeBiomeSeed(int runSeed, int biomeIdx)
    {
        uint h = DeterministicHash.Hash((uint)runSeed, biomeIdx, 0, 0xC0FFEEu);
        return (int)(h & 0x7FFFFFFF);
    }

    #endregion

    #region Streaming - Queue/Unload

    private void EnqueueNeededChunks(Vector2Int minChunk, Vector2Int maxChunk)
    {
        List<Vector2Int> needed = new List<Vector2Int>();

        Vector2Int center = new Vector2Int(
            (minChunk.x + maxChunk.x) / 2,
            (minChunk.y + maxChunk.y) / 2
        );

        for (int y = minChunk.y; y <= maxChunk.y; y++)
        {
            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                Vector2Int c = new Vector2Int(x, y);

                if (loaded.Contains(c) || queued.Contains(c))
                    continue;

                needed.Add(c);
            }
        }

        needed.Sort((a, b) =>
        {
            int da = Mathf.Abs(a.x - center.x) + Mathf.Abs(a.y - center.y);
            int db = Mathf.Abs(b.x - center.x) + Mathf.Abs(b.y - center.y);
            return da.CompareTo(db);
        });

        for (int i = 0; i < needed.Count; i++)
        {
            generateQueue.Enqueue(needed[i]);
            queued.Add(needed[i]);
        }
    }

    private void UnloadOutside(Vector2Int keepMinChunk, Vector2Int keepMaxChunk)
    {
        if (loaded.Count == 0)
            return;

        const int maxRemovalsPerFrame = 1;

        List<(Vector2Int c, int score)> candidates = null;

        foreach (var c in loaded)
        {
            if (IsChunkIn(c, keepMinChunk, keepMaxChunk))
                continue;

            int dx = 0;
            if (c.x < keepMinChunk.x) dx = keepMinChunk.x - c.x;
            else if (c.x > keepMaxChunk.x) dx = c.x - keepMaxChunk.x;

            int dy = 0;
            if (c.y < keepMinChunk.y) dy = keepMinChunk.y - c.y;
            else if (c.y > keepMaxChunk.y) dy = c.y - keepMaxChunk.y;

            int score = dx + dy;

            candidates ??= new List<(Vector2Int c, int score)>();
            candidates.Add((c, score));
        }

        if (candidates == null)
            return;

        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        int n = Mathf.Min(maxRemovalsPerFrame, candidates.Count);
        for (int i = 0; i < n; i++)
        {
            Vector2Int c = candidates[i].c;

            NotifyChunkUnloading(c);
            DespawnObjectsForChunk(c);
            applier.ClearChunk(c, profile.chunkSize);

            TileNavWorld.Instance?.ClearNavChunk(c);

            loaded.Remove(c);
        }
    }

    private static bool IsChunkIn(Vector2Int c, Vector2Int minChunk, Vector2Int maxChunk)
    {
        return c.x >= minChunk.x && c.x <= maxChunk.x &&
               c.y >= minChunk.y && c.y <= maxChunk.y;
    }
    private void ClearCachedChunkRoots()
    {
        foreach (var kvp in cachedChunkRoots)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        cachedChunkRoots.Clear();
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
        var st = ctx.ChunkStates.GetChunkState(chunkCoord);
        OnChunkLoaded?.Invoke(chunkCoord, st);
    }

    private void NotifyChunkUnloading(Vector2Int chunkCoord)
    {
        var st = ctx.ChunkStates.GetChunkState(chunkCoord);
        OnChunkUnloading?.Invoke(chunkCoord, st);
    }

    // Gate helpers
    private void SpawnGatesForChunk(Vector2Int chunkCoord)
    {
        var prefab = ctx.Biome != null ? ctx.Biome.GatePrefab : null;
        if (prefab == null || grid == null)
            return;

        if (!gateCentersByChunk.TryGetValue(chunkCoord, out var centers) || centers == null || centers.Count == 0)
            return;

        int size = profile.chunkSize;
        int baseX = chunkCoord.x * size;
        int baseY = chunkCoord.y * size;

        Transform parent = GetChunkGroup(chunkCoord, "Gates");

        for (int i = 0; i < centers.Count; i++)
        {
            Vector2Int gateCenter = centers[i];

            int lx = gateCenter.x - baseX;
            int ly = gateCenter.y - baseY;
            int localIndex = lx + ly * size;

            int spawnId = ctx.ChunkStates.MakeSpawnId(ctx.ActiveBiome.Seed, chunkCoord, localIndex, GateSpawnSalt);

            var st = ctx.ChunkStates.GetChunkState(chunkCoord);
            if (st.consumedIds.Contains(spawnId))
                continue;

            Vector3 worldPos = grid.GetCellCenterWorld(new Vector3Int(gateCenter.x, gateCenter.y, 0));

            var go = poiPool.Spawn(prefab, worldPos, Quaternion.identity, parent);
            if (go == null)
                continue;

            var gate = go.GetComponentInChildren<BiomeGateInteractable>();
            if (gate != null)
            {
                gate.Initialize(this, gateCenter);
            }
            else
            {
                Debug.LogWarning($"Gate prefab '{prefab.name}' has no GateInteractable", go);
            }

            if (!spawnedByChunk.TryGetValue(chunkCoord, out var list))
            {
                list = new List<GameObject>(4);
                spawnedByChunk.Add(chunkCoord, list);
            }
            list.Add(go);
        }
    }

    private void DespawnObjectsForChunk(Vector2Int chunkCoord)
    {
        if (spawnedByChunk.TryGetValue(chunkCoord, out var list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    poiPool.Release(list[i]);
            }

            spawnedByChunk.Remove(chunkCoord);
        }

        if (activeChunkRoots.TryGetValue(chunkCoord, out var root) && root != null)
        {
            root.gameObject.SetActive(false);
            root.SetParent(poiChunkRootsCached, false);

            activeChunkRoots.Remove(chunkCoord);
            cachedChunkRoots[chunkCoord] = root;
        }
    }

    private void DespawnAllSpawned()
    {
        foreach (var kvp in spawnedByChunk)
        {
            var list = kvp.Value;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    poiPool.Release(list[i]);
            }
        }
        spawnedByChunk.Clear();

        foreach (var kvp in activeChunkRoots)
        {
            var root = kvp.Value;
            if (root == null) continue;

            root.gameObject.SetActive(false);
            root.SetParent(poiChunkRootsCached, false);
            cachedChunkRoots[kvp.Key] = root;
        }
        activeChunkRoots.Clear();
    }

    private void EnsurePoiPool()
    {
        if (poiPool == null)
            poiPool = GetComponent<WorldPoiPoolManager>();

        if (poiPool == null)
            poiPool = gameObject.AddComponent<WorldPoiPoolManager>();

        // Chunk-root parents live under the pool's Active root so hierarchy stays tidy
        var active = poiPool.GetActiveRoot();

        if (poiChunkRootsActive == null)
        {
            var go = new GameObject("ChunkRoots");
            go.transform.SetParent(active, false);
            poiChunkRootsActive = go.transform;
        }

        if (poiChunkRootsCached == null)
        {
            var go = new GameObject("ChunkRoots (Cached)");
            go.transform.SetParent(active, false);
            poiChunkRootsCached = go.transform;
        }
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

    // Wolf Den
    private void SpawnDensForChunk(Vector2Int chunkCoord)
    {
        var prefab = ctx.Biome != null ? ctx.Biome.WolfDenPrefab : null;
        if (prefab == null || grid == null)
            return;

        if (!denCentersByChunk.TryGetValue(chunkCoord, out var centers) || centers == null || centers.Count == 0)
            return;

        int size = profile.chunkSize;
        int baseX = chunkCoord.x * size;
        int baseY = chunkCoord.y * size;

        Transform parent = GetChunkGroup(chunkCoord, "Dens");

        for (int i = 0; i < centers.Count; i++)
        {
            Vector2Int denCenter = centers[i];

            int lx = denCenter.x - baseX;
            int ly = denCenter.y - baseY;
            int localIndex = lx + ly * size;

            int spawnId = ctx.ChunkStates.MakeSpawnId(ctx.ActiveBiome.Seed, chunkCoord, localIndex, WolfDenSpawnSalt);

            //if (st.consumedIds.Contains(spawnId))
            //    continue;
            var st = ctx.ChunkStates.GetChunkState(chunkCoord);
            bool consumed = st.consumedIds.Contains(spawnId);

            Vector3 worldPos = grid.GetCellCenterWorld(new Vector3Int(denCenter.x, denCenter.y, 0));

            var go = poiPool.Spawn(prefab, worldPos, Quaternion.identity, parent);
            if (go == null)
                continue;

            var den = go.GetComponentInChildren<WolfDen>();
            if (den != null)
                den.Initialize(this, denCenter, chunkCoord, spawnId);
            else
                Debug.LogWarning($"WolfDen prefab '{prefab.name}' has no WolfDen component", go);

            if (!spawnedByChunk.TryGetValue(chunkCoord, out var list))
            {
                list = new List<GameObject>(4);
                spawnedByChunk.Add(chunkCoord, list);
            }
            list.Add(go);
        }
    }

    private void RebuildPoiLookup()
    {
        gateCentersByChunk.Clear();
        denCentersByChunk.Clear();

        int size = profile.chunkSize;

        foreach (var c in ctx.Gates.GateCenters)
        {
            Vector2Int ch = TileToChunk(c, size);

            if (!gateCentersByChunk.TryGetValue(ch, out var list))
                gateCentersByChunk[ch] = list = new List<Vector2Int>(2);

            list.Add(c);
        }

        foreach (var c in ctx.Dens.DenCenters)
        {
            Vector2Int ch = TileToChunk(c, size);

            if (!denCentersByChunk.TryGetValue(ch, out var list))
                denCentersByChunk[ch] = list = new List<Vector2Int>(2);

            list.Add(c);
        }
    }

    private Transform GetChunkRoot(Vector2Int chunkCoord)
    {
        if (activeChunkRoots.TryGetValue(chunkCoord, out var root) && root != null)
            return root;

        // Reuse cached folder if we have it
        if (cachedChunkRoots.TryGetValue(chunkCoord, out var cached) && cached != null)
        {
            cachedChunkRoots.Remove(chunkCoord);

            cached.SetParent(poiChunkRootsActive, false);
            cached.gameObject.SetActive(true);

            activeChunkRoots[chunkCoord] = cached;
            return cached;
        }

        // Create new
        var go = new GameObject($"Chunk ({chunkCoord.x}, {chunkCoord.y})");
        go.transform.SetParent(poiChunkRootsActive, false);

        activeChunkRoots[chunkCoord] = go.transform;
        return go.transform;
    }

    private Transform GetChunkGroup(Vector2Int chunkCoord, string groupName)
    {
        Transform root = GetChunkRoot(chunkCoord);

        var child = root.Find(groupName);
        if (child != null)
            return child;

        var go = new GameObject(groupName);
        go.transform.SetParent(root, false);
        return go.transform;
    }

    #endregion

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
    }

}
