using UnityEngine;
public abstract class Projectile : MonoBehaviour, IPoolable
{
    private IProjectilePool pool;
    private Projectile originalPrefab;

    public IProjectilePool Pool => pool;

    public Projectile OriginalPrefab => originalPrefab;

    public virtual void SetPool(IProjectilePool registry, Projectile prefabRef)
    {
        pool = registry;
        originalPrefab = prefabRef;
    }

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
