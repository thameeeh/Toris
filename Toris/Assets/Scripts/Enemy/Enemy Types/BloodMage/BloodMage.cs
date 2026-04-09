using System;
using UnityEngine;

public class BloodMage : Enemy
{
    private const string CastPointChildName = "CastPoint";
    private const string AttackAnimatorState = "BloodMage_Attack_Bubble";
    private const string AttackTrigger = "Attack";
    private const string DeadTrigger = "Dead";
    private const string IsMovingParameter = "IsMoving";
    private const float MinDirectionSqr = 0.0001f;

    [Space]
    [Header("Stats")]
    public float AttackDamage = 8f;
    public float MovementSpeed = 2.2f;

    [Header("Casting")]
    [SerializeField] private Transform castPoint;

    [Header("Summon Context")]
    [SerializeField] private Necromancer owner;
    [SerializeField] private int summonIndex = -1;
    [SerializeField, Min(1)] private int summonGroupSize = 1;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool debugMovementDiagnostics = false;
    [SerializeField, Min(0.05f)] private float debugMovementLogInterval = 0.2f;

    private Vector2 _lastLoggedAnimationDirection;
    private string _lastMovementDebugMessage = string.Empty;
    private float _nextMovementDebugLogTime;
    private bool _hasMovementDebugSnapshot;
#endif

    private bool _hasStarted;
    private bool _hasEnteredDeadState;
    private bool _isRegisteredWithOwner;
    private Collider2D[] _selfColliders = Array.Empty<Collider2D>();
    private Collider2D[] _projectileIgnoreColliders = Array.Empty<Collider2D>();

    public Transform CastPoint => castPoint != null ? castPoint : transform;
    public Necromancer Owner => owner;
    public int SummonIndex => summonIndex;
    public bool HasOwner => owner != null;
    public bool HasCombatContext => owner != null && PlayerTransform != null;
    public bool ShouldAttackOnOwnerCommand => owner != null && owner.ShouldCommandBloodMagesToAttack && PlayerTransform != null;
    public bool IsWithinAttackRange => IsWithinStrikingDistance;
    public Collider2D[] ProjectileIgnoreColliders => _projectileIgnoreColliders;

    #region BloodMage-Specific States
    public BloodMageIdleState IdleState { get; private set; }
    public BloodMageChaseState ChaseState { get; private set; }
    public BloodMageAttackState AttackState { get; private set; }
    public BloodMageDeadState DeadState { get; private set; }
    #endregion

    #region BloodMage-Specific ScriptableObjects
    [Space]
    [Header("BloodMage-Specific SOs")]
    [SerializeField] private BloodMageIdleSO bloodMageIdleBase;
    [SerializeField] private BloodMageChaseSO bloodMageChaseBase;
    [SerializeField] private BloodMageAttackSO bloodMageAttackBase;
    [SerializeField] private BloodMageDeadSO bloodMageDeadBase;

    public BloodMageIdleSO BloodMageIdleBaseInstance { get; private set; }
    public BloodMageChaseSO BloodMageChaseBaseInstance { get; private set; }
    public BloodMageAttackSO BloodMageAttackBaseInstance { get; private set; }
    public BloodMageDeadSO BloodMageDeadBaseInstance { get; private set; }
    #endregion

    public bool CanStartAttack =>
        BloodMageAttackBaseInstance != null
        && BloodMageAttackBaseInstance.CanUseAttack;

    public bool IsInAttackAnimation
    {
        get
        {
            if (animator == null || animator.IsInTransition(0))
                return false;

            return animator.GetCurrentAnimatorStateInfo(0).IsName(AttackAnimatorState);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        CacheCastPoint();
        CacheSelfColliders();

        if (bloodMageIdleBase != null)
            BloodMageIdleBaseInstance = Instantiate(bloodMageIdleBase);

        if (bloodMageChaseBase != null)
            BloodMageChaseBaseInstance = Instantiate(bloodMageChaseBase);

        if (bloodMageAttackBase != null)
            BloodMageAttackBaseInstance = Instantiate(bloodMageAttackBase);

        if (bloodMageDeadBase != null)
            BloodMageDeadBaseInstance = Instantiate(bloodMageDeadBase);

        IdleState = new BloodMageIdleState(this, StateMachine);
        ChaseState = new BloodMageChaseState(this, StateMachine);
        AttackState = new BloodMageAttackState(this, StateMachine);
        DeadState = new BloodMageDeadState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        BloodMageIdleBaseInstance?.Initialize(gameObject, this, PlayerTransform);
        BloodMageChaseBaseInstance?.Initialize(gameObject, this, PlayerTransform);
        BloodMageAttackBaseInstance?.Initialize(gameObject, this, PlayerTransform);
        BloodMageDeadBaseInstance?.Initialize(gameObject, this, PlayerTransform);

        RefreshProjectileIgnoreColliders();
        InitializeRuntimeState();
        _hasStarted = true;
    }

    protected override void Update()
    {
        if (CurrentHealth <= 0f && StateMachine.CurrentEnemyState != DeadState)
        {
            Die();
            return;
        }

        if (ShouldDespawnWithOwner())
        {
            RequestDespawn();
            return;
        }

        base.Update();
    }

    public override void Die()
    {
        if (CurrentHealth > 0f || _hasEnteredDeadState)
            return;

        _hasEnteredDeadState = true;
        UnregisterFromOwnerIfNeeded();
        StopForDeath();

        if (StateMachine.CurrentEnemyState == null)
        {
            StateMachine.Initialize(DeadState);
            return;
        }

        StateMachine.ChangeState(DeadState);
    }

    public override void OnSpawned()
    {
        base.OnSpawned();

        ResetSummonContext();

        RefreshProjectileIgnoreColliders();

        if (_hasStarted)
            InitializeRuntimeState();
    }

    public override void OnDespawned()
    {
        UnregisterFromOwnerIfNeeded();

        ResetSummonContext();
        _projectileIgnoreColliders = _selfColliders;

        base.OnDespawned();
    }

    public void ConfigureSummon(Necromancer summonOwner, int spawnSlot = -1, int groupSize = 1)
    {
        if (_isRegisteredWithOwner && owner != null && owner != summonOwner)
            UnregisterFromOwnerIfNeeded();

        owner = summonOwner;
        summonIndex = spawnSlot;
        summonGroupSize = Mathf.Max(1, groupSize);
        AlwaysAggroed = true;
        SetAggroStatus(true);
        RegisterWithOwnerIfNeeded();
        RefreshProjectileIgnoreColliders();
    }

    public void SetMovementAnimation(bool isMoving)
    {
        if (animator != null)
        {
            bool previousValue = animator.GetBool(IsMovingParameter);
            animator.SetBool(IsMovingParameter, isMoving);

#if UNITY_EDITOR
            if (debugMovementDiagnostics && previousValue != isMoving)
            {
                DebugMovementDecision($"IsMoving changed {previousValue} -> {isMoving}");
            }
#endif
        }
    }

    public void RequestAttackAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(AttackTrigger);
        animator.SetTrigger(AttackTrigger);
    }

    public void RequestDeathAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(DeadTrigger);
        animator.SetTrigger(DeadTrigger);
    }

    public void FacePlayer()
    {
        Vector2 direction = GetDirectionToPlayer(transform.position);
        if (direction.sqrMagnitude > MinDirectionSqr)
            UpdateAnimationDirection(direction);
    }

    public Vector2 GetDirectionToPlayer(Vector3 origin)
    {
        if (PlayerTransform == null)
            return Vector2.zero;

        Vector2 direction = PlayerTransform.position - origin;
        return direction.sqrMagnitude > MinDirectionSqr ? direction.normalized : Vector2.zero;
    }

    public bool IsOutsideLeash(float leashRadius)
    {
        if (owner == null)
            return false;

        Vector2 ownerDirection = owner.transform.position - transform.position;
        return ownerDirection.sqrMagnitude > leashRadius * leashRadius;
    }

    public Vector2 GetGuardAnchorPosition(float guardRadius, float startAngleDegrees)
    {
        if (owner == null)
            return transform.position;

        int slotCount = Mathf.Max(1, summonGroupSize);
        int slotIndex = Mathf.Clamp(summonIndex, 0, slotCount - 1);
        float angleStep = 360f / slotCount;
        float angleDegrees = startAngleDegrees + (angleStep * slotIndex);
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(
            Mathf.Cos(angleRadians),
            Mathf.Sin(angleRadians)) * guardRadius;

        return (Vector2)owner.transform.position + offset;
    }

    private void InitializeRuntimeState()
    {
        CurrentHealth = MaxHealth;
        ResetRuntimeFlags();
        RefreshProjectileIgnoreColliders();
        StateMachine.Reset();
        StateMachine.Initialize(IdleState);
    }

    private void StopForDeath()
    {
        SetMovementAnimation(false);
        MoveEnemy(Vector2.zero);
    }

    private bool ShouldDespawnWithOwner()
    {
        return owner != null && (owner.CurrentHealth <= 0f || owner.IsHumanForm);
    }

    private void RegisterWithOwnerIfNeeded()
    {
        if (_isRegisteredWithOwner || owner == null)
            return;

        owner.RegisterSummonedBloodMage();
        _isRegisteredWithOwner = true;
    }

    private void UnregisterFromOwnerIfNeeded()
    {
        if (!_isRegisteredWithOwner || owner == null)
            return;

        owner.UnregisterSummonedBloodMage();
        _isRegisteredWithOwner = false;
    }

    private void ResetSummonContext()
    {
        owner = null;
        summonIndex = -1;
        summonGroupSize = 1;
        _hasEnteredDeadState = false;
        _isRegisteredWithOwner = false;
        AlwaysAggroed = false;
        SetAggroStatus(false);
    }

    private void CacheCastPoint()
    {
        if (castPoint != null)
            return;

        Transform castPointTransform = transform.Find(CastPointChildName);
        if (castPointTransform != null)
            castPoint = castPointTransform;
    }

    private void CacheSelfColliders()
    {
        _selfColliders = GetComponentsInChildren<Collider2D>(true);
        _projectileIgnoreColliders = _selfColliders;
    }

    private void RefreshProjectileIgnoreColliders()
    {
        if (_selfColliders == null || _selfColliders.Length == 0)
            _selfColliders = GetComponentsInChildren<Collider2D>(true);

        if (owner == null || owner.ProjectileIgnoreColliders == null || owner.ProjectileIgnoreColliders.Length == 0)
        {
            _projectileIgnoreColliders = _selfColliders;
            return;
        }

        Collider2D[] ownerColliders = owner.ProjectileIgnoreColliders;
        Collider2D[] combinedColliders = new Collider2D[_selfColliders.Length + ownerColliders.Length];
        Array.Copy(_selfColliders, 0, combinedColliders, 0, _selfColliders.Length);
        Array.Copy(ownerColliders, 0, combinedColliders, _selfColliders.Length, ownerColliders.Length);
        _projectileIgnoreColliders = combinedColliders;
    }

    private void ResetRuntimeFlags()
    {
        SetAggroStatus(false);
        SetStrikingDistanceBool(false);
        AlwaysAggroed = false;
        _hasEnteredDeadState = false;

        BloodMageIdleBaseInstance?.ResetRuntimeState();
        BloodMageChaseBaseInstance?.ResetRuntimeState();
        BloodMageAttackBaseInstance?.ResetRuntimeState();
        BloodMageDeadBaseInstance?.ResetValues();

        SetMovementAnimation(false);
        MoveEnemy(Vector2.zero);

#if UNITY_EDITOR
        ResetMovementDebugState();
#endif
    }

#if UNITY_EDITOR
    public override void UpdateAnimationDirection(Vector2 direction)
    {
        base.UpdateAnimationDirection(direction);
        DebugAnimationDirection(direction);
    }

    public void DebugMovementDecision(string message)
    {
        if (!debugMovementDiagnostics)
            return;

        float now = Time.time;
        if (_lastMovementDebugMessage == message && now < _nextMovementDebugLogTime)
            return;

        _lastMovementDebugMessage = message;
        _nextMovementDebugLogTime = now + debugMovementLogInterval;
        Debug.Log($"[BloodMageMove:{GetDebugIdentity()}] {message}", this);
    }

    private void DebugAnimationDirection(Vector2 direction)
    {
        if (!debugMovementDiagnostics || animator == null)
            return;

        if (_hasMovementDebugSnapshot && direction == _lastLoggedAnimationDirection)
            return;

        _hasMovementDebugSnapshot = true;
        _lastLoggedAnimationDirection = direction;

        float directionX = animator.GetFloat("DirectionX");
        float directionY = animator.GetFloat("DirectionY");
        bool isMoving = animator.GetBool(IsMovingParameter);
        string stateName = GetAnimatorStateName(animator.GetCurrentAnimatorStateInfo(0));

        Debug.Log(
            $"[BloodMageMove:{GetDebugIdentity()}] Direction write raw=({direction.x:0.###}, {direction.y:0.###}) " +
            $"anim=({directionX:0.###}, {directionY:0.###}) " +
            $"isMoving={isMoving} " +
            $"state={stateName}",
            this);
    }

    private void ResetMovementDebugState()
    {
        _lastLoggedAnimationDirection = Vector2.zero;
        _lastMovementDebugMessage = string.Empty;
        _nextMovementDebugLogTime = 0f;
        _hasMovementDebugSnapshot = false;
    }

    private static string GetAnimatorStateName(AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsName("BloodMage_Idle"))
            return "BloodMage_Idle";

        if (stateInfo.IsName("BloodMage_Run_BT"))
            return "BloodMage_Run_BT";

        if (stateInfo.IsName("BloodMage_Attack_Bubble"))
            return "BloodMage_Attack_Bubble";

        if (stateInfo.IsName("BloodMage_Dead"))
            return "BloodMage_Dead";

        return $"Unknown(shortHash={stateInfo.shortNameHash})";
    }

    private string GetDebugIdentity()
    {
        return $"{name}|slot={summonIndex}|id={GetInstanceID()}";
    }
#endif
}
