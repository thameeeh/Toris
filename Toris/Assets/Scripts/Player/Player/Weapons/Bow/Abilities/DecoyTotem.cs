using System.Collections.Generic;
using UnityEngine;

public struct DecoyTotemSettings
{
    public float maxHealth;
    public float duration;
    public float retargetRadius;
    public float retargetInterval;
    public float targetColliderRadius;
    public LayerMask enemyLayerMask;
    public bool affectPassivePrey;
}

public sealed class DecoyTotem : MonoBehaviour, IEnemyAggroTarget, IDamageable
{
    private const int MaxRetargetResults = 64;
    private const float MinimumDuration = 0.05f;
    private const float MinimumHealth = 1f;
    private const float MinimumRetargetRadius = 0.1f;
    private const float MinimumRetargetInterval = 0.05f;
    private const float MinimumColliderRadius = 0.05f;

    private readonly Collider2D[] _retargetResults = new Collider2D[MaxRetargetResults];
    private readonly HashSet<Enemy> _affectedEnemies = new HashSet<Enemy>();
    private readonly HashSet<Enemy> _scanEnemies = new HashSet<Enemy>();

    private DecoyTotemSettings _settings;
    private Collider2D _targetCollider;
    private bool _initialized;
    private bool _expired;
    private float _expiresAtTime;
    private float _nextRetargetTime;

    public Transform TargetTransform => transform;
    public Vector2 TargetPosition => _targetCollider != null ? _targetCollider.bounds.center : transform.position;
    public bool IsTargetable =>
        _initialized
        && !_expired
        && CurrentHealth > 0f
        && gameObject.activeInHierarchy
        && gameObject.scene.IsValid();

    public float MaxHealth { get; set; }
    public float CurrentHealth { get; set; }
    public bool IsExpired => _expired;

    private void Awake()
    {
        _targetCollider = GetComponentInChildren<Collider2D>();
    }

    private void Update()
    {
        if (!_initialized || _expired)
            return;

        if (Time.time >= _expiresAtTime)
        {
            Expire("duration");
            return;
        }

        if (Time.time < _nextRetargetTime)
            return;

        RetargetEnemies();
        _nextRetargetTime = Time.time + Mathf.Max(MinimumRetargetInterval, _settings.retargetInterval);
    }

    private void OnDisable()
    {
        if (_initialized && !_expired)
            Expire("disabled", false);
    }

    private void OnDestroy()
    {
        ReleaseAffectedEnemies();
    }

    public void Initialize(DecoyTotemSettings settings)
    {
        _settings = settings;
        MaxHealth = Mathf.Max(MinimumHealth, settings.maxHealth);
        CurrentHealth = MaxHealth;
        _initialized = true;
        _expired = false;
        _expiresAtTime = Time.time + Mathf.Max(MinimumDuration, settings.duration);

        EnsureTargetCollider();
        Log($"Initialized at {FormatVector(transform.position)}. hp={CurrentHealth:F0}/{MaxHealth:F0} duration={settings.duration:F2} radius={settings.retargetRadius:F2} mask={ResolveEnemyMask()}.");
        RetargetEnemies();
        _nextRetargetTime = Time.time + Mathf.Max(MinimumRetargetInterval, _settings.retargetInterval);
    }

    public void ReceiveEnemyHit(float amount, HitData hitData)
    {
        Log($"ReceiveEnemyHit amount={amount:F0} source={(hitData.source != null ? hitData.source.name : "<null>")}.");
        Damage(amount);
    }

    public void Damage(float damageAmount)
    {
        if (_expired || CurrentHealth <= 0f)
            return;

        float previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, damageAmount));
        Log($"Damaged for {damageAmount:F0}. hp {previousHealth:F0} -> {CurrentHealth:F0}.");

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Die()
    {
        Expire("health-depleted");
    }

    public void Dismiss(string reason)
    {
        Expire(reason);
    }

    private void Expire(string reason, bool destroyObject = true)
    {
        if (_expired)
            return;

        _expired = true;
        Log($"Expiring. reason={reason} affectedEnemies={_affectedEnemies.Count}.");
        ReleaseAffectedEnemies();

        if (destroyObject)
            Destroy(gameObject);
    }

    private void RetargetEnemies()
    {
        if (!IsTargetable)
            return;

        _scanEnemies.Clear();
        float radius = Mathf.Max(MinimumRetargetRadius, _settings.retargetRadius);
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            radius,
            _retargetResults,
            ResolveEnemyMask());

        int refreshedCount = 0;
        int newCount = 0;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = _retargetResults[i];
            if (hitCollider == null)
                continue;

            Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
            if (!CanAffectEnemy(enemy))
                continue;

            if (!_scanEnemies.Add(enemy))
                continue;

            if (_affectedEnemies.Add(enemy))
                newCount++;

            enemy.SetOverrideAggroTarget(this);
            refreshedCount++;
        }

        if (newCount > 0)
        {
            Log($"Retarget scan hitColliders={hitCount} refreshed={refreshedCount} new={newCount} affected={_affectedEnemies.Count}.");
        }
    }

    private bool CanAffectEnemy(Enemy enemy)
    {
        if (enemy == null)
            return false;

        if (!_settings.affectPassivePrey && enemy.IsPassivePrey)
            return false;

        return enemy.IsTargetable;
    }

    private void ReleaseAffectedEnemies()
    {
        if (_affectedEnemies.Count == 0)
            return;

        Log($"Releasing {_affectedEnemies.Count} affected enemies.");
        foreach (Enemy enemy in _affectedEnemies)
        {
            if (enemy == null)
                continue;

            enemy.ClearOverrideAggroTarget(this);
        }

        _affectedEnemies.Clear();
        _scanEnemies.Clear();
    }

    private void EnsureTargetCollider()
    {
        if (_targetCollider != null)
            return;

        CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = Mathf.Max(MinimumColliderRadius, _settings.targetColliderRadius);
        _targetCollider = circleCollider;

        if (!TryGetComponent(out Rigidbody2D rigidbody2D))
            rigidbody2D = gameObject.AddComponent<Rigidbody2D>();

        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        rigidbody2D.gravityScale = 0f;
        rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private int ResolveEnemyMask()
    {
        return _settings.enemyLayerMask.value != 0
            ? _settings.enemyLayerMask.value
            : BowAbilityTargetingUtility.GetEnemyHurtBoxMask();
    }

    private void Log(string message)
    {
        PlayerShootDebug.Log(this, "DecoyTotem", message);
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }
}
