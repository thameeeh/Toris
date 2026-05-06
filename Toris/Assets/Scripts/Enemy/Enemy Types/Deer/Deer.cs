using System.Collections;
using UnityEngine;

public class Deer : Enemy
{
    [Header("Deer Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 1f;
    [SerializeField, Min(0f)] private float runSpeed = 3f;

    [Header("Deer Fear")]
    [SerializeField, Min(0f)] private float minimumFleeDuration = 2f;
    [SerializeField, Min(0f)] private float calmDelayAfterThreatLost = 1f;

    [Header("Deer Animation")]
    [SerializeField] private string idleStateName = "Idle BT";
    [SerializeField] private string walkStateName = "Walk BT";
    [SerializeField] private string runStateName = "Run BT";

    [Space][Space][Header("Deer-Specific SOs")]
    [SerializeField] private DeerIdleSO EnemyIdleBase;
    [SerializeField] private DeerWalkSO EnemyWalkBase;
    [SerializeField] private DeerRunAwaySO EnemyRunAwayBase;
    [SerializeField] private DeerDeadSO EnemyDeadBase;

    private Vector2 lastMoveDirection = new Vector2(1f, -1f).normalized;
    private SpriteRenderer[] cachedSpriteRenderers = System.Array.Empty<SpriteRenderer>();
    private Color[] cachedSpriteColors = System.Array.Empty<Color>();
    private Coroutine fallbackDeathRoutine;
    private bool hasStarted;
    private bool behaviorInstancesReady;
    private string currentAnimationStateName = string.Empty;
    private float fleeUntilTime;
    private bool hasLastThreatPosition;
    private Vector2 lastThreatPosition;

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public bool ShouldKeepFleeing
    {
        get
        {
            RefreshFearFromCurrentAggroTarget();
            return IsAggroed || Time.time < fleeUntilTime;
        }
    }

    public DeerIdleState IdleState { get; private set; }
    public DeerWalkState WalkState { get; private set; }
    public DeerRunAwayState RunAwayState { get; private set; }
    public DeerDeadState DeadState { get; private set; }

    public DeerIdleSO EnemyIdleBaseInstance { get; private set; }
    public DeerWalkSO EnemyWalkBaseInstance { get; private set; }
    public DeerRunAwaySO EnemyRunAwayBaseInstance { get; private set; }
    public DeerDeadSO EnemyDeadBaseInstance { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        CacheRenderers();
        Damaged += HandleDamaged;

        if (!TryCreateBehaviorInstances())
        {
            enabled = false;
            return;
        }

        behaviorInstancesReady = true;

        IdleState = new DeerIdleState(this, StateMachine);
        WalkState = new DeerWalkState(this, StateMachine);
        RunAwayState = new DeerRunAwayState(this, StateMachine);
        DeadState = new DeerDeadState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        if (!behaviorInstancesReady)
            return;

        EnemyIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyWalkBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyRunAwayBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyDeadBaseInstance.Initialize(gameObject, this, PlayerTransform);

        InitializeRuntimeState();
        hasStarted = true;
    }

    private bool TryCreateBehaviorInstances()
    {
        if (EnemyIdleBase == null
            || EnemyWalkBase == null
            || EnemyRunAwayBase == null
            || EnemyDeadBase == null)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"[Deer:{name}] Missing one or more Deer behavior ScriptableObjects. " +
                "Assign Idle, Walk, RunAway, and Dead assets before using this prefab.",
                this);
#endif
            return false;
        }

        EnemyIdleBaseInstance = Instantiate(EnemyIdleBase);
        EnemyWalkBaseInstance = Instantiate(EnemyWalkBase);
        EnemyRunAwayBaseInstance = Instantiate(EnemyRunAwayBase);
        EnemyDeadBaseInstance = Instantiate(EnemyDeadBase);

        return EnemyIdleBaseInstance != null
            && EnemyWalkBaseInstance != null
            && EnemyRunAwayBaseInstance != null
            && EnemyDeadBaseInstance != null;
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

        if (!behaviorInstancesReady)
            return;

        if (!hasStarted)
            return;

        StopFallbackDeathRoutine();
        RestoreRendererColors();
        InitializeRuntimeState();
    }

    public override void OnDespawned()
    {
        StopFallbackDeathRoutine();
        RestoreRendererColors();
        base.OnDespawned();
    }

    public void InitializeRuntimeState()
    {
        if (!behaviorInstancesReady || IdleState == null)
            return;

        CurrentHealth = MaxHealth;
        AlwaysAggroed = false;
        SetAggroStatus(false);
        ResetFearMemory();
        currentAnimationStateName = string.Empty;

        StateMachine.Reset();
        StateMachine.Initialize(IdleState);
    }

    public Vector2 GetPosition2D()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    public void MoveDeer(Vector2 direction, float speed, string animationStateName)
    {
        if (direction.sqrMagnitude > 0.0001f)
            lastMoveDirection = direction.normalized;

        PlayAnimationState(animationStateName);
        MoveEnemy(lastMoveDirection * speed);
    }

    public void Walk(Vector2 direction)
    {
        MoveDeer(direction, WalkSpeed, walkStateName);
    }

    public void Run(Vector2 direction)
    {
        MoveDeer(direction, RunSpeed, runStateName);
    }

    public void StopDeer(string animationStateName)
    {
        MoveEnemy(Vector2.zero);
        UpdateAnimationDirection(lastMoveDirection);
        PlayAnimationState(animationStateName);
    }

    public void PlayIdleAnimation() => PlayAnimationState(idleStateName);

    public void PlayWalkAnimation() => PlayAnimationState(walkStateName);

    public void PlayRunAnimation() => PlayAnimationState(runStateName);

    public void BeginFearResponse()
    {
        RefreshFearFromCurrentAggroTarget();
        fleeUntilTime = Mathf.Max(fleeUntilTime, Time.time + minimumFleeDuration);

        if (hasLastThreatPosition)
            return;

        if (TryGetFallbackThreatPosition(out Vector2 threatPosition))
        {
            lastThreatPosition = threatPosition;
            hasLastThreatPosition = true;
        }
    }

    public bool TryGetFleeThreatPosition(out Vector2 threatPosition)
    {
        RefreshFearFromCurrentAggroTarget();

        if (hasLastThreatPosition && ShouldKeepFleeing)
        {
            threatPosition = lastThreatPosition;
            return true;
        }

        threatPosition = default;
        return false;
    }

    public void BeginFallbackDeath(float holdDuration, float fadeDuration, float despawnDelay)
    {
        StopFallbackDeathRoutine();
        StopDeer(idleStateName);
        fallbackDeathRoutine = StartCoroutine(FallbackDeathRoutine(holdDuration, fadeDuration, despawnDelay));
    }

    public void StopFallbackDeath()
    {
        StopFallbackDeathRoutine();
        RestoreRendererColors();
    }

    private void PlayAnimationState(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        if (currentAnimationStateName == stateName)
            return;

        animator.Play(stateName);
        currentAnimationStateName = stateName;
    }

    private void HandleDamaged(float damageAmount)
    {
        if (!behaviorInstancesReady || CurrentHealth <= 0f || StateMachine.CurrentEnemyState == DeadState)
            return;

        BeginFearResponse();

        if (StateMachine.CurrentEnemyState == null)
            StateMachine.Initialize(RunAwayState);
        else if (StateMachine.CurrentEnemyState != RunAwayState)
            StateMachine.ChangeState(RunAwayState);
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

        fallbackDeathRoutine = null;
        RequestDespawn();
    }

    private void CacheRenderers()
    {
        cachedSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        cachedSpriteColors = new Color[cachedSpriteRenderers.Length];

        for (int i = 0; i < cachedSpriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = cachedSpriteRenderers[i];
            cachedSpriteColors[i] = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }
    }

    private void ApplyRendererAlpha(float alpha)
    {
        if (cachedSpriteRenderers == null || cachedSpriteRenderers.Length == 0)
            CacheRenderers();

        int rendererCount = Mathf.Min(cachedSpriteRenderers.Length, cachedSpriteColors.Length);
        for (int i = 0; i < rendererCount; i++)
        {
            SpriteRenderer spriteRenderer = cachedSpriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            Color color = cachedSpriteColors[i];
            color.a *= Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
        }
    }

    private void RestoreRendererColors()
    {
        if (cachedSpriteRenderers == null || cachedSpriteRenderers.Length == 0)
            CacheRenderers();

        int rendererCount = Mathf.Min(cachedSpriteRenderers.Length, cachedSpriteColors.Length);
        for (int i = 0; i < rendererCount; i++)
        {
            SpriteRenderer spriteRenderer = cachedSpriteRenderers[i];
            if (spriteRenderer != null)
                spriteRenderer.color = cachedSpriteColors[i];
        }
    }

    private void StopFallbackDeathRoutine()
    {
        if (fallbackDeathRoutine == null)
            return;

        StopCoroutine(fallbackDeathRoutine);
        fallbackDeathRoutine = null;
    }

    private void RefreshFearFromCurrentAggroTarget()
    {
        if (!IsAggroed)
            return;

        Transform threat = AggroTargetTransform;
        if (threat == null)
            return;

        lastThreatPosition = threat.position;
        hasLastThreatPosition = true;
        fleeUntilTime = Mathf.Max(fleeUntilTime, Time.time + calmDelayAfterThreatLost);
    }

    private bool TryGetFallbackThreatPosition(out Vector2 threatPosition)
    {
        Transform threat = PlayerTransform;
        if (threat != null && threat.gameObject.scene.IsValid())
        {
            threatPosition = threat.position;
            return true;
        }

        threatPosition = default;
        return false;
    }

    private void ResetFearMemory()
    {
        fleeUntilTime = 0f;
        hasLastThreatPosition = false;
        lastThreatPosition = default;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        runSpeed = Mathf.Max(0f, runSpeed);
        minimumFleeDuration = Mathf.Max(0f, minimumFleeDuration);
        calmDelayAfterThreatLost = Mathf.Max(0f, calmDelayAfterThreatLost);
    }
#endif
}
