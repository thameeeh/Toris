using UnityEngine;

// All States and ScriptableObjects specific to the Wolf enemy
// are defined and instantiated here

// 
public class Wolf : Enemy
{
    public HowlState HowlState { get; set; }
    public ChaseState ChaseState { get; set; }
    public IdleState IdleState { get; set; }

    [SerializeField] private HowlSOBase EnemyHowlBase;
    [SerializeField] private EnemyChaseSOBase EnemyChaseBase;
    [SerializeField] private EnemyIdleSOBase EnemyIdleBase;
    public HowlSOBase EnemyHowlBaseInstance { get; set; }
    public EnemyChaseSOBase EnemyChaseBaseInstance { get; set; }
    public EnemyIdleSOBase EnemyIdleBaseInstance { get; set; }

    protected override void Awake()
    {
        base.Awake();

        EnemyHowlBaseInstance = Instantiate(EnemyHowlBase);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseBase);
        EnemyIdleBaseInstance = Instantiate(EnemyIdleBase);

        IdleState = new IdleState(this, StateMachine);
        HowlState = new HowlState(this, StateMachine);
        ChaseState = new ChaseState(this, StateMachine);
    }
}
