using UnityEngine;

[CreateAssetMenu(fileName = "Deer_Idle", menuName = "Outland Haven/Enemy/Behaviors/Idle Logic/Deer Idle")]
public class DeerIdleSO : IdleSOBase<Deer>
{
    [SerializeField, Min(0f)] private float idleDurationMin = 1.5f;
    [SerializeField, Min(0f)] private float idleDurationMax = 3.5f;

    private float idleUntilTime;

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.MoveEnemy(Vector2.zero);
        enemy.PlayIdleAnimation();
        idleUntilTime = Time.time + Random.Range(idleDurationMin, Mathf.Max(idleDurationMin, idleDurationMax));
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (enemy.IsAggroed)
        {
            enemy.StateMachine.ChangeState(enemy.RunAwayState);
            return;
        }

        if (Time.time >= idleUntilTime)
            enemy.StateMachine.ChangeState(enemy.WalkState);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        idleDurationMin = Mathf.Max(0f, idleDurationMin);
        idleDurationMax = Mathf.Max(idleDurationMin, idleDurationMax);
    }
#endif
}
