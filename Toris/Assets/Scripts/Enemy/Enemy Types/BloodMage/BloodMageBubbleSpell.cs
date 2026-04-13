using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class BloodMageBubbleSpell : Projectile
{
    private const int MaxOverlapResults = 8;
    private const float MinDirectionSqr = 0.0001f;

    [Header("Pop Behavior")]
    [SerializeField] private bool disableColliderAfterPop = true;
    [SerializeField] private bool bypassPlayerIFrames = false;

    private readonly Collider2D[] _overlapResults = new Collider2D[MaxOverlapResults];

    private Animator _animator;
    private Collider2D _hitCollider;
    private ContactFilter2D _contactFilter;
    private Collider2D[] _ignoredOwnerColliders;
    private float _damage;
    private float _knockback;
    private bool _hasPopped;

    private void Awake()
    {
        TryGetComponent(out _animator);
        TryGetComponent(out _hitCollider);
        _contactFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false,
            useDepth = false,
            useNormalAngle = false
        };
    }

    public override void OnSpawned()
    {
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
        _hasPopped = false;
    }

    public override void OnDespawned()
    {
        SetOwnerIgnore(false);

        if (_hitCollider != null)
            _hitCollider.enabled = false;

        _ignoredOwnerColliders = null;
        _damage = 0f;
        _knockback = 0f;
        _hasPopped = false;
    }

    public override void Despawn()
    {
        if (Pool != null)
        {
            base.Despawn();
            return;
        }

        // Safety fallback for unpooled instances; production gameplay should use GameplayPoolManager.
        OnDespawned();
        Destroy(gameObject);
    }

    public void Initialize(
        Vector2 targetPosition,
        float damage,
        float knockback,
        Collider2D[] ownerColliders = null)
    {
        transform.position = targetPosition;
        transform.rotation = Quaternion.identity;

        _damage = damage;
        _knockback = knockback;
        _ignoredOwnerColliders = ownerColliders;
        _hasPopped = false;

        if (_hitCollider != null)
            _hitCollider.enabled = true;

        SetOwnerIgnore(true);

        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }
    }

    public void Anim_Pop()
    {
        if (_hasPopped)
            return;

        _hasPopped = true;
        ApplyPopDamage();

        if (disableColliderAfterPop && _hitCollider != null)
            _hitCollider.enabled = false;
    }

    public void Anim_Finished()
    {
        Despawn();
    }

    public void Anim_AttackHit()
    {
        Anim_Pop();
    }

    public void Anim_AttackFinished()
    {
        Anim_Finished();
    }

    private void ApplyPopDamage()
    {
        if (_hitCollider == null)
            return;

        int overlapCount = _hitCollider.Overlap(_contactFilter, _overlapResults);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlapCollider = _overlapResults[i];
            if (overlapCollider == null || IsIgnoredOwnerCollider(overlapCollider))
                continue;

            PlayerDamageReceiver playerDamageReceiver = overlapCollider.GetComponentInParent<PlayerDamageReceiver>();
            if (playerDamageReceiver == null)
                continue;

            Vector2 targetPosition = overlapCollider.bounds.center;
            Vector2 origin = _hitCollider.bounds.ClosestPoint(targetPosition);
            Vector2 hitDirection = targetPosition - origin;
            if (hitDirection.sqrMagnitude <= MinDirectionSqr)
                hitDirection = Vector2.zero;

            HitData hitData = new HitData(origin, hitDirection, _damage, _knockback, gameObject, bypassPlayerIFrames);
            playerDamageReceiver.ReceiveHit(hitData);
            return;
        }
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
}
