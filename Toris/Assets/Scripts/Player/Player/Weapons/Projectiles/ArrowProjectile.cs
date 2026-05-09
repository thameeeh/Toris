using System;
using UnityEngine;

public class ArrowProjectile : Projectile
{
    [Header("Visual")]
    [SerializeField] private float rotateOffsetDegrees = 0f;    // rotate so it points along velocity

    [Header("Hit Behavior")]
    [SerializeField] private bool despawnOnFirstHit = true; // if true, the projectile despawn on the first hit of something

    // cached components
    private Rigidbody2D rb;
    private Collider2D myCollider;

    // runtime shot data
    private float damage;
    private float despawnAtTime;
    private Collider2D ownerCollider; // ignore self-collision
    private bool _isVisualOnly;
    private bool _usesDamageLayerMask;
    private int _damageLayerMask = ~0;
    private Func<Collider2D, bool> _canDamageTargetPredicate;
    private string _debugSource = string.Empty;
    private string _pendingDespawnReason = string.Empty;
    private float _spawnTime;
    private float _configuredLifetime;
    private float _configuredSpeed;
    private Vector2 _spawnPosition;

    public event Action<ArrowProjectile, Collider2D, IDamageable, Vector2> DamageApplied;
    public event Action<ArrowProjectile> ProjectileDespawned;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        // projectiles setup
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Update()
    {
        RotateTowardVelocity();

        if (Time.time >= despawnAtTime)
            DespawnWithReason("lifetime-expired");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == ownerCollider) return;

        bool appliedDamage = TryApplyDamage(other);

        if (despawnOnFirstHit)
            DespawnWithReason($"trigger-hit target={FormatCollider(other)} damaged={appliedDamage}");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (ownerCollider && collision.collider == ownerCollider) return;

        bool appliedDamage = TryApplyDamage(collision.collider);

        if (despawnOnFirstHit)
            DespawnWithReason($"collision-hit target={FormatCollider(collision.collider)} damaged={appliedDamage}");
    }

    /// <summary>Called by the pool when the projectile is fetched (before your Initialize).</summary>
    public override void OnSpawned()
    {
        // reset physics state
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        // ensure collider is active
        if (myCollider != null)
            myCollider.enabled = true;

        // reset runtime state
        ownerCollider = null;
        despawnAtTime = float.PositiveInfinity;
        damage = 0f;
        _isVisualOnly = false;
        _usesDamageLayerMask = false;
        _damageLayerMask = ~0;
        _canDamageTargetPredicate = null;
        _debugSource = string.Empty;
        _pendingDespawnReason = string.Empty;
        _spawnTime = 0f;
        _configuredLifetime = 0f;
        _configuredSpeed = 0f;
        _spawnPosition = transform.position;
    }

    /// <summary>Called by the pool right before the projectile is returned to the pool.</summary>
    public override void OnDespawned()
    {
        SetOwnerIgnore(false);
        ownerCollider = null;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (myCollider != null)
            myCollider.enabled = false;

        despawnAtTime = float.PositiveInfinity;
        damage = 0f;
        _isVisualOnly = false;
        _usesDamageLayerMask = false;
        _damageLayerMask = ~0;
        _canDamageTargetPredicate = null;
        _debugSource = string.Empty;
        _pendingDespawnReason = string.Empty;
        _spawnTime = 0f;
        _configuredLifetime = 0f;
        _configuredSpeed = 0f;
        _spawnPosition = transform.position;
        Action<ArrowProjectile> projectileDespawned = ProjectileDespawned;
        ProjectileDespawned = null;
        DamageApplied = null;
        projectileDespawned?.Invoke(this);
    }

    /// <summary>
    /// Must be called right after Spawn to arm the projectile.
    /// </summary>
    public void Initialize(Vector2 direction, float speed, float dmg, float lifetimeSeconds, Collider2D owner = null)
    {
        damage = dmg;
        despawnAtTime = Time.time + lifetimeSeconds;
        ownerCollider = owner;
        _isVisualOnly = false;
        CaptureLaunchDebugData(speed, lifetimeSeconds);

        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        if (rb != null)
        {
            rb.linearVelocity = dir * speed;
        }

        SetOwnerIgnore(true);
        RotateTowardVelocity();
        LogProjectile($"initialized dir={FormatVector(dir)} speed={speed:F2} damage={damage:F2} lifetime={lifetimeSeconds:F2}");
    }

    public void InitializeVisualOnly(Vector2 direction, float speed, float lifetimeSeconds)
    {
        damage = 0f;
        despawnAtTime = Time.time + lifetimeSeconds;
        ownerCollider = null;
        _isVisualOnly = true;
        CaptureLaunchDebugData(speed, lifetimeSeconds);

        if (myCollider != null)
            myCollider.enabled = false;

        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        if (rb != null)
            rb.linearVelocity = dir * speed;

        RotateTowardVelocity();
        LogProjectile($"initialized-visual dir={FormatVector(dir)} speed={speed:F2} lifetime={lifetimeSeconds:F2}");
    }

    public void SetDebugSource(string debugSource)
    {
        _debugSource = string.IsNullOrEmpty(debugSource) ? string.Empty : debugSource;
    }

    public void SetDamageLayerMask(LayerMask layerMask)
    {
        _usesDamageLayerMask = true;
        _damageLayerMask = layerMask.value;
    }

    public void SetDamageTargetPredicate(Func<Collider2D, bool> predicate)
    {
        _canDamageTargetPredicate = predicate;
    }

    public void SetPlayHitEffect(bool playHitEffect)
    {
        _ = playHitEffect;
    }

    /// <summary>Return to pool (or disable/destroy if no pool available).</summary>
    public override void Despawn()
    {
        if (string.IsNullOrEmpty(_pendingDespawnReason))
            _pendingDespawnReason = "external";

        LogProjectile(
            $"despawn reason={_pendingDespawnReason} age={(Time.time - _spawnTime):F2}/{_configuredLifetime:F2} speed={_configuredSpeed:F2} distance={(Vector2.Distance(_spawnPosition, transform.position)):F2} pos={FormatVector(transform.position)}");

        base.Despawn();
    }

    // internals
    private void RotateTowardVelocity()
    {
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude <= 0.0001f) return;

        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + rotateOffsetDegrees;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private bool TryApplyDamage(Collider2D target)
    {
        if (target == null) return false;
        if (_isVisualOnly) return false;
        if (_usesDamageLayerMask && (_damageLayerMask & (1 << target.gameObject.layer)) == 0)
            return false;
        if (_canDamageTargetPredicate != null && !_canDamageTargetPredicate(target))
            return false;

        var dmgTarget = target.GetComponentInParent<IDamageable>();
        if (dmgTarget != null)
        {
            Vector2 hitPoint = target.ClosestPoint(transform.position);
            dmgTarget.Damage(damage);
            DamageApplied?.Invoke(this, target, dmgTarget, hitPoint);
            return true;
        }

        return false;
    }

    private void DespawnWithReason(string reason)
    {
        _pendingDespawnReason = reason;
        Despawn();
    }

    private void CaptureLaunchDebugData(float speed, float lifetimeSeconds)
    {
        _pendingDespawnReason = string.Empty;
        _spawnTime = Time.time;
        _configuredLifetime = lifetimeSeconds;
        _configuredSpeed = speed;
        _spawnPosition = transform.position;
    }

    private void LogProjectile(string message)
    {
        if (!string.Equals(_debugSource, "Rambow", StringComparison.Ordinal))
            return;

        PlayerShootDebug.Log(this, "ArrowProjectile", $"source={_debugSource} {message}");
    }

    private static string FormatCollider(Collider2D target)
    {
        if (target == null)
            return "null";

        return $"{target.name}/layer={LayerMask.LayerToName(target.gameObject.layer)} trigger={target.isTrigger}";
    }

    private static string FormatVector(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }


    private void SetOwnerIgnore(bool ignore)
    {
        if (ownerCollider != null && myCollider != null)
            Physics2D.IgnoreCollision(myCollider, ownerCollider, ignore);
    }

}
