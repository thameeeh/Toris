using UnityEngine;

public class RunStateAnimal : StateAnimal
{
    public override void Enter()
    {
        isComplete = false;
        _currentDirection = _isSouth;

        //play animation based on direction
        if (_isSouth) animator.Play("Run_S");
        else          animator.Play("Run_N");
    }
    public override void Do()
    {
        if (!input.IsRunning)
        {
            isComplete = true;
        }

        //to avoid restarting the animation every frame
        if (_currentDirection != _isSouth)
        {
            _currentDirection = _isSouth;
            if (_isSouth) animator.Play("Run_S");
            else          animator.Play("Run_N");
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
    }
}
