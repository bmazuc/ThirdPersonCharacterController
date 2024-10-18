using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

// The different ways a character can move
public enum MovementMode
{
    Move_None,
    Move_Running,
    Move_Falling,
    Move_Swimming,
}

/**
 * A class to manage character movements and handle player inputs.
 * It complements the built-in character controller.
 */
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private MovementMode defaultMovementMode = MovementMode.Move_Running;
    private MovementMode currentMovementMode;

    [Tooltip("How fast the character turns to face movement direction")]
    [Min(0.0f)]
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [Tooltip("Offset for max speed reaching")]
    [SerializeField] private float maxSpeedOffset = 0.1f;

    [Header("Running")]
    [Tooltip("Run speed of the character")]
    [SerializeField] private float maxRunSpeed = 6.0f;

    [Tooltip("Walk speed of the character. Used on pc for toggling run/walk")]
    [SerializeField] private float walkSpeed = 2.0f;
    [Tooltip("Used on pc for toggling run/walk. Should we hold the key ?")]
    [SerializeField] private bool shouldHoldRunKey = false;

    [Tooltip("Should the character snap to the ground ? Useful for walking down stairs for example. " +
        "\nSnapping is linked to step value from character controller.")]
    [SerializeField] private bool snapToGround = true;

    [Tooltip("How many time need to reach the target speed if speed under it")]
    [SerializeField] private float walkAccelerationTime = 0.1f;
    [Tooltip("How many time need to reach the target speed if speed over it")]
    [SerializeField] private float walkDeccelerationTime = 0.1f;

    [Header("Jumping/Falling")]
    [Tooltip("The height the player can jump")]
    [SerializeField] private float JumpHeight = 1.2f;
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    [SerializeField] private float gravity = -9.81f;
    [Tooltip("The maximum negative velocity we can reach. Set it to 0.0f to ignore it.")]
    [SerializeField] private float maxFallVelocity = 50.0f;
    [Tooltip("The minimum distance required when falling to be considered hard landing.")]
    [SerializeField] private float minDistanceForHardLanding = 3.0f;
    // Register the character position at the fall start for hard landing computations.
    private Vector3 characterPositionBeforeFall;

    [Header("EnvironmentDetection")]
    [SerializeField] private LayerDetectionSphere groundDetectionSphere;
    [SerializeField] private LayerDetectionSphere waterDetectionSphere;

    [Header("Swim")]
    [Tooltip("Swim speed of the character")]
    [SerializeField] private float maxSwimSpeed = 2.0f;
    [Tooltip("How many time need to reach the target speed if speed under it")]
    [SerializeField] private float swimAccelerationTime = 0.1f;
    [Tooltip("How many time need to reach the target speed if speed over it")]
    [SerializeField] private float swimDeccelerationTime = 0.1f;
    // Allow to modify the character collider radius when swimming. Useful as we go from a vertical to an horizontal position.
    [Tooltip("Which is the character collider radius when swimming")]
    [SerializeField] private float swimColliderRadius = 0.84f;

    [Header("Animation")]
    [SerializeField] private PlayerAnimationData animationData;

    [Header("Input")]
    [Tooltip("What is the control scheme associated to gamepad")]
    [SerializeField] private string gamepadScheme = "Gamepad";

    // Is the character running ? Used on PC as keyboard have "binary" magnitude.
    private bool isRunning = false;
    private bool mustAutoRun = false;
    private bool areMoveKeyReleasedWhileAutoRun = false;
    private bool isJumping = false;

    // Input values
    private Vector2 moveInput = Vector2.zero;
    private bool jumpInput = false;

    // The rotation towards the character is smooth damping
    private float targetRotation = 0.0f;
    // Character velocity. Compute and apply in Update.
    private Vector3 velocity;

    // Can the player move the character ?
    private bool canMove = true;
    // Can the player make jump the character ?
    private bool canJump = true;
    // Register the character collider default radius to reset it when swimming.
    private float baseColliderRadius;

    // Smooth damp velocities
    private float speedVelocity;
    private float rotationVelocity;

    // Are the player using a gamepad ? Use for speed computation.
    private bool useGamepad = false;

    private CharacterController characterController;
    private GameObject mainCamera;

    private void Awake()
    {
        if (!mainCamera)
        {
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        currentMovementMode = defaultMovementMode;

        characterController = GetComponent<CharacterController>();
        animationData.AssignAnimationIDs();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput)
        {
            useGamepad = (playerInput.currentControlScheme == gamepadScheme);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Detect environment
        if (!CheckIfInWater())
        {
            CheckIfGrounded();
        }

        ApplyGravity();
        Jump();
        Move();

        // Apply velocity
        characterController.Move(velocity * Time.deltaTime);
    }

    // Compute the character move velocity.
    private void Move()
    {
        if (currentMovementMode == MovementMode.Move_Falling || currentMovementMode == MovementMode.Move_None)
        {    
            return;
        }

        Vector3 inputDirection = GetInputDirection();

        float speed = ComputeSpeed(inputDirection);

        if (currentMovementMode == MovementMode.Move_Running || currentMovementMode == MovementMode.Move_Swimming)
        {
            // Make the character rotate toward the input relatively to the camera
            if (inputDirection.sqrMagnitude > 0.0f)
            {
                targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                if (!mustAutoRun)
                {
                    targetRotation += mainCamera.transform.eulerAngles.y;
                }

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // Move the character forward
            Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            Vector3 moveVelocity = targetDirection.normalized * speed;
            velocity.Set(moveVelocity.x, velocity.y, moveVelocity.z);
        }

        animationData.SetFloat(animationData.animIDMoveSpeed, speed); 
    }

    // In which direction does the player input points to. If we auto run, we just considered it as character forward.
    private Vector3 GetInputDirection()
    {
        if (mustAutoRun)
        {
            return new Vector3(transform.forward.x, 0.0f, transform.forward.z);
        }

        if (!canMove)
        {
            return Vector3.zero;
        }

        return new Vector3(moveInput.x, 0.0f, moveInput.y);
    }

    // Compute the character speed
    private float ComputeSpeed(Vector3 inputDirection)
    {
        float targetSpeed = 0.0f;
        if (canMove)
        {
            switch (currentMovementMode)
            {
                case MovementMode.Move_Running:
                    {
                        // When using gamepad, we always use run speed who will be modified by input magnitude
                        targetSpeed = (isRunning || useGamepad) ? maxRunSpeed : walkSpeed;
                    }
                    break;

                case MovementMode.Move_Swimming:
                    {
                        targetSpeed = maxSwimSpeed;
                    } 
                    break;

                default: break;
            }

            targetSpeed *= inputDirection.sqrMagnitude;

            // Check if we are already at the wanted speed or if we need to accelerate or deccelerate.

            float currentHorizontalSpeed = new Vector3(velocity.x, 0.0f, velocity.z).magnitude;

            bool mustAccelerate = currentHorizontalSpeed < targetSpeed - maxSpeedOffset;
            bool mustDeccelerate = currentHorizontalSpeed > targetSpeed + maxSpeedOffset;

            if (mustAccelerate || mustDeccelerate) 
            {
                float speedChangeTime = GetSpeedChangeTime(mustAccelerate);

                float speed = Mathf.SmoothDamp(currentHorizontalSpeed, targetSpeed * inputDirection.magnitude, ref speedVelocity,
                    speedChangeTime);

                return speed;
            }
        }

        return targetSpeed;
    }

    // Compute the acceleration or decceleration modifier.
    private float GetSpeedChangeTime(bool mustAccelerate)
    {
        switch (currentMovementMode)
        {
            case MovementMode.Move_Running:
                {
                    return mustAccelerate ? walkAccelerationTime : walkDeccelerationTime;
                }


            case MovementMode.Move_Swimming:
                {
                    return mustAccelerate ? swimAccelerationTime : swimDeccelerationTime;
                }

            default: return 0.0f;
        }
    }

    // Are we in water ? Apply the behaviour of entering or exiting it.
    private bool CheckIfInWater()
    {
        bool detectWater = waterDetectionSphere.CheckSphere(transform.position);

        switch (currentMovementMode)
        {
            // Enter water
            case MovementMode.Move_Running:
            case MovementMode.Move_Falling:
                {
                    if (detectWater)
                    {
                        currentMovementMode = MovementMode.Move_Swimming;
                        baseColliderRadius = characterController.radius;
                        characterController.radius = swimColliderRadius;
                    }
                } break;

            // When getting out of water, we reset the character controller radius.
            case MovementMode.Move_Swimming:
                {
                    if (!detectWater)
                    {
                        characterController.radius = baseColliderRadius;
                    }
                } break;

            default: break;
        }

        animationData.SetBool(animationData.animIDIsSwimming, currentMovementMode == MovementMode.Move_Swimming);

        return detectWater;
    }

    // Use a custom ground check instead of charactercontroller isGrounded as the built-in value doesn't seems to be reliable
    private void CheckIfGrounded()
    {
        bool detectGround = groundDetectionSphere.CheckSphere(transform.position);

        switch (currentMovementMode)
        {
            // Apply falling behaviour
            case MovementMode.Move_Running:
                {
                    if (!detectGround)
                    {
                        characterPositionBeforeFall = transform.position;
                        if (!isJumping)
                        {
                            // Cancel snap to ground velocity;
                            velocity.y = 0.0f;
                        }
                        currentMovementMode = MovementMode.Move_Falling;
                    }
                } break;

            // When landing, are we hard landing ?
            case MovementMode.Move_Falling:
                {
                    if (detectGround)
                    {
                        isJumping = false;
                        float distance = Mathf.Abs(transform.position.y - characterPositionBeforeFall.y);
                        animationData.SetBool(animationData.animIDHardLand, distance > minDistanceForHardLanding);
                        currentMovementMode = MovementMode.Move_Running;
                    }
                } break;

            // When getting out of water, are we on ground or falling ?
            case MovementMode.Move_Swimming:
                {
                    currentMovementMode = detectGround ? MovementMode.Move_Running : MovementMode.Move_Falling;
                } break;

            default: break;
        }

        animationData.SetBool(animationData.animIDIsFalling, !detectGround);
    }

    // Compute and apply a force to the velocity to make the character reach jump height
    private void Jump()
    {
        if (jumpInput && (currentMovementMode == MovementMode.Move_Running))
        {
            // Formula came from https://docs.unity3d.com/ScriptReference/CharacterController.Move.html
            velocity.y = Mathf.Sqrt(JumpHeight * -2.0f * gravity);
            
            animationData.SetTrigger(animationData.animIDJump);

            jumpInput = false;
            isJumping = true;
        }
    }

    // Apply the gravity to the velocity following the current movement mode.
    private void ApplyGravity()
    {
        switch (currentMovementMode)
        {
            // When falling we want to simulate gravity
            case MovementMode.Move_Falling:
                {
                    velocity.y += gravity * Time.deltaTime;

                    if (maxFallVelocity > 0.0f && velocity.y > maxFallVelocity)
                    {
                        velocity.y = maxFallVelocity;
                    }
                } break;

            // When running we snap to the ground if we are not jumping.
            case MovementMode.Move_Running:
                {
                    if (!isJumping)
                    {
                        velocity.y = (snapToGround && !jumpInput) ? (-characterController.stepOffset / Time.deltaTime) : 0f;
                    }
                } break;

            default: { velocity.y = 0.0f; } break;
        }
    }

    // Allow the character to move ?
    public void EnableMovement(bool enabled)
    {
        canMove = enabled;
    }

    // Allow the character to jump ?
    public void EnableJump(bool enabled)
    {
        canJump = enabled;
    }

    // Inputs

    public void OnMovementInput(InputAction.CallbackContext context)
    {
        // Disable the auto run if the player press another key.
        // Checks are done to avoid disabling it when the player press auto run and is holding move keys.
        if (mustAutoRun)
        {
            if (context.canceled)
            {
                areMoveKeyReleasedWhileAutoRun = true;
                return;
            }

            if (!areMoveKeyReleasedWhileAutoRun)
            {
                return;
            }

            mustAutoRun = false;
        }

        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.started && canJump && (currentMovementMode == MovementMode.Move_Running))
        {
            jumpInput = true;
        }
    }

    public void OnRunInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isRunning = shouldHoldRunKey ? true : !isRunning;
        }
        else if (context.canceled && shouldHoldRunKey)
        {
            isRunning = false;
        }
    }

    public void OnAutoRunInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            mustAutoRun = !mustAutoRun;
            if (mustAutoRun)
            {
                moveInput.Set(0.0f, 0.0f);
            }
        }
    }

    public void OnControlsChanged(PlayerInput playerInput)
    {
        useGamepad = (playerInput.currentControlScheme == gamepadScheme);
    }

#if UNITY_EDITOR
    // Display the two environment detections spheres.
    private void OnDrawGizmosSelected()
    {
        groundDetectionSphere.Draw(transform.position);
        waterDetectionSphere.Draw(transform.position);
    }
#endif
}
