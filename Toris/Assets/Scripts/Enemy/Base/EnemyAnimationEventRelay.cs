using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
        if (_enemy == null)
        {
#if UNITY_EDITOR
            Debug.LogError($"[EnemyAnimationEventRelay] No Enemy found in parents of {name}");
#endif
        }
    }

    public void Anim_AttackHit()
    {
#if UNITY_EDITOR
        if (_enemy != null)
            _enemy.DebugAttackLog($"Animation event Anim_AttackHit received by relay={name}");
        else
            Debug.LogWarning($"[EnemyAnimationEventRelay:{name}] Anim_AttackHit fired with no Enemy reference.", this);
#endif
        _enemy?.AnimationTriggerEvent(Enemy.AnimationTriggerType.Attack);
    }

    public void Anim_AttackFinished()
    {
#if UNITY_EDITOR
        if (_enemy != null)
            _enemy.DebugAttackLog($"Animation event Anim_AttackFinished received by relay={name}");
        else
            Debug.LogWarning($"[EnemyAnimationEventRelay:{name}] Anim_AttackFinished fired with no Enemy reference.", this);
#endif
        _enemy?.AnimationTriggerEvent(Enemy.AnimationTriggerType.AttackFinished);
    }

    public void Anim_Footstep()
    {
        _enemy?.AnimationTriggerEvent(Enemy.AnimationTriggerType.PlayFootstepSound);
    }

    public void Anim_Despawn()
    {
        _enemy?.RequestDespawn();
    }

    public void StartTunneling()
    {
        // Badger is on pause until a later ground-up rework.
    }

    public void ChangeStateToIdle()
    {
        // Badger is on pause until a later ground-up rework.
    }

    public void BadgerDealDamage()
    {
        // Badger is on pause until a later ground-up rework.
    }

    public void DestroyBadger()
    {
        // Badger is on pause until a later ground-up rework.
    }

    // necessary to change if needed for each enemy
    // follow same logic if (_enemy is X x) { }
    public void Anim_SetMoveWhileAttacking(int enabled)
    {
#if UNITY_EDITOR
        if (_enemy != null)
            _enemy.DebugAttackLog($"Animation event Anim_SetMoveWhileAttacking({enabled}) received by relay={name}");
        else
            Debug.LogWarning($"[EnemyAnimationEventRelay:{name}] Anim_SetMoveWhileAttacking fired with no Enemy reference.", this);
#endif
        if (_enemy is Wolf wolf && wolf.StateMachine?.CurrentEnemyState == wolf.AttackState)
            wolf.IsMovingWhileBiting = enabled == 1;
    }
}
