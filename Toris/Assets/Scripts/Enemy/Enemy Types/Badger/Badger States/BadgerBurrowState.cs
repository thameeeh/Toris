public class BadgerBurrowState : EnemyState<Badger>
{
    // Badger is on pause until a later ground-up rework.
    public BadgerBurrowState(Badger enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }
}
