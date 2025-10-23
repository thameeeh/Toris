using UnityEngine;

public class Badger : Enemy
{
    [Space][Header("Stats")]
    public float AttackDamage = 20;
    public float WalkingSpeed = 1;
    public float BurrowSpeed = 3;

    private HitData _hitData;

    public bool IsMovingWhileBiting { get; set; } = false;
    public void PrintMessage(string msg)
    {
        Debug.Log(msg);
    }

    #region Badger-Specific States
    public BadgerTunnelState TunnelState { get; set; }
    public BadgerIdleState IdleState { get; set; }
    public BadgerBurrowState AttackState { get; set; }
    #endregion

    #region Badger-Specific ScriptableObjects
    [Space][Space][Header("Wolf-Specific SOs")]
    [SerializeField] private BadgerIdleSO BadgerIdleBase;
    [SerializeField] private BadgerTunnelSO BadgerTunnelBase;
    [SerializeField] private BadgerBurrowSO BadgerBurrowBase;

    public BadgerIdleSO BadgerIdleBaseInstance { get; set; }
    public BadgerTunnelSO BadgerTunnelBaseInstance { get; set; }
    public BadgerBurrowSO BadgerBurrowBaseInstance { get; set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        BadgerIdleBaseInstance = Instantiate(BadgerIdleBase);
        BadgerTunnelBaseInstance = Instantiate(BadgerTunnelBase);
        BadgerBurrowBaseInstance = Instantiate(BadgerBurrowBase);

        IdleState = new BadgerIdleState(this, StateMachine);
        TunnelState = new BadgerTunnelState(this, StateMachine);
        AttackState = new BadgerBurrowState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();
        
        BadgerIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BadgerTunnelBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BadgerBurrowBaseInstance.Initialize(gameObject, this, PlayerTransform);

        StateMachine.Initialize(IdleState);
        _hitData = new HitData(Vector2.zero, Vector2.zero, AttackDamage, 1, gameObject);
    }
}
