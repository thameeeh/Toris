using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
        if (_enemy == null)
            Debug.LogError($"[EnemyAnimationEventRelay] No Enemy found in parents of {name}");
    }

    public void Anim_AttackHit()
    {
        _enemy?.AnimationTriggerEvent(Enemy.AnimationTriggerType.Attack);
    }

    public void Anim_AttackFinished()
    {
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

    // necessary to change if needed for each enemy
    // follow same logic if (_enemy is X x) { }
    public void Anim_SetMoveWhileAttacking(int enabled)
    {
        if (_enemy is Wolf wolf)
            wolf.IsMovingWhileBiting = (enabled == 1);
    }
}
