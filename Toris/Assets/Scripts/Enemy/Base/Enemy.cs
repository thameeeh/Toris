using System;
using System.Collections.Generic;
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

    // pooling
    public Enemy OriginalPrefab { get; private set; }
    public IEnemyPool OwningPool { get; private set; }

    public EnemyLoadout ActiveLoadout { get; private set; }
    public Transform SpawnPoint { get; private set; }
    public string FactionId { get; private set; } = string.Empty;
    public int DifficultyTier { get; private set; }

    private readonly List<IStatusEffect> _statusEffects = new List<IStatusEffect>();
    private bool _isReleasing;
    private float _baseMaxHealth;
    private Collider2D[] _cachedColliders = Array.Empty<Collider2D>();
    private bool[] _cachedColliderEnabledStates = Array.Empty<bool>();
    private bool _collidersDisabledForDeath;

    public event Action<Enemy> Died;
    public event Action<Enemy> Despawned;
    public event Action<float> Damaged; // for sfx

    private GameObject _player;
    private PlayerDamageReceiver _playerDamageReceiver;
    protected virtual void Awake()
    {
        StateMachine = new EnemyStateMachine();
        animator = GetComponentInChildren<Animator>();
        CacheOwnedColliders();

        _baseMaxHealth = MaxHealth;
        
        if (animator == null)
            Debug.LogError("Animator component is missing on the enemy.");
    }
    protected virtual void Start()
    {
        CurrentHealth = MaxHealth;
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        _player = GameObject.FindGameObjectWithTag("Player");
#if UNITY_EDITOR
        if (_player == null) Debug.Log("_player Null");
#endif
        if (_player != null)
        {
            _player.TryGetComponent(out _playerDamageReceiver);
            if (ShouldBindScenePlayerTransform(playerTransform))
                playerTransform = _player.transform;
        }
    }

    private static bool ShouldBindScenePlayerTransform(Transform currentPlayerTransform)
    {
        return currentPlayerTransform == null || !currentPlayerTransform.gameObject.scene.IsValid();
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
        if (!CanTakeDamage()) return;

        CurrentHealth -= damageAmount;

        Damaged?.Invoke(damageAmount);
        //Debug.Log($"Health left: {CurrentHealth}");
        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    protected virtual bool CanTakeDamage() => CurrentHealth > 0f;

    public virtual void Die()
    {
        //Debug.Log("Dead");
        if (CurrentHealth > 0f) return;
        DisableCollidersForDeath();
        Died?.Invoke(this);
    }

    #endregion

    #region Movement functions
    public void MoveEnemy(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        if(velocity != Vector2.zero) UpdateAnimationDirection(velocity);
    }
    public virtual void UpdateAnimationDirection(Vector2 direction)
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

    public enum AnimationTriggerType
    {
        EnemyDamaged,
        PlayFootstepSound,
        Attack,
        AttackFinished,
        Chase,
        Idle,
        Howl
    }

    #endregion

    #region Pooling
    public virtual void SetPool(IEnemyPool pool, Enemy prefabRef)
    {
        OwningPool = pool;
        OriginalPrefab = prefabRef;
    }

    public virtual void PrepareSpawn(EnemySpawnRequest request)
    {
        SpawnPoint = request.SpawnPoint;
        FactionId = string.IsNullOrEmpty(request.FactionId) ? string.Empty : request.FactionId;
        DifficultyTier = Mathf.Max(0, request.DifficultyTier);
        ActiveLoadout = request.Loadout;

        MaxHealth = _baseMaxHealth;

        if (ActiveLoadout != null)
        {
            ActiveLoadout.Apply(this);
        }
    }

    public virtual void OnSpawned()
    {
        _isReleasing = false;
        RestoreCachedColliderStates();
        CurrentHealth = MaxHealth;
        IsAggroed = false;
        IsWithinStrikingDistance = false;
        AlwaysAggroed = false;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        StateMachine.Reset();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        ClearStatusEffects();
    }

    public virtual void OnDespawned()
    {
        ClearStatusEffects();
        AggroStatusChanged = null;
        Despawned?.Invoke(this);
        Despawned = null;
        Died = null;
        _isReleasing = false;
        StateMachine.Reset();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        IsAggroed = false;
        IsWithinStrikingDistance = false;
        AlwaysAggroed = false;
        CurrentHealth = MaxHealth;
        ActiveLoadout = null;
    }

    public void RegisterStatusEffect(IStatusEffect effect)
    {
        if (effect == null || _statusEffects.Contains(effect)) return;
        _statusEffects.Add(effect);
    }

    protected void ClearStatusEffects()
    {
        for (int i = 0; i < _statusEffects.Count; i++)
        {
            _statusEffects[i]?.OnRemoved();
        }
        _statusEffects.Clear();
    }

    public void RequestDespawn()
    {
        if (_isReleasing) return;

        _isReleasing = true;

        if (OwningPool != null)
        {
            OwningPool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CacheOwnedColliders()
    {
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);
        _cachedColliderEnabledStates = new bool[_cachedColliders.Length];

        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            Collider2D collider = _cachedColliders[i];
            _cachedColliderEnabledStates[i] = collider != null && collider.enabled;
        }
    }

    protected void DisableCollidersForDeath()
    {
        if (_collidersDisabledForDeath)
            return;

        if (_cachedColliders == null || _cachedColliders.Length == 0)
            CacheOwnedColliders();

        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            Collider2D collider = _cachedColliders[i];
            if (collider == null)
                continue;

            collider.enabled = false;
        }

        _collidersDisabledForDeath = true;
        IsAggroed = false;
        IsWithinStrikingDistance = false;
        AlwaysAggroed = false;
    }

    private void RestoreCachedColliderStates()
    {
        if (_cachedColliders == null || _cachedColliders.Length == 0)
            CacheOwnedColliders();

        int colliderCount = Mathf.Min(_cachedColliders.Length, _cachedColliderEnabledStates.Length);
        for (int i = 0; i < colliderCount; i++)
        {
            Collider2D collider = _cachedColliders[i];
            if (collider == null)
                continue;

            collider.enabled = _cachedColliderEnabledStates[i];
        }

        _collidersDisabledForDeath = false;
    }

    #endregion
}
