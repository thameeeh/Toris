using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePoolRegistry : MonoBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
        public ArrowProjectile prefab;      // projectile prefab
        public int initialAllocation = 16;  // initial allocation on awake
        public int defaultCapacity = 32;    // initial capacity used by underlying ObjectPool
        public int maxPoolSize = 256;       // hard cap for pool size to avoid runaway growth
    }

    [Header("Pools decalred in the scene")]
    [SerializeField] private List<PoolConfig> poolsToCreate = new List<PoolConfig>();

    // maps the original prefab to its ObjectPool

    private readonly Dictionary<ArrowProjectile, ObjectPool<ArrowProjectile>>
        pools = new Dictionary<ArrowProjectile, ObjectPool<ArrowProjectile>>();

    private void Awake()
    {
        // create configured pools
        foreach (var cfg in poolsToCreate)
            CreatePoolIfMissing(cfg);

        // preallocate instanaces
        foreach (var cfg in poolsToCreate)
            Preallocate(cfg.prefab, cfg.initialAllocation);
    }

    /// <summary>
    /// Create a pool for the given config if it does not exist yet.
    /// </summary>
    private void CreatePoolIfMissing(PoolConfig cfg)
    {
        if (cfg == null || cfg.prefab == null) return;
        if (pools.ContainsKey(cfg.prefab)) return;

        var prefab = cfg.prefab;

        var pool = new ObjectPool<ArrowProjectile>(
            createFunc: () =>
            {
                // instantiate under the registry
                var inst = Instantiate(prefab, transform);
                inst.gameObject.SetActive(false);
                // give the instance a handle back to the registry and its original prefab key
                inst.SetPool(this, prefab);
                return inst;
            },
            actionOnGet: (obj) =>
            {
                obj.gameObject.SetActive(true);
                obj.OnSpawned(); // reset state (velocity, collider and such)
            },
            actionOnRelease: (obj) =>
            {
                obj.OnDespawned();
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(transform, false);
            },
            actionOnDestroy: (obj) =>
            {
                if (obj) Destroy(obj.gameObject);
            },
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, cfg.defaultCapacity),
            maxSize: Mathf.Max(1, cfg.maxPoolSize)
        );

        pools[prefab] = pool;
    }
    /// <summary>
    /// Preallocate a number of instances for the prefab and return them to the pool.
    /// </summary>
    private void Preallocate(ArrowProjectile prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        if (!pools.TryGetValue(prefab, out var pool)) return;

        var temp = new List<ArrowProjectile>(count);
        for (int i = 0; i < count; i++) temp.Add(pool.Get());
        for (int i = 0; i < count; i++) pool.Release(temp[i]);
    }

    // public API

    /// <summary>
    /// Spawn an instance of the given projectile prefab at position/rotation.
    /// If the prefab was not declared in the inspector, a pool is created on demand.
    /// </summary>
    public ArrowProjectile Spawn(ArrowProjectile prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab, out var pool))
        {
            // lazy-create a small pool if the prefab was not configured
            var cfg = new PoolConfig
            {
                prefab = prefab,
                initialAllocation = 0,
                defaultCapacity = 8,
                maxPoolSize = 128
            };
            CreatePoolIfMissing(cfg);
            pool = pools[prefab];
        }

        var obj = pool.Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    /// <summary>
    /// Return an instance to its pool. If no pool exists for it, destroys the object.
    /// </summary>
    public void Release(ArrowProjectile instance)
    {
        if (instance == null) return;

        // use the instance's recorded OriginalPrefab as dictionary key
        var key = instance.OriginalPrefab ? instance.OriginalPrefab : instance;
        if(pools.TryGetValue(key, out var pool))
        {
            pool.Release(instance);
        }
        else
        {
            Destroy(instance.gameObject);
        }
    }
}
