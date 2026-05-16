using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BalloonPhysics2D : MonoBehaviour
{
    Rigidbody2D rb;
    public float centerForce = 0.5f;
    public float jitterAmount = 0.2f;

    void Start() => rb = GetComponent<Rigidbody2D>();

    public float repulsionForce = 10f;
    public float repulsionRange = 3f;

    void FixedUpdate()
    {
        // 1. Gentle pull toward (0,0) so they don't drift off screen
        Vector2 directionToCenter = (Vector2)transform.position * -1;
        rb.AddForce(directionToCenter * centerForce);

        // 2. Adaptive Repulsion (Crowding Awareness)
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, repulsionRange);
        
        // The more neighbors, the more "pressure" this node feels.
        // This causes lines to 'extend' or stretch when a third-party script gets too close.
        float pressureMultiplier = 1f + (nearby.Length * 2.0f);

        foreach (var other in nearby)
        {
            if (other.gameObject == gameObject) continue;
            
            Vector2 diff = (Vector2)transform.position - (Vector2)other.transform.position;
            float distance = diff.magnitude;
            
            if (distance < 0.1f) distance = 0.1f; // Prevent division by zero
            
            // Use Inverse Square for much stronger push at close range
            // F = (Repulsion * Pressure) / Distance^2
            float force = (repulsionForce * pressureMultiplier) / (distance * distance);
            rb.AddForce(diff.normalized * force);
        }

        // 3. Tiny bit of noise/wobble
        rb.AddForce(Random.insideUnitCircle * jitterAmount, ForceMode2D.Impulse);
    }
}