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
    private IEnemyAggroTarget _intendedTarget;
#if UNITY_EDITOR
    private string _debugOwnerName;
#endif
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
        _intendedTarget = null;
#if UNITY_EDITOR
        _debugOwnerName = string.Empty;
#endif
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
        _intendedTarget = null;
#if UNITY_EDITOR
        _debugOwnerName = string.Empty;
#endif
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
        Collider2D[] ownerColliders = null,
        IEnemyAggroTarget intendedTarget = null,
        string debugOwnerName = null)
    {
        transform.position = targetPosition;
        transform.rotation = Quaternion.identity;

        _damage = damage;
        _knockback = knockback;
        _ignoredOwnerColliders = ownerColliders;
        _intendedTarget = intendedTarget;
#if UNITY_EDITOR
        _debugOwnerName = debugOwnerName;
        Debug.Log(
            $"[EnemyAttack:BloodMageBubble:{name}:{GetInstanceID()}] Initialized by={_debugOwnerName ?? "unknown"} " +
            $"pos=({targetPosition.x:0.##},{targetPosition.y:0.##}) damage={damage:0.##} knockback={knockback:0.##} " +
            $"intended={GetDebugTargetSummary(_intendedTarget)}",
            this);
#endif
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
#if UNITY_EDITOR
        Debug.Log($"[EnemyAttack:BloodMageBubble:{name}:{GetInstanceID()}] Anim_Pop -> applying area damage.", this);
#endif
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

            if (!TryResolveDamageTarget(overlapCollider, out IEnemyAggroTarget damageTarget))
                continue;

            Vector2 targetPosition = damageTarget.TargetPosition;
            Vector2 origin = _hitCollider.bounds.ClosestPoint(targetPosition);
            Vector2 hitDirection = targetPosition - origin;
            if (hitDirection.sqrMagnitude <= MinDirectionSqr)
                hitDirection = Vector2.zero;

            HitData hitData = new HitData(origin, hitDirection, _damage, _knockback, gameObject, bypassPlayerIFrames);
#if UNITY_EDITOR
            Debug.Log(
                $"[EnemyAttack:BloodMageBubble:{name}:{GetInstanceID()}] Hit {GetDebugTargetSummary(damageTarget)} " +
                $"damage={_damage:0.##} knockback={_knockback:0.##}",
                this);
#endif
            damageTarget.ReceiveEnemyHit(_damage, hitData);
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[EnemyAttack:BloodMageBubble:{name}:{GetInstanceID()}] Pop found no valid damage target.", this);
#endif
    }

    private bool TryResolveDamageTarget(Collider2D overlapCollider, out IEnemyAggroTarget damageTarget)
    {
        damageTarget = null;

        PlayerDamageReceiver playerDamageReceiver = overlapCollider.GetComponentInParent<PlayerDamageReceiver>();
        if (playerDamageReceiver != null)
            damageTarget = playerDamageReceiver;
        else
            damageTarget = overlapCollider.GetComponentInParent<IEnemyAggroTarget>();

        if (damageTarget == null || !damageTarget.IsTargetable)
            return false;

        return _intendedTarget == null || AreSameTarget(damageTarget, _intendedTarget);
    }

    private static bool AreSameTarget(IEnemyAggroTarget left, IEnemyAggroTarget right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        Transform leftTransform = left.TargetTransform;
        Transform rightTransform = right.TargetTransform;
        if (leftTransform == null || rightTransform == null)
            return false;

        return leftTransform == rightTransform || leftTransform.root == rightTransform.root;
    }

#if UNITY_EDITOR
    private static string GetDebugTargetSummary(IEnemyAggroTarget target)
    {
        if (target == null)
            return "target=null";

        Transform targetTransform = target.TargetTransform;
        string targetName = targetTransform != null ? targetTransform.name : "null-transform";
        return $"{target.GetType().Name}:{targetName} targetable={target.IsTargetable}";
    }
#endif

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
