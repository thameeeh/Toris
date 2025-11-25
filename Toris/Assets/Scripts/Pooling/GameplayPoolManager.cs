using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class GameplayPoolManager : MonoBehaviour, IProjectilePool
{
    public static GameplayPoolManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private GameplayPoolConfiguration configuration;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Parents (optional)")]
    [SerializeField] private Transform projectileRoot;
    [SerializeField] private Transform enemyRoot;

    [Header("Fallback sizing (used when prefab is not configured)")]
    [SerializeField] private int defaultProjectilePrewarm = 0;
    [SerializeField] private int defaultProjectileCapacity = 16;
    [SerializeField] private int defaultProjectileMaxSize = 128;
    [SerializeField] private int defaultEnemyPrewarm = 0;
    [SerializeField] private int defaultEnemyCapacity = 8;
    [SerializeField] private int defaultEnemyMaxSize = 32;

    private readonly Dictionary<ArrowProjectile, ObjectPool<ArrowProjectile>> projectilePools = new();
    private readonly Dictionary<ArrowProjectile, PoolCounters> projectileCounters = new();
    private readonly Dictionary<Enemy, ObjectPool<Enemy>> enemyPools = new();
    private readonly Dictionary<Enemy, PoolCounters> enemyCounters = new();
    private readonly Dictionary<ArrowProjectile, ProjectilePoolSettings> projectileSettings = new();
    private readonly Dictionary<Enemy, EnemyPoolSettings> enemySettings = new();

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
        enemyPools.Clear();
        enemyCounters.Clear();
        enemySettings.Clear();

        if (configuration != null)
        {
            foreach (var cfg in configuration.ProjectilePools)
            {
                if (cfg?.prefab == null)
                    continue;

                projectileSettings[cfg.prefab] = cfg;
                var pool = EnsureProjectilePool(cfg.prefab, cfg);
                PrewarmProjectilePool(pool, cfg.prewarmCount);
            }

            foreach (var cfg in configuration.EnemyPools)
            {
                if (cfg?.prefab == null)
                    continue;

                enemySettings[cfg.prefab] = cfg;
                var pool = EnsureEnemyPool(cfg.prefab, cfg);
                PrewarmEnemyPool(pool, cfg.prewarmCount);
            }
        }
    }

    // --- Projectile API ---

    public ArrowProjectile SpawnProjectile(ArrowProjectile prefab, Vector3 position, Quaternion rotation)
    {
        var pool = EnsureProjectilePool(prefab);
        if (pool == null)
            return null;

        var proj = pool.Get();
        proj.transform.SetPositionAndRotation(position, rotation);
        return proj;
    }
    public void Release(Projectile instance)
    {
        if (instance == null)
            return;

        // Our projectile pools manage ArrowProjectile instances
        if (instance is ArrowProjectile arrow)
        {
            Release(arrow);
            return;
        }

        // Fallback: unsupported projectile type
        Debug.LogWarning(
            $"GameplayPoolManager.Release received unsupported projectile type {instance.GetType().Name}, destroying.");
        Destroy(instance.gameObject);
    }

    public PoolReport GetProjectileReport(ArrowProjectile prefab)
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
        {
            yield return GetProjectileReport(kvp.Key);
        }
    }

    // --- Enemy API ---

    public Enemy SpawnEnemy(Enemy prefab, Vector3 position, Quaternion rotation)
    {
        var pool = EnsureEnemyPool(prefab);
        if (pool == null)
            return null;

        var enemy = pool.Get();
        enemy.transform.SetPositionAndRotation(position, rotation);
        return enemy;
    }

    public void Release(Enemy instance)
    {
        if (instance == null)
            return;

        var key = instance;

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

    private ObjectPool<ArrowProjectile> EnsureProjectilePool(ArrowProjectile prefab, ProjectilePoolSettings settings = null)
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

    private ObjectPool<Enemy> EnsureEnemyPool(Enemy prefab, EnemyPoolSettings settings = null)
    {
        if (prefab == null)
            return null;

        if (!enemyPools.TryGetValue(prefab, out var pool))
        {
            var effectiveSettings = settings ?? TryGetEnemySettings(prefab) ?? CreateDefaultEnemySettings(prefab);
            pool = CreateEnemyPool(prefab, effectiveSettings);
            enemyPools[prefab] = pool;

            if (settings == null)
            {
                PrewarmEnemyPool(pool, effectiveSettings.prewarmCount);
            }
        }

        return pool;
    }

    private ProjectilePoolSettings TryGetProjectileSettings(ArrowProjectile prefab)
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

    private ProjectilePoolSettings CreateDefaultProjectileSettings(ArrowProjectile prefab)
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

    private ObjectPool<ArrowProjectile> CreateProjectilePool(ArrowProjectile prefab, ProjectilePoolSettings settings)
    {
        var counters = GetOrCreateCounters(projectileCounters, prefab);
        var parent = ResolveParent(settings?.parentOverride, projectileRoot, "ProjectilePools");

        return new ObjectPool<ArrowProjectile>(
            () => CreateProjectileInstance(prefab, parent, counters),
            obj =>
            {
                counters.ActiveCount++;
                counters.PeakActive = Mathf.Max(counters.PeakActive, counters.ActiveCount);
                obj.gameObject.SetActive(true);
                obj.OnSpawned();
            },
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
            },
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, settings?.defaultCapacity ?? defaultProjectileCapacity),
            maxSize: Mathf.Max(1, settings?.maxSize ?? defaultProjectileMaxSize)
        );
    }

    private ObjectPool<Enemy> CreateEnemyPool(Enemy prefab, EnemyPoolSettings settings)
    {
        var counters = GetOrCreateCounters(enemyCounters, prefab);
        var parent = ResolveParent(settings?.parentOverride, enemyRoot, "EnemyPools");

        return new ObjectPool<Enemy>(
            () => CreateEnemyInstance(prefab, parent, counters),
            obj =>
            {
                counters.ActiveCount++;
                counters.PeakActive = Mathf.Max(counters.PeakActive, counters.ActiveCount);
                obj.gameObject.SetActive(true);
            },
            obj =>
            {
                counters.ActiveCount = Mathf.Max(0, counters.ActiveCount - 1);
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(parent, false);
            },
            obj =>
            {
                if (obj)
                {
                    Destroy(obj.gameObject);
                }
            },
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, settings?.defaultCapacity ?? defaultEnemyCapacity),
            maxSize: Mathf.Max(1, settings?.maxSize ?? defaultEnemyMaxSize)
        );
    }

    private ArrowProjectile CreateProjectileInstance(ArrowProjectile prefab, Transform parent, PoolCounters counters)
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
        counters.TotalCreated++;
        return inst;
    }

    // --- Internal: prewarm & helpers ---

    private void PrewarmProjectilePool(ObjectPool<ArrowProjectile> pool, int count)
    {
        if (pool == null || count <= 0)
            return;

        var temp = new List<ArrowProjectile>(count);
        for (int i = 0; i < count; i++) temp.Add(pool.Get());
        for (int i = 0; i < count; i++) pool.Release(temp[i]);
    }

    private void PrewarmEnemyPool(ObjectPool<Enemy> pool, int count)
    {
        if (pool == null || count <= 0)
            return;

        var temp = new List<Enemy>(count);
        for (int i = 0; i < count; i++) temp.Add(pool.Get());
        for (int i = 0; i < count; i++) pool.Release(temp[i]);
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