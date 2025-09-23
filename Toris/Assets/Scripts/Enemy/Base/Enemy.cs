using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable, IEnemyMoveable, ITriggerCheckable
{
    [field: SerializeField] public float MaxHealth { get; set; } = 100f;
    public float CurrentHealth { get; set; }
    public bool IsFacingRight { get; set; } = true;
    [field: SerializeField] public Rigidbody2D rb { get; set; }

    [SerializeField] private Transform playerTransform;
    public Transform PlayerTransform => playerTransform;

    #region Aggro Check Variables
    public bool IsAggroed { get; set; }
    public bool IsWithinStrikingDistance { get; set; }

    #endregion

    #region State Machine Variables

    public EnemyStateMachine StateMachine { get; set; }
    public EnemyIdleState IdleState { get; set; }
    public EnemyChaseState ChaseState { get; set; }
    public EnemyAttackState AttackState { get; set; }

    #endregion

    #region ScriptableObject Variables

    [SerializeField] private EnemyIdleSOBase EnemyIdleBase;
    [SerializeField] private EnemyChaseSOBase EnemyChaseBase;
    [SerializeField] private EnemyAttackSOBase EnemyAttackBase;

    public EnemyIdleSOBase EnemyIdleBaseInstance { get; set; }
    public EnemyChaseSOBase EnemyChaseBaseInstance { get; set; }
    public EnemyAttackSOBase EnemyAttackBaseInstance { get; set; }

    #endregion

    private void Awake()
    {
        EnemyIdleBaseInstance = Instantiate(EnemyIdleBase);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseBase);
        EnemyAttackBaseInstance = Instantiate(EnemyAttackBase);

        StateMachine = new EnemyStateMachine();

        IdleState = new EnemyIdleState(this, StateMachine);
        ChaseState = new EnemyChaseState(this, StateMachine);
        AttackState = new EnemyAttackState(this, StateMachine);
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;

        rb.GetComponent<Rigidbody2D>();

        if (!playerTransform)
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        EnemyIdleBaseInstance.Initialize(gameObject, this, playerTransform);
        EnemyChaseBaseInstance.Initialize(gameObject, this, playerTransform);
        EnemyAttackBaseInstance.Initialize(gameObject, this, playerTransform);
        
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        StateMachine.CurrentEnemyState.FrameUpdate();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentEnemyState.PhysicsUpdate();
    }

    #region Health / Die Functions

    public void Damage(float damageAmount)
    {
        CurrentHealth -= damageAmount;
        Debug.Log($"Health left: {CurrentHealth}");
        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log("Die() called");
        Destroy(gameObject);
    }

    #endregion

    #region Movement functions
    public void MoveEnemy(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        CheckForLeftOrRightFacing(velocity);
    }

    public void CheckForLeftOrRightFacing(Vector2 velocity)
    {
        if (IsFacingRight && velocity.x < 0f)
        {
            Vector3 rotator = new Vector3(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            IsFacingRight = !IsFacingRight;
        }

        else if (!IsFacingRight && velocity.x > 0f)
        {
            Vector3 rotator = new Vector3(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            IsFacingRight = !IsFacingRight;
        }
    }

    #endregion

    #region Distance Checks
    public void SetAggroStatus(bool isAggroed)
    {
        IsAggroed = isAggroed;
    }
    public void SetStrikingDistanceBool(bool isWithinStrikingDistance)
    {
        IsWithinStrikingDistance = isWithinStrikingDistance;
    }
    #endregion

    #region Animation triggers

    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        StateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
    }

    public enum AnimationTriggerType //test
    {
        EnemyDamaged,
        PlayFootstepSound
    }

    #endregion
}
