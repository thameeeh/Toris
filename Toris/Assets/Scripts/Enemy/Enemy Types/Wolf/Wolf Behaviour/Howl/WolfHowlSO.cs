using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Howl_Alert", menuName = "Enemy Logic/Howl Logic/Wolf Howl Alert")]
public class WolfHowlSO : HowlSOBase<Wolf>
{
    private bool _isRunning = false;
    private float _timer;

    public float howlDuration = 1f;
    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        bool hasPack = enemy.pack != null;
        bool isLeader = hasPack && enemy.pack.EnsureLeader(enemy);

        if (!enemy.CanHowl || !hasPack || !isLeader || !enemy.pack.CanLeaderHowl(enemy))
        {
            enemy.animator.ResetTrigger("Howl");
            enemy.animator.ResetTrigger("Attack");
            enemy.animator.ResetTrigger("Dead");
            enemy.StateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        _isRunning = true;
        _timer = 0f;
        enemy.animator.ResetTrigger("Attack");
        enemy.animator.ResetTrigger("Dead");
        enemy.animator.SetTrigger("Howl");
        enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (!_isRunning) return;

        _timer += Time.deltaTime;
        if (_timer >= howlDuration)
        {
            enemy.pack.HandleLeaderHowl(enemy);
            _isRunning = false;

            enemy.StateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void DoAnimationTriggerEventLogic(Wolf.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }
}
