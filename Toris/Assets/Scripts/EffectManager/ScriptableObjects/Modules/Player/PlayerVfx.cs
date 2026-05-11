using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerVfx : MonoBehaviour
{
    [Header("Rules (ScriptableObjects)")]
    [SerializeField] private PlayerVfxRuleSO[] rules;

    [Header("Legacy Modules")]
    [SerializeField] private PlayerVfxModule[] legacyModules;

    private readonly Dictionary<string, EffectHandle> persistentHandles = new();
    private readonly Dictionary<PlayerVfxRuleSO, float> ruleCooldownTimes = new();

    private PlayerVfxEventContext latestContext;
    private bool hasLatestContext;

    private void OnDisable()
    {
        ReleaseAllPersistentEffects();
        ruleCooldownTimes.Clear();
        hasLatestContext = false;
    }

    private void Update()
    {
        if (legacyModules == null || !hasLatestContext)
            return;

        PlayerVfxContext legacyContext = CreateLegacyContext(latestContext);
        float unscaledDeltaTime = Time.unscaledDeltaTime;
        for (int i = 0; i < legacyModules.Length; i++)
        {
            legacyModules[i]?.Tick(legacyContext, unscaledDeltaTime);
        }
    }

    public void InitializeRuntime(in PlayerVfxEventContext context)
    {
        latestContext = context;
        hasLatestContext = true;

        if (legacyModules == null)
            return;

        PlayerVfxContext legacyContext = CreateLegacyContext(context);
        for (int i = 0; i < legacyModules.Length; i++)
        {
            legacyModules[i]?.Initialize(legacyContext);
        }
    }

    public void DisposeRuntime(in PlayerVfxEventContext context)
    {
        if (legacyModules != null)
        {
            PlayerVfxContext legacyContext = CreateLegacyContext(context);
            for (int i = 0; i < legacyModules.Length; i++)
            {
                legacyModules[i]?.Dispose(legacyContext);
            }
        }

        ReleaseAllPersistentEffects();
        ruleCooldownTimes.Clear();
        hasLatestContext = false;
    }

    public void HandleEvent(in PlayerVfxEventContext context)
    {
        latestContext = context;
        hasLatestContext = true;

        if (rules != null)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                rules[i]?.Evaluate(context);
            }
        }

        DispatchLegacyModules(context);
    }

    public bool TryUseRuleCooldown(PlayerVfxRuleSO rule, float cooldownSeconds)
    {
        if (rule == null)
            return false;

        if (cooldownSeconds <= 0f)
            return true;

        float now = Time.time;
        if (ruleCooldownTimes.TryGetValue(rule, out float lastPlayedTime) &&
            now - lastPlayedTime < cooldownSeconds)
        {
            return false;
        }

        ruleCooldownTimes[rule] = now;
        return true;
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

    private void DispatchLegacyModules(in PlayerVfxEventContext eventContext)
    {
        if (legacyModules == null)
            return;

        PlayerVfxContext legacyContext = CreateLegacyContext(eventContext);
        for (int i = 0; i < legacyModules.Length; i++)
        {
            PlayerVfxModule module = legacyModules[i];
            if (module == null)
                continue;

            switch (eventContext.EventType)
            {
                case PlayerVfxEventType.BowDrawStarted:
                    module.OnBowDrawStarted(legacyContext);
                    break;
                case PlayerVfxEventType.BowShootReady:
                    module.OnBowShootReady(legacyContext);
                    break;
                case PlayerVfxEventType.BowShotReleased:
                    module.OnBowShotReleased(legacyContext);
                    break;
                case PlayerVfxEventType.BowShotFired:
                    module.OnBowShotFired(legacyContext);
                    break;
                case PlayerVfxEventType.BowDryReleased:
                    module.OnBowDryReleased(legacyContext);
                    break;
                case PlayerVfxEventType.DashStarted:
                    module.OnDashStarted(legacyContext, eventContext.Direction);
                    break;
                case PlayerVfxEventType.DashCompleted:
                    module.OnDashCompleted(legacyContext);
                    break;
            }
        }
    }

    private static PlayerVfxContext CreateLegacyContext(in PlayerVfxEventContext context)
    {
        return new PlayerVfxContext(
            hub: context.Hub,
            transform: context.Transform,
            bow: context.Bow,
            dash: context.Dash,
            motor: context.Motor,
            rb: context.Rb,
            facing: context.Facing);
    }
}
