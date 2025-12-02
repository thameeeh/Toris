using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Attack_QuickBite", menuName = "Enemy Logic/Attack Logic/Wolf Attack QuickBite")]
public class WolfAttackSO : AttackSOBase<Wolf>
{
    private Vector2 _animationDirection = Vector2.zero;
    private int _attackTagHash = Animator.StringToHash("AttackAnimations");
    public bool _isAttackAnimationFinished { get; set; } = false;

    private GridPathAgent _pathAgent;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        _pathAgent = enemy.GetComponent<GridPathAgent>();
        if (_pathAgent == null)
        {
            Debug.LogWarning($"[WolfAttackSO] No GridPathAgent on {enemy.name}. Attack lunge will not use pathfinding.");
        }
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        _isAttackAnimationFinished = false;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (!enemy.IsMovingWhileBiting)
        {
            enemy.MoveEnemy(Vector2.zero);
            return;
        }

        Vector2 moveDirection = Vector2.zero;

        if (_pathAgent != null && TileNavWorld.Instance != null)
        {
            moveDirection = _pathAgent.GetMoveDirection(playerTransform.position);
        }

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            enemy.MoveEnemy(moveDirection * enemy.MovementSpeed);
        }
        else
        {
            enemy.MoveEnemy(Vector2.zero);
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (_isAttackAnimationFinished) return;

        AnimatorStateInfo stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.tagHash == _attackTagHash && stateInfo.normalizedTime >= 1.0f)
        {
            _isAttackAnimationFinished = true;
            enemy.animator.ResetTrigger("Attack");
        }

        _animationDirection = enemy.PlayerTransform.position - enemy.transform.position;
        if (_animationDirection.sqrMagnitude > 0.0001f)
        {
            enemy.UpdateAnimationDirection(_animationDirection.normalized);
        }
    }

    public override void ResetValues()
    {
        base.ResetValues();

        //Debug.Log("Values have been reset");
    }

    public override void DoAnimationTriggerEventLogic(Wolf.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }
}
