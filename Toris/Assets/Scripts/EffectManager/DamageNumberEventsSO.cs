using System;
using UnityEngine;

public enum DamageNumberTargetKind
{
    Enemy,
    Player
}

public enum DamageNumberFeedbackKind
{
    Damage,
    PostHitGrace,
    Invulnerable,
    Blocked,
    ShotFailed,
    PoisonApplied,
    BurningApplied,
    BleedingApplied,
    PoisonTick,
    BurningTick,
    BleedingTick
}

[Serializable]
public struct DamageNumberRequest
{
    public float Amount;
    public Vector3 WorldPosition;
    public DamageNumberTargetKind TargetKind;
    public DamageNumberFeedbackKind FeedbackKind;

    public DamageNumberRequest(
        float amount,
        Vector3 worldPosition,
        DamageNumberTargetKind targetKind,
        DamageNumberFeedbackKind feedbackKind)
    {
        Amount = Mathf.Max(0f, amount);
        WorldPosition = worldPosition;
        TargetKind = targetKind;
        FeedbackKind = feedbackKind;
    }
}

[CreateAssetMenu(menuName = "Effects/Damage Numbers/Event Channel", fileName = "DamageNumberEvents")]
public sealed class DamageNumberEventsSO : ScriptableObject
{
    public event Action<DamageNumberRequest> DirectHitResolved;
    public event Action<DamageNumberRequest> StatusEffectApplied;
    public event Action<DamageNumberRequest> StatusDamageTickResolved;

    public void RaiseDirectHitResolved(in DamageNumberRequest request)
    {
        DirectHitResolved?.Invoke(request);
    }

    public void RaiseStatusEffectApplied(in DamageNumberRequest request)
    {
        StatusEffectApplied?.Invoke(request);
    }

    public void RaiseStatusDamageTickResolved(in DamageNumberRequest request)
    {
        StatusDamageTickResolved?.Invoke(request);
    }
}
