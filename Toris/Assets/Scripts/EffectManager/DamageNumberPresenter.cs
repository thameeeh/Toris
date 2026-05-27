using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageNumberPresenter : MonoBehaviour
{
    private const string DefaultDamageNumberEffectId = "damage_number";
    private const string DamageVariantId = "damage";
    private const string InvulnerableVariantId = "invulnerable";
    private const string BlockedVariantId = "blocked";

    [Header("Events")]
    [SerializeField] private DamageNumberEventsSO damageNumberEvents;

    [Header("Effect")]
    [SerializeField] private string damageNumberEffectId = DefaultDamageNumberEffectId;
    [SerializeField] private Color outgoingDamageColor = new Color(0.94f, 0.94f, 0.9f, 1f);
    [SerializeField] private Color incomingDamageColor = new Color(0.94f, 0.2f, 0.18f, 1f);

    private bool _eventsBound;

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
        if (_eventsBound || damageNumberEvents == null)
            return;

        damageNumberEvents.DirectHitResolved += HandleDirectHitResolved;
        _eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (!_eventsBound || damageNumberEvents == null)
            return;

        damageNumberEvents.DirectHitResolved -= HandleDirectHitResolved;
        _eventsBound = false;
    }

    private void HandleDirectHitResolved(DamageNumberRequest request)
    {
        if (string.IsNullOrWhiteSpace(damageNumberEffectId))
            return;

        Color displayColor = request.TargetKind == DamageNumberTargetKind.Player
            ? incomingDamageColor
            : outgoingDamageColor;

        EffectManagerBehavior.Instance.Play(new EffectRequest
        {
            EffectId = damageNumberEffectId,
            Position = request.WorldPosition,
            Rotation = Quaternion.identity,
            Parent = null,
            Variant = new EffectVariant
            {
                VariantId = ResolveVariantId(request.FeedbackKind),
                ColorOverride = displayColor
            },
            Magnitude = request.Amount
        });
    }

    private static string ResolveVariantId(DamageNumberFeedbackKind feedbackKind)
    {
        return feedbackKind switch
        {
            DamageNumberFeedbackKind.Invulnerable => InvulnerableVariantId,
            DamageNumberFeedbackKind.Blocked => BlockedVariantId,
            _ => DamageVariantId
        };
    }
}
