using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Howl_Alert", menuName = "Enemy Logic/Howl Logic/Wolf Howl Alert")]
public class WolfHowlSO : HowlSOBase<Wolf>
{
    [SerializeField] private float howlDuration = 1f;

    public bool isComplete { get; private set; }
    private float _timer;
    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        isComplete = false;
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

        _timer += Time.deltaTime;
        if (_timer > howlDuration) 
            isComplete = true;
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();

        isComplete = false;
        _timer = 0f;
    }
}
