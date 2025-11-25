using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePoolRegistry : MonoBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
        public Projectile prefab;           // projectile prefab
        public int initialAllocation = 16;  // initial allocation on awake
        public int defaultCapacity = 32;    // initial capacity used by underlying ObjectPool
        public int maxPoolSize = 256;       // hard cap for pool size to avoid runaway growth
        [Tooltip("Optional parent transform for pooled instances. Falls back to registry transform.")]
        public Transform parentOverride;
    }

    [Header("Pools decalred in the scene")]
    [SerializeField] private List<PoolConfig> poolsToCreate = new List<PoolConfig>();

    // maps the original prefab to its ObjectPool

    private readonly Dictionary<Projectile, ProjectilePool> pools = new Dictionary<Projectile, ProjectilePool>();

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
        var parent = ResolveParent(cfg);

        //var pool = new ObjectPool<ArrowProjectile>(
        //    createFunc: () =>
        //    {
        //        var inst = Instantiate(prefab, parent);
        //        inst.gameObject.SetActive(false);
        //        inst.SetPool(this, prefab);
        //        return inst;
        //    },
        //    actionOnGet: (obj) =>
        //    {
        //        obj.gameObject.SetActive(true);
        //        obj.OnSpawned();
        //    },
        //    actionOnRelease: (obj) =>
        //    {
        //        obj.OnDespawned();
        //        obj.gameObject.SetActive(false);
        //        obj.transform.SetParent(parent, false);
        //    },
        //    actionOnDestroy: (obj) =>
        //    {
        //        if (obj) Destroy(obj.gameObject);
        //    },
        //    collectionCheck: false,
        //    defaultCapacity: Mathf.Max(1, cfg.defaultCapacity),
        //    maxSize: Mathf.Max(1, cfg.maxPoolSize)
        //    );

        var pool = new ProjectilePool(
    this,
    prefab,
    parent,
    Mathf.Max(1, cfg.defaultCapacity),
    Mathf.Max(1, cfg.maxPoolSize)
);

        pools[prefab] = pool;
    }
    private Transform ResolveParent(PoolConfig cfg)
    {
        if (cfg != null && cfg.parentOverride != null)
            return cfg.parentOverride;

        return transform;
    }
    /// <summary>
    /// Preallocate a number of instances for the prefab and return them to the pool.
    /// </summary>
    private void Preallocate(Projectile prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        if (!pools.TryGetValue(prefab, out var pool)) return;

        pool.Preallocate(count);
    }

    // public API

    /// <summary>
    /// Spawn an instance of the given projectile prefab at position/rotation.
    /// If the prefab was not declared in the inspector, a pool is created on demand.
    /// </summary>
    public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Projectile
    {
        if (prefab == null) return null;

        var pool = GetOrCreatePool(prefab);
        return (T)pool.Spawn(position, rotation);
    }

    /// <summary>
    /// Return an instance to its pool. If no pool exists for it, destroys the object.
    /// </summary>
    public void Release(Projectile instance)
    {
        if (instance == null) return;

        // use the instance's recorded OriginalPrefab as dictionary key
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

    private ProjectilePool GetOrCreatePool(Projectile prefab)
    {
        if (pools.TryGetValue(prefab, out var pool))
            return pool;

        var cfg = new PoolConfig
        {
            prefab = prefab,
            initialAllocation = 0,
            defaultCapacity = 8,
            maxPoolSize = 128
        };
        CreatePoolIfMissing(cfg);
        return pools[prefab];
    }

    private class ProjectilePool
    {
        private readonly ProjectilePoolRegistry registry;
        private readonly Projectile prefab;
        private readonly ObjectPool<Projectile> pool;
        private readonly Transform parent;

        public ProjectilePool(ProjectilePoolRegistry registry, Projectile prefab, Transform parent,
                              int defaultCapacity, int maxPoolSize)
        {
            this.registry = registry;
            this.prefab = prefab;
            this.parent = parent;

            pool = new ObjectPool<Projectile>(
                createFunc: CreateInstance,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroy,
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );
        }

        public Projectile Spawn(Vector3 position, Quaternion rotation)
        {
            var instance = pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public void Release(Projectile projectile)
        {
            pool.Release(projectile);
        }

        public void Preallocate(int count)
        {
            var temp = new List<Projectile>(count);
            for (int i = 0; i < count; i++) temp.Add(pool.Get());
            for (int i = 0; i < count; i++) pool.Release(temp[i]);
        }

        private Projectile CreateInstance()
        {
            var inst = Object.Instantiate(prefab, parent);
            inst.gameObject.SetActive(false);
            inst.SetPool((IProjectilePool)registry, prefab);
            return inst;
        }

        private void OnGet(Projectile obj)
        {
            obj.gameObject.SetActive(true);
            obj.OnSpawned();
        }

        private void OnRelease(Projectile obj)
        {
            obj.OnDespawned();
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(parent, false);
        }

        private void OnDestroy(Projectile obj)
        {
            if (obj)
                Object.Destroy(obj.gameObject);
        }
    }
}
