using UnityEngine;

public abstract class Projectile : MonoBehaviour, IPoolable
{
    private IProjectilePool pool;
    private Projectile originalPrefab;

    /// <summary>Pool that owns this projectile instance.</summary>
    public IProjectilePool Pool => pool;

    /// <summary>Prefab used as the pool key for this instance.</summary>
    public Projectile OriginalPrefab => originalPrefab;

    /// <summary>Assigned by the pool when the instance is created.</summary>
    public virtual void SetPool(IProjectilePool registry, Projectile prefabRef)
    {
        pool = registry;
        originalPrefab = prefabRef;
    }

    /// <summary>Return to the owning pool or disable the object when unpooled.</summary>
    public virtual void Despawn()
    {
        if (pool != null)
            pool.Release(this);
        else
            gameObject.SetActive(false);
    }

    public abstract void OnSpawned();

    public abstract void OnDespawned();
}
