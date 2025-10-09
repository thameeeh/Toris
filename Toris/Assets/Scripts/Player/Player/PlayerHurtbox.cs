using UnityEngine;

// PURPOSE: Entry point for player damage. On trigger enter with an allowed layer,
// it queries the attacker for IHitPayloadProvider and forwards the resulting HitData
// to PlayerDamageReceiver. Falls back to a small generic hit when provider is absent.

public class PlayerHurtbox : MonoBehaviour
{
    [Tooltip("Layers that are allowed to damage the player (e.g., EnemyHitbox).")]
    public LayerMask damagingLayers;

    private PlayerDamageReceiver _receiver;

    void Awake() => _receiver = GetComponentInParent<PlayerDamageReceiver>();

    void OnTriggerEnter2D(Collider2D other)
    {
        // Reject anything not on a damaging layer
        if (((1 << other.gameObject.layer) & damagingLayers.value) == 0) return;

        // Prefer attacker-provided payload; otherwise use a sane fallback
        var provider = other.GetComponent<IHitPayloadProvider>();
        HitData hit;
        if (provider != null)
        {
            // Attacker defines damage, direction, knockback, bypass flag, etc.
            hit = provider.BuildHitData(transform.position);
        }
        else
        {
            // Generic fallback
            Vector2 origin = other.bounds.ClosestPoint(transform.position);
            Vector2 dir = (Vector2)(transform.position - (Vector3)origin);
            hit = new HitData(origin, dir, dmg: 10f, kb: 2f, src: other.gameObject);
        }

        _receiver.ReceiveHit(hit);
    }
}
