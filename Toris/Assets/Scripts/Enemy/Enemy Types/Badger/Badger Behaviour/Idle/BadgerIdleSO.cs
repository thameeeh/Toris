using UnityEngine;
using UnityEngine.EventSystems;

[CreateAssetMenu(fileName = "Badger_Idle", menuName = "Enemy Logic/Idle Logic/Badger Idle")]
public class BadgerIdleSO : IdleSOBase<Badger>
{
    [SerializeField] private float IdleTimer = 3f;

    private float _timer;
    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.MoveEnemy(Vector2.zero);
        enemy.isBurrowed = false;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
        _timer += Time.fixedDeltaTime;

        if (!enemy.isWondering)
        {
            if (_timer >= IdleTimer)
            {
                enemy.isWondering = true;
            }
        }

        enemy.ForcedIdleCalclulation(Time.fixedDeltaTime);
    }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();
        _timer = 0f;
    }
}