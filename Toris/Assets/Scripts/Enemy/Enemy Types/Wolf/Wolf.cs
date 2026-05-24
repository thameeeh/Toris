using System;
using UnityEngine;

// All States and ScriptableObjects specific to the Wolf enemy
// are defined and instantiated here

// enum for wolf roles
public enum WolfRole { Leader, Minion }

public class Wolf : Enemy
{
    [Space][Space][Header("Stats")]
    public float AttackDamage = 20f;
    public float MovementSpeed = 2f;

    // leader/minion implement
    [Header("Role")]
    public WolfRole role = WolfRole.Minion;
    [Range(0.5f, 3f)] public float healthMultiplier = 1f;
    public bool CanHowl => role == WolfRole.Leader;

    // wolf knowledge of home
    private HomeAnchor _homeAnchor;
    [SerializeField] private float fallbackHomeRadius = 4f;

    public bool HasHome => _homeAnchor != null;
    public Vector3 HomeCenter => HasHome ? _homeAnchor.Center : transform.position;
    public float HomeRadius => HasHome ? _homeAnchor.Radius : fallbackHomeRadius;
    public float DistanceToHome => Vector2.Distance(transform.position, HomeCenter);
    public bool IsOutsideHome(float extraPadding)
    {
        return DistanceToHome > (HomeRadius + Mathf.Max(0f, extraPadding));
    }
    public void RefreshHomeAnchor()
    {
        _homeAnchor = GetComponent<HomeAnchor>();
    }

    [Header("Leader Pack")]
    public PackController pack;

    private HitData _hitData;
    private float _baseAttackDamage;
    private float _baseMaxHealth;
    private float _spawnBaseMaxHealth;
    private bool _hasStarted;

    [Header("Combat Responsiveness")]
    [SerializeField] private float minimumChaseCommitmentSeconds = 0.65f;
    [SerializeField] private float lostTargetChaseGraceSeconds = 1.25f;
    [SerializeField] private float forcedPlayerAggroSeconds = 6f;

    private float _chaseCommitmentUntilTime;
    private float _forcedPlayerAggroUntilTime;
    private Vector2 _lastKnownAggroTargetPosition;
    private bool _hasLastKnownAggroTargetPosition;

    public bool IsMovingWhileBiting { get; set; } = false;
    public bool IsChasingPlayer { get; private set; }
    public void SetChasingPlayer(bool chasingP) => IsChasingPlayer = chasingP;
    public void PrintMessage(string msg) 
    {
#if UNITY_EDITOR
        Debug.Log(msg);
#endif
    }

    [Header("Investigation")]
    public bool HasInvestigationTarget { get; private set; }
    public Vector3 InvestigationTarget { get; private set; }
    public float InvestigationUntilTime { get; private set; }
    public float InvestigationStandDurationBonus { get; private set; }

    public void SetInvestigationTarget(Vector3 target, float duration, float standDurationBonus = 0f)
    {
        InvestigationTarget = target;
        InvestigationUntilTime = Time.time + Mathf.Max(0f, duration);
        InvestigationStandDurationBonus = Mathf.Max(0f, standDurationBonus);
        HasInvestigationTarget = true;
        TriggerAlert(EnemyAlertReason.SiteAlerted);
    }

    public void ClearInvestigationTarget()
    {
        HasInvestigationTarget = false;
        InvestigationTarget = transform.position;
        InvestigationUntilTime = 0f;
        InvestigationStandDurationBonus = 0f;
    }

    public bool IsInvestigationTargetActive()
    {
        if (!HasInvestigationTarget)
            return false;

        if (Time.time > InvestigationUntilTime)
        {
            ClearInvestigationTarget();
            return false;
        }

        return true;
    }


    #region Wolf-Specific States
    public WolfHowlState HowlState { get; set; }
    public WolfChaseState ChaseState { get; set; }
    public WolfIdleState IdleState { get; set; }
    public WolfAttackState AttackState { get; set; }
    public WolfDeadState DeadState { get; set; }

    public WolfReturnHomeState ReturnHomeState { get; set; }
    #endregion

    #region Wolf-Specific ScriptableObjects
    [Space][Space][Header("Wolf-Specific SOs")]
    [SerializeField] private WolfHowlSO EnemyHowlBase;
    [SerializeField] private WolfChaseSO EnemyChaseBase;
    [SerializeField] private WolfIdleSO EnemyIdleBase;
    [SerializeField] private WolfAttackSO EnemyAttackBase;
    [SerializeField] private WolfDeadSO EnemyDeadBase;
    [SerializeField] private WolfReturnHomeSO EnemyReturnHomeBase;

    public WolfHowlSO EnemyHowlBaseInstance { get; set; }
    public WolfChaseSO EnemyChaseBaseInstance { get; set; }
    public WolfIdleSO EnemyIdleBaseInstance { get; set; }
    public WolfAttackSO EnemyAttackBaseInstance { get; set; }
    public WolfDeadSO EnemyDeadBaseInstance { get; set; }

    public WolfReturnHomeSO EnemyReturnHomeBaseInstance { get; set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        RefreshHomeAnchor();
        _baseAttackDamage = AttackDamage;
        _baseMaxHealth = Mathf.Max(1f, MaxHealth);
        _spawnBaseMaxHealth = _baseMaxHealth;

        EnemyHowlBaseInstance = Instantiate(EnemyHowlBase);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseBase);
        EnemyIdleBaseInstance = Instantiate(EnemyIdleBase);
        EnemyAttackBaseInstance = Instantiate(EnemyAttackBase);
        EnemyDeadBaseInstance = Instantiate(EnemyDeadBase);
        EnemyReturnHomeBaseInstance = Instantiate(EnemyReturnHomeBase);

        SubscribeDamageHandler();
        SubscribeAggroMemoryHandler();

        IdleState = new WolfIdleState(this, StateMachine);
        HowlState = new WolfHowlState(this, StateMachine);
        ChaseState = new WolfChaseState(this, StateMachine);
        AttackState = new WolfAttackState(this, StateMachine);
        DeadState = new WolfDeadState(this, StateMachine);
        ReturnHomeState = new WolfReturnHomeState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        EnemyIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyChaseBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyHowlBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyAttackBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyDeadBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyReturnHomeBaseInstance.Initialize(gameObject, this, PlayerTransform);

        ApplyScaling();
        InitializeRuntimeState();

        _hasStarted = true;
    }

    protected override void Update()
    {
        RefreshForcedPlayerAggro();
        RefreshAggroTargetMemory();
        base.Update();

        if(CurrentHealth <= 0 && StateMachine.CurrentEnemyState != DeadState)
        {
            Die();
        }
    }

    public override void Die()
    {
        if (CurrentHealth > 0f)
            return;

        base.Die();

        if (StateMachine.CurrentEnemyState == null)
        {
            StateMachine.Initialize(DeadState);
            return;
        }

        if (StateMachine.CurrentEnemyState != DeadState)
            StateMachine.ChangeState(DeadState);
    }

    public override void PrepareSpawn(EnemySpawnRequest request)
    {
        base.PrepareSpawn(request);
        _spawnBaseMaxHealth = Mathf.Max(1f, MaxHealth);
    }

    public override void OnSpawned()
    {
        SubscribeDamageHandler();
        SubscribeAggroMemoryHandler();
        RefreshHomeAnchor();
        ApplyScaling();
        ClearInvestigationTarget();

        base.OnSpawned();

        if (!_hasStarted)
            return;

        InitializeRuntimeState();
    }

    public override void OnDespawned()
    {
        Damaged -= HandleWolfDamaged;
        AggroTargetChanged -= HandleAggroTargetChanged;
        base.OnDespawned();
        _spawnBaseMaxHealth = _baseMaxHealth;
    }
    private float GetDifficultyMultiplier()
    {
        return 1f + (0.2f * DifficultyTier);
    }

    private void ApplyScaling()
    {
        MaxHealth = Mathf.RoundToInt(Mathf.Max(1f, _spawnBaseMaxHealth) * healthMultiplier);
        AttackDamage = _baseAttackDamage * GetDifficultyMultiplier();
    }

    public void InitializeRuntimeState()
    {
        CurrentHealth = MaxHealth;
        _hitData = new HitData(Vector2.zero, Vector2.zero, AttackDamage, 1, gameObject);

        _chaseCommitmentUntilTime = 0f;
        _forcedPlayerAggroUntilTime = 0f;
        _hasLastKnownAggroTargetPosition = false;

        AlwaysAggroed = false;
        SetAggroStatus(false);

        StateMachine.Reset();
        StateMachine.Initialize(IdleState);
    }

    public void BeginChaseCommitment()
    {
        ExtendChaseCommitment(minimumChaseCommitmentSeconds);
        RefreshAggroTargetMemory();
    }

    public bool ShouldRemainInChase()
    {
        return IsAggroed || Time.time < _chaseCommitmentUntilTime;
    }

    public bool TryGetLastKnownAggroTargetPosition(out Vector2 position)
    {
        position = _lastKnownAggroTargetPosition;
        return _hasLastKnownAggroTargetPosition;
    }

    public void ForcePlayerAggro()
    {
        if (CurrentHealth <= 0f)
            return;

        ClearInvestigationTarget();
        AlwaysAggroed = true;
        _forcedPlayerAggroUntilTime = Time.time + Mathf.Max(0f, forcedPlayerAggroSeconds);
        SetAggroStatus(true);
        BeginChaseCommitment();

        if (StateMachine.CurrentEnemyState == DeadState
            || StateMachine.CurrentEnemyState == AttackState
            || StateMachine.CurrentEnemyState == HowlState)
        {
            return;
        }

        IEnemyState responseState = ShouldHowlBeforeChase() ? HowlState : ChaseState;

        if (StateMachine.CurrentEnemyState == null)
        {
            StateMachine.Initialize(responseState);
            return;
        }

        if (StateMachine.CurrentEnemyState == responseState)
            return;

        StateMachine.ChangeState(responseState);
    }

    private bool ShouldHowlBeforeChase()
    {
        return CanHowl
               && pack != null
               && pack.EnsureLeader(this)
               && pack.CanLeaderHowl(this);
    }

    private void RefreshForcedPlayerAggro()
    {
        if (!AlwaysAggroed)
            return;

        if (_forcedPlayerAggroUntilTime <= 0f)
            return;

        if (Time.time < _forcedPlayerAggroUntilTime)
            return;

        AlwaysAggroed = false;
        _forcedPlayerAggroUntilTime = 0f;
    }

    private void ExtendChaseCommitment(float duration)
    {
        float resolvedDuration = Mathf.Max(0f, duration);
        _chaseCommitmentUntilTime = Mathf.Max(
            _chaseCommitmentUntilTime,
            Time.time + resolvedDuration);
    }

    private void RefreshAggroTargetMemory()
    {
        if (!TryGetAggroTargetPosition(out Vector2 position))
            return;

        _lastKnownAggroTargetPosition = position;
        _hasLastKnownAggroTargetPosition = true;
    }

    private void SubscribeDamageHandler()
    {
        Damaged -= HandleWolfDamaged;
        Damaged += HandleWolfDamaged;
    }

    private void SubscribeAggroMemoryHandler()
    {
        AggroTargetChanged -= HandleAggroTargetChanged;
        AggroTargetChanged += HandleAggroTargetChanged;
    }

    private void HandleWolfDamaged(float damageAmount)
    {
        if (damageAmount <= 0f)
            return;

        ForcePlayerAggro();
    }

    private void HandleAggroTargetChanged(IEnemyAggroTarget target)
    {
        if (target != null)
        {
            RefreshAggroTargetMemory();
            ExtendChaseCommitment(minimumChaseCommitmentSeconds);
            return;
        }

        if (_hasLastKnownAggroTargetPosition)
            ExtendChaseCommitment(lostTargetChaseGraceSeconds);
    }

    public void DestroyGameObject()
    {
        RequestDespawn();
    }

    public void DamagePlayer(float damage)
    {
#if UNITY_EDITOR
        DebugAttackLog($"Wolf legacy DamagePlayer wrapper damage={damage:0.##}");
#endif
        base.DamagePlayer(damage, _hitData);
    }

    public bool DamageCurrentTarget(float damage)
    {
#if UNITY_EDITOR
        DebugAttackLog($"Wolf bite DamageCurrentTarget damage={damage:0.##} {GetAttackDebugTargetSummary()}");
#endif
        return DamageAggroTarget(damage, _hitData);
    }
}
