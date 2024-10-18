using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A class to manage the link to the animator component.
 */
[Serializable]
public class PlayerAnimationData
{
    // The animator component.
    [SerializeField] private Animator animator;

    // animation parameters as named in the animator. Will be used to generate the ids.

    [SerializeField] private string moveSpeedParameterName = "MoveSpeed";
    [SerializeField] private string jumpTriggerParameterName = "Jump";
    [SerializeField] private string isFallingParameterName = "IsFalling";
    [SerializeField] private string hardLandTriggerParameterName = "HardLand";
    [SerializeField] private string isSwimmingParameterName = "isSwimming";

    // animation IDs
    public int animIDMoveSpeed { get; private set; }
    public int animIDJump { get; private set; }
    public int animIDIsFalling { get; private set; }
    public int animIDHardLand { get; private set; }
    public int animIDIsSwimming { get; private set; }

    // Generated the differents parameter id from the strings 
    public void AssignAnimationIDs()
    {
        animIDMoveSpeed = Animator.StringToHash(moveSpeedParameterName);
        animIDJump = Animator.StringToHash(jumpTriggerParameterName);
        animIDIsFalling = Animator.StringToHash(isFallingParameterName);
        animIDHardLand = Animator.StringToHash(hardLandTriggerParameterName);
        animIDIsSwimming = Animator.StringToHash(isSwimmingParameterName);
    }


    // Helper functions who contains the validity check of the animator

    public void SetFloat(int id, float value)
    {
        if (animator)
        {
            animator.SetFloat(id, value);
        }
    }

    public void SetBool(int id, bool value)
    {
        if (animator)
        {
            animator.SetBool(id, value);
        }
    }

    public void SetTrigger(int id)
    {
        if (animator)
        {
            animator.SetTrigger(id);
        }
    }
}
