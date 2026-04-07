using UnityEngine;

public class Necromancer : Enemy
{
    private const string HumanIdleAnimatorState = "Necromancer_Human_Idle";
    private const string HumanToFloaterAnimatorState = "Necromancer_Human_To_Floater";
    private const string FloaterIdleAnimatorState = "Necromancer_Floater_Idle";
    private const string FloaterToHumanAnimatorState = "Necromancer_Floater_To_Human";
    private const string RunAnimatorState = "Necromancer_Run";
    private const string AttackAnimatorState = "Necromancer_Attack";
    private const string ProjectileAttackAnimatorState = "Necromancer_Projectile";
    private const string PanicSwingAnimatorState = "Necromancer_Air_Slash";
    private const string SummonAnimatorState = "Necromancer_Summon";
    private const string HumanDeadAnimatorState = "Necromancer_Human_Dead";
    private const string FloaterDeadAnimatorState = "Necromancer_Floater_Dead";
    private const string LegacyAttackTrigger = "Attack";
    private const string SpellCastTrigger = "SpellCast";
    private const string PanicSwingTrigger = "PanicSwing";
    private const string SummonTrigger = "Summon";
    private const string DeadTrigger = "Dead";
    private const string BecomeFloaterTrigger = "BecomeFloater";
    private const string BecomeHumanTrigger = "BecomeHuman";
    private const string IsMovingParameter = "IsMoving";
    private const string AnimatorChildName = "Animator";
    private const string ShadowChildName = "Shadow";
    private const float MinDirectionSqr = 0.0001f;
    private const float DifficultyTierScale = 0.2f;

    [Space]
    [Header("Stats")]
    public float AttackDamage = 15f;
    public float MovementSpeed = 1.5f;

    [Header("Casting")]
    [SerializeField] private Transform castPoint;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodySpriteRenderer;
    [SerializeField] private SpriteRenderer shadowSpriteRenderer;
    [SerializeField] private bool hideShadowInHumanForm = true;
    [SerializeField] private bool flipSpriteHorizontally = true;
    [SerializeField] private float horizontalFlipThreshold = 0.01f;

    [Header("Human Rescue Variant")]
    [SerializeField] private bool enableHumanRescueVariant = true;
    [SerializeField, Range(0f, 1f)] private float humanRescueVariantChance = 0.05f;
    [SerializeField] private bool humanRescueVariantInvincible = true;

    [Header("Summon")]
    [SerializeField] private bool enableHealthThresholdSummon = true;
    [SerializeField, Range(0f, 1f)] private float summonHealthThreshold = 0.5f;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool debugAnimationTransitions = true;
    [SerializeField] private float debugDecisionLogInterval = 0.75f;

    private int _lastAnimatorStateHash;
    private int _lastNextAnimatorStateHash;
    private bool _lastAnimatorIsInTransition;
    private bool _hasAnimationDebugSnapshot;
    private bool _lastDebugIsMoving;
    private bool _lastDebugAggro;
    private bool _lastDebugStrike;
    private bool _lastDebugCanAttack;
    private bool _lastDebugReadyToCast;
    private string _lastDebugDecisionMessage = string.Empty;
    private float _nextDebugDecisionLogTime;
#endif

    private float _baseAttackDamage;
    private bool _hasStarted;
    private bool _requiresPostCastReposition;
    private bool _hasResolvedAggroTransition;
    private bool _isHumanRescueVariant;
    private bool _hasEnteredDeadState;

    public Transform CastPoint => castPoint != null ? castPoint : transform;
    public bool RequiresPostCastReposition => _requiresPostCastReposition;
    public bool IsWithinCastingRange { get; private set; }
    public bool IsHumanRescueVariant => _isHumanRescueVariant;
    public NecromancerAttackType PendingAttackType { get; private set; } = NecromancerAttackType.SpellCast;
    public bool IsPhaseTwoSummonUnlocked =>
        enableHealthThresholdSummon
        && CurrentHealth <= MaxHealth * summonHealthThreshold;

    #region Necromancer-Specific States
    public NecromancerIdleState IdleState { get; private set; }
    public NecromancerChaseState ChaseState { get; private set; }
    public NecromancerAttackState AttackState { get; private set; }
    public NecromancerDeadState DeadState { get; private set; }
    #endregion

    #region Necromancer-Specific ScriptableObjects
    [Space]
    [Header("Necromancer-Specific SOs")]
    [SerializeField] private NecromancerIdleSO NecromancerIdleBase;
    [SerializeField] private NecromancerChaseSO NecromancerChaseBase;
    [SerializeField] private NecromancerAttackSO NecromancerAttackBase;
    [SerializeField] private NecromancerDeadSO NecromancerDeadBase;

    public NecromancerIdleSO NecromancerIdleBaseInstance { get; private set; }
    public NecromancerChaseSO NecromancerChaseBaseInstance { get; private set; }
    public NecromancerAttackSO NecromancerAttackBaseInstance { get; private set; }
    public NecromancerDeadSO NecromancerDeadBaseInstance { get; private set; }
    #endregion

    public bool CanStartAnyAttack =>
        NecromancerAttackBaseInstance != null
        && (NecromancerAttackBaseInstance.CanUseAttack(NecromancerAttackType.SpellCast)
            || NecromancerAttackBaseInstance.CanUseAttack(NecromancerAttackType.PanicSwing)
            || (IsPhaseTwoSummonUnlocked && NecromancerAttackBaseInstance.CanUseAttack(NecromancerAttackType.Summon)));

    public bool CanStartAttack(NecromancerAttackType attackType)
    {
        return NecromancerAttackBaseInstance != null
            && NecromancerAttackBaseInstance.CanUseAttack(attackType);
    }

    public bool IsReadyToCastAnimation
    {
        get
        {
            if (animator == null || animator.IsInTransition(0))
                return false;

            return animator.GetCurrentAnimatorStateInfo(0).IsName(FloaterIdleAnimatorState);
        }
    }

    public bool IsHumanForm
    {
        get
        {
            if (animator == null || animator.IsInTransition(0))
                return false;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(HumanIdleAnimatorState) || stateInfo.IsName(RunAnimatorState);
        }
    }

    public bool IsFloaterForm
    {
        get
        {
            if (animator == null || animator.IsInTransition(0))
                return false;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(FloaterIdleAnimatorState)
                || stateInfo.IsName(AttackAnimatorState)
                || stateInfo.IsName(ProjectileAttackAnimatorState)
                || stateInfo.IsName(PanicSwingAnimatorState)
                || stateInfo.IsName(SummonAnimatorState);
        }
    }

    public bool IsChangingForm
    {
        get
        {
            if (animator == null)
                return false;

            if (animator.IsInTransition(0))
                return true;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(HumanToFloaterAnimatorState)
                || stateInfo.IsName(FloaterToHumanAnimatorState);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        _baseAttackDamage = AttackDamage;
        CacheCastPoint();
        CacheVisualRenderers();

        if (NecromancerIdleBase != null)
            NecromancerIdleBaseInstance = Instantiate(NecromancerIdleBase);

        if (NecromancerChaseBase != null)
            NecromancerChaseBaseInstance = Instantiate(NecromancerChaseBase);

        if (NecromancerAttackBase != null)
            NecromancerAttackBaseInstance = Instantiate(NecromancerAttackBase);

        if (NecromancerDeadBase != null)
            NecromancerDeadBaseInstance = Instantiate(NecromancerDeadBase);

        IdleState = new NecromancerIdleState(this, StateMachine);
        ChaseState = new NecromancerChaseState(this, StateMachine);
        AttackState = new NecromancerAttackState(this, StateMachine);
        DeadState = new NecromancerDeadState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        NecromancerIdleBaseInstance?.Initialize(gameObject, this, PlayerTransform);
        NecromancerChaseBaseInstance?.Initialize(gameObject, this, PlayerTransform);
        NecromancerAttackBaseInstance?.Initialize(gameObject, this, PlayerTransform);
        NecromancerDeadBaseInstance?.Initialize(gameObject, this, PlayerTransform);

        ApplyScaling();
        InitializeRuntimeState();
        _hasStarted = true;
    }

    protected override void Update()
    {
        if (CurrentHealth <= 0f && StateMachine.CurrentEnemyState != DeadState)
        {
            Die();
#if UNITY_EDITOR
            DebugAnimatorStateChanges();
#endif
            UpdateVisualState();
            return;
        }

        base.Update();

        if (CurrentHealth <= 0f && StateMachine.CurrentEnemyState != DeadState)
            Die();

#if UNITY_EDITOR
        DebugAnimatorStateChanges();
#endif
        UpdateVisualState();
    }

    public override void OnSpawned()
    {
        ApplyScaling();
        CacheVisualRenderers();
        base.OnSpawned();

        if (!_hasStarted)
            return;

        InitializeRuntimeState();
        UpdateVisualState();
    }

    public override void OnDespawned()
    {
        ResetRuntimeFlags();
        base.OnDespawned();
    }

    public void InitializeRuntimeState()
    {
        CurrentHealth = MaxHealth;
        ResetRuntimeFlags();

        StateMachine.Reset();
        StateMachine.Initialize(IdleState);

#if UNITY_EDITOR
        ResetAnimationDebugState();
        DebugAnimationLog("Runtime initialized -> gameplay state IdleState.");
#endif
    }

    public void FacePlayer()
    {
        Vector2 direction = GetDirectionToPlayer();
        if (direction.sqrMagnitude > MinDirectionSqr)
            UpdateAnimationDirection(direction);
    }

    public override void UpdateAnimationDirection(Vector2 direction)
    {
        base.UpdateAnimationDirection(direction);
        UpdateHorizontalFacing(direction);
    }

    public Vector2 GetDirectionToPlayer()
    {
        if (PlayerTransform == null)
            return IsFacingRight ? Vector2.right : Vector2.left;

        Vector2 direction = PlayerTransform.position - transform.position;
        return direction.sqrMagnitude > MinDirectionSqr ? direction.normalized : Vector2.zero;
    }

    public void DestroyGameObject()
    {
        RequestDespawn();
    }

    protected override bool CanTakeDamage()
    {
        if (_isHumanRescueVariant && humanRescueVariantInvincible)
            return false;

        return base.CanTakeDamage();
    }

    public override void Die()
    {
        if (CurrentHealth > 0f)
            return;

        StopForDeath();

        if (_hasEnteredDeadState)
            return;

        _hasEnteredDeadState = true;
        base.Die();
        StateMachine.ChangeState(DeadState);
    }

    public void ResolveAggroTransition()
    {
        if (_hasResolvedAggroTransition)
            return;

        _hasResolvedAggroTransition = true;
        _isHumanRescueVariant = enableHumanRescueVariant
            && IsHumanForm
            && Random.value < humanRescueVariantChance;

#if UNITY_EDITOR
        DebugAnimationLog(_isHumanRescueVariant
            ? "Aggro transition resolved -> human rescue variant. Floater/attack logic disabled."
            : "Aggro transition resolved -> hostile necromancer.");
#endif
    }

    public void SetCastingRangeBool(bool isWithinCastingRange)
    {
        IsWithinCastingRange = isWithinCastingRange;
    }

    public void SetPendingAttackType(NecromancerAttackType attackType)
    {
        PendingAttackType = attackType;
    }

    public bool RequestBecomeFloater()
    {
        if (animator == null || _isHumanRescueVariant || IsFloaterForm || IsChangingForm)
            return false;

        animator.ResetTrigger(BecomeHumanTrigger);
        animator.SetTrigger(BecomeFloaterTrigger);
        animator.SetBool(IsMovingParameter, false);
        MoveEnemy(Vector2.zero);

#if UNITY_EDITOR
        DebugAnimationLog("Requested form change -> Floater.");
#endif
        return true;
    }

    public bool RequestBecomeHuman()
    {
        if (animator == null || IsHumanForm || IsChangingForm)
            return false;

        animator.ResetTrigger(BecomeFloaterTrigger);
        animator.SetTrigger(BecomeHumanTrigger);
        animator.SetBool(IsMovingParameter, false);
        MoveEnemy(Vector2.zero);

#if UNITY_EDITOR
        DebugAnimationLog("Requested form change -> Human.");
#endif
        return true;
    }

    public void SetMovementAnimation(bool isMoving)
    {
        if (animator != null)
            animator.SetBool(IsMovingParameter, isMoving);
    }

    public void RequestAttackAnimation()
    {
        if (animator == null)
            return;

        animator.SetTrigger(GetAttackTrigger(PendingAttackType));
    }

    public void RequestDeathAnimation()
    {
        if (animator == null)
            return;

        animator.SetTrigger(DeadTrigger);
    }

    public void RequirePostCastReposition()
    {
        _requiresPostCastReposition = true;

#if UNITY_EDITOR
        DebugAnimationLog("Post-cast reposition required.");
#endif
    }

    public void ClearPostCastReposition()
    {
        if (!_requiresPostCastReposition)
            return;

        _requiresPostCastReposition = false;

#if UNITY_EDITOR
        DebugAnimationLog("Post-cast reposition completed.");
#endif
    }

    private void CacheCastPoint()
    {
        if (castPoint != null)
            return;

        Transform foundCastPoint = transform.Find("CastPoint");
        if (foundCastPoint != null)
            castPoint = foundCastPoint;
    }

    private void CacheVisualRenderers()
    {
        if (bodySpriteRenderer == null && animator != null)
            animator.TryGetComponent(out bodySpriteRenderer);

        if (shadowSpriteRenderer != null)
            return;

        Transform animatorTransform = animator != null ? animator.transform : transform.Find(AnimatorChildName);
        Transform shadowTransform = animatorTransform != null ? animatorTransform.Find(ShadowChildName) : null;

        if (shadowTransform != null)
            shadowTransform.TryGetComponent(out shadowSpriteRenderer);
    }

    private void UpdateHorizontalFacing(Vector2 direction)
    {
        if (!flipSpriteHorizontally || bodySpriteRenderer == null)
            return;

        if (Mathf.Abs(direction.x) <= horizontalFlipThreshold)
            return;

        IsFacingRight = direction.x > 0f;
        bodySpriteRenderer.flipX = !IsFacingRight;
    }

    private void UpdateVisualState()
    {
        if (shadowSpriteRenderer == null)
            return;

        shadowSpriteRenderer.enabled = !hideShadowInHumanForm || !IsHumanForm;
    }

    private void ApplyScaling()
    {
        AttackDamage = _baseAttackDamage * GetDifficultyMultiplier();
    }

    private float GetDifficultyMultiplier()
    {
        return 1f + (DifficultyTierScale * DifficultyTier);
    }

    private void ResetRuntimeFlags()
    {
        AlwaysAggroed = false;
        SetAggroStatus(false);
        SetStrikingDistanceBool(false);
        SetCastingRangeBool(false);
        ClearPostCastReposition();
        _hasResolvedAggroTransition = false;
        _isHumanRescueVariant = false;
        _hasEnteredDeadState = false;
        PendingAttackType = NecromancerAttackType.SpellCast;
        ResetAnimatorRequests();
        if (rb != null)
            MoveEnemy(Vector2.zero);

        NecromancerIdleBaseInstance?.ResetValues();
        NecromancerChaseBaseInstance?.ResetValues();
        NecromancerAttackBaseInstance?.ResetRuntimeState();
        NecromancerDeadBaseInstance?.ResetValues();
    }

    private void ResetAnimatorRequests()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(BecomeFloaterTrigger);
        animator.ResetTrigger(BecomeHumanTrigger);
        animator.ResetTrigger(LegacyAttackTrigger);
        animator.ResetTrigger(SpellCastTrigger);
        animator.ResetTrigger(PanicSwingTrigger);
        animator.ResetTrigger(SummonTrigger);
        animator.ResetTrigger(DeadTrigger);
        animator.SetBool(IsMovingParameter, false);
    }

    private void StopForDeath()
    {
        SetMovementAnimation(false);

        if (rb != null)
            MoveEnemy(Vector2.zero);
    }

    private string GetAttackTrigger(NecromancerAttackType attackType)
    {
        switch (attackType)
        {
            case NecromancerAttackType.PanicSwing:
                return PanicSwingTrigger;
            case NecromancerAttackType.Summon:
                return SummonTrigger;
            default:
                return SpellCastTrigger;
        }
    }

#if UNITY_EDITOR
    public void DebugAnimationLog(string message)
    {
        if (!debugAnimationTransitions)
            return;

        Debug.Log($"[NecromancerAnim:{name}] {message}", this);
    }

    public void DebugAnimationDecision(string message)
    {
        if (!debugAnimationTransitions)
            return;

        float now = Time.time;
        if (_lastDebugDecisionMessage == message && now < _nextDebugDecisionLogTime)
            return;

        _lastDebugDecisionMessage = message;
        _nextDebugDecisionLogTime = now + Mathf.Max(0.1f, debugDecisionLogInterval);
        DebugAnimationLog(message);
    }

    private void DebugAnimatorStateChanges()
    {
        if (!debugAnimationTransitions || animator == null)
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        bool isInTransition = animator.IsInTransition(0);
        int nextStateHash = 0;
        string nextStateName = "None";

        if (isInTransition)
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            nextStateHash = nextState.fullPathHash;
            nextStateName = GetAnimatorStateName(nextState);
        }

        bool isMoving = animator.GetBool(IsMovingParameter);
        bool canAttack = CanStartAnyAttack;
        bool readyToCast = IsReadyToCastAnimation;

        if (_hasAnimationDebugSnapshot
            && _lastAnimatorStateHash == currentState.fullPathHash
            && _lastNextAnimatorStateHash == nextStateHash
            && _lastAnimatorIsInTransition == isInTransition
            && _lastDebugIsMoving == isMoving
            && _lastDebugAggro == IsAggroed
            && _lastDebugStrike == IsWithinStrikingDistance
            && _lastDebugCanAttack == canAttack
            && _lastDebugReadyToCast == readyToCast)
        {
            return;
        }

        _hasAnimationDebugSnapshot = true;
        _lastAnimatorStateHash = currentState.fullPathHash;
        _lastNextAnimatorStateHash = nextStateHash;
        _lastAnimatorIsInTransition = isInTransition;
        _lastDebugIsMoving = isMoving;
        _lastDebugAggro = IsAggroed;
        _lastDebugStrike = IsWithinStrikingDistance;
        _lastDebugCanAttack = canAttack;
        _lastDebugReadyToCast = readyToCast;

        DebugAnimationLog(
            $"Animator state={GetAnimatorStateName(currentState)} " +
            $"next={nextStateName} " +
            $"transitioning={isInTransition} " +
            $"normalized={currentState.normalizedTime:0.00} " +
            $"IsMoving={isMoving} " +
            $"Aggro={IsAggroed} " +
            $"Strike={IsWithinStrikingDistance} " +
            $"CanAttack={canAttack} " +
            $"ReadyToCast={readyToCast}"
        );
    }

    private void ResetAnimationDebugState()
    {
        _lastAnimatorStateHash = 0;
        _lastNextAnimatorStateHash = 0;
        _lastAnimatorIsInTransition = false;
        _hasAnimationDebugSnapshot = false;
        _lastDebugIsMoving = false;
        _lastDebugAggro = false;
        _lastDebugStrike = false;
        _lastDebugCanAttack = false;
        _lastDebugReadyToCast = false;
        _lastDebugDecisionMessage = string.Empty;
        _nextDebugDecisionLogTime = 0f;
    }

    private string GetAnimatorStateName(AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsName(HumanIdleAnimatorState))
            return HumanIdleAnimatorState;

        if (stateInfo.IsName(HumanToFloaterAnimatorState))
            return HumanToFloaterAnimatorState;

        if (stateInfo.IsName(FloaterIdleAnimatorState))
            return FloaterIdleAnimatorState;

        if (stateInfo.IsName(FloaterToHumanAnimatorState))
            return FloaterToHumanAnimatorState;

        if (stateInfo.IsName(RunAnimatorState))
            return RunAnimatorState;

        if (stateInfo.IsName(AttackAnimatorState))
            return AttackAnimatorState;

        if (stateInfo.IsName(ProjectileAttackAnimatorState))
            return ProjectileAttackAnimatorState;

        if (stateInfo.IsName(PanicSwingAnimatorState))
            return PanicSwingAnimatorState;

        if (stateInfo.IsName(SummonAnimatorState))
            return SummonAnimatorState;

        if (stateInfo.IsName(HumanDeadAnimatorState))
            return HumanDeadAnimatorState;

        if (stateInfo.IsName(FloaterDeadAnimatorState))
            return FloaterDeadAnimatorState;

        return $"Unknown(fullPathHash={stateInfo.fullPathHash})";
    }
#endif
}
