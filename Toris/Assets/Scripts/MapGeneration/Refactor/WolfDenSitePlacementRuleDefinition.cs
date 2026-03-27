using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Wolf Den Site Rule",
    fileName = "WolfDenSitePlacementRuleDefinition")]
public sealed class WolfDenSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private const uint DenPickSalt = 0xD311C0DEu;
    private const int AvoidOriginRadiusTiles = 18;
    private const int BaseAttempts = 200;
    private const int AttemptsPerTarget = 250;
    private const int RelaxSteps = 6;
    private const int RelaxSpacingStep = 4;
    private const int RelaxedSpacingFloor = 4;
    private const int RelaxStartIndexBase = 100000;

    public override void BuildSites(WorldContext ctx)
    {
        BiomeProfile biomeProfile = ctx.Biome;
        if (biomeProfile == null)
            return;

        int targetMin = Mathf.Max(0, biomeProfile.minWolfDenCount);
        if (targetMin == 0)
            return;

        int spacingTiles = Mathf.Max(1, biomeProfile.wolfDenMinSpacingTiles);
        int stampSize = Mathf.Max(1, biomeProfile.wolfDenStampSize);

        Vector2Int originTile = ctx.ActiveBiome.OriginTile;
        List<Vector2Int> chosenCenters = new List<Vector2Int>(targetMin);

        int attempts = Mathf.Max(BaseAttempts, targetMin * AttemptsPerTarget);
        float radiusTiles = ctx.ActiveBiome.RadiusTiles * 0.90f;

        for (int i = 0; i < attempts && chosenCenters.Count < targetMin; i++)
        {
            Vector2Int candidateTile = PickPointInDisk(ctx.ActiveBiome.Seed, i, originTile, radiusTiles);

            if ((candidateTile - originTile).sqrMagnitude < AvoidOriginRadiusTiles * AvoidOriginRadiusTiles)
                continue;

            Vector2Int localTile = ctx.ActiveBiome.ToLocal(candidateTile);
            if (!ctx.Mask.IsLand(localTile, ctx))
                continue;

            if (!IsFarEnough(candidateTile, chosenCenters, spacingTiles))
                continue;

            chosenCenters.Add(candidateTile);
        }

        for (int relaxStep = 0; relaxStep < RelaxSteps && chosenCenters.Count < targetMin; relaxStep++)
        {
            int relaxedSpacing = Mathf.Max(
                RelaxedSpacingFloor,
                spacingTiles - (relaxStep + 1) * RelaxSpacingStep);

            int startIndex = RelaxStartIndexBase + relaxStep * RelaxStartIndexBase;

            for (int i = 0; i < attempts && chosenCenters.Count < targetMin; i++)
            {
                Vector2Int candidateTile = PickPointInDisk(
                    ctx.ActiveBiome.Seed,
                    startIndex + i,
                    originTile,
                    radiusTiles);

                Vector2Int localTile = ctx.ActiveBiome.ToLocal(candidateTile);
                if (!ctx.Mask.IsLand(localTile, ctx))
                    continue;

                if (!IsFarEnough(candidateTile, chosenCenters, relaxedSpacing))
                    continue;

                chosenCenters.Add(candidateTile);
            }
        }

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            Vector2Int centerTile = chosenCenters[i];

            if (biomeProfile.wolfDenGroundTile != null)
                ctx.Stamps.StampRectGround(centerTile, stampSize, stampSize, biomeProfile.wolfDenGroundTile);

            ctx.SiteBlockers.AddSquareFootprint(centerTile, stampSize);
            ctx.RegisterSite(WorldSiteType.WolfDen, centerTile);
        }

        if (chosenCenters.Count < targetMin)
        {
            Debug.LogWarning(
                $"[WolfDenSiteRule] Only placed {chosenCenters.Count}/{targetMin} dens (area too constrained).",
                this);
        }
    }

    private static bool IsFarEnough(Vector2Int candidateTile, List<Vector2Int> chosenCenters, int spacingTiles)
    {
        int spacingSquared = spacingTiles * spacingTiles;

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            if ((chosenCenters[i] - candidateTile).sqrMagnitude < spacingSquared)
                return false;
        }

        return true;
    }

    private static Vector2Int PickPointInDisk(int biomeSeed, int index, Vector2Int originTile, float radiusTiles)
    {
        uint angleHash = DeterministicHash.Hash((uint)biomeSeed, index, 0, DenPickSalt);
        uint radiusHash = DeterministicHash.Hash((uint)biomeSeed, index, 1, DenPickSalt);

        float angle01 = DeterministicHash.Hash01(angleHash);
        float radius01 = DeterministicHash.Hash01(radiusHash);

        float angleRadians = angle01 * Mathf.PI * 2f;
        float distance = Mathf.Sqrt(radius01) * radiusTiles;

        int offsetX = Mathf.RoundToInt(Mathf.Cos(angleRadians) * distance);
        int offsetY = Mathf.RoundToInt(Mathf.Sin(angleRadians) * distance);

        return originTile + new Vector2Int(offsetX, offsetY);
    }
}