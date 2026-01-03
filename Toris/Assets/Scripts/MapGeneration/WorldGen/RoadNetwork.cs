using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic "main roads from spawn" + per-chunk cached influence.
///
/// Performance fix:
/// - Stop doing expensive distance-to-polyline per tile.
/// - Instead, bake an influence field per chunk:
///   1) Rasterize road centerlines into a small grid around the chunk
///   2) Compute an approximate distance field (8-neighbor chamfer)
///   3) Convert distance -> influence (0..1) for the chunk tiles
/// - ComputeInfluence(tile) becomes O(1) lookup.
/// </summary>
public sealed class RoadNetwork
{
    private readonly WorldGenProfile profile;
    private readonly int seed;
    private readonly Vector2 origin;

    // Generated as world-space polylines (tile units)
    private readonly Vector2[][] roads;

    // Per-chunk cached road influence values (0..1), sized chunkSize*chunkSize
    private readonly Dictionary<Vector2Int, float[]> chunkInfluence = new Dictionary<Vector2Int, float[]>(128);

    // Reused temp buffers during baking (avoid allocations)
    private int[] distField;        // chamfer distance (int cost, 10 = 1 tile)
    private int distW, distH;
    private int padCached = -1;

    public RoadNetwork(WorldContext ctx)
    {
        profile = ctx.Profile;
        seed = ctx.Seed;
        origin = ctx.SpawnPosTiles;

        int roadCount = GetField(profile, "mainRoadCount", 6);
        roadCount = Mathf.Clamp(roadCount, 0, 32);

        roads = new Vector2[roadCount][];
        for (int i = 0; i < roadCount; i++)
            roads[i] = BuildMainRoad(i);
    }

    /// <summary> Clear cached data for an unloaded chunk. Safe if not cached. </summary>
    public void ClearCachedChunk(Vector2Int chunkCoord)
    {
        chunkInfluence.Remove(chunkCoord);
    }

    /// <summary>
    /// Ensure chunk influence is baked and cached.
    /// Call once at the start of chunk generation.
    /// </summary>
    public void BakeChunk(Vector2Int chunkCoord, int chunkSize)
    {
        // Cache zeros so lookup stays O(1) even when disabled.
        if (!GetField(profile, "enableRoads", true) || roads == null || roads.Length == 0)
        {
            if (!chunkInfluence.ContainsKey(chunkCoord))
                chunkInfluence[chunkCoord] = new float[chunkSize * chunkSize];
            return;
        }

        if (chunkInfluence.ContainsKey(chunkCoord))
            return;

        // Influence pad around the chunk so roads slightly outside still affect inside.
        Vector2 halfWidthNearFar = GetField(profile, "roadHalfWidthNearFar", new Vector2(2.5f, 1.4f));
        float maxHalfWidth = Mathf.Max(halfWidthNearFar.x, halfWidthNearFar.y);
        int pad = Mathf.Clamp(Mathf.CeilToInt(maxHalfWidth) + 3, 3, 64);

        EnsureDistFieldCapacity(chunkSize, pad);

        int baseX = chunkCoord.x * chunkSize;
        int baseY = chunkCoord.y * chunkSize;

        int minX = baseX - pad;
        int minY = baseY - pad;
        int maxX = baseX + chunkSize - 1 + pad;
        int maxY = baseY + chunkSize - 1 + pad;

        // 1) init INF
        const int INF = 1_000_000;
        int total = distW * distH;
        for (int i = 0; i < total; i++) distField[i] = INF;

        // 2) rasterize road centerlines as sources (dist=0)
        RasterizeRoadsToSources(minX, minY, maxX, maxY);

        // 3) chamfer distance transform (fast)
        ChamferDistanceTransform();

        // 4) convert distance -> influence for the chunk interior
        float[] influence = new float[chunkSize * chunkSize];

        float roadWidthFalloffTiles = GetField(profile, "roadWidthFalloffTiles", 250f);
        roadWidthFalloffTiles = Mathf.Max(1f, roadWidthFalloffTiles);

        for (int ly = 0; ly < chunkSize; ly++)
        {
            for (int lx = 0; lx < chunkSize; lx++)
            {
                int wx = baseX + lx;
                int wy = baseY + ly;

                int gx = (wx - minX);
                int gy = (wy - minY);

                int idxG = gy * distW + gx;
                int dCost = distField[idxG];

                float distTiles = dCost >= INF ? 9999f : (dCost / 10f);

                float fromOrigin = Vector2.Distance(new Vector2(wx, wy), origin);
                float t01 = Mathf.Clamp01(fromOrigin / roadWidthFalloffTiles);

                float halfWidth = Mathf.Lerp(halfWidthNearFar.x, halfWidthNearFar.y, t01);
                halfWidth = Mathf.Max(0.1f, halfWidth);

                float v = 1f - Mathf.Clamp01(distTiles / halfWidth);
                // smoothstep edge
                v = v * v * (3f - 2f * v);

                influence[ly * chunkSize + lx] = v;
            }
        }

        chunkInfluence[chunkCoord] = influence;
    }

    /// <summary>
    /// Influence in [0..1] at a tile. Uses cache; auto-bakes on demand (debug HUD safe).
    /// </summary>
    public float ComputeInfluence(Vector2Int tilePos)
    {
        if (!GetField(profile, "enableRoads", true) || roads == null || roads.Length == 0)
            return 0f;

        int chunkSize = profile.chunkSize;
        Vector2Int chunk = WorldToChunk(tilePos, chunkSize);

        if (!chunkInfluence.TryGetValue(chunk, out float[] influence))
        {
            BakeChunk(chunk, chunkSize);
            chunkInfluence.TryGetValue(chunk, out influence);
        }

        if (influence == null) return 0f;

        int lx = Mod(tilePos.x, chunkSize);
        int ly = Mod(tilePos.y, chunkSize);
        int idx = ly * chunkSize + lx;

        if ((uint)idx >= (uint)influence.Length) return 0f;
        return influence[idx];
    }

    // -------------------- baking helpers --------------------

    private void EnsureDistFieldCapacity(int chunkSize, int pad)
    {
        if (pad == padCached && distField != null && distW == chunkSize + pad * 2 && distH == chunkSize + pad * 2)
            return;

        padCached = pad;
        distW = chunkSize + pad * 2;
        distH = chunkSize + pad * 2;
        distField = new int[distW * distH];
    }

    private void RasterizeRoadsToSources(int minX, int minY, int maxX, int maxY)
    {
        for (int r = 0; r < roads.Length; r++)
        {
            Vector2[] pts = roads[r];
            if (pts == null || pts.Length < 2) continue;

            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];

                float len = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len)); // ~1 sample / tile

                for (int s = 0; s <= steps; s++)
                {
                    float t = steps == 0 ? 0f : (s / (float)steps);
                    Vector2 p = Vector2.Lerp(a, b, t);

                    int x = Mathf.RoundToInt(p.x);
                    int y = Mathf.RoundToInt(p.y);

                    if (x < minX || x > maxX || y < minY || y > maxY) continue;

                    int gx = x - minX;
                    int gy = y - minY;

                    int idx = gy * distW + gx;
                    distField[idx] = 0;
                }
            }
        }
    }

    private void ChamferDistanceTransform()
    {
        const int w1 = 10; // orthogonal
        const int w2 = 14; // diagonal approx sqrt(2)

        // forward
        for (int y = 0; y < distH; y++)
        {
            int row = y * distW;
            for (int x = 0; x < distW; x++)
            {
                int idx = row + x;
                int d = distField[idx];
                if (d == 0) continue;

                int best = d;

                if (x > 0) best = Mathf.Min(best, distField[idx - 1] + w1);
                if (y > 0) best = Mathf.Min(best, distField[idx - distW] + w1);
                if (x > 0 && y > 0) best = Mathf.Min(best, distField[idx - distW - 1] + w2);
                if (x < distW - 1 && y > 0) best = Mathf.Min(best, distField[idx - distW + 1] + w2);

                distField[idx] = best;
            }
        }

        // backward
        for (int y = distH - 1; y >= 0; y--)
        {
            int row = y * distW;
            for (int x = distW - 1; x >= 0; x--)
            {
                int idx = row + x;
                int d = distField[idx];
                if (d == 0) continue;

                int best = d;

                if (x < distW - 1) best = Mathf.Min(best, distField[idx + 1] + w1);
                if (y < distH - 1) best = Mathf.Min(best, distField[idx + distW] + w1);
                if (x < distW - 1 && y < distH - 1) best = Mathf.Min(best, distField[idx + distW + 1] + w2);
                if (x > 0 && y < distH - 1) best = Mathf.Min(best, distField[idx + distW - 1] + w2);

                distField[idx] = best;
            }
        }
    }

    // -------------------- road generation --------------------

    private Vector2[] BuildMainRoad(int roadIndex)
    {
        float worldRadius = GetField(profile, "worldRadiusTiles", 1500f);
        float islandRadius01 = GetField(profile, "islandRadius01", 0.9f);
        float maxDist = Mathf.Max(50f, worldRadius * islandRadius01);

        Vector2 segMinMax = GetField(profile, "roadSegmentLenMinMax", new Vector2(20f, 250f));
        float segMin = Mathf.Max(5f, segMinMax.x);
        float segMax = Mathf.Max(segMin, segMinMax.y);

        float avg = (segMin + segMax) * 0.5f;
        int segments = Mathf.Clamp(Mathf.CeilToInt(maxDist / avg), 6, 64);

        int n = segments + 1;
        Vector2[] pts = new Vector2[n];
        pts[0] = origin;

        int mainRoadCount = Mathf.Max(1, GetField(profile, "mainRoadCount", roads != null ? roads.Length : 6));

        float baseAngle = (roadIndex / Mathf.Max(1f, mainRoadCount)) * Mathf.PI * 2f;
        float roadTwist = (Hash01(seed, roadIndex, 901) - 0.5f) * Mathf.Deg2Rad * 20f;
        float dirAngle = baseAngle + roadTwist;

        Vector2 dir = new Vector2(Mathf.Cos(dirAngle), Mathf.Sin(dirAngle));
        float traveled = 0f;

        for (int i = 1; i < n; i++)
        {
            float t01 = Hash01(seed, roadIndex * 1000 + i, 111);
            float segLen = Mathf.Lerp(segMin, segMax, t01);

            float wiggle = (Hash01(seed, roadIndex * 1000 + i, 222) - 0.5f) * Mathf.Deg2Rad * 35f;
            float outward01 = Mathf.Clamp01(traveled / maxDist);
            float wiggleScaled = wiggle * Mathf.Lerp(0.25f, 1.0f, outward01);

            float ca = Mathf.Cos(wiggleScaled);
            float sa = Mathf.Sin(wiggleScaled);
            dir = new Vector2(dir.x * ca - dir.y * sa, dir.x * sa + dir.y * ca).normalized;

            traveled += segLen;
            pts[i] = pts[i - 1] + dir * segLen;
        }

        return pts;
    }

    // -------------------- utilities --------------------

    private static Vector2Int WorldToChunk(Vector2Int tilePos, int chunkSize)
    {
        int cx = FloorDiv(tilePos.x, chunkSize);
        int cy = FloorDiv(tilePos.y, chunkSize);
        return new Vector2Int(cx, cy);
    }

    private static int FloorDiv(int a, int b)
    {
        int q = a / b;
        int r = a % b;
        if (r != 0 && ((r < 0) != (b < 0))) q--;
        return q;
    }

    private static int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }

    private static float Hash01(int seed, int x, int salt)
    {
        uint h = DeterministicHash.Hash((uint)seed, x, salt, 0xBEEF);
        return DeterministicHash.Hash01(h);
    }

    private static T GetField<T>(object obj, string fieldName, T fallback)
    {
        if (obj == null) return fallback;

        var t = obj.GetType();
        var f = t.GetField(fieldName);
        if (f == null) return fallback;

        try
        {
            object v = f.GetValue(obj);
            if (v is T tv) return tv;

            if (typeof(T) == typeof(Vector2) && v is Vector2 vv2)
                return (T)(object)vv2;

            if (typeof(T) == typeof(float) && v is float vf)
                return (T)(object)vf;

            if (typeof(T) == typeof(int) && v is int vi)
                return (T)(object)vi;

            if (typeof(T) == typeof(bool) && v is bool vb)
                return (T)(object)vb;
        }
        catch { }

        return fallback;
    }
}
