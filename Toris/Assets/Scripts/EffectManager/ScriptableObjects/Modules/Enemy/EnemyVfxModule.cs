using UnityEngine;

public abstract class EnemyVfxModule : ScriptableObject
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public virtual void Initialize(in EnemyVfxContext ctx) { }
    public virtual void Dispose(in EnemyVfxContext ctx) { }
    public virtual void OnDamaged(in EnemyVfxContext ctx, float damage) { }
    public virtual void OnDied(in EnemyVfxContext ctx, Enemy enemy) { }
    public virtual void OnDespawned(in EnemyVfxContext ctx, Enemy enemy) { }

    protected static Quaternion RotationFromDirection2D(Vector2 direction, Quaternion fallback)
    {
        if (direction.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
            return fallback;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }

    protected static Quaternion ToLocalRotation(Transform parent, Quaternion worldRotation)
    {
        return parent != null
            ? Quaternion.Inverse(parent.rotation) * worldRotation
            : worldRotation;
    }
}

public readonly struct EnemyVfxContext
{
    public readonly EnemyVfx Hub;
    public readonly Transform Transform;
    public readonly Enemy Enemy;

    public EnemyVfxContext(
        EnemyVfx hub,
        Transform transform,
        Enemy enemy)
    {
        Hub = hub;
        Transform = transform;
        Enemy = enemy;
    }

    public bool HasEffects =>
        EffectManagerBehavior.Instance != null &&
        EffectManagerBehavior.Instance != NullEffectManager.Instance;
}
