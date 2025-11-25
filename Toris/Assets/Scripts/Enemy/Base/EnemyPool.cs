using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
        public Enemy prefab;
        [Min(0)] public int prewarmCount = 0;
        [Min(1)] public int defaultCapacity = 8;
        [Min(1)] public int maxPoolSize = 64;
    }

    [SerializeField] private List<PoolConfig> poolsToCreate = new List<PoolConfig>();

    private readonly Dictionary<Enemy, ObjectPool<Enemy>> pools = new Dictionary<Enemy, ObjectPool<Enemy>>();

    private void Awake()
    {
        foreach (var cfg in poolsToCreate)
            CreatePoolIfMissing(cfg);

        foreach (var cfg in poolsToCreate)
            Prewarm(cfg.prefab, cfg.prewarmCount);
    }

    public Enemy Spawn(EnemySpawnRequest request)
    {
        if (request.Prefab == null) return null;

        if (!pools.TryGetValue(request.Prefab, out var pool))
        {
            var cfg = new PoolConfig
            {
                prefab = request.Prefab,
                prewarmCount = 0,
                defaultCapacity = 8,
                maxPoolSize = 128
            };
            CreatePoolIfMissing(cfg);
            pool = pools[request.Prefab];
        }

        var enemy = pool.Get();
        enemy.transform.SetParent(request.Parent ? request.Parent : transform, false);
        enemy.transform.SetPositionAndRotation(request.Position, request.Rotation);
        enemy.PrepareSpawn(request);
        enemy.OnSpawned();
        enemy.gameObject.SetActive(true);
        return enemy;
    }

    public void Release(Enemy instance)
    {
        if (instance == null) return;

        var key = instance.OriginalPrefab ? instance.OriginalPrefab : instance;
        if (pools.TryGetValue(key, out var pool))
        {
            pool.Release(instance);
        }
        else
        {
            Destroy(instance.gameObject);
        }
    }

    private void CreatePoolIfMissing(PoolConfig cfg)
    {
        if (cfg == null || cfg.prefab == null) return;
        if (pools.ContainsKey(cfg.prefab)) return;

        var prefab = cfg.prefab;

        var pool = new ObjectPool<Enemy>(
            createFunc: () =>
            {
                var inst = Instantiate(prefab, transform);
                inst.gameObject.SetActive(false);
                inst.SetPool(this, prefab);
                return inst;
            },
            actionOnGet: (enemy) =>
            {
                enemy.SetPool(this, prefab);
            },
            actionOnRelease: (enemy) =>
            {
                enemy.OnDespawned();
                enemy.gameObject.SetActive(false);
                enemy.transform.SetParent(transform, false);
            },
            actionOnDestroy: (enemy) =>
            {
                if (enemy) Destroy(enemy.gameObject);
            },
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, cfg.defaultCapacity),
            maxSize: Mathf.Max(1, cfg.maxPoolSize)
        );

        pools[prefab] = pool;
    }

    private void Prewarm(Enemy prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        if (!pools.TryGetValue(prefab, out var pool)) return;

        var temp = new List<Enemy>(count);
        for (int i = 0; i < count; i++) temp.Add(pool.Get());
        for (int i = 0; i < count; i++) pool.Release(temp[i]);
    }
}