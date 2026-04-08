using UnityEngine;

public class NecromancerShotProjectile : Projectile, IHitPayloadProvider
{
    private const float MinDirectionSqr = 0.0001f;

    [Header("Visual")]
    [SerializeField] private bool rotateTowardVelocity = true;
    [SerializeField] private float rotateOffsetDegrees = 0f;

    [Header("Hit Behavior")]
    [SerializeField] private bool despawnOnFirstImpact = true;

    private Rigidbody2D _rb;
    private Collider2D _hitCollider;
    private Animator _animator;

    private Collider2D[] _ignoredOwnerColliders;
    private float _damage;
    private float _knockback;
    private float _despawnAtTime = float.PositiveInfinity;
    private Vector2 _lastTravelDirection = Vector2.right;
    private bool _pendingDespawn;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.collider);
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
        _despawnAtTime = float.PositiveInfinity;
        _lastTravelDirection = Vector2.right;
        _pendingDespawn = false;
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
        _despawnAtTime = float.PositiveInfinity;
        _lastTravelDirection = Vector2.right;
        _pendingDespawn = false;
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
        _despawnAtTime = Time.time + lifetimeSeconds;
        _lastTravelDirection = direction.sqrMagnitude > MinDirectionSqr ? direction.normalized : Vector2.right;
        _ignoredOwnerColliders = ownerColliders;

        SetOwnerIgnore(true);

        if (_rb != null)
            _rb.linearVelocity = _lastTravelDirection * speed;

        if (rotateTowardVelocity)
            RotateTowardVelocity();
    }

    public HitData BuildHitData(Vector3 targetPosition)
    {
        Vector2 origin = _hitCollider != null
            ? _hitCollider.bounds.ClosestPoint(targetPosition)
            : transform.position;

        Vector2 hitDirection = GetCurrentTravelDirection(targetPosition);
        return new HitData(origin, hitDirection, _damage, _knockback, gameObject);
    }

    private void HandleImpact(Collider2D other)
    {
        if (_pendingDespawn || other == null)
            return;

        if (IsIgnoredOwnerCollider(other))
            return;

        if (!despawnOnFirstImpact)
            return;

        _pendingDespawn = true;
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
}
