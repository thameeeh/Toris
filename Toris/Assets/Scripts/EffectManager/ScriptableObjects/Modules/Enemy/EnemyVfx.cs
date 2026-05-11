using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyVfx : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private EnemyVfxModule[] modules;

    private readonly Dictionary<string, EffectHandle> persistentHandles = new();

    private EnemyVfxContext ctx;

    private void Awake()
    {
        if (enemy == null)
            TryGetComponent(out enemy);

        ctx = new EnemyVfxContext(
            hub: this,
            transform: transform,
            enemy: enemy);
    }

    private void Reset()
    {
        if (enemy == null)
            TryGetComponent(out enemy);
    }

    private void OnEnable()
    {
        if (enemy != null)
        {
            enemy.Damaged += OnDamaged;
            enemy.Died += OnDied;
            enemy.Despawned += OnDespawned;
        }

        if (modules == null)
            return;

        for (int i = 0; i < modules.Length; i++)
        {
            modules[i]?.Initialize(ctx);
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.Damaged -= OnDamaged;
            enemy.Died -= OnDied;
            enemy.Despawned -= OnDespawned;
        }

        if (modules != null)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                modules[i]?.Dispose(ctx);
            }
        }

        ReleaseAllPersistentEffects();
    }

    public void PlayOneShot(
        string effectId,
        Vector3 worldPosition,
        Quaternion rotation,
        EffectVariant variant,
        float magnitude)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return;

        EffectManagerBehavior.Instance.Play(new EffectRequest
        {
            EffectId = effectId,
            Position = worldPosition,
            Rotation = rotation,
            Parent = null,
            Variant = variant,
            Magnitude = Mathf.Max(0f, magnitude)
        });
    }

    public void PlayAttachedOneShot(
        string effectId,
        Vector3 localPosition,
        Quaternion localRotation,
        EffectVariant variant,
        float magnitude)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return;

        EffectManagerBehavior.Instance.PlayAttached(new AttachedEffectRequest
        {
            EffectId = effectId,
            Anchor = transform,
            LocalPosition = localPosition,
            LocalRotation = localRotation,
            Variant = variant,
            Magnitude = Mathf.Max(0f, magnitude)
        });
    }

    public EffectHandle StartPersistentEffect(
        string key,
        string effectId,
        Vector3 localPosition,
        Quaternion localRotation,
        EffectVariant variant,
        float magnitude)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(effectId))
            return EffectHandle.Invalid;

        if (persistentHandles.TryGetValue(key, out EffectHandle existingHandle))
        {
            if (existingHandle.IsValid)
                return existingHandle;

            persistentHandles.Remove(key);
        }

        EffectHandle handle = EffectManagerBehavior.Instance.PlayPersistent(new PersistentEffectRequest
        {
            EffectId = effectId,
            Anchor = transform,
            LocalPosition = localPosition,
            LocalRotation = localRotation,
            Variant = variant,
            Magnitude = Mathf.Max(0f, magnitude)
        });

        if (handle.IsValid)
        {
            persistentHandles.Add(key, handle);
        }

        return handle;
    }

    public void ReleasePersistentEffect(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!persistentHandles.TryGetValue(key, out EffectHandle handle))
            return;

        persistentHandles.Remove(key);

        if (handle.IsValid)
        {
            EffectManagerBehavior.Instance.Release(handle);
        }
    }

    private void ReleaseAllPersistentEffects()
    {
        foreach (EffectHandle handle in persistentHandles.Values)
        {
            if (handle.IsValid)
            {
                EffectManagerBehavior.Instance.Release(handle);
            }
        }

        persistentHandles.Clear();
    }

    private void OnDamaged(float damage)
    {
        if (modules == null) return;
        for (int i = 0; i < modules.Length; i++) modules[i]?.OnDamaged(ctx, damage);
    }

    private void OnDied(Enemy deadEnemy)
    {
        if (modules == null) return;
        for (int i = 0; i < modules.Length; i++) modules[i]?.OnDied(ctx, deadEnemy);
    }

    private void OnDespawned(Enemy despawnedEnemy)
    {
        if (modules == null) return;
        for (int i = 0; i < modules.Length; i++) modules[i]?.OnDespawned(ctx, despawnedEnemy);
        ReleaseAllPersistentEffects();
    }
}
