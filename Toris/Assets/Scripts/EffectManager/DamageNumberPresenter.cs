using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageNumberPresenter : MonoBehaviour
{
    private const string DefaultDamageNumberEffectId = "damage_number";
    private const string DamageVariantId = "damage";
    private const string InvulnerableVariantId = "invulnerable";
    private const string BlockedVariantId = "blocked";
    private const string ShotFailedVariantId = "shot_failed";
    private const string PoisonAppliedVariantId = "poison_applied";
    private const string BurningAppliedVariantId = "burning_applied";
    private const string BleedingAppliedVariantId = "bleeding_applied";

    [Header("Events")]
    [SerializeField] private DamageNumberEventsSO damageNumberEvents;
    [SerializeField] private PlayerBowEventsSO bowEvents;

    [Header("Effect")]
    [SerializeField] private string damageNumberEffectId = DefaultDamageNumberEffectId;
    [SerializeField] private Color outgoingDamageColor = new Color(0.94f, 0.94f, 0.9f, 1f);
    [SerializeField] private Color incomingDamageColor = new Color(0.94f, 0.2f, 0.18f, 1f);
    [SerializeField] private Color poisonDamageColor = new Color(0.3f, 0.86f, 0.3f, 1f);
    [SerializeField] private Color burningDamageColor = new Color(1f, 0.45f, 0.12f, 1f);

    private bool _damageEventsBound;
    private bool _bowEventsBound;

    private void OnEnable()
    {
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (!_damageEventsBound && damageNumberEvents != null)
        {
            damageNumberEvents.DirectHitResolved += HandleDirectHitResolved;
            damageNumberEvents.StatusEffectApplied += HandleStatusEffectApplied;
            damageNumberEvents.StatusDamageTickResolved += HandleStatusDamageTickResolved;
            _damageEventsBound = true;
        }

        if (!_bowEventsBound && bowEvents != null)
        {
            bowEvents.UnderdrawReleased += HandleUnderdrawReleased;
            _bowEventsBound = true;
        }
    }

    private void UnbindEvents()
    {
        if (_damageEventsBound && damageNumberEvents != null)
        {
            damageNumberEvents.DirectHitResolved -= HandleDirectHitResolved;
            damageNumberEvents.StatusEffectApplied -= HandleStatusEffectApplied;
            damageNumberEvents.StatusDamageTickResolved -= HandleStatusDamageTickResolved;
            _damageEventsBound = false;
        }

        if (_bowEventsBound && bowEvents != null)
        {
            bowEvents.UnderdrawReleased -= HandleUnderdrawReleased;
            _bowEventsBound = false;
        }
    }

    private void HandleDirectHitResolved(DamageNumberRequest request)
    {
        // Settings hook: visual feedback may be hidden without changing resolved combat events.
        if (!CanShowFeedback())
            return;

        Color displayColor = request.TargetKind == DamageNumberTargetKind.Player
            ? incomingDamageColor
            : outgoingDamageColor;

        PlayPopup(
            request.WorldPosition,
            ResolveVariantId(request.FeedbackKind),
            request.Amount,
            displayColor);
    }

    private void HandleStatusEffectApplied(DamageNumberRequest request)
    {
        if (!CanShowFeedback())
            return;

        PlayPopup(
            request.WorldPosition,
            ResolveVariantId(request.FeedbackKind),
            request.Amount,
            ResolveStatusColor(request.FeedbackKind));
    }

    private void HandleStatusDamageTickResolved(DamageNumberRequest request)
    {
        if (!CanShowFeedback())
            return;

        PlayPopup(
            request.WorldPosition,
            DamageVariantId,
            request.Amount,
            ResolveStatusColor(request.FeedbackKind));
    }

    private void HandleUnderdrawReleased(PlayerBowController source)
    {
        if (source == null || !CanShowFeedback())
            return;

        PlayPopup(
            source.transform.position,
            ResolveVariantId(DamageNumberFeedbackKind.ShotFailed),
            0f,
            outgoingDamageColor);
    }

    private bool CanShowFeedback()
    {
        return DamageNumberSettings.ShowDamageNumbers
            && !string.IsNullOrWhiteSpace(damageNumberEffectId);
    }

    private void PlayPopup(
        Vector3 worldPosition,
        string variantId,
        float magnitude,
        Color displayColor)
    {
        EffectManagerBehavior.Instance.Play(new EffectRequest
        {
            EffectId = damageNumberEffectId,
            Position = worldPosition,
            Rotation = Quaternion.identity,
            Parent = null,
            Variant = new EffectVariant
            {
                VariantId = variantId,
                ColorOverride = displayColor
            },
            Magnitude = magnitude
        });
    }

    private Color ResolveStatusColor(DamageNumberFeedbackKind feedbackKind)
    {
        return feedbackKind switch
        {
            DamageNumberFeedbackKind.PoisonApplied => poisonDamageColor,
            DamageNumberFeedbackKind.PoisonTick => poisonDamageColor,
            DamageNumberFeedbackKind.BurningApplied => burningDamageColor,
            DamageNumberFeedbackKind.BurningTick => burningDamageColor,
            DamageNumberFeedbackKind.BleedingApplied => incomingDamageColor,
            DamageNumberFeedbackKind.BleedingTick => incomingDamageColor,
            _ => incomingDamageColor
        };
    }

    private static string ResolveVariantId(DamageNumberFeedbackKind feedbackKind)
    {
        return feedbackKind switch
        {
            DamageNumberFeedbackKind.PostHitGrace => DamageVariantId,
            DamageNumberFeedbackKind.Invulnerable => InvulnerableVariantId,
            DamageNumberFeedbackKind.Blocked => BlockedVariantId,
            DamageNumberFeedbackKind.ShotFailed => ShotFailedVariantId,
            DamageNumberFeedbackKind.PoisonApplied => PoisonAppliedVariantId,
            DamageNumberFeedbackKind.BurningApplied => BurningAppliedVariantId,
            DamageNumberFeedbackKind.BleedingApplied => BleedingAppliedVariantId,
            _ => DamageVariantId
        };
    }
}
