using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Outland Haven/WorldGen/Biomes/Site Rules/Collectible Site Rule",
    fileName = "CollectibleSitePlacementRuleDefinition")]
public sealed class CollectibleSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private const uint CollectiblePickSalt = 0xF10E7001u;
    private const uint CollectibleCountSalt = 0xF10E7002u;
    private const int BaseAttempts = 200;
    private const int AttemptsPerTarget = 250;
    private const int RelaxSteps = 6;
    private const int RelaxSpacingStep = 2;
    private const int RelaxedSpacingFloor = 2;
    private const int RelaxStartIndexBase = 100000;

    [Header("Site")]
    [SerializeField] private WorldSiteDefinition[] collectibleSiteDefinitions;

    [Header("Count")]
    [SerializeField, Min(0)] private int minCount = 5;
    [SerializeField, Min(0)] private int maxCount = 12;

    [Header("Placement")]
    [SerializeField, Min(1)] private int minSpacingTiles = 8;
    [SerializeField, Range(0.1f, 1f)] private float placementRadiusFactor = 0.85f;
    [SerializeField, Min(0)] private int avoidOriginRadiusTiles = 10;
    [SerializeField] private bool avoidExistingSites = true;

    public override void BuildSites(WorldContext ctx)
    {
        if (collectibleSiteDefinitions == null || collectibleSiteDefinitions.Length == 0)
            return;

        WorldBuildOutput buildOutput = ctx.BuildOutput;
        if (buildOutput == null)
            return;

        int targetCount = ResolveTargetCount(ctx.ActiveBiome.Seed);
        if (targetCount == 0)
            return;

        int spacingTiles = Mathf.Max(1, minSpacingTiles);

        Vector2Int originTile = ctx.ActiveBiome.OriginTile;
        float radiusTiles = ctx.ActiveBiome.RadiusTiles * Mathf.Clamp(placementRadiusFactor, 0.1f, 1f);

        List<Vector2Int> chosenCenters = SitePlacementSampling.PickSpacedCentersInBiomeDisk(
            ctx.ActiveBiome.Seed,
            originTile,
            radiusTiles,
            targetCount,
            spacingTiles,
            Mathf.Max(0, avoidOriginRadiusTiles),
            AttemptsPerTarget,
            BaseAttempts,
            RelaxSteps,
            RelaxSpacingStep,
            RelaxedSpacingFloor,
            RelaxStartIndexBase,
            CollectiblePickSalt,
            candidateTile =>
            {
                return IsValidCandidateTile(ctx, candidateTile, spacingTiles);
            });

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            Vector2Int centerTile = chosenCenters[i];
            
            // Deterministically select site definition based on index and seed
            uint selectionHash = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, i, 0, CollectiblePickSalt);
            int selectedIndex = Mathf.FloorToInt(DeterministicHash.Hash01(selectionHash) * collectibleSiteDefinitions.Length);
            WorldSiteDefinition selectedDefinition = collectibleSiteDefinitions[Mathf.Clamp(selectedIndex, 0, collectibleSiteDefinitions.Length - 1)];

            if (selectedDefinition != null && selectedDefinition.IsValid)
            {
                buildOutput.RegisterSite(selectedDefinition, centerTile, ctx.World.chunkSize);
            }
        }

#if UNITY_EDITOR
        if (chosenCenters.Count < targetCount)
        {
            Debug.LogWarning(
                $"[CollectibleSitePlacementRule] Only placed {chosenCenters.Count}/{targetCount} collectibles (area too constrained).",
                this);
        }
#endif
    }

    private int ResolveTargetCount(int biomeSeed)
    {
        int resolvedMin = Mathf.Max(0, minCount);
        int resolvedMax = Mathf.Max(resolvedMin, maxCount);
        if (resolvedMax == resolvedMin)
            return resolvedMin;

        uint countHash = DeterministicHash.Hash((uint)biomeSeed, resolvedMin, resolvedMax, CollectibleCountSalt);
        int range = resolvedMax - resolvedMin + 1;
        int offset = Mathf.FloorToInt(DeterministicHash.Hash01(countHash) * range);
        return Mathf.Clamp(resolvedMin + offset, resolvedMin, resolvedMax);
    }

    private bool IsValidCandidateTile(WorldContext ctx, Vector2Int candidateTile, int spacingTiles)
    {
        Vector2Int localTile = ctx.ActiveBiome.ToLocal(candidateTile);
        if (!ctx.Mask.IsLand(localTile, ctx))
            return false;

        return !HasNearbyExistingSite(ctx, candidateTile, spacingTiles);
    }

    private bool HasNearbyExistingSite(WorldContext ctx, Vector2Int candidateTile, int spacingTiles)
    {
        if (!avoidExistingSites || ctx?.BuildOutput?.SitePlacements?.All == null)
            return false;

        int spacingTilesSquared = Mathf.Max(1, spacingTiles) * Mathf.Max(1, spacingTiles);
        IReadOnlyList<SitePlacement> placements = ctx.BuildOutput.SitePlacements.All;
        for (int i = 0; i < placements.Count; i++)
        {
            SitePlacement placement = placements[i];
            
            // We want to avoid spawning on top of any existing site or other collectibles
            if ((placement.CenterTile - candidateTile).sqrMagnitude < spacingTilesSquared)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minCount = Mathf.Max(0, minCount);
        maxCount = Mathf.Max(minCount, maxCount);
        minSpacingTiles = Mathf.Max(1, minSpacingTiles);
        placementRadiusFactor = Mathf.Clamp(placementRadiusFactor, 0.1f, 1f);
        avoidOriginRadiusTiles = Mathf.Max(0, avoidOriginRadiusTiles);
    }
#endif
}
