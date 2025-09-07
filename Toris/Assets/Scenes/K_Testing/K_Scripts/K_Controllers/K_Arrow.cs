using UnityEngine;

public class K_Arrow : MonoBehaviour
{
    [SerializeField] float defaultSpeed = 12f;
    [SerializeField] float defaultLifetime = 3f;

    Rigidbody2D rb;
    Collider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (rb) { rb.gravityScale = 0f; rb.linearDamping = 0f; rb.interpolation = RigidbodyInterpolation2D.Interpolate; }
    }

    public void Init(Vector2 dir, GameObject owner, float? speedOverride = null, float? lifetimeOverride = null)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float speed = speedOverride ?? defaultSpeed;
        float life  = lifetimeOverride ?? defaultLifetime;

        // face travel direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // set velocity
        if (rb) rb.linearVelocity = dir.normalized * speed;

        // ignore collision with shooter if possible
        if (owner && col)
        {
            var ownerCol = owner.GetComponent<Collider2D>();
            if (ownerCol) Physics2D.IgnoreCollision(col, ownerCol, true);
        }

        Destroy(gameObject, life);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(gameObject);
    }
}
