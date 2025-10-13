using UnityEngine;

// All States and ScriptableObjects specific to the Wolf enemy
// are defined and instantiated here

// 
public class Wolf : Enemy
{
    #region Wolf-Specific States
    public WolfHowlState HowlState { get; set; }
    public WolfChaseState ChaseState { get; set; }
    public WolfIdleState IdleState { get; set; }
    public WolfAttackState AttackState { get; set; }
    #endregion

    #region Wolf-Specific ScriptableObjects
    [Header("Wolf-Specific SOs")]
    [SerializeField] private WolfHowlSO EnemyHowlBase;
    [SerializeField] private WolfChaseSO EnemyChaseBase;
    [SerializeField] private WolfIdleSO EnemyIdleBase;
    [SerializeField] private WolfAttackSO EnemyAttackBase;

    public WolfHowlSO EnemyHowlBaseInstance { get; set; }
    public WolfChaseSO EnemyChaseBaseInstance { get; set; }
    public WolfIdleSO EnemyIdleBaseInstance { get; set; }
    public WolfAttackSO EnemyAttackBaseInstance { get; set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        EnemyHowlBaseInstance = Instantiate(EnemyHowlBase);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseBase);
        EnemyIdleBaseInstance = Instantiate(EnemyIdleBase);
        EnemyAttackBaseInstance = Instantiate(EnemyAttackBase);

        IdleState = new WolfIdleState(this, StateMachine);
        HowlState = new WolfHowlState(this, StateMachine);
        ChaseState = new WolfChaseState(this, StateMachine);
        AttackState = new WolfAttackState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        EnemyIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyChaseBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyHowlBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyAttackBaseInstance.Initialize(gameObject, this, PlayerTransform);

        StateMachine.Initialize(IdleState);
    }
}
