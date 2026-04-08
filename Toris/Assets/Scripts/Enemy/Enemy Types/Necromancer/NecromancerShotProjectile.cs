using UnityEngine;

public class NecromancerShotProjectile : Projectile
{
    private const float MinDirectionSqr = 0.0001f;

    [Header("Visual")]
    [SerializeField] private bool rotateTowardVelocity = true;
    [SerializeField] private float rotateOffsetDegrees = 0f;

    [Header("Hit Behavior")]
    [SerializeField] private bool despawnOnFirstImpact = true;

    [Header("Sustain Contact Damage")]
    [SerializeField] private bool enableSustainContactDamage = false;
    [SerializeField, Min(0.05f)] private float sustainDamageInterval = 0.4f;
    [SerializeField, Min(0f)] private float sustainDamageMultiplier = 0.35f;
    [SerializeField, Min(0f)] private float sustainKnockbackMultiplier = 0f;
    [SerializeField] private bool sustainDamageBypassesIFrames = false;

    [Header("Speed Profile")]
    [SerializeField] private bool useBurstDecaySpeedProfile = true;
    [SerializeField, Min(1f)] private float launchSpeedMultiplier = 3f;
    [SerializeField, Min(0f)] private float burstTravelDistance = 1.5f;
    [SerializeField, Min(0f)] private float exponentialDecayRate = 1.5f;
    [SerializeField, Min(0f)] private float minimumSpeedMultiplier = 0.15f;

    private Rigidbody2D _rb;
    private Collider2D _hitCollider;
    private Animator _animator;

    private Collider2D[] _ignoredOwnerColliders;
    private float _damage;
    private float _knockback;
    private float _despawnAtTime = float.PositiveInfinity;
    private float _baseSpeed;
    private Vector2 _lastTravelDirection = Vector2.right;
    private Vector2 _spawnPosition;
    private bool _pendingDespawn;
    private float _nextAllowedSustainDamageTime = float.PositiveInfinity;

    private void Awake()
    {
        TryGetComponent(out _rb);
        TryGetComponent(out _hitCollider);
        TryGetComponent(out _animator);

        if (_rb != null)
        {
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void LateUpdate()
    {
        if (rotateTowardVelocity)
            RotateTowardVelocity();

        if (_pendingDespawn || Time.time >= _despawnAtTime)
            Despawn();
    }

    private void FixedUpdate()
    {
        UpdateTravelVelocity();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplySustainDamage(other);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplySustainDamage(collision.collider);
    }

    public override void OnSpawned()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }

        if (_hitCollider != null)
            _hitCollider.enabled = true;

        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }

        _ignoredOwnerColliders = null;
        _damage = 0f;
        _knockback = 0f;
        _baseSpeed = 0f;
        _despawnAtTime = float.PositiveInfinity;
        _lastTravelDirection = Vector2.right;
        _spawnPosition = transform.position;
        _pendingDespawn = false;
        _nextAllowedSustainDamageTime = float.PositiveInfinity;
    }

    public override void OnDespawned()
    {
        SetOwnerIgnore(false);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }

        if (_hitCollider != null)
            _hitCollider.enabled = false;

        _ignoredOwnerColliders = null;
        _damage = 0f;
        _knockback = 0f;
        _baseSpeed = 0f;
        _despawnAtTime = float.PositiveInfinity;
        _lastTravelDirection = Vector2.right;
        _spawnPosition = transform.position;
        _pendingDespawn = false;
        _nextAllowedSustainDamageTime = float.PositiveInfinity;
    }

    public override void Despawn()
    {
        if (Pool != null)
        {
            base.Despawn();
            return;
        }

        OnDespawned();
        Destroy(gameObject);
    }

    public void Initialize(
        Vector2 direction,
        float speed,
        float damage,
        float lifetimeSeconds,
        float knockback,
        Collider2D[] ownerColliders = null)
    {
        _damage = damage;
        _knockback = knockback;
        _baseSpeed = speed;
        _despawnAtTime = Time.time + lifetimeSeconds;
        _lastTravelDirection = direction.sqrMagnitude > MinDirectionSqr ? direction.normalized : Vector2.right;
        _spawnPosition = transform.position;
        _ignoredOwnerColliders = ownerColliders;
        _nextAllowedSustainDamageTime = Time.time + sustainDamageInterval;

        SetOwnerIgnore(true);

        if (_rb != null)
            _rb.linearVelocity = _lastTravelDirection * GetCurrentSpeed();

        if (rotateTowardVelocity)
            RotateTowardVelocity();
    }

    private void HandleImpact(Collider2D other)
    {
        if (_pendingDespawn || other == null)
            return;

        if (IsIgnoredOwnerCollider(other))
            return;

        if (TryApplyPlayerDamage(other, _damage, _knockback, false))
            return;

        if (!despawnOnFirstImpact)
            return;

        _pendingDespawn = true;
    }

    private void TryApplySustainDamage(Collider2D other)
    {
        if (!enableSustainContactDamage || despawnOnFirstImpact || _pendingDespawn)
            return;

        if (other == null || IsIgnoredOwnerCollider(other))
            return;

        if (Time.time < _nextAllowedSustainDamageTime)
            return;

        float sustainDamage = _damage * sustainDamageMultiplier;
        float sustainKnockback = _knockback * sustainKnockbackMultiplier;
        if (!TryApplyPlayerDamage(other, sustainDamage, sustainKnockback, sustainDamageBypassesIFrames))
            return;

        _nextAllowedSustainDamageTime = Time.time + sustainDamageInterval;
    }

    private bool TryApplyPlayerDamage(
        Collider2D other,
        float damageAmount,
        float knockbackAmount,
        bool bypassIFrames)
    {
        PlayerDamageReceiver playerDamageReceiver = other.GetComponentInParent<PlayerDamageReceiver>();
        if (playerDamageReceiver == null)
            return false;

        Vector2 targetPosition = other.bounds.center;
        Vector2 origin = _hitCollider != null
            ? _hitCollider.bounds.ClosestPoint(targetPosition)
            : transform.position;

        Vector2 hitDirection = GetCurrentTravelDirection(targetPosition);
        HitData hitData = new HitData(origin, hitDirection, damageAmount, knockbackAmount, gameObject, bypassIFrames);
        playerDamageReceiver.ReceiveHit(hitData);

        if (despawnOnFirstImpact)
            _pendingDespawn = true;

        return true;
    }

    private Vector2 GetCurrentTravelDirection(Vector3 targetPosition)
    {
        if (_rb != null && _rb.linearVelocity.sqrMagnitude > MinDirectionSqr)
            return _rb.linearVelocity.normalized;

        Vector2 fallbackDirection = targetPosition - transform.position;
        if (fallbackDirection.sqrMagnitude > MinDirectionSqr)
            return fallbackDirection.normalized;

        return _lastTravelDirection;
    }

    private bool IsIgnoredOwnerCollider(Collider2D other)
    {
        if (_ignoredOwnerColliders == null)
            return false;

        for (int i = 0; i < _ignoredOwnerColliders.Length; i++)
        {
            Collider2D ownerCollider = _ignoredOwnerColliders[i];
            if (ownerCollider != null && other == ownerCollider)
                return true;
        }

        return false;
    }

    private void SetOwnerIgnore(bool ignore)
    {
        if (_hitCollider == null || _ignoredOwnerColliders == null)
            return;

        for (int i = 0; i < _ignoredOwnerColliders.Length; i++)
        {
            Collider2D ownerCollider = _ignoredOwnerColliders[i];
            if (ownerCollider == null || ownerCollider == _hitCollider)
                continue;

            Physics2D.IgnoreCollision(_hitCollider, ownerCollider, ignore);
        }
    }

    private void RotateTowardVelocity()
    {
        Vector2 velocity = _rb != null ? _rb.linearVelocity : Vector2.zero;
        if (velocity.sqrMagnitude <= MinDirectionSqr)
            return;

        _lastTravelDirection = velocity.normalized;
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + rotateOffsetDegrees;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateTravelVelocity()
    {
        if (_rb == null || _lastTravelDirection.sqrMagnitude <= MinDirectionSqr || _baseSpeed <= 0f)
            return;

        _rb.linearVelocity = _lastTravelDirection * GetCurrentSpeed();
    }

    private float GetCurrentSpeed()
    {
        if (!useBurstDecaySpeedProfile)
            return _baseSpeed;

        float traveledDistanceSqr = GetTraveledDistanceSqr();
        float burstTravelDistanceSqr = burstTravelDistance * burstTravelDistance;
        if (traveledDistanceSqr <= burstTravelDistanceSqr)
            return _baseSpeed * launchSpeedMultiplier;

        float traveledDistance = Mathf.Sqrt(traveledDistanceSqr);
        float distancePastBurst = traveledDistance - burstTravelDistance;
        float decayBlend = 1f - Mathf.Exp(-exponentialDecayRate * distancePastBurst);
        float clampedMinimumMultiplier = Mathf.Min(launchSpeedMultiplier, minimumSpeedMultiplier);
        float speedMultiplier = Mathf.Lerp(launchSpeedMultiplier, clampedMinimumMultiplier, decayBlend);
        return _baseSpeed * speedMultiplier;
    }

    private float GetTraveledDistanceSqr()
    {
        Vector2 currentPosition = _rb != null ? _rb.position : (Vector2)transform.position;
        Vector2 traveledVector = currentPosition - _spawnPosition;
        return traveledVector.sqrMagnitude;
    }
}
