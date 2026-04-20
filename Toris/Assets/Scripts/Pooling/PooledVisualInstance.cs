using System.Collections.Generic;
using UnityEngine;

public sealed class PooledVisualInstance : MonoBehaviour
{
    private IVisualPool _pool;
    private GameObject _originalPrefab;
    private IPoolable[] _poolables = System.Array.Empty<IPoolable>();

    public GameObject OriginalPrefab => _originalPrefab;

    public void SetPool(IVisualPool pool, GameObject prefabRef)
    {
        _pool = pool;
        _originalPrefab = prefabRef;
        CachePoolables();
    }

    public void NotifySpawned()
    {
        CachePoolables();
        for (int i = 0; i < _poolables.Length; i++)
            _poolables[i]?.OnSpawned();
    }

    public void NotifyDespawned()
    {
        CachePoolables();
        for (int i = 0; i < _poolables.Length; i++)
            _poolables[i]?.OnDespawned();
    }

    public void Despawn()
    {
        if (_pool != null)
            _pool.Release(this);
        else
            gameObject.SetActive(false);
    }

    private void CachePoolables()
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        List<IPoolable> poolables = new List<IPoolable>(behaviours.Length);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPoolable poolable)
                poolables.Add(poolable);
        }

        _poolables = poolables.ToArray();
    }
}
