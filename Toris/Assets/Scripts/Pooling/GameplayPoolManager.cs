using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameplayPoolManager : MonoBehaviour, IProjectilePool, IEnemyPool, IVisualPool
{
    public static GameplayPoolManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private GameplayPoolConfiguration configuration;
    [SerializeField] private bool persistAcrossScenes = false;

    [Header("Parents (optional)")]
    [SerializeField] private Transform projectileRoot;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform enemyRoot;

    [Header("Fallback sizing (used when prefab is not configured)")]
    [SerializeField] private int defaultProjectilePrewarm = 0;
    [SerializeField] private int defaultProjectileCapacity = 16;
    [SerializeField] private int defaultProjectileMaxSize = 128;
    [SerializeField] private int defaultVisualPrewarm = 0;
    [SerializeField] private int defaultVisualCapacity = 16;
    [SerializeField] private int defaultVisualMaxSize = 128;
    [SerializeField] private int defaultEnemyPrewarm = 0;
    [SerializeField] private int defaultEnemyCapacity = 8;
    [SerializeField] private int defaultEnemyMaxSize = 32;

    [Header("Enemy Spawn Safety")]
    [SerializeField, Min(0)] private int enemyWalkableSearchRadiusTiles = 6;

    // Projectiles now use SafeRuntimePool (custom) so we never get destroyed refs back.
    private readonly Dictionary<Projectile, SafeRuntimePool<Projectile>> projectilePools = new();
    private readonly Dictionary<Projectile, PoolCounters> projectileCounters = new();

    private readonly Dictionary<GameObject, SafeRuntimePool<PooledVisualInstance>> visualPools = new();
    private readonly Dictionary<GameObject, PoolCounters> visualCounters = new();

    // Enemies still use UnityEngine.Pool.ObjectPool<T>
    private readonly Dictionary<Enemy, SafeRuntimePool<Enemy>> enemyPools = new();
    private readonly Dictionary<Enemy, PoolCounters> enemyCounters = new();

    private readonly Dictionary<Projectile, ProjectilePoolSettings> projectileSettings = new();
    private readonly Dictionary<GameObject, VisualPoolSettings> visualSettings = new();
    private readonly Dictionary<Enemy, EnemyPoolSettings> enemySettings = new();

    private bool _isPrewarmingEnemies = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        BuildConfiguredPools();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Initialization ---

    public void Configure(GameplayPoolConfiguration config, Transform projectileParent = null, Transform enemyParent = null)
    {
        configuration = config;

        if (projectileParent != null)
            projectileRoot = projectileParent;

        if (enemyParent != null)
            enemyRoot = enemyParent;

        BuildConfiguredPools();
    }

    public void BuildConfiguredPools()
    {
        projectilePools.Clear();
        projectileCounters.Clear();
        projectileSettings.Clear();
        visualPools.Clear();
        visualCounters.Clear();
        visualSettings.Clear();
        enemyPools.Clear();
        enemyCounters.Clear();
        enemySettings.Clear();

        if (configuration != null)
        {
            // Projectiles
            foreach (var cfg in configuration.ProjectilePools)
            {
                if (cfg?.prefab == null)
                    continue;

                projectileSettings[cfg.prefab] = cfg;
                var pool = EnsureProjectilePool(cfg.prefab, cfg);
                PrewarmProjectilePool(pool, cfg.prewarmCount);
            }

            foreach (var cfg in configuration.VisualPools)
            {
                if (cfg?.prefab == null)
                    continue;

                visualSettings[cfg.prefab] = cfg;
                var pool = EnsureVisualPool(cfg.prefab, cfg);
                PrewarmVisualPool(pool, cfg.prewarmCount);
            }

            // Enemies
            foreach (var cfg in configuration.EnemyPools)
            {
                if (cfg?.prefab == null)
                    continue;

                //Debug.Log($"[Pool] Config enemy pool for {cfg.prefab.name}, prewarm={cfg.prewarmCount}");

                enemySettings[cfg.prefab] = cfg;
                var pool = EnsureEnemyPool(cfg.prefab, cfg);
                PrewarmEnemyPool(pool, cfg.prewarmCount);
            }
        }
    }

    // --- Projectile API ---

    public Projectile SpawnProjectile(Projectile prefab, Vector3 position, Quaternion rotation)
    {
        var pool = EnsureProjectilePool(prefab);
        if (pool == null)
            return null;

        var proj = pool.Get();
        if (!proj)
        {
            // Should not normally happen with SafeRuntimePool, but guard anyway.
            return null;
        }

        proj.transform.SetPositionAndRotation(position, rotation);
        return proj;
    }

    public void Release(Projectile instance)
    {
        if (instance == null)
            return;

        var key = instance.OriginalPrefab ? instance.OriginalPrefab : instance;

        if (projectilePools.TryGetValue(key, out var pool))
        {
            pool.Release(instance);
        }
        else
        {
            Destroy(instance.gameObject);
        }
    }

    public PoolReport GetProjectileReport(Projectile prefab)
    {
        if (prefab == null)
            return PoolReport.Empty;

        projectilePools.TryGetValue(prefab, out var pool);
        projectileCounters.TryGetValue(prefab, out var counters);

        return new PoolReport(
            prefab.name,
            counters.ActiveCount,
            pool?.CountInactive ?? 0,
            counters.TotalCreated,
            counters.PeakActive
        );
    }

    public IEnumerable<PoolReport> EnumerateProjectileReports()
    {
        foreach (var kvp in projectilePools)
            yield return GetProjectileReport(kvp.Key);
    }

    // --- Visual API ---

    public PooledVisualInstance SpawnVisual(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var pool = EnsureVisualPool(prefab);
        if (pool == null)
            return null;

        var visual = pool.Get();
        if (!visual)
            return null;

        visual.transform.SetPositionAndRotation(position, rotation);
        return visual;
    }

    public void Release(PooledVisualInstance instance)
    {
        if (instance == null)
            return;

        var key = instance.OriginalPrefab ? instance.OriginalPrefab : instance.gameObject;

        if (visualPools.TryGetValue(key, out var pool))
        {
            pool.Release(instance);
        }
        else
        {
            Destroy(instance.gameObject);
        }
    }

    // --- Enemy API ---

    public Enemy SpawnEnemy(Enemy prefab, Vector3 position, Quaternion rotation)
    {
        var pool = EnsureEnemyPool(prefab);
        if (pool == null)
            return null;

        var enemy = pool.Get();
        if (!enemy)
            return null;

        position = ResolveEnemySpawnPosition(position);
        enemy.transform.SetPositionAndRotation(position, rotation);
        return enemy;
    }


    public void Release(Enemy instance)
    {
        if (instance == null)
            return;

        var key = instance.OriginalPrefab ? instance.OriginalPrefab : instance;

        if (enemyPools.TryGetValue(key, out var pool))
        {
            pool.Release(instance);
        }
        else
        {
            Destroy(instance.gameObject);
        }
    }


    public PoolReport GetEnemyReport(Enemy prefab)
    {
        if (prefab == null)
            return PoolReport.Empty;

        enemyPools.TryGetValue(prefab, out var pool);
        enemyCounters.TryGetValue(prefab, out var counters);

        return new PoolReport(
            prefab.name,
            counters.ActiveCount,
            pool?.CountInactive ?? 0,
            counters.TotalCreated,
            counters.PeakActive
        );
    }

    public IEnumerable<PoolReport> EnumerateEnemyReports()
    {
        foreach (var kvp in enemyPools)
        {
            yield return GetEnemyReport(kvp.Key);
        }
    }

    // --- Internal: pool construction ---

    private SafeRuntimePool<Projectile> EnsureProjectilePool(Projectile prefab, ProjectilePoolSettings settings = null)
    {
        if (prefab == null)
            return null;

        if (!projectilePools.TryGetValue(prefab, out var pool))
        {
            var effectiveSettings = settings ?? TryGetProjectileSettings(prefab) ?? CreateDefaultProjectileSettings(prefab);
            pool = CreateProjectilePool(prefab, effectiveSettings);
            projectilePools[prefab] = pool;

            if (settings == null)
            {
                PrewarmProjectilePool(pool, effectiveSettings.prewarmCount);
            }
        }

        return pool;
    }

    private SafeRuntimePool<Enemy> EnsureEnemyPool(Enemy prefab, EnemyPoolSettings settings = null)
    {
        if (prefab == null)
            return null;

        if (enemyPools.TryGetValue(prefab, out var existing))
            return existing;

        var effectiveSettings = settings
                                ?? TryGetEnemySettings(prefab)
                                ?? CreateDefaultEnemySettings(prefab);

        var pool = CreateEnemyPool(prefab, effectiveSettings);
        enemyPools[prefab] = pool;

        if (settings == null)
        {
            PrewarmEnemyPool(pool, effectiveSettings.prewarmCount);
        }

        return pool;
    }

    private SafeRuntimePool<PooledVisualInstance> EnsureVisualPool(GameObject prefab, VisualPoolSettings settings = null)
    {
        if (prefab == null)
            return null;

        if (!visualPools.TryGetValue(prefab, out var pool))
        {
            var effectiveSettings = settings ?? TryGetVisualSettings(prefab) ?? CreateDefaultVisualSettings(prefab);
            pool = CreateVisualPool(prefab, effectiveSettings);
            visualPools[prefab] = pool;

            if (settings == null)
                PrewarmVisualPool(pool, effectiveSettings.prewarmCount);
        }

        return pool;
    }


    private ProjectilePoolSettings TryGetProjectileSettings(Projectile prefab)
    {
        if (prefab == null)
            return null;

        projectileSettings.TryGetValue(prefab, out var settings);
        return settings;
    }

    private EnemyPoolSettings TryGetEnemySettings(Enemy prefab)
    {
        if (prefab == null)
            return null;

        enemySettings.TryGetValue(prefab, out var settings);
        return settings;
    }

    private VisualPoolSettings TryGetVisualSettings(GameObject prefab)
    {
        if (prefab == null)
            return null;

        visualSettings.TryGetValue(prefab, out var settings);
        return settings;
    }

    private ProjectilePoolSettings CreateDefaultProjectileSettings(Projectile prefab)
    {
        return new ProjectilePoolSettings
        {
            prefab = prefab,
            prewarmCount = defaultProjectilePrewarm,
            defaultCapacity = defaultProjectileCapacity,
            maxSize = defaultProjectileMaxSize,
            parentOverride = projectileRoot
        };
    }

    private EnemyPoolSettings CreateDefaultEnemySettings(Enemy prefab)
    {
        return new EnemyPoolSettings
        {
            prefab = prefab,
            prewarmCount = defaultEnemyPrewarm,
            defaultCapacity = defaultEnemyCapacity,
            maxSize = defaultEnemyMaxSize,
            parentOverride = enemyRoot
        };
    }

    private VisualPoolSettings CreateDefaultVisualSettings(GameObject prefab)
    {
        return new VisualPoolSettings
        {
            prefab = prefab,
            prewarmCount = defaultVisualPrewarm,
            defaultCapacity = defaultVisualCapacity,
            maxSize = defaultVisualMaxSize,
            parentOverride = visualRoot
        };
    }

    private SafeRuntimePool<Projectile> CreateProjectilePool(Projectile prefab, ProjectilePoolSettings settings)
    {
        var counters = GetOrCreateCounters(projectileCounters, prefab);
        var parent = ResolveParent(settings?.parentOverride, projectileRoot, "ProjectilePools");

        return new SafeRuntimePool<Projectile>(
            // Factory
            () => CreateProjectileInstance(prefab, parent, counters),
            // OnGet
            obj =>
            {
                counters.ActiveCount++;
                counters.PeakActive = Mathf.Max(counters.PeakActive, counters.ActiveCount);
                obj.gameObject.SetActive(true);
                obj.OnSpawned();
            },
            // OnRelease
            obj =>
            {
                counters.ActiveCount = Mathf.Max(0, counters.ActiveCount - 1);
                obj.OnDespawned();
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(parent, false);
            },
            obj =>
            {
                if (obj)
                    Destroy(obj.gameObject);
            }
        );
    }

    private SafeRuntimePool<Enemy> CreateEnemyPool(Enemy prefab, EnemyPoolSettings settings)
    {
        var counters = GetOrCreateCounters(enemyCounters, prefab);
        var parent = ResolveParent(settings?.parentOverride, enemyRoot, "EnemyPools");

        var pool = new SafeRuntimePool<Enemy>(
            // Factory
            () => CreateEnemyInstance(prefab, parent, counters),

            // OnGet
            obj =>
            {
                counters.ActiveCount++;
                counters.PeakActive = Mathf.Max(counters.PeakActive, counters.ActiveCount);

                obj.gameObject.SetActive(true);

                if (!_isPrewarmingEnemies)
                {
                    obj.OnSpawned();
                }
            },

            // OnRelease
            obj =>
            {
                counters.ActiveCount = Mathf.Max(0, counters.ActiveCount - 1);

                obj.OnDespawned();
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(parent, false);
            },

            obj =>
            {
                if (obj)
                {
                    Destroy(obj.gameObject);
                }
            });

        return pool;
    }

    private SafeRuntimePool<PooledVisualInstance> CreateVisualPool(GameObject prefab, VisualPoolSettings settings)
    {
        var counters = GetOrCreateCounters(visualCounters, prefab);
        var parent = ResolveParent(settings?.parentOverride, visualRoot, "VisualPools");

        return new SafeRuntimePool<PooledVisualInstance>(
            () => CreateVisualInstance(prefab, parent, counters),
            obj =>
            {
                counters.ActiveCount++;
                counters.PeakActive = Mathf.Max(counters.PeakActive, counters.ActiveCount);
                obj.gameObject.SetActive(true);
                obj.NotifySpawned();
            },
            obj =>
            {
                counters.ActiveCount = Mathf.Max(0, counters.ActiveCount - 1);
                obj.NotifyDespawned();
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(parent, false);
            },
            obj =>
            {
                if (obj)
                    Destroy(obj.gameObject);
            });
    }


    private Projectile CreateProjectileInstance(Projectile prefab, Transform parent, PoolCounters counters)
    {
        var inst = Instantiate(prefab, parent);
        inst.gameObject.SetActive(false);
        inst.SetPool(this, prefab);
        counters.TotalCreated++;
        return inst;
    }

    private Enemy CreateEnemyInstance(Enemy prefab, Transform parent, PoolCounters counters)
    {
        var inst = Instantiate(prefab, parent);
        inst.gameObject.SetActive(false);
        inst.SetPool(this, prefab);
        counters.TotalCreated++;
        //Debug.Log($"[Pool] Created pooled enemy instance of {prefab.name}");
        return inst;
    }

    private PooledVisualInstance CreateVisualInstance(GameObject prefab, Transform parent, PoolCounters counters)
    {
        GameObject inst = Instantiate(prefab, parent);
        PooledVisualInstance pooledVisual = inst.GetComponent<PooledVisualInstance>();
        if (pooledVisual == null)
            pooledVisual = inst.AddComponent<PooledVisualInstance>();

        pooledVisual.SetPool(this, prefab);
        inst.SetActive(false);
        counters.TotalCreated++;
        return pooledVisual;
    }

    // --- Internal: prewarm & helpers ---

    private void PrewarmProjectilePool(SafeRuntimePool<Projectile> pool, int count)
    {
        if (pool == null || count <= 0)
            return;

        var temp = new List<Projectile>(count);
        for (int i = 0; i < count; i++)
            temp.Add(pool.Get());

        for (int i = 0; i < count; i++)
            pool.Release(temp[i]);
    }

    private void PrewarmEnemyPool(SafeRuntimePool<Enemy> pool, int count)
    {
        if (pool == null || count <= 0)
            return;

        _isPrewarmingEnemies = true;

        var temp = new List<Enemy>(count);
        for (int i = 0; i < count; i++)
            temp.Add(pool.Get());

        _isPrewarmingEnemies = false;

        for (int i = 0; i < count; i++)
            pool.Release(temp[i]);
    }

    private void PrewarmVisualPool(SafeRuntimePool<PooledVisualInstance> pool, int count)
    {
        if (pool == null || count <= 0)
            return;

        var temp = new List<PooledVisualInstance>(count);
        for (int i = 0; i < count; i++)
            temp.Add(pool.Get());

        for (int i = 0; i < count; i++)
            pool.Release(temp[i]);
    }

    private Transform ResolveParent(Transform overrideParent, Transform categoryRoot, string categoryName)
    {
        if (overrideParent != null)
            return overrideParent;

        if (categoryRoot == null)
        {
            var go = new GameObject(categoryName);
            go.transform.SetParent(transform, false);
            categoryRoot = go.transform;

            if (categoryName == "ProjectilePools")
                projectileRoot = categoryRoot;
            else if (categoryName == "VisualPools")
                visualRoot = categoryRoot;
            else if (categoryName == "EnemyPools")
                enemyRoot = categoryRoot;
        }

        return categoryRoot;
    }

    private PoolCounters GetOrCreateCounters<TKey>(Dictionary<TKey, PoolCounters> dict, TKey key)
    {
        if (!dict.TryGetValue(key, out var counters))
        {
            counters = new PoolCounters();
            dict[key] = counters;
        }

        return counters;
    }

    private Vector3 ResolveEnemySpawnPosition(Vector3 desiredPosition)
    {
        if (!TryFindNearestWalkableEnemySpawn(desiredPosition, out Vector3 resolvedPosition))
            return desiredPosition;

        return resolvedPosition;
    }

    private bool TryFindNearestWalkableEnemySpawn(Vector3 desiredPosition, out Vector3 resolvedPosition)
    {
        resolvedPosition = desiredPosition;

        TileNavWorld navigationWorld = TileNavWorld.Instance;
        if (navigationWorld == null)
            return false;

        Vector2Int startCell = navigationWorld.WorldToCell(desiredPosition);
        if (navigationWorld.IsWalkableCell(startCell))
            return false;

        int maxTileRadius = enemyWalkableSearchRadiusTiles;
        for (int radius = 1; radius <= maxTileRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                Vector2Int top = startCell + new Vector2Int(x, radius);
                if (navigationWorld.IsWalkableCell(top))
                {
                    resolvedPosition = navigationWorld.CellToWorldCenter(top);
                    return true;
                }

                Vector2Int bottom = startCell + new Vector2Int(x, -radius);
                if (navigationWorld.IsWalkableCell(bottom))
                {
                    resolvedPosition = navigationWorld.CellToWorldCenter(bottom);
                    return true;
                }
            }

            for (int y = -radius + 1; y <= radius - 1; y++)
            {
                Vector2Int right = startCell + new Vector2Int(radius, y);
                if (navigationWorld.IsWalkableCell(right))
                {
                    resolvedPosition = navigationWorld.CellToWorldCenter(right);
                    return true;
                }

                Vector2Int left = startCell + new Vector2Int(-radius, y);
                if (navigationWorld.IsWalkableCell(left))
                {
                    resolvedPosition = navigationWorld.CellToWorldCenter(left);
                    return true;
                }
            }
        }

        return false;
    }

    // --- Data types ---

    private class PoolCounters
    {
        public int ActiveCount;
        public int PeakActive;
        public int TotalCreated;
    }
}

public readonly struct PoolReport
{
    public static readonly PoolReport Empty = new PoolReport(string.Empty, 0, 0, 0, 0);

    public readonly string Key;
    public readonly int Active;
    public readonly int Inactive;
    public readonly int TotalCreated;
    public readonly int PeakActive;

    public PoolReport(string key, int active, int inactive, int totalCreated, int peakActive)
    {
        Key = key;
        Active = active;
        Inactive = inactive;
        TotalCreated = totalCreated;
        PeakActive = peakActive;
    }
}

/// <summary>
/// - Never auto-destroys on Release
/// - Filters out destroyed instances before reuse
/// - You can destroy everything explicitly via Clear() if needed
/// </summary>
public sealed class SafeRuntimePool<T> where T : Component
{
    private readonly Stack<T> _inactive = new Stack<T>();

    private readonly System.Func<T> _factory;
    private readonly System.Action<T> _onGet;
    private readonly System.Action<T> _onRelease;
    private readonly System.Action<T> _onDestroy;

    public int CountInactive => _inactive.Count;

    public SafeRuntimePool(
        System.Func<T> factory,
        System.Action<T> onGet,
        System.Action<T> onRelease,
        System.Action<T> onDestroy)
    {
        _factory = factory;
        _onGet = onGet;
        _onRelease = onRelease;
        _onDestroy = onDestroy;
    }

    public T Get()
    {
        T instance = null;

        while (_inactive.Count > 0 && instance == null)
        {
            var candidate = _inactive.Pop();
            if (candidate)
                instance = candidate;
        }

        if (instance == null)
        {
            instance = _factory();
        }

        _onGet?.Invoke(instance);
        return instance;
    }

    public void Release(T instance)
    {
        if (instance == null)
            return;

        if (!instance)
            return;

        _onRelease?.Invoke(instance);
        _inactive.Push(instance);
    }

    public void Clear()
    {
        foreach (var inst in _inactive)
        {
            if (inst)
            {
                _onDestroy?.Invoke(inst);
            }
        }

        _inactive.Clear();
    }
}
