using System.Linq;
using UnityEngine;

public class Badger : Enemy
{

    [Space][Header("Stats")]
    public float AttackDamage = 20f;
    public float WalkSpeed = 1;
    public float TunnelingSpeed = 3;
    public float LineTunnelingSpeed = 6f;
    public float RunAwayDistance = 8f;
    public float BurrowRoamSpeedMultiplier = 0.65f;
    public float PostAttackIdleDuration = 0.35f;
    public float DeathBurrowAcceleration = 2f;
    [Space, Header("Badger Burrow Attack Settings")]
    [Tooltip("How far beyond the player the badger will unburrow.")]
    public float TunnelLineLength = 4f;
    [Tooltip("Damage dealt at the moment of diving underground.")]
    public float BurrowDamage = 20f;
    [Tooltip("Radius around the burrow point used for the entry hit check.")]
    public float BurrowDamageRadius = 1.0f;
    [Tooltip("Damage dealt at the unburrow point.")]
    public float UnburrowDamage = 20f;
    [Tooltip("Radius around the unburrow point used for the exit hit check.")]
    public float UnburrowDamageRadius = 1.0f;


    [Tooltip("How long until it attacks again")]
    public float ForcedIdleDuration = 0f;

    private HitData _hitData;
    private float _baseAttackDamage;

    public Vector2 TargetPlayerPosition { get; set; }
    public Vector2 TunnelLineTarget { get; set; }
    public Vector2 RunAwayTargetPosition { get; set; }
    public bool isWondering { get; set; } = false;
    public bool isTunneling { get; set; } = false;
    public bool isBurrowed { get; set; } = false;
    public bool isRetreating { get; set; } = false;
    public bool ShouldRunAwayOnNextBurrow { get; set; } = false;
    public void PrintMessage(string msg)
    {
        Debug.Log(msg);
    }

    private Renderer[] _renderers;

    #region Badger-Specific States
    public BadgerWalkState WalkState { get; set; }
    public BadgerIdleState IdleState { get; set; }
    public BadgerBurrowState BurrowState { get; set; }
    public BadgerTunnelState TunnelState { get; set; }
    public BadgerUnburrowState UnburrowState { get; set; }
    public BadgerDeadState DeadState { get; set; }
    #endregion

    #region Badger-Specific ScriptableObjects
    [Space][Space][Header("Badger-Specific SOs")]
    [SerializeField] private BadgerIdleSO BadgerIdleBase;
    [SerializeField] private BadgerWalkSO BadgerWalkBase;
    [SerializeField] private BadgerBurrowSO BadgerBurrowBase;
    [SerializeField] private BadgerTunnelSO BadgerTunnelBase;
    [SerializeField] private BadgerUnburrowSO BadgerUnburrowBase;

    public BadgerIdleSO BadgerIdleBaseInstance { get; set; }
    public BadgerWalkSO BadgerWalkBaseInstance { get; set; }
    public BadgerBurrowSO BadgerBurrowBaseInstance { get; set; }
    public BadgerTunnelSO BadgerTunnelBaseInstance { get; set; }
    public BadgerUnburrowSO BadgerUnburrowBaseInstance { get; set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        _renderers = GetComponentsInChildren<Renderer>();

        _baseAttackDamage = AttackDamage;

        BadgerIdleBaseInstance = Instantiate(BadgerIdleBase);
        BadgerWalkBaseInstance = Instantiate(BadgerWalkBase);
        BadgerBurrowBaseInstance = Instantiate(BadgerBurrowBase);
        BadgerTunnelBaseInstance = Instantiate(BadgerTunnelBase);
        BadgerUnburrowBaseInstance = Instantiate(BadgerUnburrowBase);

        IdleState = new BadgerIdleState(this, StateMachine);
        WalkState = new BadgerWalkState(this, StateMachine);
        BurrowState = new BadgerBurrowState(this, StateMachine);
        TunnelState = new BadgerTunnelState(this, StateMachine);
        UnburrowState = new BadgerUnburrowState(this, StateMachine);

        DeadState = new BadgerDeadState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        BadgerIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BadgerWalkBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BadgerBurrowBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BadgerTunnelBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BadgerUnburrowBaseInstance.Initialize(gameObject, this, PlayerTransform);

        // Apply base scaling (uses DifficultyTier if you ever set it)
        ApplyScaling();

        // If this badger is NOT managed by a pool (e.g. placed directly in scene),
        // start its runtime state machine immediately.
        if (OwningPool == null)
        {
            CurrentHealth = MaxHealth;
            _hitData = new HitData(Vector2.zero, Vector2.zero, AttackDamage, 1, gameObject);

            StateMachine.Initialize(IdleState);
            ResetFlags();
        }
    }
    protected override bool CanTakeDamage()
    {
        return !isBurrowed && base.CanTakeDamage();
    }

    protected override void Update()
    {
        base.Update();

        if (CurrentHealth <= 0 && StateMachine.CurrentEnemyState != DeadState)
        {
            Die();
            StateMachine.ChangeState(DeadState);
        }

    }
    public override void OnSpawned()
    {
        base.OnSpawned();

        ApplyScaling();

        CurrentHealth = MaxHealth;
        _hitData = new HitData(Vector2.zero, Vector2.zero, AttackDamage, 1, gameObject);

        ResetFlags();

        StateMachine.Reset();
        StateMachine.Initialize(IdleState);
    }
    private float GetDifficultyMultiplier()
    {
        return 1f + (0.2f * DifficultyTier);
    }

    private void ApplyScaling()
    {
        AttackDamage = _baseAttackDamage * GetDifficultyMultiplier();
    }

    private void ResetFlags()
    {
        isWondering = false;
        isTunneling = false;
        isBurrowed = false;
        isRetreating = false;
        ShouldRunAwayOnNextBurrow = false;
    }
    public void DestroyBadger()
    { 
        RequestDespawn();
    }
    public void DamagePlayer(float damage)
    {
        base.DamagePlayer(damage, _hitData);
    }

    public void ForcedIdleCalclulation(in float deltaTime)
    {
        if (ForcedIdleDuration > 0)
        {
            ForcedIdleDuration -= deltaTime;
        }
    }
    public bool IsVisibleOnScreen()
    {
        return _renderers != null && _renderers.Any(r => r != null && r.isVisible);
    }
}
