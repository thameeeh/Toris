using UnityEngine;

public class Badger : Enemy
{

    [Space][Header("Stats")]
    public float AttackDamage = 20f;
    public float WalkSpeed = 1;
    public float TunnelingSpeed = 3;

    private HitData _hitData; //struct for damaging player 

    public Vector2 TargetPlayerPosition { get; set; }
    public bool IsWondering { get; set; } = false;
    public bool IsTunneling { get; set; } = false;
    public void PrintMessage(string msg)
    {
        Debug.Log(msg);
    }

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

        StateMachine.Initialize(IdleState);

        _hitData = new HitData(Vector2.zero, Vector2.zero, AttackDamage, 1, gameObject);
    }
    protected override void Update()
    {
        base.Update();

        if (CurrentHealth <= 0 && StateMachine.CurrentEnemyState != DeadState)
        {
            Die();
            StateMachine.ChangeState(DeadState);
            Destroy(gameObject);
        }

    }

    public void DamagePlayer(float damage)
    {
        base.DamagePlayer(damage, _hitData);
    }
}
