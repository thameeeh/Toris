using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class WorldGenRunner : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private WorldGenProfile profile;
    [SerializeField] private Grid grid;

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

    private WorldContext ctx;
    private ChunkGenerator generator;
    private TilemapApplier applier;
    private Vector2Int streamingAnchorChunk;
    private bool anchorInitialized;

    private readonly Queue<Vector2Int> generateQueue = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> queued = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> loaded = new HashSet<Vector2Int>();

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

        Vector2Int spawnTile = new Vector2Int(
            Mathf.FloorToInt(ctx.SpawnPosTiles.x),
            Mathf.FloorToInt(ctx.SpawnPosTiles.y)
        );

        streamingAnchorChunk = TileToChunk(spawnTile, profile.chunkSize);
        anchorInitialized = true;
    }
    private void Update()
    {
        if (profile == null || groundMap == null || waterMap == null || decorMap == null)
            return;

        Vector2 focusWorld = followTarget != null
            ? (Vector2)followTarget.position
            : ctx.SpawnPosTiles;

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

        // --- REAL time budget over the WHOLE streaming work (unload + gen + apply) ---
        double budgetMs = Mathf.Max(0.1f, genBudgetMs);

        long frameStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        long freq = System.Diagnostics.Stopwatch.Frequency;

        double ElapsedMs()
            => (System.Diagnostics.Stopwatch.GetTimestamp() - frameStartTicks) * 1000.0 / freq;

        // 1) Unload first, but also budget it.
        long unloadT0 = System.Diagnostics.Stopwatch.GetTimestamp();
        UnloadFarChunks(streamingAnchorChunk, profile.viewDistanceChunks);
        long unloadT1 = System.Diagnostics.Stopwatch.GetTimestamp();
        double unloadMs = (unloadT1 - unloadT0) * 1000.0 / freq;

        // If unload alone ate the budget, do nothing else this frame.
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

        // 2) Generation + apply, fully budgeted.
        int hardCap = Mathf.Max(0, maxChunksPerFrame); // allow 0 if you want to pause streaming
        int genCount = 0;

        long genTicksTotal = 0;
        long applyTicksTotal = 0;

        // Rolling estimate for "one chunk cost" (gen+apply).
        // Start with a conservative estimate to avoid blowing budget on the first chunk.
        double estChunkMs = 6.0; // safe default; will converge quickly

        while (generateQueue.Count > 0 && genCount < hardCap)
        {
            double elapsedMs = ElapsedMs();
            double remainingMs = budgetMs - elapsedMs;
            if (remainingMs <= 0.0)
                break;

            // If we already have measurements this frame, update estimate.
            if (genCount > 0)
            {
                double genMsSoFar = genTicksTotal * 1000.0 / freq;
                double applyMsSoFar = applyTicksTotal * 1000.0 / freq;
                estChunkMs = (genMsSoFar + applyMsSoFar) / genCount;
            }

            // Don�t start a chunk if we probably can�t finish it inside the remaining budget.
            // Small safety margin to avoid borderline overruns.
            const double safetyMs = 0.25;
            if (remainingMs < (estChunkMs + safetyMs))
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
        int r = viewDist + preloadChunks + 1;
        int r2 = r * r;

        if (loaded.Count == 0) return;

        const int maxRemovalsPerFrame = 1;

        List<Vector2Int> toRemove = null;

        foreach (var c in loaded)
        {
            int dx = c.x - centerChunk.x;
            int dy = c.y - centerChunk.y;

            if (dx * dx + dy * dy > r2)
            {
                toRemove ??= new List<Vector2Int>();
                toRemove.Add(c);

                if (toRemove.Count >= maxRemovalsPerFrame)
                    break;
            }
        }

        if (toRemove == null) return;

        for (int i = 0; i < toRemove.Count; i++)
        {
            Vector2Int c = toRemove[i];

            applier.ClearChunk(c, profile.chunkSize);

            ctx.Roads.ClearCachedChunk(c);

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
}
