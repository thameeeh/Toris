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

    //Effect spawning attempt
    private const string ArrowHitEffectId = "hit_arrow_square";

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
            Despawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == ownerCollider) return;

        TryApplyDamage(other);

        if (despawnOnFirstHit)
            Despawn();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (ownerCollider && collision.collider == ownerCollider) return;

        TryApplyDamage(collision.collider);

        if (despawnOnFirstHit)
            Despawn();
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
    }

    /// <summary>
    /// Must be called right after Spawn to arm the projectile.
    /// </summary>
    public void Initialize(Vector2 direction, float speed, float dmg, float lifetimeSeconds, Collider2D owner = null)
    {
        damage = dmg;
        despawnAtTime = Time.time + lifetimeSeconds;
        ownerCollider = owner;

        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        if (rb != null)
        {
            rb.linearVelocity = dir * speed;
        }

        SetOwnerIgnore(true);
        RotateTowardVelocity();
    }

    /// <summary>Return to pool (or disable/destroy if no pool available).</summary>
    public override void Despawn() => base.Despawn();

    // internals
    private void RotateTowardVelocity()
    {
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude <= 0.0001f) return;

        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + rotateOffsetDegrees;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void TryApplyDamage(Collider2D target)
    {
        if (target == null) return;

        if (target.TryGetComponent<IDamageable>(out var dmgTarget))
        {
            dmgTarget.Damage(damage);

            SpawnHitEffect(transform.position);
        }
    }

    private void SetOwnerIgnore(bool ignore)
    {
        if (ownerCollider != null && myCollider != null)
            Physics2D.IgnoreCollision(myCollider, ownerCollider, ignore);
    }

    // effect spawn
    private void SpawnHitEffect(Vector3 position)
    {
        var request = new EffectRequest
        {
            EffectId = ArrowHitEffectId,
            Position = position,
            Rotation = Quaternion.identity,
            Parent = null,
            Variant = default,
            Magnitude = 1f
        };

        EffectManagerBehavior.Instance.Play(request);
    }
}
