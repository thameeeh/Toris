public class BadgerUnburrowState : EnemyState<Badger>
{
    // Badger is on pause until a later ground-up rework.
    public BadgerUnburrowState(Badger enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }
}
