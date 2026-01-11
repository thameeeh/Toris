using System.Collections.Generic;
using UnityEngine;

public static class DenFeatureBuilder
{
    private const uint DenPickSalt = 0xD311C0DEu;

    public static void Build(WorldContext ctx)
    {
        var bp = ctx.Biome;
        if (bp == null) return;

        int targetMin = Mathf.Max(0, bp.minWolfDenCount);
        if (targetMin == 0) return;

        int spacing = Mathf.Max(1, bp.wolfDenMinSpacingTiles);
        int stampSize = Mathf.Max(1, bp.wolfDenStampSize);

        Vector2Int origin = ctx.ActiveBiome.OriginTile;
        int avoidOriginRadius = 18;

        var chosen = new List<Vector2Int>(targetMin);

        int attempts = Mathf.Max(200, targetMin * 250);

        float radius = ctx.ActiveBiome.RadiusTiles * 0.90f;

        for (int i = 0; i < attempts && chosen.Count < targetMin; i++)
        {
            Vector2Int p = PickPointInDisk(ctx.ActiveBiome.Seed, i, origin, radius);

            if ((p - origin).sqrMagnitude < avoidOriginRadius * avoidOriginRadius)
                continue;

            Vector2Int local = ctx.ActiveBiome.ToLocal(p);
            if (!ctx.Mask.IsLand(local, ctx))
                continue;

            if (!IsFarEnough(p, chosen, spacing))
                continue;

            chosen.Add(p);
        }


        int relaxSteps = 6;
        for (int r = 0; r < relaxSteps && chosen.Count < targetMin; r++)
        {
            int relaxed = Mathf.Max(4, spacing - (r + 1) * 4);
            int start = 100000 + r * 100000;

            for (int i = 0; i < attempts && chosen.Count < targetMin; i++)
            {
                Vector2Int p = PickPointInDisk(ctx.ActiveBiome.Seed, start + i, origin, radius);

                Vector2Int local = ctx.ActiveBiome.ToLocal(p);
                if (!ctx.Mask.IsLand(local, ctx))
                    continue;

                if (!IsFarEnough(p, chosen, relaxed))
                    continue;

                chosen.Add(p);
            }
        }

        for (int i = 0; i < chosen.Count; i++)
        {
            Vector2Int c = chosen[i];

            if (bp.wolfDenGroundTile != null)
                ctx.Stamps.StampRectGround(c, stampSize, stampSize, bp.wolfDenGroundTile);

            ctx.Dens.AddDenFootprint(c, stampSize);
        }

        if (chosen.Count < targetMin)
        {
            Debug.LogWarning(
                $"[WolfDenFeature] Only placed {chosen.Count}/{targetMin} dens (island too constrained?)."
            );
        }
    }

    private static bool IsFarEnough(Vector2Int p, List<Vector2Int> chosen, int spacing)
    {
        int s2 = spacing * spacing;
        for (int i = 0; i < chosen.Count; i++)
            if ((chosen[i] - p).sqrMagnitude < s2)
                return false;
        return true;
    }

    private static Vector2Int PickPointInDisk(int biomeSeed, int index, Vector2Int origin, float radius)
    {
        uint hA = DeterministicHash.Hash((uint)biomeSeed, index, 0, DenPickSalt);
        uint hR = DeterministicHash.Hash((uint)biomeSeed, index, 1, DenPickSalt);

        float a01 = DeterministicHash.Hash01(hA);
        float r01 = DeterministicHash.Hash01(hR);

        float ang = a01 * Mathf.PI * 2f;
        float rr = Mathf.Sqrt(r01) * radius;

        int x = origin.x + Mathf.RoundToInt(Mathf.Cos(ang) * rr);
        int y = origin.y + Mathf.RoundToInt(Mathf.Sin(ang) * rr);

        return new Vector2Int(x, y);
    }
}
