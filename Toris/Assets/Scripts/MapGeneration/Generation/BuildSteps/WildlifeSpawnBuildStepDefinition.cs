using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Build Steps/Wildlife Spawn Step",
    fileName = "WildlifeSpawnBuildStepDefinition")]
public sealed class WildlifeSpawnBuildStepDefinition : BiomeBuildStepDefinition
{
    private const uint WildlifeCountSalt = 0xD33C001u;
    private const uint WildlifePickSalt = 0xD33BEEFu;
    private const uint WildlifeRuleSalt = 0xD33A11u;
    private const int BaseAttempts = 200;
    private const int AttemptsPerTarget = 250;
    private const int RelaxSteps = 6;
    private const int RelaxSpacingStep = 4;
    private const int RelaxedSpacingFloor = 4;
    private const int RelaxStartIndexBase = 100000;
    private const float MinPlacementRadiusFactor = 0.1f;

    [SerializeField] private WildlifeSpawnRule[] wildlifeRules;

    [System.NonSerialized] private TileResolver tileResolver;

    public override void Build(WorldContext ctx)
    {
        if (ctx == null || ctx.BuildOutput == null || wildlifeRules == null || wildlifeRules.Length == 0)
            return;

        for (int i = 0; i < wildlifeRules.Length; i++)
        {
            WildlifeSpawnRule rule = wildlifeRules[i];
            if (rule == null || !rule.IsValid)
                continue;

            BuildRule(ctx, rule, i);
        }
    }

    private void BuildRule(WorldContext ctx, WildlifeSpawnRule rule, int ruleIndex)
    {
        int targetCount = ResolveSpawnCount(ctx.ActiveBiome.Seed, rule, ruleIndex);
        if (targetCount <= 0)
            return;

        int spacingTiles = Mathf.Max(1, rule.MinSpacingTiles);
        int avoidOriginRadiusTiles = Mathf.Max(0, rule.AvoidOriginRadiusTiles);
        float radiusTiles = ctx.ActiveBiome.RadiusTiles * Mathf.Clamp(
            rule.PlacementRadiusFactor,
            MinPlacementRadiusFactor,
            1f);

        uint sampleSalt = DeterministicHash.Hash(
            WildlifePickSalt,
            ruleIndex,
            targetCount,
            WildlifeRuleSalt);

        List<Vector2Int> spawnTiles = SitePlacementSampling.PickSpacedCentersInBiomeDisk(
            ctx.ActiveBiome.Seed,
            ctx.ActiveBiome.OriginTile,
            radiusTiles,
            targetCount,
            spacingTiles,
            avoidOriginRadiusTiles,
            AttemptsPerTarget,
            BaseAttempts,
            RelaxSteps,
            RelaxSpacingStep,
            RelaxedSpacingFloor,
            RelaxStartIndexBase,
            sampleSalt,
            candidateTile => IsCandidateAllowed(ctx, candidateTile, rule));

        for (int i = 0; i < spawnTiles.Count; i++)
        {
            ctx.BuildOutput.RegisterWildlifeSpawn(
                rule.SpawnDefinition,
                spawnTiles[i],
                ctx.World.chunkSize);
        }

#if UNITY_EDITOR
        if (spawnTiles.Count < targetCount)
        {
            Debug.LogWarning(
                $"[WildlifeSpawnStep] Only placed {spawnTiles.Count}/{targetCount} wildlife spawns for '{rule.SpawnDefinition.name}'.",
                this);
        }
#endif
    }

    private int ResolveSpawnCount(int biomeSeed, WildlifeSpawnRule rule, int ruleIndex)
    {
        int minCount = Mathf.Max(0, rule.MinSpawnCount);
        int maxCount = Mathf.Max(minCount, rule.MaxSpawnCount);
        if (maxCount == minCount)
            return minCount;

        uint countHash = DeterministicHash.Hash(
            (uint)biomeSeed,
            ruleIndex,
            minCount + maxCount,
            WildlifeCountSalt);
        int range = maxCount - minCount + 1;
        int offset = Mathf.FloorToInt(DeterministicHash.Hash01(countHash) * range);
        return Mathf.Clamp(minCount + offset, minCount, maxCount);
    }

    private bool IsCandidateAllowed(WorldContext ctx, Vector2Int worldTile, WildlifeSpawnRule rule)
    {
        Vector2Int localTile = ctx.ActiveBiome.ToLocal(worldTile);
        if (!ctx.Mask.IsLand(localTile, ctx))
            return false;

        if (rule.AvoidTerrainOverrides
            && ctx.BuildOutput.TerrainOverrides != null
            && ctx.BuildOutput.TerrainOverrides.TryGet(worldTile, out _))
        {
            return false;
        }

        if (rule.AvoidNavigationBlockers
            && ctx.BuildOutput.NavigationContributions != null
            && ctx.BuildOutput.NavigationContributions.GetNavigationContribution(worldTile).BlocksNavigation)
        {
            return false;
        }

        TileResult tileResult = GetTileResolver().Resolve(worldTile, ctx);
        if (tileResult.ground == null || tileResult.HasWater)
            return false;

        if (rule.AvoidObstacles && tileResult.HasObstacle)
            return false;

        return true;
    }

    private TileResolver GetTileResolver()
    {
        if (tileResolver == null)
            tileResolver = new TileResolver();

        return tileResolver;
    }

    [System.Serializable]
    private sealed class WildlifeSpawnRule
    {
        [SerializeField] private WorldWildlifeSpawnDefinition spawnDefinition;
        [SerializeField, Min(0)] private int minSpawnCount = 0;
        [SerializeField, Min(0)] private int maxSpawnCount = 8;
        [SerializeField, Min(1)] private int minSpacingTiles = 18;
        [SerializeField, Range(0.1f, 1f)] private float placementRadiusFactor = 0.9f;
        [SerializeField, Min(0)] private int avoidOriginRadiusTiles = 18;
        [SerializeField] private bool avoidTerrainOverrides = true;
        [SerializeField] private bool avoidNavigationBlockers = true;
        [SerializeField] private bool avoidObstacles = true;

        public WorldWildlifeSpawnDefinition SpawnDefinition => spawnDefinition;
        public int MinSpawnCount => minSpawnCount;
        public int MaxSpawnCount => maxSpawnCount;
        public int MinSpacingTiles => minSpacingTiles;
        public float PlacementRadiusFactor => placementRadiusFactor;
        public int AvoidOriginRadiusTiles => avoidOriginRadiusTiles;
        public bool AvoidTerrainOverrides => avoidTerrainOverrides;
        public bool AvoidNavigationBlockers => avoidNavigationBlockers;
        public bool AvoidObstacles => avoidObstacles;
        public bool IsValid => spawnDefinition != null && spawnDefinition.IsValid;
    }
}
