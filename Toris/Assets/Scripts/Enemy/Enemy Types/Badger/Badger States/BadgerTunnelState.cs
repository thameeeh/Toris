public class BadgerTunnelState : EnemyState<Badger>
{
    // Badger is on pause until a later ground-up rework.
    public BadgerTunnelState(Badger enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }
}
