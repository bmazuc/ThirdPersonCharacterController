using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerAnimationData
{
    public Animator animator;

    public string moveSpeedParameterName = "MoveSpeed";
    public string jumpTriggerParameterName = "Jump";
    public string groundedParameterName = "Grounded";
    public string hardLandParameterName = "isHardLanding";
    public string isInWaterParameterName = "isInWater";

    // animation IDs
    public int animIDMoveSpeed { get; private set; }
    public int animIDJump { get; private set; }
    public int animIDGrounded { get; private set; }
    public int animIDHardLand { get; private set; }
    public int animIDIsInWater { get; private set; }

    public void AssignAnimationIDs()
    {
        animIDMoveSpeed = Animator.StringToHash(moveSpeedParameterName);
        animIDJump = Animator.StringToHash(jumpTriggerParameterName);
        animIDGrounded = Animator.StringToHash(groundedParameterName);
        animIDHardLand = Animator.StringToHash(hardLandParameterName);
        animIDIsInWater = Animator.StringToHash(isInWaterParameterName);
    }

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
