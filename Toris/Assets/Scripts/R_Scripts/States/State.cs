using UnityEngine;

public abstract class State : MonoBehaviour
{
    protected Animator animator;
    protected PlayerMovement input;

    public bool isComplete { get; protected set; }

    protected float startTime;

    float time => Time.time - startTime;



    public virtual void Enter() { }
    public virtual void Do() { }
    public virtual void FixedDo() { }
    public virtual void Exit() { }

    public void Setup(Animator animator, PlayerMovement input)
    {
        this.animator = animator;
        this.input = input;
    }
}
