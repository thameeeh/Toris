using System.Collections.Generic;
using UnityEngine;

/// Pools world POIs (dens, gates, etc) under a clean WorldSpawns root.
/// Uses SafeRuntimePool<T> from GameplayPoolManager.cs. :contentReference[oaicite:2]{index=2}
public sealed class WorldPoiPoolManager : MonoBehaviour
{
    [SerializeField] private Transform worldSpawnsRoot;

    private Transform activeRoot;
    private Transform pooledRoot;

    // pool key: prefab GameObject
    private readonly Dictionary<GameObject, SafeRuntimePool<PooledPoiIdentity>> pools = new();
    // instance -> prefab key
    private readonly Dictionary<PooledPoiIdentity, GameObject> prefabByInstance = new();

    private void Awake()
    {
        EnsureRoots();
    }

    private void EnsureRoots()
    {
        if (worldSpawnsRoot == null)
        {
            var ws = new GameObject("WorldSpawns");
            ws.transform.SetParent(transform, false);
            worldSpawnsRoot = ws.transform;
        }

        if (activeRoot == null)
        {
            var go = new GameObject("Active");
            go.transform.SetParent(worldSpawnsRoot, false);
            activeRoot = go.transform;
        }

        if (pooledRoot == null)
        {
            var go = new GameObject("Pooled");
            go.transform.SetParent(worldSpawnsRoot, false);
            pooledRoot = go.transform;
        }
    }

    public Transform GetActiveRoot() => activeRoot;

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null) return null;
        EnsureRoots();

        var pool = EnsurePool(prefab);
        var id = pool.Get();
        if (!id) return null;

        var go = id.gameObject;

        if (go.activeSelf)
            go.SetActive(false);

        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);

        InvokeOnSpawned(go);

        return go;
    }


    public void Release(GameObject instance)
    {
        if (instance == null) return;

        var id = instance.GetComponent<PooledPoiIdentity>();
        if (id == null)
        {
            // Not pooled (fallback)
            instance.SetActive(false);
            instance.transform.SetParent(pooledRoot, false);
            return;
        }

        if (!prefabByInstance.TryGetValue(id, out var prefab) || prefab == null)
        {
            // Unknown key (fallback)
            InvokeOnDespawned(instance);
            instance.SetActive(false);
            instance.transform.SetParent(pooledRoot, false);
            return;
        }

        if (pools.TryGetValue(prefab, out var pool))
        {
            pool.Release(id);
        }
        else
        {
            // Shouldn't happen, but safe fallback
            InvokeOnDespawned(instance);
            instance.SetActive(false);
            instance.transform.SetParent(pooledRoot, false);
        }
    }

    // --- Internals ---

    private SafeRuntimePool<PooledPoiIdentity> EnsurePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var existing))
            return existing;

        Transform prefabPooledParent = GetOrCreatePooledBucket(prefab.name);

        var pool = new SafeRuntimePool<PooledPoiIdentity>(
            // Factory
            () =>
            {
                var go = Instantiate(prefab, prefabPooledParent);
                go.SetActive(false);

                var id = go.GetComponent<PooledPoiIdentity>();
                if (id == null) id = go.AddComponent<PooledPoiIdentity>();

                prefabByInstance[id] = prefab;
                return id;
            },
            // OnGet
            id =>
            {
                //if (!id) return;
                //id.gameObject.SetActive(true);
            },
            // OnRelease
            id =>
            {
                if (!id) return;

                var go = id.gameObject;
                InvokeOnDespawned(go);

                go.SetActive(false);
                go.transform.SetParent(prefabPooledParent, false);
            },
            // OnDestroy
            id =>
            {
                if (id) Destroy(id.gameObject);
            }
        );

        pools[prefab] = pool;
        return pool;
    }

    private Transform GetOrCreatePooledBucket(string name)
    {
        var t = pooledRoot.Find(name);
        if (t != null) return t;

        var go = new GameObject(name);
        go.transform.SetParent(pooledRoot, false);
        return go.transform;
    }

    private static void InvokeOnSpawned(GameObject go)
    {
        var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPoolable p)
                p.OnSpawned();
        }
    }

    private static void InvokeOnDespawned(GameObject go)
    {
        var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPoolable p)
                p.OnDespawned();
        }
    }
}

/// Marker that makes a POI instance pool-able.
public sealed class PooledPoiIdentity : MonoBehaviour { }
