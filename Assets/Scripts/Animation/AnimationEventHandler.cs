using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class contains all the methods called in character animation event.
 * It is in another class than person controller has animator is not at the same level as the controller.
 */
public class AnimationEventHandler : MonoBehaviour
{
    [Header("Footstep sounds")]
    [SerializeField] private AudioClip footstepAudioClip;
    [SerializeField] private float footstepAudioVolume;

    [Header("Swim sounds")]
    [SerializeField] private AudioClip swimAudioClip;
    [SerializeField] private float swimAudioVolume;

    [Header("Landing on ground sounds")]
    [SerializeField] private AudioClip landingOnGroundAudioClip;
    [SerializeField] private float landingOnGroundAudioVolume;

    [Header("Landing in water sounds")]
    [SerializeField] private AudioClip landingInWaterAudioClip;
    [SerializeField] private float landingInWaterAudioVolume;

    [Header("Jump grunts")]
    [SerializeField] private AudioClip[] jumpGruntAudioClips;
    [SerializeField] private float jumpGruntAudioVolume;

    [Header("Bones")]
    [SerializeField] private GameObject leftHandBone;
    [SerializeField] private GameObject rightHandBone;
    [SerializeField] private GameObject leftFootBone;
    [SerializeField] private GameObject rightFootBone;
    [SerializeField] private GameObject headBone;
    [Tooltip("Which string we should receive from the animation event to use left bone.")]
    [SerializeField] private string leftString = "Left";

    private void OnFootstepAnim(AnimationEvent animationEvent)
    {
        GameObject footBone = (animationEvent.stringParameter == leftString) ? leftFootBone : rightFootBone;
        if (footstepAudioClip && footBone)
        {
            AudioSource.PlayClipAtPoint(footstepAudioClip, footBone.transform.position, footstepAudioVolume);
        }
    }

    private void OnSwimAnim(AnimationEvent animationEvent)
    {
        GameObject handBone = (animationEvent.stringParameter == leftString) ? leftHandBone : rightHandBone;
        if (swimAudioClip && handBone)
        {
            AudioSource.PlayClipAtPoint(swimAudioClip, handBone.transform.position, swimAudioVolume);
        }
    }

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
