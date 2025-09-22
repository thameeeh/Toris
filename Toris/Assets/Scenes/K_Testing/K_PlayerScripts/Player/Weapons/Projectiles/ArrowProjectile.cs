using UnityEngine;

public class ArrowProjectile : MonoBehaviour
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

    // pooling
    private ProjectilePoolRegistry pool;
    private ArrowProjectile originalPrefab;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        // projectiles setup (optional)
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

    // public API (pool contract)

    /// <summary>ProjectilePoolRegistry calls this once when creating or lazy-creating an instance.</summary>
    public void SetPool(ProjectilePoolRegistry registry, ArrowProjectile prefabRef)
    {
        pool = registry;
        originalPrefab = prefabRef;
    }

    /// <summary>Original prefab used as the pool key.</summary>
    public ArrowProjectile OriginalPrefab => originalPrefab;

    /// <summary>Called by the pool when the projectile is fetched (before your Initialize).</summary>
    public void OnSpawned()
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
    }

    /// <summary>Called by the pool right before the projectile is returned to the pool.</summary>
    public void OnDespawned()
    {
        SetOwnerIgnore(false);
        ownerCollider = null;
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
    public void Despawn()
    {
        if (pool != null) pool.Release(this);
        else gameObject.SetActive(false);
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

    private void TryApplyDamage(Collider2D target)
    {
        if (target == null) return;

        if (target.TryGetComponent<IDamageable>(out var dmgTarget))
        {
            dmgTarget.Damage(damage);
        }
    }

    private void SetOwnerIgnore(bool ignore)
    {
        if (ownerCollider != null && myCollider != null)
            Physics2D.IgnoreCollision(myCollider, ownerCollider, ignore);
    }
}
