using System;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDamageReceiver : MonoBehaviour
{
    [Header("I-Frames")]
    [SerializeField] private float iFrameDuration = 0.35f;
    [SerializeField] private float hurtFlashTime = 0.12f;

    [Header("Knockback")]
    [SerializeField] private float knockbackMultiplier = 1f;

    [Header("Status")]
    [SerializeField] private PlayerStatusController _statusController;

    private float _iFrameUntil;
    private float _flashUntil;

    private PlayerStats _stats;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;

    private Color _originalColor;
    private bool _flashActive;

    public event Action OnHurtReceived;

    public bool IsInvulnerable => Time.time < _iFrameUntil;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();

        if (_statusController == null)
        {
            _statusController = GetComponent<PlayerStatusController>();
        }

        if (_sr != null)
        {
            _originalColor = _sr.color;
        }
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
            return;

        float finalDamage = CalculateFinalDamage(hit.damage);

        _stats.ApplyDamage(finalDamage);
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
