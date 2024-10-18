using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class contains all the methods called in character animation event.
 */
public class AnimationEventHandler : MonoBehaviour
{
    // Footstep clip and volume
    [Header("Footstep sounds")]
    [SerializeField] private AudioClip footstepAudioClip;
    [SerializeField] private float footstepAudioVolume;

    // Swim clip and volume
    [Header("Swim sounds")]
    [SerializeField] private AudioClip swimAudioClip;
    [SerializeField] private float swimAudioVolume;

    // landing on ground clip and volume
    [Header("Landing on ground sounds")]
    [SerializeField] private AudioClip landingOnGroundAudioClip;
    [SerializeField] private float landingOnGroundAudioVolume;

    // landing in water clip and volume
    [Header("Landing in water sounds")]
    [SerializeField] private AudioClip landingInWaterAudioClip;
    [SerializeField] private float landingInWaterAudioVolume;

    // landing on ground from a too high place clip and volume
    [Header("Hard landing")]
    [SerializeField] private AudioClip hardLandingAudioClip;
    [SerializeField] private float hardLandingAudioVolume;

    // jump grunts clips and volume. Clips will be played randomly.
    [Header("Jump grunts")]
    [SerializeField] private AudioClip[] jumpGruntAudioClips;
    [SerializeField] private float jumpGruntAudioVolume;


    // The bones used to play audio clips at bones location.
    [Header("Bones")]
    [SerializeField] private GameObject leftHandBone;
    [SerializeField] private GameObject rightHandBone;
    [SerializeField] private GameObject leftFootBone;
    [SerializeField] private GameObject rightFootBone;
    [SerializeField] private GameObject headBone;
    [Tooltip("Which string we should receive from the animation event to use left bone. Check is done with the string value in animation event.")]
    [SerializeField] private string leftString = "Left";

    // Play the clip associated to the footstep at foot location.
    private void OnFootstepAnim(AnimationEvent animationEvent)
    {
        GameObject footBone = (animationEvent.stringParameter == leftString) ? leftFootBone : rightFootBone;
        if (footstepAudioClip && footBone)
        {
            AudioSource.PlayClipAtPoint(footstepAudioClip, footBone.transform.position, footstepAudioVolume);
        }
    }

    // Play the clip associated to the swimming at hand location.
    private void OnSwimAnim(AnimationEvent animationEvent)
    {
        GameObject handBone = (animationEvent.stringParameter == leftString) ? leftHandBone : rightHandBone;
        if (swimAudioClip && handBone)
        {
            AudioSource.PlayClipAtPoint(swimAudioClip, handBone.transform.position, swimAudioVolume);
        }
    }

    // Play the clip associated to the landing on ground at foot location.
    private void OnLandonGroundAnim(AnimationEvent animationEvent)
    {
        GameObject footBone = (animationEvent.stringParameter == leftString) ? leftFootBone : rightFootBone;
        if (landingOnGroundAudioClip && footBone)
        {
            AudioSource.PlayClipAtPoint(landingOnGroundAudioClip, footBone.transform.position, landingOnGroundAudioVolume);
        }
    }

    // This class should be private as the others. But we need it private for the landing in water trick. Cf LandingInWaterBehaviour class comment for more infos.
    public void OnLandonInWaterAnim(AnimationEvent animationEvent)
    {
        GameObject footBone = (animationEvent.stringParameter == leftString) ? leftFootBone : rightFootBone;
        if (landingInWaterAudioClip && footBone)
        {
            AudioSource.PlayClipAtPoint(landingInWaterAudioClip, footBone.transform.position, landingInWaterAudioVolume);
        }
    }

    // Play a grunt when landing from a too high place at head location.
    private void OnHardLandingAnim(AnimationEvent animationEvent)
    {
        if (hardLandingAudioClip && headBone)
        {
            AudioSource.PlayClipAtPoint(hardLandingAudioClip, headBone.transform.position, hardLandingAudioVolume);
        }
    }

    // Play a random grunt when jumping at head location.
    private void OnJumpAnim(AnimationEvent animationEvent)
    {
        if (jumpGruntAudioClips.Length > 0 && headBone)
        {
            int index = UnityEngine.Random.Range(0, jumpGruntAudioClips.Length);
            if (jumpGruntAudioClips[index])
            {
                AudioSource.PlayClipAtPoint(jumpGruntAudioClips[index], headBone.transform.position, jumpGruntAudioVolume);
            }
        }
    }
}
