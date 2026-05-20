using System.Collections;
using UnityEngine;

public class Boar : Enemy
{
    private const string DirectionXParameter = "DirectionX";
    private const string DirectionYParameter = "DirectionY";
    private const string IsMovingParameter = "IsMoving";
    private const float MinDirectionSqr = 0.0001f;
    private const float DifficultyTierScale = 0.2f;

    [Header("Stats")]
    public float ChargeDamage = 12f;
    public float WanderSpeed = 1.4f;
    public float ChargeSpeed = 3f;
    public float FleeSpeed = 2.2f;
    public float ChargeKnockback = 4f;

    [Header("Home")]
    [SerializeField] private float fallbackHomeRadius = 4f;

    [Space]
    [Header("Boar-Specific SOs")]
    [SerializeField] private BoarIdleSO BoarIdleBase;
    [SerializeField] private BoarWanderSO BoarWanderBase;
    [SerializeField] private BoarChargeSO BoarChargeBase;
    [SerializeField] private BoarFleeSO BoarFleeBase;
    [SerializeField] private BoarDeadSO BoarDeadBase;

    private HitData _hitData;
    private float _baseChargeDamage;
    private float _baseChargeKnockback;
    private bool _hasStarted;
    private bool _behaviorInstancesReady;
    private SpriteRenderer[] _cachedSpriteRenderers = System.Array.Empty<SpriteRenderer>();
    private Color[] _cachedSpriteColors = System.Array.Empty<Color>();
    private Coroutine _fallbackDeathRoutine;
    private Vector2 _lastMoveDirection = new Vector2(1f, -1f).normalized;
    private Vector2 _lastThreatPosition;
    private Vector2 _lastChargeDirection;
    private float _ignoreStartleUntilTime;
    private bool _hasLastThreatPosition;
    private bool _hasLastChargeDirection;
    private HomeAnchor _homeAnchor;

    public BoarIdleState IdleState { get; private set; }
    public BoarWanderState WanderState { get; private set; }
    public BoarChargeState ChargeState { get; private set; }
    public BoarFleeState FleeState { get; private set; }
    public BoarDeadState DeadState { get; private set; }

    public BoarIdleSO BoarIdleBaseInstance { get; private set; }
    public BoarWanderSO BoarWanderBaseInstance { get; private set; }
    public BoarChargeSO BoarChargeBaseInstance { get; private set; }
    public BoarFleeSO BoarFleeBaseInstance { get; private set; }
    public BoarDeadSO BoarDeadBaseInstance { get; private set; }
    public bool CanStartleCharge =>
        !ShouldIgnoreStartle
        && IsAggroed
        && HasAggroTarget
        && BoarChargeBaseInstance != null
        && BoarChargeBaseInstance.CanStartCharge;
    public bool ShouldIgnoreStartle => Time.time < _ignoreStartleUntilTime;
    public bool HasHome => _homeAnchor != null;
    public Vector3 HomeCenter => HasHome ? _homeAnchor.Center : transform.position;
    public float HomeRadius => HasHome ? _homeAnchor.Radius : Mathf.Max(0.01f, fallbackHomeRadius);
    public float DistanceToHome => Vector2.Distance(GetPosition2D(), (Vector2)HomeCenter);
    public bool IsOutsideHome(float extraPadding)
    {
        return DistanceToHome > HomeRadius + Mathf.Max(0f, extraPadding);
    }
    public void RefreshHomeAnchor()
    {
        _homeAnchor = GetComponent<HomeAnchor>();
    }

    protected override void Awake()
    {
        base.Awake();

        RefreshHomeAnchor();
        _baseChargeDamage = ChargeDamage;
        _baseChargeKnockback = ChargeKnockback;
        CacheRenderers();

        if (!TryCreateBehaviorInstances())
        {
            enabled = false;
            return;
        }

        _behaviorInstancesReady = true;

        IdleState = new BoarIdleState(this, StateMachine);
        WanderState = new BoarWanderState(this, StateMachine);
        ChargeState = new BoarChargeState(this, StateMachine);
        FleeState = new BoarFleeState(this, StateMachine);
        DeadState = new BoarDeadState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        if (!_behaviorInstancesReady)
            return;

        BoarIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BoarWanderBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BoarChargeBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BoarFleeBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BoarDeadBaseInstance.Initialize(gameObject, this, PlayerTransform);

        ApplyScaling();
        InitializeRuntimeState();
        _hasStarted = true;
    }

    protected override void Update()
    {
        base.Update();

        if (CurrentHealth <= 0f && StateMachine.CurrentEnemyState != DeadState)
            Die();
    }

    public override void Die()
    {
        if (CurrentHealth > 0f)
            return;

        base.Die();

        if (DeadState == null)
            return;

        if (StateMachine.CurrentEnemyState == null)
        {
            StateMachine.Initialize(DeadState);
            return;
        }

        if (StateMachine.CurrentEnemyState != DeadState)
            StateMachine.ChangeState(DeadState);
    }

    public override void OnSpawned()
    {
        base.OnSpawned();

        if (!_behaviorInstancesReady || !_hasStarted)
            return;

        StopFallbackDeathRoutine();
        RestoreRendererColors();
        ApplyScaling();
        InitializeRuntimeState();
    }

    public override void OnDespawned()
    {
        StopFallbackDeathRoutine();
        RestoreRendererColors();
        StopBoar();
        _homeAnchor = null;
        base.OnDespawned();
    }

    public override void UpdateAnimationDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > MinDirectionSqr)
            _lastMoveDirection = direction.normalized;

        if (animator == null)
            return;

        animator.SetFloat(DirectionXParameter, _lastMoveDirection.x);
        animator.SetFloat(DirectionYParameter, _lastMoveDirection.y);
    }

    public void InitializeRuntimeState()
    {
        if (!_behaviorInstancesReady || IdleState == null)
            return;

        CurrentHealth = MaxHealth;
        _hitData = new HitData(Vector2.zero, Vector2.zero, ChargeDamage, 1, gameObject);
        UpdateAnimationDirection(_lastMoveDirection);
        SetMovementAnimation(false);
        _ignoreStartleUntilTime = 0f;
        _hasLastThreatPosition = false;
        _lastThreatPosition = default;
        _hasLastChargeDirection = false;
        _lastChargeDirection = default;
        BoarChargeBaseInstance?.ResetCooldown();

        AlwaysAggroed = false;
        SetAggroStatus(false);

        StateMachine.Reset();
        StateMachine.Initialize(IdleState);
    }

    public void MoveBoar(Vector2 direction, float speed)
    {
        if (direction.sqrMagnitude <= MinDirectionSqr)
        {
            StopBoar();
            return;
        }

        Vector2 resolvedDirection = direction.normalized;
        SetMovementAnimation(true, resolvedDirection);
        MoveEnemy(resolvedDirection * speed);
    }

    public void StopBoar()
    {
        MoveEnemy(Vector2.zero);
        SetMovementAnimation(false);
    }

    public void SetMovementAnimation(bool isMoving)
    {
        if (animator != null)
            animator.SetBool(IsMovingParameter, isMoving);
    }

    public void SetMovementAnimation(bool isMoving, Vector2 direction)
    {
        UpdateAnimationDirection(direction);
        SetMovementAnimation(isMoving);
    }

    public Vector2 GetPosition2D()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    public void RememberCurrentThreatPosition()
    {
        if (TryGetAggroTargetPosition(out Vector2 threatPosition))
        {
            _lastThreatPosition = threatPosition;
            _hasLastThreatPosition = true;
        }
    }

    public bool TryGetLastThreatPosition(out Vector2 threatPosition)
    {
        if (_hasLastThreatPosition)
        {
            threatPosition = _lastThreatPosition;
            return true;
        }

        threatPosition = default;
        return false;
    }

    public void RememberChargeDirection(Vector2 chargeDirection)
    {
        if (chargeDirection.sqrMagnitude <= MinDirectionSqr)
            return;

        _lastChargeDirection = chargeDirection.normalized;
        _hasLastChargeDirection = true;
    }

    public bool TryGetLastChargeDirection(out Vector2 chargeDirection)
    {
        if (_hasLastChargeDirection)
        {
            chargeDirection = _lastChargeDirection;
            return true;
        }

        chargeDirection = default;
        return false;
    }

    public void IgnoreStartleFor(float duration)
    {
        _ignoreStartleUntilTime = Mathf.Max(_ignoreStartleUntilTime, Time.time + Mathf.Max(0f, duration));
    }

    public void DamageCurrentTarget(float damage, Vector2 chargeDirection)
    {
        Vector2 origin = GetPosition2D();
        Vector2 hitDirection = GetSideKnockbackDirection(chargeDirection);
        _hitData = new HitData(origin, hitDirection, damage, ChargeKnockback, gameObject);
        DamageAggroTarget(damage, _hitData);
    }

    public void BeginFallbackDeath(float holdDuration, float fadeDuration, float despawnDelay)
    {
        StopFallbackDeathRoutine();
        StopBoar();
        _fallbackDeathRoutine = StartCoroutine(FallbackDeathRoutine(holdDuration, fadeDuration, despawnDelay));
    }

    public void StopFallbackDeath()
    {
        StopFallbackDeathRoutine();
        RestoreRendererColors();
    }

    public void DestroyBoar()
    {
        RequestDespawn();
    }

    private bool TryCreateBehaviorInstances()
    {
        if (BoarIdleBase == null
            || BoarWanderBase == null
            || BoarChargeBase == null
            || BoarFleeBase == null
            || BoarDeadBase == null)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"[Boar:{name}] Missing one or more Boar behavior ScriptableObjects. " +
                "Assign Idle, Wander, Charge, Flee, and Dead assets before using this prefab.",
                this);
#endif
            return false;
        }

        BoarIdleBaseInstance = Instantiate(BoarIdleBase);
        BoarWanderBaseInstance = Instantiate(BoarWanderBase);
        BoarChargeBaseInstance = Instantiate(BoarChargeBase);
        BoarFleeBaseInstance = Instantiate(BoarFleeBase);
        BoarDeadBaseInstance = Instantiate(BoarDeadBase);

        return BoarIdleBaseInstance != null
            && BoarWanderBaseInstance != null
            && BoarChargeBaseInstance != null
            && BoarFleeBaseInstance != null
            && BoarDeadBaseInstance != null;
    }

    private void ApplyScaling()
    {
        ChargeDamage = _baseChargeDamage * GetDifficultyMultiplier();
        ChargeKnockback = _baseChargeKnockback * GetDifficultyMultiplier();
    }

    private float GetDifficultyMultiplier()
    {
        return 1f + DifficultyTierScale * DifficultyTier;
    }

    private Vector2 GetSideKnockbackDirection(Vector2 chargeDirection)
    {
        if (chargeDirection.sqrMagnitude <= MinDirectionSqr)
            return _lastMoveDirection;

        Vector2 sideDirection = new Vector2(-chargeDirection.y, chargeDirection.x).normalized;
        if (!TryGetAggroTargetPosition(out Vector2 targetPosition))
            return sideDirection;

        Vector2 toTarget = targetPosition - GetPosition2D();
        if (Vector2.Dot(sideDirection, toTarget) < 0f)
            sideDirection = -sideDirection;

        return sideDirection;
    }

    private IEnumerator FallbackDeathRoutine(float holdDuration, float fadeDuration, float despawnDelay)
    {
        float resolvedHoldDuration = Mathf.Max(0f, holdDuration);
        if (resolvedHoldDuration > 0f)
            yield return new WaitForSeconds(resolvedHoldDuration);

        float resolvedFadeDuration = Mathf.Max(0f, fadeDuration);
        if (resolvedFadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < resolvedFadeDuration)
            {
                ApplyRendererAlpha(1f - Mathf.Clamp01(elapsed / resolvedFadeDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        ApplyRendererAlpha(0f);

        float resolvedDespawnDelay = Mathf.Max(0f, despawnDelay);
        if (resolvedDespawnDelay > 0f)
            yield return new WaitForSeconds(resolvedDespawnDelay);

        _fallbackDeathRoutine = null;
        RequestDespawn();
    }

    private void CacheRenderers()
    {
        _cachedSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _cachedSpriteColors = new Color[_cachedSpriteRenderers.Length];

        for (int i = 0; i < _cachedSpriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _cachedSpriteRenderers[i];
            _cachedSpriteColors[i] = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }
    }

    private void ApplyRendererAlpha(float alpha)
    {
        if (_cachedSpriteRenderers == null || _cachedSpriteRenderers.Length == 0)
            CacheRenderers();

        int rendererCount = Mathf.Min(_cachedSpriteRenderers.Length, _cachedSpriteColors.Length);
        for (int i = 0; i < rendererCount; i++)
        {
            SpriteRenderer spriteRenderer = _cachedSpriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            Color color = _cachedSpriteColors[i];
            color.a *= Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
        }
    }

    private void RestoreRendererColors()
    {
        if (_cachedSpriteRenderers == null || _cachedSpriteRenderers.Length == 0)
            CacheRenderers();

        int rendererCount = Mathf.Min(_cachedSpriteRenderers.Length, _cachedSpriteColors.Length);
        for (int i = 0; i < rendererCount; i++)
        {
            SpriteRenderer spriteRenderer = _cachedSpriteRenderers[i];
            if (spriteRenderer != null)
                spriteRenderer.color = _cachedSpriteColors[i];
        }
    }

    private void StopFallbackDeathRoutine()
    {
        if (_fallbackDeathRoutine == null)
            return;

        StopCoroutine(_fallbackDeathRoutine);
        _fallbackDeathRoutine = null;
    }
}
