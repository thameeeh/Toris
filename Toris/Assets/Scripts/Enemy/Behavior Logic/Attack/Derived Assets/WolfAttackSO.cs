using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Attack_QuickBite", menuName = "Enemy Logic/Attack Logic/Wolf Attack QuickBite")]
public class WolfAttackSO : AttackSOBase<Wolf>
{
    [SerializeField] private float _timeBetweenBites = 2f;
    [SerializeField] private float _timeTillExit = 1f;

    private float _timer;
    private float _exitTimer;

    private Vector2 _animationDirection = Vector2.zero;
    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
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

        enemy.MoveEnemy(Vector2.zero, false);

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

        _animationDirection = enemy.PlayerTransform.position - enemy.transform.position;
        enemy.UpdateAnimationDirection(_animationDirection.normalized);
    }

    public override void ResetValues()
    {
        base.ResetValues();

        Debug.Log("Values have been reset");

        _timer = 0f;
        _exitTimer = 0f;
    }
    
    public override void DoAnimationTriggerEventLogic(Wolf.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }
}
