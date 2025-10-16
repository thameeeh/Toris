using UnityEngine;

public class Attack : StateMachineBehaviour
{
    Wolf _wolf;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_wolf == null)
        {
            _wolf = animator.GetComponentInParent<Wolf>();

            // --- ADD THIS CHECK! ---
            if (_wolf == null)
            {
                Debug.LogError("Attack StateMachineBehaviour could not find the Wolf script on any parent!", animator.gameObject);
            }
            else
            {
                Debug.Log("Successfully found the Wolf script!", animator.gameObject);
            }
        }
        _wolf.IsAttackAnimationEnded = false;
        _wolf.PrintMessage("Attack animation Started.");
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _wolf.IsAttackAnimationEnded = true;
        _wolf.PrintMessage("Attack animation Started.");
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
