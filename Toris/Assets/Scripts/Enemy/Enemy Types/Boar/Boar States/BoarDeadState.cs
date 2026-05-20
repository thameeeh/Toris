public class BoarDeadState : EnemyState<Boar>
{
    public BoarDeadState(Boar enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.BoarDeadBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BoarDeadBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.BoarDeadBaseInstance == null)
        {
            enemy.DestroyBoar();
            return;
        }

        enemy.BoarDeadBaseInstance?.DoFrameUpdateLogic();
    }
}
