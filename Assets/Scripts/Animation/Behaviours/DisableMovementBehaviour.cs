using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Behaviour used to disable the player ability to move during an animation state
 */
public class DisableMovementBehaviour : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ThirdPersonController controller = animator.gameObject.transform.parent.GetComponent<ThirdPersonController>();
        if (controller)
        {
            controller.EnableMovement(false);
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ThirdPersonController controller = animator.gameObject.transform.parent.GetComponent<ThirdPersonController>();
        if (controller)
        {
            controller.EnableMovement(true);
        }
    }
}
