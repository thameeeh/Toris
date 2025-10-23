using UnityEngine;

public class EnemyStateMachine
{
    public IEnemyState CurrentEnemyState { get; set; }
    public void Initialize(IEnemyState startingState)
    {
        CurrentEnemyState = startingState;
        CurrentEnemyState.EnterState();
    }

    public void ChangeState(IEnemyState newState)
    {
        CurrentEnemyState.ExitState();
        CurrentEnemyState = newState;
        CurrentEnemyState.EnterState();
    }
}
