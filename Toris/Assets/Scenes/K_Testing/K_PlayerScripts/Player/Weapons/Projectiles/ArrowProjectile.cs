using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("Rotate sprite so its forward points along velocity. Offset if your art faces up/left.")]
    [SerializeField] float rotateOffsetDegrees = 0f;
    [Tooltip("If true, Destroy on first hit. If false, you can extend to pierce/ricochet.")]
    [SerializeField] bool destroyOnHit = true;

    Rigidbody2D rb;
    Collider2D myCol;

    // runtime
    float damage;
    float despawnAt;
    Collider2D ownerCollider;   // to ignore hitting the shooter

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();

        // Top-down projectile settings are usually like this:
        if (rb)
        {
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    /// <summary>
    /// Call right after Instantiate to arm the arrow.
    /// </summary>
    public void Initialize(Vector2 dir, float speed, float dmg, float lifetime, Collider2D owner = null)
    {
        damage = dmg;
        despawnAt = Time.time + lifetime;
        ownerCollider = owner;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = dir.normalized * speed;
#else
        rb.velocity = dir.normalized * speed;
#endif
        PointAlongVelocity();
        // Ignore collision with owner (optional)
        if (ownerCollider && myCol)
            Physics2D.IgnoreCollision(myCol, ownerCollider, true);
    }

    void Update()
    {
        PointAlongVelocity();
        if (Time.time >= despawnAt) Destroy(gameObject);
    }

    void PointAlongVelocity()
    {
        if (!rb) return;
#if UNITY_6000_0_OR_NEWER
        Vector2 v = rb.linearVelocity;
#else
        Vector2 v = rb.velocity;
#endif
        if (v.sqrMagnitude > 0.0001f)
        {
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + rotateOffsetDegrees;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }

    // Use Trigger for simple hit logic (set your prefab collider to IsTrigger)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == ownerCollider) return; // ignore shooter

        // Deal damage if target supports it
        if (other.TryGetComponent<IDamageable>(out var dmgTarget))
        {
            dmgTarget.Damage(damage);
            if (destroyOnHit) Destroy(gameObject);
            return;
        }

        // Hit environment (walls etc.)
        if (destroyOnHit) Destroy(gameObject);
    }

    // If you prefer non-trigger colliders, you can mirror logic here:
    void OnCollisionEnter2D(Collision2D col)
    {
        if (ownerCollider && col.collider == ownerCollider) return;

        if (col.collider.TryGetComponent<IDamageable>(out var dmgTarget))
        {
            dmgTarget.Damage(damage);
            if (destroyOnHit) Destroy(gameObject);
            return;
        }

        if (destroyOnHit) Destroy(gameObject);
    }
}
