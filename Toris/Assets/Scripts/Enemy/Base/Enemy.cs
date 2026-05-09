using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour, IDamageable, IEnemyMoveable, ITriggerCheckable, IEnemyAggroTarget
{
    private bool _isAggroed;
    public bool IsAggroed
    {
        get => _isAggroed;
        set
        {
            if (_isAggroed == value) return;
            _isAggroed = value;
            AggroStatusChanged?.Invoke(_isAggroed);
        }
    }
    public event Action<bool> AggroStatusChanged;

    //---- Shared Interfaces -------------
    [field: SerializeField] public float MaxHealth { get; set; } = 100f;
    public float CurrentHealth { get; set; }
    public bool IsFacingRight { get; set; } = true;
    [field: SerializeField] public Rigidbody2D rb { get; set; }
    public bool AlwaysAggroed { get; set; }
    public bool IsWithinStrikingDistance { get; set; }

    //--------------------------------
    [SerializeField] private Transform playerTransform;
    public Transform PlayerTransform => playerTransform;

    public Animator animator { get; set; }
    public EnemyStateMachine StateMachine { get; set; }

    // pooling
    public Enemy OriginalPrefab { get; private set; }
    public IEnemyPool OwningPool { get; private set; }
    [Header("Loot")]
    [SerializeField] private EnemyLootTableSO lootTable;

    [Header("Health Bar")]
    [SerializeField] private bool autoCreateHealthBar = true;
    [SerializeField] private EnemyHealthBar healthBar;

    [Header("Alert Indicator")]
    [SerializeField] private bool autoCreateAlertIndicator = true;
    [SerializeField] private EnemyAlertIndicator alertIndicator;

    // Quest reporting stays data-driven: enemies expose stable IDs, then report facts.
    // Quest-specific progress mapping stays outside enemy gameplay code.
    [Header("Quest Reporting")]
    [SerializeField] private string questEnemyId = string.Empty;
    [SerializeField] private string questEnemyTypeOrTag = string.Empty;

    [Header("Targeting")]
    [SerializeField] private bool isPassivePrey;
    [SerializeField] private bool threatensPassiveCreatures;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool debugAttackLogs = true;
#endif

    public EnemyLoadout ActiveLoadout { get; private set; }
    public Transform SpawnPoint { get; private set; }
    public string FactionId { get; private set; } = string.Empty;
    public int DifficultyTier { get; private set; }
    public bool IsPassivePrey => isPassivePrey;
    public bool ThreatensPassiveCreatures => threatensPassiveCreatures;
    public IEnemyAggroTarget AggroTarget => ResolveAggroTarget();
    public IEnemyAggroTarget ExplicitAggroTarget => ResolveExplicitAggroTarget();
    public Transform AggroTargetTransform => AggroTarget?.TargetTransform;
    public Enemy AggroTargetEnemy => AggroTarget as Enemy;
    public bool HasAggroTarget => HasActiveCombatTarget;
    public bool HasExplicitAggroTarget => ResolveExplicitAggroTarget() != null;
    public bool HasActiveCombatTarget =>
        HasExplicitAggroTarget
        || (AlwaysAggroed && IsAggroTargetValid(playerAggroTarget));
    public Transform TargetTransform => transform;
    public Vector2 TargetPosition => GetTargetPosition();
    public bool IsTargetable =>
        CurrentHealth > 0f
        && gameObject.activeInHierarchy
        && gameObject.scene.IsValid();

    private readonly List<IStatusEffect> _statusEffects = new List<IStatusEffect>();
    private bool _isReleasing;
    private float _baseMaxHealth;
    private Collider2D[] _cachedColliders = Array.Empty<Collider2D>();
    private bool[] _cachedColliderEnabledStates = Array.Empty<bool>();
    private bool _collidersDisabledForDeath;
    private bool _hasResolvedDeathLoot;

    public event Action<Enemy> Died;
    public event Action<Enemy> Despawned;
    public event Action<float> Damaged; // for sfx
    public event Action<Enemy, EnemyAlertReason> AlertTriggered;
    public event Action<IEnemyAggroTarget> AggroTargetChanged;

    private GameObject _player;
    private PlayerDamageReceiver _playerDamageReceiver;
    private PlayerProgression _playerProgression;
    private IEnemyAggroTarget sensorAggroTarget;
    private IEnemyAggroTarget overrideAggroTarget;
    private IEnemyAggroTarget playerAggroTarget;
    private IEnemyAggroTarget lastResolvedAggroTarget;
    public EnemyLootTableSO LootTable => lootTable;
    public string QuestEnemyId => questEnemyId;
    public string QuestEnemyTypeOrTag => questEnemyTypeOrTag;
    protected virtual void Awake()
    {
        StateMachine = new EnemyStateMachine();
        animator = GetComponentInChildren<Animator>();
        CacheOwnedColliders();
        _baseMaxHealth = MaxHealth;
        CurrentHealth = MaxHealth;
        EnsureHealthBar();
        EnsureAlertIndicator();
        
#if UNITY_EDITOR
        if (animator == null)
            Debug.LogError("Animator component is missing on the enemy.");
#endif
    }
    protected virtual void Start()
    {
        CurrentHealth = MaxHealth;
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        RefreshScenePlayerReferences();
    }

    private static bool ShouldBindScenePlayerTransform(Transform currentPlayerTransform)
    {
        return currentPlayerTransform == null || !currentPlayerTransform.gameObject.scene.IsValid();
    }

    protected virtual void Update()
    {
        RefreshAggroTargetState();
        StateMachine.CurrentEnemyState?.FrameUpdate();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentEnemyState?.PhysicsUpdate();
    }

    #region Health / Die Functions

    public void Damage(float damageAmount)
    {
        if (!CanTakeDamage()) return;

        CurrentHealth -= damageAmount;

        Damaged?.Invoke(damageAmount);
        if (CurrentHealth > 0f)
            TriggerAlert(EnemyAlertReason.Damaged);

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    protected virtual bool CanTakeDamage() => CurrentHealth > 0f;

    public virtual void Die()
    {
        if (CurrentHealth > 0f) return;
        DisableCollidersForDeath();
        TryResolveDeathLoot();
        ReportQuestKillIfNeeded();
        Died?.Invoke(this);
    }

    #endregion

    #region Movement functions
    public void MoveEnemy(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        if(velocity != Vector2.zero) UpdateAnimationDirection(velocity);
    }
    public virtual void UpdateAnimationDirection(Vector2 direction)
    {
        direction = direction.normalized;
        animator.SetFloat("DirectionX", direction.x);
        animator.SetFloat("DirectionY", direction.y);
    }

    #endregion

    #region Distance Checks
    //those two are set by enemy children trigger_check scripts
    //also children have colliders set as triggers for those checks
    public void SetAggroStatus(bool isAggroed)
    {
        if (!isAggroed)
            ClearSensorAggroTarget();

        IsAggroed = isAggroed || HasActiveCombatTarget;

        if (isAggroed)
            TriggerAlert(EnemyAlertReason.PlayerDetected);

        RefreshAggroTargetState();
    }

    public void SetAggroTarget(Transform targetTransform, Enemy targetEnemy = null)
    {
        if (targetTransform == null)
            return;

        IEnemyAggroTarget target = targetEnemy != null
            ? targetEnemy
            : targetTransform.GetComponentInParent<IEnemyAggroTarget>();

        if (target == null)
            target = targetTransform.GetComponentInChildren<IEnemyAggroTarget>();

        SetSensorAggroTarget(target);
    }

    public void ClearAggroTarget(Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        if (IsSameAggroTarget(sensorAggroTarget, targetTransform))
            ClearSensorAggroTarget();
    }

    public void SetSensorAggroTarget(IEnemyAggroTarget target)
    {
        if (!IsAggroTargetValid(target))
            return;

        if (ReferenceEquals(sensorAggroTarget, target))
        {
            IsAggroed = true;
            RefreshAggroTargetState();
            return;
        }

        sensorAggroTarget = target;
        IsAggroed = true;
        TriggerAlert(EnemyAlertReason.PlayerDetected);
        RefreshAggroTargetState();
    }

    public void ClearSensorAggroTarget(IEnemyAggroTarget target)
    {
        if (!ReferenceEquals(sensorAggroTarget, target))
            return;

        ClearSensorAggroTarget();
    }

    public void ClearSensorAggroTarget()
    {
        if (sensorAggroTarget == null)
        {
            RefreshAggroTargetState();
            return;
        }

        sensorAggroTarget = null;
        RefreshAggroTargetState();
    }

    public void SetOverrideAggroTarget(IEnemyAggroTarget target)
    {
        if (!IsAggroTargetValid(target))
            return;

        if (ReferenceEquals(overrideAggroTarget, target))
        {
            IsAggroed = true;
            RefreshAggroTargetState();
            return;
        }

        overrideAggroTarget = target;
        IsAggroed = true;
        TriggerAlert(EnemyAlertReason.SiteAlerted);
        RefreshAggroTargetState();
    }

    public void ClearOverrideAggroTarget(IEnemyAggroTarget target)
    {
        if (!ReferenceEquals(overrideAggroTarget, target))
            return;

        overrideAggroTarget = null;
        RefreshAggroTargetState();
    }

    public void ClearAllAggroTargets()
    {
        sensorAggroTarget = null;
        overrideAggroTarget = null;
        RefreshAggroTargetState();
    }

    public void TriggerAlert(EnemyAlertReason reason)
    {
        if (CurrentHealth <= 0f)
            return;

        AlertTriggered?.Invoke(this, reason);
    }

    public void SetStrikingDistanceBool(bool isWithinStrikingDistance)
    {
        IsWithinStrikingDistance = isWithinStrikingDistance;
    }
    #endregion

    public void DamagePlayer(float amount, HitData hitData)
    {
#if UNITY_EDITOR
        DebugAttackLog($"Legacy DamagePlayer requested amount={amount:0.##} striking={IsWithinStrikingDistance}");
#endif

        if (!IsAggroTargetValid(playerAggroTarget))
            RefreshScenePlayerReferences();

        if (!IsAggroTargetValid(playerAggroTarget))
        {
#if UNITY_EDITOR
            DebugAttackLog("Legacy DamagePlayer aborted: no valid player target.");
#endif
            return;
        }

        if (IsWithinStrikingDistance)
        {
            hitData.damage = amount;
#if UNITY_EDITOR
            DebugAttackLog($"Legacy DamagePlayer hit -> {GetAttackDebugTargetSummary(playerAggroTarget)} amount={amount:0.##}");
#endif
            playerAggroTarget.ReceiveEnemyHit(amount, hitData);
        }
#if UNITY_EDITOR
        else
        {
            DebugAttackLog($"Legacy DamagePlayer blocked: target not in striking distance -> {GetAttackDebugTargetSummary(playerAggroTarget)}");
        }
#endif
    }

    public void DamageAggroTarget(float amount, HitData hitData, bool requireStrikingDistance = true)
    {
        if (requireStrikingDistance && !IsWithinStrikingDistance)
        {
#if UNITY_EDITOR
            DebugAttackLog($"DamageAggroTarget blocked: striking distance false amount={amount:0.##} target={GetAttackDebugTargetSummary()}");
#endif
            return;
        }

        IEnemyAggroTarget target = AggroTarget;
        if (!IsAggroTargetValid(target))
        {
#if UNITY_EDITOR
            DebugAttackLog($"DamageAggroTarget current target invalid -> {GetAttackDebugTargetSummary(target)}. Trying player fallback.");
#endif
            if (!IsAggroTargetValid(playerAggroTarget))
                RefreshScenePlayerReferences();

            target = playerAggroTarget;
        }

        if (!IsAggroTargetValid(target))
        {
#if UNITY_EDITOR
            DebugAttackLog($"DamageAggroTarget aborted: no valid target amount={amount:0.##}");
#endif
            return;
        }

        hitData.damage = amount;
#if UNITY_EDITOR
        DebugAttackLog($"DamageAggroTarget hit -> {GetAttackDebugTargetSummary(target)} amount={amount:0.##}");
#endif
        target.ReceiveEnemyHit(amount, hitData);
    }

    public bool TryGetAggroTargetPosition(out Vector2 position)
    {
        IEnemyAggroTarget target = AggroTarget;
        if (!IsAggroTargetValid(target))
        {
            position = default;
            return false;
        }

        position = target.TargetPosition;
        return true;
    }

    public Vector2 GetAggroTargetPositionOrSelf()
    {
        return TryGetAggroTargetPosition(out Vector2 position)
            ? position
            : (Vector2)transform.position;
    }

    public Vector2 GetDirectionToAggroTarget()
    {
        return GetDirectionToAggroTarget(transform.position);
    }

    public Vector2 GetDirectionToAggroTarget(Vector3 origin)
    {
        if (!TryGetAggroTargetPosition(out Vector2 targetPosition))
            return Vector2.zero;

        Vector2 direction = targetPosition - (Vector2)origin;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
    }

    public void FaceAggroTarget()
    {
        Vector2 direction = GetDirectionToAggroTarget();
        if (direction.sqrMagnitude > 0.0001f)
            UpdateAnimationDirection(direction);
    }

    public bool IsCurrentAggroTarget(IEnemyAggroTarget target)
    {
        IEnemyAggroTarget currentTarget = AggroTarget;
        return IsAggroTargetValid(target)
            && IsAggroTargetValid(currentTarget)
            && AreSameAggroTargets(currentTarget, target);
    }

    public bool IsCurrentAggroTarget(Transform targetTransform)
    {
        return targetTransform != null && IsSameAggroTarget(AggroTarget, targetTransform);
    }

    public void ReceiveEnemyHit(float amount, HitData hitData)
    {
#if UNITY_EDITOR
        DebugAttackLog($"Received enemy hit amount={amount:0.##} source={(hitData.source != null ? hitData.source.name : "null")}");
#endif
        Damage(amount);
    }

    #region Animation
    public void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        StateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
    }

    public enum AnimationTriggerType
    {
        EnemyDamaged,
        PlayFootstepSound,
        Attack,
        AttackFinished,
        Chase,
        Idle,
        Howl
    }

    #endregion

    #region Pooling
    public virtual void SetPool(IEnemyPool pool, Enemy prefabRef)
    {
        OwningPool = pool;
        OriginalPrefab = prefabRef;
    }

    public virtual void PrepareSpawn(EnemySpawnRequest request)
    {
        SpawnPoint = request.SpawnPoint;
        FactionId = string.IsNullOrEmpty(request.FactionId) ? string.Empty : request.FactionId;
        DifficultyTier = Mathf.Max(0, request.DifficultyTier);
        ActiveLoadout = request.Loadout;

        MaxHealth = _baseMaxHealth;

        if (ActiveLoadout != null)
        {
            ActiveLoadout.Apply(this);
        }
    }

    public virtual void OnSpawned()
    {
        _isReleasing = false;
        _hasResolvedDeathLoot = false;
        RestoreCachedColliderStates();
        CurrentHealth = MaxHealth;
        IsAggroed = false;
        IsWithinStrikingDistance = false;
        AlwaysAggroed = false;
        ClearAllAggroTargets();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        StateMachine.Reset();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        ClearStatusEffects();
    }

    public virtual void OnDespawned()
    {
        ClearStatusEffects();
        AggroStatusChanged = null;
        AlertTriggered = null;
        Despawned?.Invoke(this);
        Despawned = null;
        Died = null;
        _isReleasing = false;
        StateMachine.Reset();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        IsAggroed = false;
        AlwaysAggroed = false;
        ClearAllAggroTargets();
        IsWithinStrikingDistance = false;
        CurrentHealth = MaxHealth;
        ActiveLoadout = null;
        _hasResolvedDeathLoot = false;
    }

    public void RegisterStatusEffect(IStatusEffect effect)
    {
        if (effect == null || _statusEffects.Contains(effect)) return;
        _statusEffects.Add(effect);
    }

    protected void ClearStatusEffects()
    {
        for (int i = 0; i < _statusEffects.Count; i++)
        {
            _statusEffects[i]?.OnRemoved();
        }
        _statusEffects.Clear();
    }

    public void RequestDespawn()
    {
        if (_isReleasing) return;

        _isReleasing = true;

        if (OwningPool != null)
        {
            OwningPool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CacheOwnedColliders()
    {
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);
        _cachedColliderEnabledStates = new bool[_cachedColliders.Length];

        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            Collider2D collider = _cachedColliders[i];
            _cachedColliderEnabledStates[i] = collider != null && collider.enabled;
        }
    }

    protected void DisableCollidersForDeath()
    {
        if (_collidersDisabledForDeath)
            return;

        if (_cachedColliders == null || _cachedColliders.Length == 0)
            CacheOwnedColliders();

        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            Collider2D collider = _cachedColliders[i];
            if (collider == null)
                continue;

            collider.enabled = false;
        }

        _collidersDisabledForDeath = true;
        IsAggroed = false;
        IsWithinStrikingDistance = false;
        AlwaysAggroed = false;
        ClearAllAggroTargets();
    }

    private void RestoreCachedColliderStates()
    {
        if (_cachedColliders == null || _cachedColliders.Length == 0)
            CacheOwnedColliders();

        int colliderCount = Mathf.Min(_cachedColliders.Length, _cachedColliderEnabledStates.Length);
        for (int i = 0; i < colliderCount; i++)
        {
            Collider2D collider = _cachedColliders[i];
            if (collider == null)
                continue;

            collider.enabled = _cachedColliderEnabledStates[i];
        }

        _collidersDisabledForDeath = false;
    }

    private void EnsureHealthBar()
    {
        if (!autoCreateHealthBar)
            return;

        if (healthBar == null)
            TryGetComponent(out healthBar);

        if (healthBar == null)
            healthBar = gameObject.AddComponent<EnemyHealthBar>();
    }

    private void EnsureAlertIndicator()
    {
        if (!autoCreateAlertIndicator)
            return;

        if (alertIndicator == null)
            TryGetComponent(out alertIndicator);

        if (alertIndicator == null)
            alertIndicator = gameObject.AddComponent<EnemyAlertIndicator>();
    }

    private void TryResolveDeathLoot()
    {
        if (_hasResolvedDeathLoot)
            return;

        _hasResolvedDeathLoot = true;
        EnemyLootRuntime.ResolveDeathLoot(this, _playerProgression);
    }

    private void ReportQuestKillIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(questEnemyId))
            return;

        PixelCrushersQuestFactReporter.Report(QuestFact.Kill(questEnemyId, questEnemyTypeOrTag));
    }

    private void RefreshScenePlayerReferences()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
#if UNITY_EDITOR
        if (_player == null) Debug.Log("_player Null");
#endif
        if (_player == null)
        {
            _playerDamageReceiver = null;
            playerAggroTarget = null;
            return;
        }

        _player.TryGetComponent(out _playerDamageReceiver);
        if (_playerDamageReceiver == null)
            _playerDamageReceiver = _player.GetComponentInChildren<PlayerDamageReceiver>();

        playerAggroTarget = _playerDamageReceiver != null
            ? _playerDamageReceiver
            : _player.GetComponentInChildren<IEnemyAggroTarget>();

        if (ShouldBindScenePlayerTransform(playerTransform))
            playerTransform = _player.transform;

        if (playerTransform != null)
            playerTransform.TryGetComponent(out _playerProgression);
    }

    public bool IsAggroTargetValid(IEnemyAggroTarget target)
    {
        if (target == null || IsUnityObjectDestroyed(target))
            return false;

        Transform targetTransform = target.TargetTransform;
        if (targetTransform == null || !targetTransform.gameObject.scene.IsValid())
            return false;

        if (!target.IsTargetable)
            return false;

        return !(target is IDamageable damageable) || damageable.CurrentHealth > 0f;
    }

    private IEnemyAggroTarget ResolveAggroTarget()
    {
        IEnemyAggroTarget explicitTarget = ResolveExplicitAggroTarget();
        if (explicitTarget != null)
            return explicitTarget;

        return AlwaysAggroed && IsAggroTargetValid(playerAggroTarget)
            ? playerAggroTarget
            : null;
    }

    private IEnemyAggroTarget ResolveExplicitAggroTarget()
    {
        if (overrideAggroTarget != null && !IsAggroTargetValid(overrideAggroTarget))
            overrideAggroTarget = null;

        if (overrideAggroTarget != null)
            return overrideAggroTarget;

        if (sensorAggroTarget != null && !IsAggroTargetValid(sensorAggroTarget))
            sensorAggroTarget = null;

        return sensorAggroTarget;
    }

    private void RefreshAggroTargetState()
    {
        IEnemyAggroTarget resolvedTarget = ResolveAggroTarget();
        IsAggroed = resolvedTarget != null;

        if (AreSameAggroTargets(lastResolvedAggroTarget, resolvedTarget))
            return;

        lastResolvedAggroTarget = resolvedTarget;
        AggroTargetChanged?.Invoke(resolvedTarget);
    }

    private Vector2 GetTargetPosition()
    {
        if (_cachedColliders == null || _cachedColliders.Length == 0)
            CacheOwnedColliders();

        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            Collider2D cachedCollider = _cachedColliders[i];
            if (cachedCollider != null && cachedCollider.enabled && cachedCollider.gameObject.activeInHierarchy)
                return cachedCollider.bounds.center;
        }

        return transform.position;
    }

    private static bool AreSameAggroTargets(IEnemyAggroTarget left, IEnemyAggroTarget right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null || IsUnityObjectDestroyed(left) || IsUnityObjectDestroyed(right))
            return false;

        return AreSameTransforms(left.TargetTransform, right.TargetTransform);
    }

    private static bool IsSameAggroTarget(IEnemyAggroTarget target, Transform targetTransform)
    {
        if (target == null || targetTransform == null || IsUnityObjectDestroyed(target))
            return false;

        return AreSameTransforms(target.TargetTransform, targetTransform);
    }

    private static bool AreSameTransforms(Transform left, Transform right)
    {
        if (left == null || right == null)
            return false;

        return left == right || left.root == right.root;
    }

    private static bool IsUnityObjectDestroyed(IEnemyAggroTarget target)
    {
        return target is UnityEngine.Object unityObject && unityObject == null;
    }

#if UNITY_EDITOR
    public void DebugAttackLog(string message)
    {
        if (!debugAttackLogs)
            return;

        Debug.Log($"[EnemyAttack:{GetType().Name}:{name}:{GetInstanceID()}] {message}", this);
    }

    public string GetAttackDebugTargetSummary()
    {
        return GetAttackDebugTargetSummary(AggroTarget);
    }

    public string GetAttackDebugTargetSummary(IEnemyAggroTarget target)
    {
        if (target == null)
            return "target=null";

        if (IsUnityObjectDestroyed(target))
            return "target=destroyed";

        Transform targetTransform = target.TargetTransform;
        string targetName = targetTransform != null ? targetTransform.name : "null-transform";
        string targetPosition = targetTransform != null
            ? $"pos=({targetTransform.position.x:0.##},{targetTransform.position.y:0.##})"
            : "pos=n/a";
        string health = target is IDamageable damageable
            ? $"health={damageable.CurrentHealth:0.##}/{damageable.MaxHealth:0.##}"
            : "health=n/a";

        return $"target={target.GetType().Name}:{targetName} targetable={target.IsTargetable} {health} {targetPosition}";
    }
#endif

    #endregion
}
