using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * As I can't found a satisfying landing in water animation on Mixamo, I used this tricks to play the correct sfx.
 * Ideally we shouldn't use this class and have an animation event on landing in water animation.
 */
public class LandingInWaterBehaviour : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AnimationEventHandler handler = animator.gameObject.GetComponent<AnimationEventHandler>();
        if (handler)
        {
            handler.OnLandonInWaterAnim(null);
        }
    }
}
