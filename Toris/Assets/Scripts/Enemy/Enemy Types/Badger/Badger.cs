using System.Xml;
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
    public BadgerDeadState DeadState { get; set; }
    #endregion

    #region Badger-Specific ScriptableObjects
    [Space][Space][Header("Badger-Specific SOs")]
    [SerializeField] private BadgerIdleSO BadgerIdleBase;
    [SerializeField] private BadgerTunnelSO BadgerTunnelBase;
    [SerializeField] private BadgerBurrowSO BadgerBurrowBase;
    [SerializeField] private BadgerDeadSO BadgerDeadBase;

    public BadgerIdleSO BadgerIdleBaseInstance { get; set; }
    public BadgerTunnelSO BadgerTunnelBaseInstance { get; set; }
    public BadgerBurrowSO BadgerBurrowBaseInstance { get; set; }
    public BadgerDeadSO BadgerDeadBaseInstance { get; set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        BadgerIdleBaseInstance = Instantiate(BadgerIdleBase);
        BadgerTunnelBaseInstance = Instantiate(BadgerTunnelBase);
        BadgerBurrowBaseInstance = Instantiate(BadgerBurrowBase);
        BadgerDeadBaseInstance = Instantiate(BadgerDeadBase);

        IdleState = new BadgerIdleState(this, StateMachine);
        TunnelState = new BadgerTunnelState(this, StateMachine);
        AttackState = new BadgerBurrowState(this, StateMachine);
        DeadState = new BadgerDeadState(this, StateMachine);
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

    private void DealDamageToPlayer(float damage, HitData hitData)
    {
        base.DamagePlayer(damage, hitData);
    }
}
