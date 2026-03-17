using UnityEngine;

// PURPOSE: Responds to PlayerStats.OnPlayerDied by disabling gameplay components and colliders.
// Keeps animation components running so the death animation can play to completion.

public class PlayerLifeGate : MonoBehaviour
{
    [Header("Disable these on death")]
    [SerializeField] Behaviour[] _disableOnDeath;      // e.g., PlayerController, PlayerMotor, PlayerBowController, PlayerInputReader
    [SerializeField] Collider2D[] _disableColliders;   // e.g., body collider, hurtbox, interact triggers
    [SerializeField] Rigidbody2D _rb;                  // to zero velocity on death

    bool _dead;

    void Reset()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        var stats = GetComponent<PlayerStats>();
        if (stats != null) stats.OnPlayerDied += HandleDeath;
    }

    void HandleDeath()
    {
        if (_dead) return;
        _dead = true;

        if (_rb) _rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < _disableOnDeath.Length; i++)
            if (_disableOnDeath[i]) _disableOnDeath[i].enabled = false;

        for (int i = 0; i < _disableColliders.Length; i++)
            if (_disableColliders[i]) _disableColliders[i].enabled = false;
    }

    public void RespawnEnableAll()
    {
        _dead = false;
        for (int i = 0; i < _disableOnDeath.Length; i++)
            if (_disableOnDeath[i]) _disableOnDeath[i].enabled = true;

        for (int i = 0; i < _disableColliders.Length; i++)
            if (_disableColliders[i]) _disableColliders[i].enabled = true;

        if (_rb) _rb.linearVelocity = Vector2.zero;
    }
}
