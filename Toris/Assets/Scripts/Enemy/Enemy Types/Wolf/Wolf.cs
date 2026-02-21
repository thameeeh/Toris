using System;
using UnityEngine;
using System.Collections;
using TMPro;
using OutlandHaven.UIToolkit;

// All States and ScriptableObjects specific to the Wolf enemy
// are defined and instantiated here

// enum for wolf roles
public enum WolfRole { Leader, Minion }

public class Wolf : Enemy
{
    [Space][Space][Header("Stats")]
    public float AttackDamage = 20;
    public float MovementSpeed = 2;

    [SerializeField]
    private PlayerDataSO PlayerData;

    // leader/minion implement
    [Header("Role")]
    public WolfRole role = WolfRole.Minion;
    [Range(0.5f, 3f)] public float healthMultiplier = 1f;
    public bool CanHowl => role == WolfRole.Leader;

    [Header("Leader Pack")]
    public PackController pack;

    private HitData _hitData;
    private float _baseAttackDamage;
    private bool _hasStarted;

    public bool IsMovingWhileBiting { get; set; } = false;
    public bool IsChasingPlayer { get; private set; }
    public void SetChasingPlayer(bool chasingP) => IsChasingPlayer = chasingP;
    public void PrintMessage(string msg) 
    {
        Debug.Log(msg);
    }


    #region Wolf-Specific States
    public WolfHowlState HowlState { get; set; }
    public WolfChaseState ChaseState { get; set; }
    public WolfIdleState IdleState { get; set; }
    public WolfAttackState AttackState { get; set; }
    public WolfDeadState DeadState { get; set; }
    #endregion

    #region Wolf-Specific ScriptableObjects
    [Space][Space][Header("Wolf-Specific SOs")]
    [SerializeField] private WolfHowlSO EnemyHowlBase;
    [SerializeField] private WolfChaseSO EnemyChaseBase;
    [SerializeField] private WolfIdleSO EnemyIdleBase;
    [SerializeField] private WolfAttackSO EnemyAttackBase;
    [SerializeField] private WolfDeadSO EnemyDeadBase;

    public WolfHowlSO EnemyHowlBaseInstance { get; set; }
    public WolfChaseSO EnemyChaseBaseInstance { get; set; }
    public WolfIdleSO EnemyIdleBaseInstance { get; set; }
    public WolfAttackSO EnemyAttackBaseInstance { get; set; }
    public WolfDeadSO EnemyDeadBaseInstance { get; set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();

        _baseAttackDamage = AttackDamage;

        EnemyHowlBaseInstance = Instantiate(EnemyHowlBase);
        EnemyChaseBaseInstance = Instantiate(EnemyChaseBase);
        EnemyIdleBaseInstance = Instantiate(EnemyIdleBase);
        EnemyAttackBaseInstance = Instantiate(EnemyAttackBase);
        EnemyDeadBaseInstance = Instantiate(EnemyDeadBase);

        IdleState = new WolfIdleState(this, StateMachine);
        HowlState = new WolfHowlState(this, StateMachine);
        ChaseState = new WolfChaseState(this, StateMachine);
        AttackState = new WolfAttackState(this, StateMachine);
        DeadState = new WolfDeadState(this, StateMachine);
    }

    protected override void Start()
    {
        base.Start();

        EnemyIdleBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyChaseBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyHowlBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyAttackBaseInstance.Initialize(gameObject, this, PlayerTransform);
        EnemyDeadBaseInstance.Initialize(gameObject, this, PlayerTransform);

        ApplyScaling();
        InitializeRuntimeState();

        _hasStarted = true;   
    }

    protected override void Update()
    {
        base.Update();

        if(CurrentHealth <= 0 && StateMachine.CurrentEnemyState != DeadState)
        {
            Die();
            StateMachine.ChangeState(DeadState);
        }
    }
    public override void OnSpawned()
    {
        ApplyScaling();
        base.OnSpawned();

        if (!_hasStarted)
            return;

        InitializeRuntimeState();
    }

    private float GetDifficultyMultiplier()
    {
        return 1f + (0.2f * DifficultyTier);
    }

    private void ApplyScaling()
    {
        MaxHealth = Mathf.RoundToInt(Mathf.Max(1f, MaxHealth) * healthMultiplier);
        AttackDamage = _baseAttackDamage * GetDifficultyMultiplier();
    }

    public void InitializeRuntimeState()
    {
        CurrentHealth = MaxHealth;
        _hitData = new HitData(Vector2.zero, Vector2.zero, AttackDamage, 1, gameObject);

        AlwaysAggroed = false;
        SetAggroStatus(false);

        StateMachine.Reset();
        StateMachine.Initialize(IdleState);
    }
    public void DestroyGameObject()
    {
        RequestDespawn();
    }

    public void DamagePlayer(float damage) {
        base.DamagePlayer(damage, _hitData);
        PlayerData.ModifyHealth(-20);
    }
}
