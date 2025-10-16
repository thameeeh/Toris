using System;
using UnityEngine;

public abstract class Enemy : MonoBehaviour, IDamageable, IEnemyMoveable, ITriggerCheckable
{
    //---- Shared Interfaces -------------
    [field: SerializeField] public float MaxHealth { get; set; } = 100f;
    public float CurrentHealth { get; set; }
    public bool IsFacingRight { get; set; } = true;
    [field: SerializeField] public Rigidbody2D rb { get; set; }
    public bool IsAggroed { get; set; }
    public bool IsWithinStrikingDistance { get; set; }

    //--------------------------------
    [SerializeField] private Transform playerTransform;
    public Transform PlayerTransform => playerTransform;

    public Animator animator { get; set; }
    public EnemyStateMachine StateMachine { get; set; }

    private GameObject _player;
    private PlayerDamageReceiver _playerDamageReceiver;
    protected HitData _hitData;
    protected virtual void Awake()
    {
        StateMachine = new EnemyStateMachine();
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError("Animator component is missing on the enemy.");
    }
    protected virtual void Start()
    {
        CurrentHealth = MaxHealth;
        if (rb == null)
        {
            rb.GetComponent<Rigidbody2D>();
        }
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player == null) Debug.Log("_player Null");
        _playerDamageReceiver = _player.GetComponent<PlayerDamageReceiver>();
        if (!playerTransform)
            playerTransform = _player.transform;
    }

    protected virtual void Update()
    {
        StateMachine.CurrentEnemyState?.FrameUpdate();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentEnemyState?.PhysicsUpdate();
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
        Debug.Log("Dead");
    }

    #endregion

    #region Movement functions
    //bool is for animation blend trres, when velocity is (0, 0)
    //blend trees does not work correctly with 4 direction sprite animation
    public void MoveEnemy(Vector2 velocity, bool t = true)
    {
        rb.linearVelocity = velocity;
        if(t)UpdateAnimationDirection(velocity);
    }
    //direction is updated by default
    //only in cases when explicitly told it will skip this step
    public void UpdateAnimationDirection(Vector2 direction)
    {
        direction = direction.normalized;
        animator.SetFloat("DirectionX", direction.x);
        animator.SetFloat("DirectionY", direction.y);
    }

    #endregion

    #region Distance Checks
    //those two are set by enemy children trigger_check scripts
    //also children have colliders set as triggers for those checks
    public void SetAggroStatus(bool isAggroed)
    {
        IsAggroed = isAggroed;
    }
    public void SetStrikingDistanceBool(bool isWithinStrikingDistance)
    {
        IsWithinStrikingDistance = isWithinStrikingDistance;
    }
    #endregion

    public void DealDamageToPlayer(int amount) 
    {
        _hitData.damage = amount;
        _playerDamageReceiver.ReceiveHit(_hitData);
    }

    #region Animation
    public void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        StateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
    }

    public enum AnimationTriggerType //test
    {
        EnemyDamaged,
        PlayFootstepSound,
        Attack,
        Chase,
        Idle,
        Howl
    }

    #endregion
}
