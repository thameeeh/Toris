using UnityEngine;

[CreateAssetMenu(fileName = "Wolf Attack", menuName = "Enemy Logic/Attack Logic/Wolf Attack")]
public class WolfAttack : EnemyAttackSOBase
{
    [SerializeField] private float _timeBetweenBites = 2f;
    [SerializeField] private float _timeTillExit = 1f;

    private float _timer;
    private float _exitTimer;
    public override void Initialize(GameObject gameObject, Enemy enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }
  

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (!enemy.IsWithinStrikingDistance) 
        {
            _exitTimer += Time.deltaTime;
            if (_exitTimer > _timeTillExit)
                enemy.StateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void ResetValues()
    {
        base.ResetValues();

        Debug.Log("Values have been reset");

        _timer = 0f;
        _exitTimer = 0f;
    }
    
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
        animator.Play("Attack_NW");
    }
}
