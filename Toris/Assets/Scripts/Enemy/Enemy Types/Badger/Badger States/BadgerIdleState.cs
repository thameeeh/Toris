public class BadgerIdleState : EnemyState<Badger>
{
    // Badger is on pause until a later ground-up rework.
    public BadgerIdleState(Badger enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }
}
