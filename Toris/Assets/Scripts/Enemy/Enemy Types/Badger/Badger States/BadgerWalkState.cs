public class BadgerWalkState : EnemyState<Badger>
{
    // Badger is on pause until a later ground-up rework.
    public BadgerWalkState(Badger enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }
}
