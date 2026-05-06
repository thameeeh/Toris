using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Build Steps/Wildlife Spawn Step",
    fileName = "WildlifeSpawnBuildStepDefinition")]
public sealed class WildlifeSpawnBuildStepDefinition : BiomeBuildStepDefinition
{
    private const uint WildlifeCountSalt = 0xD33C001u;
    private const uint WildlifePickSalt = 0xD33BEEFu;
    private const uint WildlifeRuleSalt = 0xD33A11u;
    private const uint WildlifeClusterSizeSalt = 0xD33F00Du;
    private const uint WildlifeClusterMemberSalt = 0xD33FA11u;
    private const uint WildlifeGroupIdSalt = 0xD33C0DEu;
    private const int BaseAttempts = 200;
    private const int AttemptsPerTarget = 250;
    private const int ClusterMemberAttemptsPerTarget = 40;
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
        int targetGroupCount = ResolveGroupCount(ctx.ActiveBiome.Seed, rule, ruleIndex);
        if (targetGroupCount <= 0)
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
            targetGroupCount,
            WildlifeRuleSalt);

        List<Vector2Int> groupCenters = SitePlacementSampling.PickSpacedCentersInBiomeDisk(
            ctx.ActiveBiome.Seed,
            ctx.ActiveBiome.OriginTile,
            radiusTiles,
            targetGroupCount,
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

        int spawnedMemberCount = 0;
        for (int i = 0; i < groupCenters.Count; i++)
        {
            Vector2Int groupCenter = groupCenters[i];
            int groupId = ResolveGroupId(ctx.ActiveBiome.Seed, ruleIndex, i, groupCenter);
            int memberCount = ResolveClusterSize(ctx.ActiveBiome.Seed, rule, ruleIndex, i);
            List<Vector2Int> memberTiles = PickClusterMemberTiles(ctx, rule, groupCenter, memberCount, groupId);

            for (int memberIndex = 0; memberIndex < memberTiles.Count; memberIndex++)
            {
                ctx.BuildOutput.RegisterWildlifeSpawn(
                    rule.SpawnDefinition,
                    memberTiles[memberIndex],
                    ctx.World.chunkSize,
                    groupId);
            }

            spawnedMemberCount += memberTiles.Count;
        }

#if UNITY_EDITOR
        if (groupCenters.Count < targetGroupCount)
        {
            Debug.LogWarning(
                $"[WildlifeSpawnStep] Only placed {groupCenters.Count}/{targetGroupCount} wildlife groups for '{rule.SpawnDefinition.name}' with {spawnedMemberCount} total members.",
                this);
        }
#endif
    }

    private int ResolveGroupCount(int biomeSeed, WildlifeSpawnRule rule, int ruleIndex)
    {
        int minCount = Mathf.Max(0, rule.MinGroupCount);
        int maxCount = Mathf.Max(minCount, rule.MaxGroupCount);
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

    private int ResolveClusterSize(int biomeSeed, WildlifeSpawnRule rule, int ruleIndex, int groupIndex)
    {
        int minSize = Mathf.Max(1, rule.MinClusterSize);
        int maxSize = Mathf.Max(minSize, rule.MaxClusterSize);
        if (maxSize == minSize)
            return minSize;

        uint sizeHash = DeterministicHash.Hash(
            (uint)biomeSeed,
            ruleIndex,
            groupIndex,
            WildlifeClusterSizeSalt);
        int range = maxSize - minSize + 1;
        int offset = Mathf.FloorToInt(DeterministicHash.Hash01(sizeHash) * range);
        return Mathf.Clamp(minSize + offset, minSize, maxSize);
    }

    private List<Vector2Int> PickClusterMemberTiles(
        WorldContext ctx,
        WildlifeSpawnRule rule,
        Vector2Int groupCenter,
        int memberCount,
        int groupId)
    {
        List<Vector2Int> memberTiles = new List<Vector2Int>(Mathf.Max(1, memberCount));
        if (memberCount <= 0)
            return memberTiles;

        memberTiles.Add(groupCenter);

        int attempts = Mathf.Max(ClusterMemberAttemptsPerTarget, memberCount * ClusterMemberAttemptsPerTarget);
        float clusterRadiusTiles = Mathf.Max(0.1f, rule.ClusterRadiusTiles);
        float memberSpacingSqr = rule.MinClusterMemberSpacingTiles * rule.MinClusterMemberSpacingTiles;

        for (int i = 0; i < attempts && memberTiles.Count < memberCount; i++)
        {
            Vector2Int candidateTile = PickPointInDisk(
                ctx.ActiveBiome.Seed,
                i,
                groupCenter,
                clusterRadiusTiles,
                DeterministicHash.Hash(WildlifeClusterMemberSalt, groupId, memberCount, WildlifeRuleSalt));

            if (!IsCandidateAllowed(ctx, candidateTile, rule))
                continue;

            if (!IsFarEnoughFromClusterMembers(candidateTile, memberTiles, memberSpacingSqr))
                continue;

            memberTiles.Add(candidateTile);
        }

        return memberTiles;
    }

    private static bool IsFarEnoughFromClusterMembers(
        Vector2Int candidateTile,
        List<Vector2Int> memberTiles,
        float minSpacingSqr)
    {
        for (int i = 0; i < memberTiles.Count; i++)
        {
            if ((memberTiles[i] - candidateTile).sqrMagnitude < minSpacingSqr)
                return false;
        }

        return true;
    }

    private static Vector2Int PickPointInDisk(
        int biomeSeed,
        int index,
        Vector2Int originTile,
        float radiusTiles,
        uint sampleSalt)
    {
        uint angleHash = DeterministicHash.Hash((uint)biomeSeed, index, 0, sampleSalt);
        uint radiusHash = DeterministicHash.Hash((uint)biomeSeed, index, 1, sampleSalt);

        float angle01 = DeterministicHash.Hash01(angleHash);
        float radius01 = DeterministicHash.Hash01(radiusHash);

        float angleRadians = angle01 * Mathf.PI * 2f;
        float distance = Mathf.Sqrt(radius01) * radiusTiles;

        int offsetX = Mathf.RoundToInt(Mathf.Cos(angleRadians) * distance);
        int offsetY = Mathf.RoundToInt(Mathf.Sin(angleRadians) * distance);

        return originTile + new Vector2Int(offsetX, offsetY);
    }

    private static int ResolveGroupId(int biomeSeed, int ruleIndex, int groupIndex, Vector2Int groupCenter)
    {
        uint hash = DeterministicHash.Hash(
            (uint)biomeSeed,
            groupCenter.x + ruleIndex,
            groupCenter.y + groupIndex,
            WildlifeGroupIdSalt);
        return Mathf.Max(1, (int)(hash & 0x7FFFFFFFu));
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
        [FormerlySerializedAs("minSpawnCount")]
        [SerializeField, Min(0)] private int minGroupCount = 0;
        [FormerlySerializedAs("maxSpawnCount")]
        [SerializeField, Min(0)] private int maxGroupCount = 8;
        [SerializeField, Min(1)] private int minClusterSize = 2;
        [SerializeField, Min(1)] private int maxClusterSize = 4;
        [SerializeField, Min(0.1f)] private float clusterRadiusTiles = 3f;
        [SerializeField, Min(0f)] private float minClusterMemberSpacingTiles = 1f;
        [SerializeField, Min(1)] private int minSpacingTiles = 18;
        [SerializeField, Range(0.1f, 1f)] private float placementRadiusFactor = 0.9f;
        [SerializeField, Min(0)] private int avoidOriginRadiusTiles = 18;
        [SerializeField] private bool avoidTerrainOverrides = true;
        [SerializeField] private bool avoidNavigationBlockers = true;
        [SerializeField] private bool avoidObstacles = true;

        public WorldWildlifeSpawnDefinition SpawnDefinition => spawnDefinition;
        public int MinGroupCount => minGroupCount;
        public int MaxGroupCount => maxGroupCount;
        public int MinClusterSize => minClusterSize;
        public int MaxClusterSize => maxClusterSize;
        public float ClusterRadiusTiles => clusterRadiusTiles;
        public float MinClusterMemberSpacingTiles => minClusterMemberSpacingTiles;
        public int MinSpacingTiles => minSpacingTiles;
        public float PlacementRadiusFactor => placementRadiusFactor;
        public int AvoidOriginRadiusTiles => avoidOriginRadiusTiles;
        public bool AvoidTerrainOverrides => avoidTerrainOverrides;
        public bool AvoidNavigationBlockers => avoidNavigationBlockers;
        public bool AvoidObstacles => avoidObstacles;
        public bool IsValid => spawnDefinition != null && spawnDefinition.IsValid;
    }
}
