public class BadgerDeadState : EnemyState<Badger>
{
    // Badger is on pause until a later ground-up rework.
    public BadgerDeadState(Badger enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }
}
