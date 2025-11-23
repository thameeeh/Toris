using System;
using UnityEngine;

public abstract class Enemy : MonoBehaviour, IDamageable, IEnemyMoveable, ITriggerCheckable
{
    //temporary for testing
    private bool _isAggroed;
    public bool IsAggroed
    {
        get => _isAggroed;
        set
        {
            if (_isAggroed == value) return;
            _isAggroed = value;
            AggroStatusChanged?.Invoke(_isAggroed);
        }
    }
    public event Action<bool> AggroStatusChanged;

    //---- Shared Interfaces -------------
    [field: SerializeField] public float MaxHealth { get; set; } = 100f;
    public float CurrentHealth { get; set; }
    public bool IsFacingRight { get; set; } = true;
    [field: SerializeField] public Rigidbody2D rb { get; set; }
    //public bool IsAggroed { get; set; }
    public bool AlwaysAggroed { get; set; }
    public bool IsWithinStrikingDistance { get; set; }

    //--------------------------------
    [SerializeField] private Transform playerTransform;
    public Transform PlayerTransform => playerTransform;

    public Animator animator { get; set; }
    public EnemyStateMachine StateMachine { get; set; }

    private GameObject _player;
    private PlayerDamageReceiver _playerDamageReceiver;
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
    public void MoveEnemy(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        if(velocity != Vector2.zero)UpdateAnimationDirection(velocity);
    }
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
    //public void SetAggroStatus(bool isAggroed)
    //{
    //    IsAggroed = isAggroed;

    //    if (AlwaysAggroed)
    //    {
    //        IsAggroed = true;
    //        return;
    //    }
    //}
    public void SetAggroStatus(bool isAggroed) => IsAggroed = isAggroed;
    public void SetStrikingDistanceBool(bool isWithinStrikingDistance)
    {
        IsWithinStrikingDistance = isWithinStrikingDistance;
    }
    #endregion

    public void DamagePlayer(float amount, HitData hitData) 
    {
        if (IsWithinStrikingDistance)
        {
            hitData.damage = amount;
            _playerDamageReceiver.ReceiveHit(hitData);
        }
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
