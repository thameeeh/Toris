using UnityEngine;

// PURPOSE: Applies incoming HitData to the player (damage, knockback, i-frames),
// and triggers Hurt/Death animations. Does NOT decide what is a hit—that's Hurtbox.

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDamageReceiver : MonoBehaviour
{
    [Header("I-Frames")]
    [SerializeField] float iFrameDuration = 0.35f;  // how long subsequent hits are ignored
    [SerializeField] float hurtFlashTime = 0.12f;   // brief visual feedback tint

    [Header("Knockback")]
    [SerializeField] float knockbackMultiplier = 1f;    // global scale on incoming knockback

    float _iFrameUntil;                                 // world time when i-frames end
    PlayerStats _stats; Rigidbody2D _rb; SpriteRenderer _sr; PlayerAnimationController _anim;

    public bool IsInvulnerable => Time.time < _iFrameUntil;

    void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();
        _anim = GetComponentInChildren<PlayerAnimationController>();
    }

    public void ReceiveHit(in HitData hit)
    {
        Debug.Log($"Hit from: {hit.source?.name}, bypass={hit.bypassIFrames}");

        if (IsInvulnerable && !hit.bypassIFrames) return;

        // Apply damage; PlayerStats is the single source of truth for death
        _stats.ApplyDamage(hit.damage);

        // If HP is now zero, play death and exit (LifeGate listens to OnPlayerDied)
        if (_stats.currentHP <= 0f)
        {
            _anim?.PlayDeath();
            // Optional broadcast for legacy hooks—safe to keep, but avoid double-handling
            BroadcastMessage("OnPlayerDied", SendMessageOptions.DontRequireReceiver);
            return;
        }

        // Apply knockback impulse if requested (testing)
        if (hit.knockback > 0f && _rb != null)
            _rb.AddForce(hit.direction * hit.knockback * knockbackMultiplier, ForceMode2D.Impulse);

        // Start i-frames and play hurt feedback
        _iFrameUntil = Time.time + iFrameDuration;
        _anim?.PlayHurt();
        if (_sr != null) StartCoroutine(FlashRoutine());
    }

    // Brief tint to communicate damage taken (will be switched out later with proper graphics)
    System.Collections.IEnumerator FlashRoutine()
    {
        var original = _sr.color;
        _sr.color = new Color(original.r, original.g * 0.6f, original.b * 0.6f, 1f);
        float end = Time.time + hurtFlashTime; while (Time.time < end) yield return null;
        _sr.color = original;
    }
}
