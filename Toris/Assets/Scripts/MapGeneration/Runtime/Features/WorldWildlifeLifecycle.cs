using System.Collections.Generic;
using UnityEngine;

public sealed class WorldWildlifeLifecycle
{
    private readonly WorldContext worldContext;
    private readonly WorldSceneServices worldSceneServices;
    private readonly IEnemySpawnService enemySpawnService;

    private readonly Dictionary<Vector2Int, ActiveWildlifeChunk> activeChunks =
        new Dictionary<Vector2Int, ActiveWildlifeChunk>();
    private readonly Dictionary<Enemy, Vector2Int> chunkByEnemy =
        new Dictionary<Enemy, Vector2Int>();
    private readonly Dictionary<int, WildlifeGroup> groupById =
        new Dictionary<int, WildlifeGroup>();
    private readonly Dictionary<Enemy, WildlifeGroup> groupByEnemy =
        new Dictionary<Enemy, WildlifeGroup>();

    private WildlifeSpawnPlacementIndex wildlifeSpawnIndex;
    private Transform rootContainer;

    public WorldWildlifeLifecycle(
        WorldContext worldContext,
        WorldSceneServices worldSceneServices,
        IEnemySpawnService enemySpawnService)
    {
        this.worldContext = worldContext;
        this.worldSceneServices = worldSceneServices;
        this.enemySpawnService = enemySpawnService;
    }

    public void RebuildPlacements()
    {
        wildlifeSpawnIndex = worldContext != null && worldContext.BuildOutput != null
            ? worldContext.BuildOutput.WildlifeSpawns
            : null;
    }

    public void ClearAll()
    {
        List<Vector2Int> chunkKeys = new List<Vector2Int>(activeChunks.Keys);
        for (int i = 0; i < chunkKeys.Count; i++)
            DeactivateChunk(chunkKeys[i]);

        activeChunks.Clear();
        chunkByEnemy.Clear();
        groupById.Clear();
        groupByEnemy.Clear();
        wildlifeSpawnIndex = null;

        if (rootContainer != null)
        {
            Object.Destroy(rootContainer.gameObject);
            rootContainer = null;
        }
    }

    public void ActivateChunk(Vector2Int chunkCoord)
    {
        if (wildlifeSpawnIndex == null || worldSceneServices == null)
            return;

        if (activeChunks.ContainsKey(chunkCoord))
            return;

        if (!wildlifeSpawnIndex.TryGetChunk(chunkCoord, out List<WildlifeSpawnPlacement> placements)
            || placements == null
            || placements.Count == 0)
        {
            return;
        }

        ActiveWildlifeChunk activeChunk = CreateActiveChunk(chunkCoord);

        for (int i = 0; i < placements.Count; i++)
            SpawnWildlife(placements[i], activeChunk);

        activeChunks.Add(chunkCoord, activeChunk);
    }

    public void DeactivateChunk(Vector2Int chunkCoord)
    {
        if (!activeChunks.TryGetValue(chunkCoord, out ActiveWildlifeChunk activeChunk))
            return;

        for (int i = activeChunk.Enemies.Count - 1; i >= 0; i--)
            ReleaseWildlife(activeChunk.Enemies[i]);

        activeChunk.Enemies.Clear();

        if (activeChunk.Root != null)
            Object.Destroy(activeChunk.Root.gameObject);

        activeChunks.Remove(chunkCoord);
    }

    public int GetActiveWildlifeChunkCount()
    {
        return activeChunks.Count;
    }

    public int GetActiveWildlifeCount()
    {
        int totalCount = 0;

        foreach (KeyValuePair<Vector2Int, ActiveWildlifeChunk> pair in activeChunks)
            totalCount += pair.Value.Enemies.Count;

        return totalCount;
    }

    public int GetTotalPlacedWildlifeCount()
    {
        return wildlifeSpawnIndex != null ? wildlifeSpawnIndex.Count : 0;
    }

    private void SpawnWildlife(WildlifeSpawnPlacement placement, ActiveWildlifeChunk activeChunk)
    {
        WorldWildlifeSpawnDefinition spawnDefinition = placement.SpawnDefinition;
        if (spawnDefinition == null || !spawnDefinition.IsValid)
            return;

        Vector3 worldPosition = worldSceneServices.GetCellCenterWorld(placement.CenterTile);
        Enemy enemy = enemySpawnService != null
            ? enemySpawnService.SpawnEnemy(spawnDefinition.EnemyPrefab, worldPosition, Quaternion.identity)
            : null;

        if (enemy == null)
            return;

        enemy.transform.SetParent(activeChunk.Root, true);
        TryAttachToWildlifeGroup(enemy, placement.GroupId);
        enemy.Despawned += HandleWildlifeDespawned;
        activeChunk.Enemies.Add(enemy);
        chunkByEnemy[enemy] = activeChunk.ChunkCoord;
    }

    private void ReleaseWildlife(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.Despawned -= HandleWildlifeDespawned;
        DetachFromWildlifeGroup(enemy);
        chunkByEnemy.Remove(enemy);
        enemy.RequestDespawn();
    }

    private void HandleWildlifeDespawned(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.Despawned -= HandleWildlifeDespawned;
        DetachFromWildlifeGroup(enemy);

        if (!chunkByEnemy.TryGetValue(enemy, out Vector2Int chunkCoord))
            return;

        chunkByEnemy.Remove(enemy);

        if (activeChunks.TryGetValue(chunkCoord, out ActiveWildlifeChunk activeChunk))
            activeChunk.Enemies.Remove(enemy);
    }

    private void TryAttachToWildlifeGroup(Enemy enemy, int groupId)
    {
        if (enemy == null || groupId <= 0)
            return;

        if (!(enemy is IWildlifeGroupMember groupMember))
            return;

        WildlifeGroup group = GetOrCreateGroup(groupId);
        group.AddMember(groupMember);
        groupByEnemy[enemy] = group;
    }

    private void DetachFromWildlifeGroup(Enemy enemy)
    {
        if (enemy == null || !groupByEnemy.TryGetValue(enemy, out WildlifeGroup group))
            return;

        if (enemy is IWildlifeGroupMember groupMember)
            group.RemoveMember(groupMember);

        groupByEnemy.Remove(enemy);

        if (group.MemberCount <= 0)
            groupById.Remove(group.GroupId);
    }

    private WildlifeGroup GetOrCreateGroup(int groupId)
    {
        if (groupById.TryGetValue(groupId, out WildlifeGroup existingGroup))
            return existingGroup;

        WildlifeGroup group = new WildlifeGroup(groupId);
        groupById.Add(groupId, group);
        return group;
    }

    private ActiveWildlifeChunk CreateActiveChunk(Vector2Int chunkCoord)
    {
        EnsureRootContainer();

        GameObject chunkRootObject = new GameObject($"Wildlife_{chunkCoord.x}_{chunkCoord.y}");
        Transform chunkRoot = chunkRootObject.transform;
        chunkRoot.SetParent(rootContainer, false);

        return new ActiveWildlifeChunk(chunkCoord, chunkRoot);
    }

    private void EnsureRootContainer()
    {
        if (rootContainer != null)
            return;

        GameObject rootObject = new GameObject("WorldWildlifeLifecycle_Root");
        rootContainer = rootObject.transform;
    }

    private sealed class ActiveWildlifeChunk
    {
        public readonly Vector2Int ChunkCoord;
        public readonly Transform Root;
        public readonly List<Enemy> Enemies = new List<Enemy>();

        public ActiveWildlifeChunk(Vector2Int chunkCoord, Transform root)
        {
            ChunkCoord = chunkCoord;
            Root = root;
        }
    }
}
