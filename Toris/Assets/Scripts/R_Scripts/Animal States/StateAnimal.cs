using UnityEngine;

public abstract class StateAnimal : MonoBehaviour
{
    protected Animator animator;
    protected AnimalBehaviour input;

    protected bool _isSouth = false; //to change animation direction
    protected bool _currentDirection = false; //to track current direction and avoid restarting animation every frame
    public bool isComplete { get; protected set; }

    protected float startTime;

    float time => Time.time - startTime;



    public virtual void Enter() { }
    public virtual void Do() { }
    public virtual void FixedDo() { }
    public virtual void Exit() { }

    public void Setup(Animator animator, AnimalBehaviour input)
    {
        this.animator = animator;
        this.input = input;
    }
    public void AnimationDirection(Vector2 movementVector)
    {
        if (movementVector != Vector2.zero)
        {
            if (movementVector.y <= 0) _isSouth = true;
            else _isSouth = false;

            input._spriteRenderer.flipX = movementVector.x < 0;
        }
    }
}
