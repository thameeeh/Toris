using System.Collections.Generic;
using UnityEngine;

public sealed class WorldWildlifeLifecycle
{
    private readonly WorldContext worldContext;
    private readonly WorldSceneServices worldSceneServices;
    private readonly IEnemySpawnService enemySpawnService;
    private readonly Camera streamCamera;

    private readonly Dictionary<Vector2Int, ActiveWildlifeChunk> activeChunks =
        new Dictionary<Vector2Int, ActiveWildlifeChunk>();
    private readonly Dictionary<Enemy, Vector2Int> chunkByEnemy =
        new Dictionary<Enemy, Vector2Int>();
    private readonly Dictionary<Enemy, RetainedWildlife> retainedWildlife =
        new Dictionary<Enemy, RetainedWildlife>();
    private readonly Dictionary<int, WildlifeGroup> groupById =
        new Dictionary<int, WildlifeGroup>();
    private readonly Dictionary<Enemy, WildlifeGroup> groupByEnemy =
        new Dictionary<Enemy, WildlifeGroup>();

    private WildlifeSpawnPlacementIndex wildlifeSpawnIndex;
    private Transform rootContainer;
    private const float RetainedReleaseDelaySeconds = 1.5f;
    private const float ViewportReleasePadding = 0.15f;

    public WorldWildlifeLifecycle(
        WorldContext worldContext,
        WorldSceneServices worldSceneServices,
        IEnemySpawnService enemySpawnService,
        Camera streamCamera)
    {
        this.worldContext = worldContext;
        this.worldSceneServices = worldSceneServices;
        this.enemySpawnService = enemySpawnService;
        this.streamCamera = streamCamera;
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
            DeactivateChunk(chunkKeys[i], forceRelease: true);

        ReleaseAllRetainedWildlife();

        activeChunks.Clear();
        chunkByEnemy.Clear();
        retainedWildlife.Clear();
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

        int adoptedCount = AdoptRetainedWildlifeForChunk(chunkCoord, activeChunk);

        for (int i = 0; i < placements.Count; i++)
        {
            if (adoptedCount > 0)
            {
                adoptedCount--;
                continue;
            }

            SpawnWildlife(placements[i], activeChunk);
        }

        activeChunks.Add(chunkCoord, activeChunk);
    }

    public void DeactivateChunk(Vector2Int chunkCoord)
    {
        DeactivateChunk(chunkCoord, forceRelease: false);
    }

    public void Tick(float deltaTime)
    {
        if (retainedWildlife.Count == 0)
            return;

        List<Enemy> releaseBuffer = null;
        List<Enemy> keys = new List<Enemy>(retainedWildlife.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            Enemy enemy = keys[i];
            if (enemy == null || !retainedWildlife.TryGetValue(enemy, out RetainedWildlife retained))
                continue;

            if (IsEnemyVisible(enemy, retained.Renderers))
            {
                retained.OffscreenTimer = 0f;
                retainedWildlife[enemy] = retained;
                continue;
            }

            retained.OffscreenTimer += deltaTime;
            retainedWildlife[enemy] = retained;

            if (retained.OffscreenTimer < RetainedReleaseDelaySeconds)
                continue;

            releaseBuffer ??= new List<Enemy>();
            releaseBuffer.Add(enemy);
        }

        if (releaseBuffer == null)
            return;

        for (int i = 0; i < releaseBuffer.Count; i++)
            ReleaseRetainedWildlife(releaseBuffer[i]);
    }

    private void DeactivateChunk(Vector2Int chunkCoord, bool forceRelease)
    {
        if (!activeChunks.TryGetValue(chunkCoord, out ActiveWildlifeChunk activeChunk))
            return;

        for (int i = activeChunk.Enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeChunk.Enemies[i];
            if (!forceRelease && TryRetainWildlifeOnChunkUnload(enemy, chunkCoord))
                continue;

            ReleaseWildlife(enemy);
        }

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
        int totalCount = retainedWildlife.Count;

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
        retainedWildlife.Remove(enemy);
        chunkByEnemy.Remove(enemy);
        enemy.RequestDespawn();
    }

    private void HandleWildlifeDespawned(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.Despawned -= HandleWildlifeDespawned;
        retainedWildlife.Remove(enemy);
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

    private bool TryRetainWildlifeOnChunkUnload(Enemy enemy, Vector2Int sourceChunk)
    {
        if (enemy == null || enemy.CurrentHealth <= 0f)
            return false;

        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(false);
        if (!IsEnemyVisible(enemy, renderers))
            return false;

        EnsureRootContainer();
        enemy.transform.SetParent(rootContainer, true);
        chunkByEnemy.Remove(enemy);
        retainedWildlife[enemy] = new RetainedWildlife(sourceChunk, renderers);
        return true;
    }

    private int AdoptRetainedWildlifeForChunk(Vector2Int chunkCoord, ActiveWildlifeChunk activeChunk)
    {
        if (retainedWildlife.Count == 0)
            return 0;

        int adoptedCount = 0;
        List<Enemy> keys = new List<Enemy>(retainedWildlife.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            Enemy enemy = keys[i];
            if (enemy == null || !retainedWildlife.TryGetValue(enemy, out RetainedWildlife retained))
                continue;

            if (retained.SourceChunk != chunkCoord)
                continue;

            retainedWildlife.Remove(enemy);
            chunkByEnemy[enemy] = chunkCoord;
            activeChunk.Enemies.Add(enemy);

            if (activeChunk.Root != null)
                enemy.transform.SetParent(activeChunk.Root, true);

            adoptedCount++;
        }

        return adoptedCount;
    }

    private void ReleaseRetainedWildlife(Enemy enemy)
    {
        if (enemy == null)
            return;

        retainedWildlife.Remove(enemy);
        ReleaseWildlife(enemy);
    }

    private void ReleaseAllRetainedWildlife()
    {
        if (retainedWildlife.Count == 0)
            return;

        List<Enemy> keys = new List<Enemy>(retainedWildlife.Keys);
        for (int i = 0; i < keys.Count; i++)
            ReleaseRetainedWildlife(keys[i]);
    }

    private bool IsEnemyVisible(Enemy enemy, Renderer[] renderers)
    {
        Camera camera = streamCamera != null ? streamCamera : Camera.main;
        if (camera != null)
        {
            Vector3 viewportPosition = camera.WorldToViewportPoint(enemy.transform.position);
            return viewportPosition.z > 0f
                   && viewportPosition.x >= -ViewportReleasePadding
                   && viewportPosition.x <= 1f + ViewportReleasePadding
                   && viewportPosition.y >= -ViewportReleasePadding
                   && viewportPosition.y <= 1f + ViewportReleasePadding;
        }

        if (renderers == null)
            return false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null && renderer.isVisible)
                return true;
        }

        return false;
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

    private struct RetainedWildlife
    {
        public readonly Vector2Int SourceChunk;
        public readonly Renderer[] Renderers;
        public float OffscreenTimer;

        public RetainedWildlife(Vector2Int sourceChunk, Renderer[] renderers)
        {
            SourceChunk = sourceChunk;
            Renderers = renderers;
            OffscreenTimer = 0f;
        }
    }
}
