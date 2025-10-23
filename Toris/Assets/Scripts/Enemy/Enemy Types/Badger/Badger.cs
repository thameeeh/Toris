using UnityEngine;

public class Badger : Enemy
{

    [Space][Header("Stats")]
    public float AttackDamage = 20f;
    public float WalkingSpeed = 1f;
    public float BurrowSpeed = 3f;

    private HitData _hitData;

    public Vector2 TargetPlayerPosition { get; set; }
    public bool IsBurrowing { get; private set; } = false;
    public bool IsWondering { get; private set; } = false;

    public void PrintMessage(string msg)
    {
        Debug.Log(msg);
    }

    #region Badger-Specific States
    public BadgerAttackState AttackState { get; set; }
    public BadgerIdleState IdleState { get; set; }
    public BadgerBurrowState BurrowState { get; set; }
    public BadgerDeadState DeadState { get; set; }
    #endregion

    #region Badger-Specific ScriptableObjects
    [Space][Space][Header("Badger-Specific SOs")]
    [SerializeField] private BadgerIdleSO BadgerIdleBase;
    [SerializeField] private BadgerAttackSO BadgerAttackBase;
    [SerializeField] private BadgerBurrowSO BadgerBurrowBase;
    [SerializeField] private BadgerDeadSO BadgerDeadBase;

    public BadgerIdleSO BadgerIdleBaseInstance { get; set; }
    public BadgerAttackSO BadgerAttackBaseInstance { get; set; }
    public BadgerBurrowSO BadgerBurrowBaseInstance { get; set; }
    public BadgerDeadSO BadgerDeadBaseInstance { get; set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        BadgerIdleBaseInstance = Instantiate(BadgerIdleBase);
        BadgerAttackBaseInstance = Instantiate(BadgerAttackBase);
        BadgerBurrowBaseInstance = Instantiate(BadgerBurrowBase);
        BadgerDeadBaseInstance = Instantiate(BadgerDeadBase);

        IdleState = new BadgerIdleState(this, StateMachine);
        AttackState = new BadgerAttackState(this, StateMachine);
        BurrowState = new BadgerBurrowState(this, StateMachine);
        DeadState = new BadgerDeadState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();
        
        BadgerIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        BadgerAttackBaseInstance.Initialize(gameObject, this, PlayerTransform);
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

        if (IsWondering)
        {
            StateMachine.ChangeState(IdleState);
            animator.SetBool("IsWondering", true);
        }
    }
    public void IsCurrentlyBurrowing(bool t) 
    {
        if(t) 
        {
            IsBurrowing = true;
        }
        else 
        {
            IsBurrowing = false;
        }
    }

    public void IsCurrentlyWondering(bool t) 
    {
        IsWondering = t;
    }
    public void DamagePlayer(float damage)
    {
        base.DamagePlayer(damage, _hitData);
    }
}
