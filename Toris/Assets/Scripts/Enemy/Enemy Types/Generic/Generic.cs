using UnityEngine;

public class Generic : Enemy
{
    public GenericIdleState IdleState { get; set; }
    public GenericChaseState ChaseState { get; set; }
    public GenericAttackState AttackState { get; set; }

    [Header("Generic-Specific SOs")]
    [SerializeField] private GenericIdleSO EnemyIdleBase;
    [SerializeField] private GenericChaseSO EnemyChaseBase;
    [SerializeField] private GenericAttackSO EnemyAttackBase;

    public GenericIdleSO EnemyIdleBaseInstance { get; set; }
    public GenericChaseSO EnemyChaseBaseInstance { get; set; }
    public GenericAttackSO EnemyAttackBaseInstance { get; set; }

    protected override void Awake()
    {
        base.Awake();

        EnemyIdleBaseInstance = Instantiate(EnemyIdleBase);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseBase);
        EnemyAttackBaseInstance = Instantiate(EnemyAttackBase);

        IdleState = new GenericIdleState(this, StateMachine);
        ChaseState = new GenericChaseState(this, StateMachine);
        AttackState = new GenericAttackState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        EnemyIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyChaseBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyAttackBaseInstance.Initialize(gameObject, this, PlayerTransform);

        StateMachine.Initialize(IdleState);
    }
}
