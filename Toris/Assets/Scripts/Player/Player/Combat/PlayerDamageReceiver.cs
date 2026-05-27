using System;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDamageReceiver : MonoBehaviour, IEnemyAggroTarget
{
    [Header("I-Frames")]
    [SerializeField] private float iFrameDuration = 0.35f;
    [SerializeField] private float hurtFlashTime = 0.12f;

    [Header("Knockback")]
    [SerializeField] private float knockbackMultiplier = 1f;

    [Header("Status")]
    [SerializeField] private PlayerStatusController _statusController;

    [Header("Presentation Feedback")]
    [SerializeField] private DamageNumberEventsSO damageNumberEvents;

    private float _iFrameUntil;
    private float _flashUntil;

    private PlayerStats _stats;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Collider2D _targetCollider;

    private Color _originalColor;
    private bool _flashActive;
    private bool _statusEventsBound;

    public event Action OnHurtReceived;

    public bool IsInvulnerable => Time.time < _iFrameUntil;

    public Transform TargetTransform => transform;
    public Vector2 TargetPosition => _targetCollider != null ? _targetCollider.bounds.center : transform.position;
    public bool IsTargetable =>
        _stats != null
        && !_stats.IsDead
        && gameObject.activeInHierarchy
        && gameObject.scene.IsValid();

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();
        PlayerHurtbox playerHurtbox = GetComponentInChildren<PlayerHurtbox>();
        if (playerHurtbox != null)
            playerHurtbox.TryGetComponent(out _targetCollider);

        if (_targetCollider == null)
            _targetCollider = GetComponentInChildren<Collider2D>();

        if (_statusController == null)
        {
            _statusController = GetComponent<PlayerStatusController>();
        }

        if (_sr != null)
        {
            _originalColor = _sr.color;
        }
    }

    private void OnEnable()
    {
        BindStatusEvents();
    }

    private void OnDisable()
    {
        UnbindStatusEvents();
    }

    private void Update()
    {
        UpdateFlash();
    }

    public void ReceiveHit(in HitData hit)
    {
        if (_stats == null)
            return;

        if (IsInvulnerable && !hit.bypassIFrames)
        {
            RaiseDamageNumber(hit, 0f, DamageNumberFeedbackKind.PostHitGrace);
            return;
        }

        float finalDamage = CalculateFinalDamage(hit.damage);

        _stats.ApplyDamage(finalDamage, DeathCauseSnapshot.FromHit(hit));
        RaiseDamageNumber(hit, finalDamage, DamageNumberFeedbackKind.Damage);
        TryApplyStatus(hit);

        if (_stats.IsDead)
            return;

        if (hit.knockback > 0f && _rb != null)
        {
            _rb.AddForce(hit.direction * hit.knockback * knockbackMultiplier, ForceMode2D.Impulse);
        }

        _iFrameUntil = Time.time + iFrameDuration;
        OnHurtReceived?.Invoke();
        StartFlash();
    }

    public void ReceiveEnemyHit(float amount, HitData hitData)
    {
        hitData.damage = amount;
        ReceiveHit(hitData);
    }

    public void ReportRejectedDirectHit(in HitData hit)
    {
        RaiseDamageNumber(hit, 0f, DamageNumberFeedbackKind.PostHitGrace);
    }

    private float CalculateFinalDamage(float baseDamage)
    {
        const float minDamageMultiplier = 0f;

        float validatedBaseDamage = Mathf.Max(0f, baseDamage);
        float incomingDamageMultiplier = 1f;

        if (_stats != null)
        {
            incomingDamageMultiplier = Mathf.Max(
                minDamageMultiplier,
                _stats.ResolvedEffects.incomingDamageMultiplier);
        }

        return validatedBaseDamage * incomingDamageMultiplier;
    }

    private void RaiseDamageNumber(
        in HitData hit,
        float amount,
        DamageNumberFeedbackKind feedbackKind)
    {
        if (!hit.showDamageNumber || damageNumberEvents == null)
            return;

        damageNumberEvents.RaiseDirectHitResolved(new DamageNumberRequest(
            amount,
            ResolveHitWorldPosition(hit),
            DamageNumberTargetKind.Player,
            feedbackKind));
    }

    private Vector2 ResolveHitWorldPosition(in HitData hit)
    {
        return _targetCollider != null
            ? _targetCollider.ClosestPoint(hit.origin)
            : TargetPosition;
    }

    private void TryApplyStatus(in HitData hit)
    {
        if (_statusController == null || !hit.appliesStatus)
            return;

        _statusController.TryApplyStatus(
            hit.statusType,
            hit.statusDamagePerSecond,
            hit.statusDuration,
            hit.statusTickInterval,
            hit.statusStacks);
    }

    private void BindStatusEvents()
    {
        if (_statusEventsBound || _statusController == null)
            return;

        _statusController.OnStatusApplied += HandleStatusApplied;
        _statusController.OnStatusDamageTick += HandleStatusDamageTick;
        _statusEventsBound = true;
    }

    private void UnbindStatusEvents()
    {
        if (!_statusEventsBound || _statusController == null)
            return;

        _statusController.OnStatusApplied -= HandleStatusApplied;
        _statusController.OnStatusDamageTick -= HandleStatusDamageTick;
        _statusEventsBound = false;
    }

    private void HandleStatusApplied(PlayerStatusEffectType statusType)
    {
        if (damageNumberEvents == null)
            return;

        damageNumberEvents.RaiseStatusEffectApplied(new DamageNumberRequest(
            0f,
            TargetPosition,
            DamageNumberTargetKind.Player,
            ResolveStatusAppliedFeedbackKind(statusType)));
    }

    private void HandleStatusDamageTick(PlayerStatusEffectType statusType, float amount)
    {
        if (amount <= 0f || damageNumberEvents == null)
            return;

        damageNumberEvents.RaiseStatusDamageTickResolved(new DamageNumberRequest(
            amount,
            TargetPosition,
            DamageNumberTargetKind.Player,
            ResolveStatusFeedbackKind(statusType)));
    }

    private static DamageNumberFeedbackKind ResolveStatusAppliedFeedbackKind(PlayerStatusEffectType statusType)
    {
        return statusType switch
        {
            PlayerStatusEffectType.Poison => DamageNumberFeedbackKind.PoisonApplied,
            PlayerStatusEffectType.Burning => DamageNumberFeedbackKind.BurningApplied,
            PlayerStatusEffectType.Bleeding => DamageNumberFeedbackKind.BleedingApplied,
            _ => DamageNumberFeedbackKind.Damage
        };
    }

    private static DamageNumberFeedbackKind ResolveStatusFeedbackKind(PlayerStatusEffectType statusType)
    {
        return statusType switch
        {
            PlayerStatusEffectType.Poison => DamageNumberFeedbackKind.PoisonTick,
            PlayerStatusEffectType.Burning => DamageNumberFeedbackKind.BurningTick,
            PlayerStatusEffectType.Bleeding => DamageNumberFeedbackKind.BleedingTick,
            _ => DamageNumberFeedbackKind.Damage
        };
    }

    private void StartFlash()
    {
        if (_sr == null)
            return;

        _originalColor = _sr.color;
        _sr.color = new Color(_originalColor.r, _originalColor.g * 0.6f, _originalColor.b * 0.6f, 1f);

        _flashUntil = Time.time + hurtFlashTime;
        _flashActive = true;
    }

    private void UpdateFlash()
    {
        if (!_flashActive || _sr == null)
            return;

        if (Time.time < _flashUntil)
            return;

        _sr.color = _originalColor;
        _flashActive = false;
    }
}
