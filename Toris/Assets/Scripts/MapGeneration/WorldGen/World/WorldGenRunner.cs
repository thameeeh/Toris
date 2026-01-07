using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class WorldGenRunner : MonoBehaviour
{
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
    [SerializeField] private int maxChunksPerFrame = 2;
    [SerializeField] private int preloadChunks = 1;
    [SerializeField] private int anchorShiftThreshold = 1;
    [SerializeField] private float genBudgetMs = 1;
    [SerializeField] private bool clearOnDisable = false;

    private int biomeIndex = 0;
    private float lastGateTime = -999f;
    [SerializeField] private float gateCooldownSeconds = 1f;

    private WorldContext ctx;
    private ChunkGenerator generator;
    private TilemapApplier applier;
    private Vector2Int streamingAnchorChunk;
    private bool anchorInitialized;

    private readonly Queue<Vector2Int> generateQueue = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> queued = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> loaded = new HashSet<Vector2Int>();

    public WorldContext Context => ctx;

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

        ctx = new WorldContext(profile);
        generator = new ChunkGenerator(ctx);
        applier = new TilemapApplier(groundMap, waterMap, decorMap);

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

        if (Time.time - lastGateTime > gateCooldownSeconds)
        {
            if (ctx.Gates.IsGateTile(focusTile))
            {
                lastGateTime = Time.time;

                // If you have a fade/teleport system, call it here,
                // teleport player to new platform tile, then call StartBiome with that tile.
                // For now: rebind biome around the gate tile.
                int next = biomeIndex + 1;

                if (biomeDb != null && biomeDb.Count > 0)
                    next = next % biomeDb.Count;
                StartBiome(next, focusTile);
                return;
            }
        }

        Vector2Int focusChunk = TileToChunk(focusTile, profile.chunkSize);

        if (!anchorInitialized)
        {
            streamingAnchorChunk = focusChunk;
            anchorInitialized = true;
        }
        else
        {
            int dx = Mathf.Abs(focusChunk.x - streamingAnchorChunk.x);
            int dy = Mathf.Abs(focusChunk.y - streamingAnchorChunk.y);

            if (dx > anchorShiftThreshold || dy > anchorShiftThreshold)
                streamingAnchorChunk = focusChunk;
        }

        EnqueueNeededChunks(streamingAnchorChunk, profile.viewDistanceChunks + preloadChunks);

        double budgetMs = Mathf.Max(0.1f, genBudgetMs);

        long frameStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        long freq = System.Diagnostics.Stopwatch.Frequency;

        double ElapsedMs()
            => (System.Diagnostics.Stopwatch.GetTimestamp() - frameStartTicks) * 1000.0 / freq;

        long unloadT0 = System.Diagnostics.Stopwatch.GetTimestamp();
        UnloadFarChunks(streamingAnchorChunk, profile.viewDistanceChunks);
        long unloadT1 = System.Diagnostics.Stopwatch.GetTimestamp();
        double unloadMs = (unloadT1 - unloadT0) * 1000.0 / freq;

        if (ElapsedMs() >= budgetMs)
        {
            if (unloadMs >= 2.0)
            {
                Debug.Log(
                    $"[WorldGen] unload={(int)unloadMs}ms, genChunks=0/{Mathf.Max(1, maxChunksPerFrame)} " +
                    $"budget={budgetMs:F1}ms gen=0.00ms apply=0.00ms, queue={generateQueue.Count} loaded={loaded.Count} chunkSize={profile.chunkSize}"
                );
            }
            return;
        }

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
            // allow at least one chunk even if exceeding budget, otherwise stalling at nothing loaded might happen
            if (genCount > 0 && remainingMs < (estChunkMs + safetyMs))
                break;

            Vector2Int c = generateQueue.Dequeue();
            queued.Remove(c);

            if (loaded.Contains(c))
                continue;

            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            ChunkResult chunk = generator.GenerateChunk(c);
            long t1 = System.Diagnostics.Stopwatch.GetTimestamp();

            applier.Apply(chunk);
            long t2 = System.Diagnostics.Stopwatch.GetTimestamp();

            loaded.Add(c);

            genTicksTotal += (t1 - t0);
            applyTicksTotal += (t2 - t1);
            genCount++;
        }

        double genMsTotal = genTicksTotal * 1000.0 / freq;
        double applyMsTotal = applyTicksTotal * 1000.0 / freq;

        // Log only when notable (same spirit as your original)
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

    private void EnqueueNeededChunks(Vector2Int centerChunk, int radius)
    {
        List<Vector2Int> needed = new List<Vector2Int>();

        float loadRadius = radius + 0.5f;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                Vector2Int c = new Vector2Int(centerChunk.x + dx, centerChunk.y + dy);

                if (dx * dx + dy * dy > loadRadius * loadRadius)
                    continue;

                if (loaded.Contains(c) || queued.Contains(c)) continue;
                needed.Add(c);
            }
        }

        needed.Sort((a, b) =>
        {
            int da = Mathf.Abs(a.x - centerChunk.x) + Mathf.Abs(a.y - centerChunk.y);
            int db = Mathf.Abs(b.x - centerChunk.x) + Mathf.Abs(b.y - centerChunk.y);
            return da.CompareTo(db);
        });

        for (int i = 0; i < needed.Count; i++)
        {
            generateQueue.Enqueue(needed[i]);
            queued.Add(needed[i]);
        }
    }

    private void UnloadFarChunks(Vector2Int centerChunk, int viewDist)
    {
        int r = viewDist + preloadChunks;
        int r2 = r * r;

        if (loaded.Count == 0) return;

        const int maxRemovalsPerFrame = 1;

        List<(Vector2Int c, int d2)> candidates = null;

        foreach (var c in loaded)
        {
            int dx = c.x - centerChunk.x;
            int dy = c.y - centerChunk.y;
            int d2 = dx*dx + dy * dy;

            if (d2 > r2)
            {
                candidates ??= new List<(Vector2Int c, int)>();
                candidates.Add((c, d2));
            }
        }

        if (candidates == null) return;

        candidates.Sort((a, b) => b.d2.CompareTo(a.d2));

        int n = Mathf.Min(maxRemovalsPerFrame, candidates.Count);
        for (int i = 0; i < n; i++)
        {
            Vector2Int c = candidates[i].c;
            applier.ClearChunk(c, profile.chunkSize);
            loaded.Remove(c);
        }
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
        if (r != 0 && ((r > 0) != (b > 0))) q--;
        return q;
    }

    public bool IsChunkLoaded(Vector2Int chunk)
    {
        return loaded.Contains(chunk);
    }

    private void OnDisable()
    {
        if (!clearOnDisable || profile == null) return;

        foreach (var c in loaded)
            applier.ClearChunk(c, profile.chunkSize);

        loaded.Clear();
        queued.Clear();
        generateQueue.Clear();
    }
    private int ComputeBiomeSeed(int runSeed, int biomeIdx)
    {
        uint h = DeterministicHash.Hash((uint)runSeed, biomeIdx, 0, 0xC0FFEEu);
        return (int)(h & 0x7FFFFFFF);
    }

    private Vector2Int WorldToTile(Vector2 world)
    {
        if (grid != null)
        {
            Vector3Int cell = grid.WorldToCell((Vector3)world);
            return new Vector2Int(cell.x, cell.y);
        }

        return new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
    }

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

        applier.ClearAll();

        loaded.Clear();
        queued.Clear();
        generateQueue.Clear();

        anchorInitialized = false;
    }

}
