using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

public enum MovementMode
{
    Move_None,
    Move_Running,
    Move_Falling,
    Move_Swimming,
}

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("General")]
    public MovementMode defaultMovementMode = MovementMode.Move_Running;
    public MovementMode currentMovementMode;

    [Tooltip("How fast the character turns to face movement direction")]
    [Min(0.0f)]
    public float rotationSmoothTime = 0.12f;
    [Tooltip("Offset for max speed reaching")]
    public float maxSpeedOffset = 0.1f;

    [Header("Running")]
    [Tooltip("Run speed of the character")]
    public float maxRunSpeed = 6.0f;

    [Tooltip("Walk speed of the character. Used on pc for toggling run/walk")]
    public float walkSpeed = 2.0f;
    [Tooltip("Used on pc for toggling run/walk. Should we hold the key ?")]
    public bool shouldHoldRunKey = false;

    [Tooltip("Should the character snap to the ground ? Useful for walking down stairs for example. " +
        "\nSnapping is linked to step value from character controller.")]
    public bool snapToGround = true;

    [Tooltip("How many time need to reach the target speed if speed under it")]
    public float walkAccelerationTime = 0.1f;
    [Tooltip("How many time need to reach the target speed if speed over it")]
    public float walkDeccelerationTime = 0.1f;

    [Header("Jumping/Falling")]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float gravity = -9.81f;
    [Tooltip("The maximum negative velocity we can reach. Set it to 0.0f to ignore it.")]
    public float maxFallVelocity = 50.0f;
    public float minDistanceForHardLanding = 3.0f;
    private Vector3 characterPositionBeforeFall;

    [Header("EnvironmentDetection")]
    public LayerDetectionSphere groundDetectionSphere = new LayerDetectionSphere(0.0f, 0.28f);
    public LayerDetectionSphere waterDetectionSphere = new LayerDetectionSphere(0.1f, 0.01f);

    [Header("Swim")]
    public float maxSwimSpeed = 2.0f;
    [Tooltip("How many time need to reach the target speed if speed under it")]
    public float swimAccelerationTime = 0.1f;
    [Tooltip("How many time need to reach the target speed if speed over it")]
    public float swimDeccelerationTime = 0.1f;
    public float swimColliderRadius = 0.84f;

    [Header("Animation")]
    public PlayerAnimationData animationData;

    [Header("Input")]
    public string gamepadScheme = "Gamepad";

    private bool isRunning = false;
    private bool mustAutoRun = false;
    private bool areMoveKeyReleasedWhileAutoRun = false;
    private bool isJumping = false;
    private Vector2 moveInput = Vector2.zero;
    private bool jumpInput = false;

    private float targetRotation = 0.0f;

    private Vector3 velocity;

    private CharacterController characterController;
    private GameObject mainCamera;
    private bool canMove = true;
    private float baseColliderRadius;

    // Smooth damp velocities
    private float speedVelocity;
    private float rotationVelocity;

    public bool useGamepad = false;

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
            if (inputDirection.sqrMagnitude > 0.0f)
            {
                // Make the character rotate toward the input relatively to the camera
                targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                if (!mustAutoRun)
                {
                    targetRotation += mainCamera.transform.eulerAngles.y;
                }

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }
        
            Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            Vector3 moveVelocity = targetDirection.normalized * speed;
            velocity.Set(moveVelocity.x, velocity.y, moveVelocity.z);
        }

        animationData.SetFloat(animationData.animIDMoveSpeed, speed); 
    }

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

    private float ComputeSpeed(Vector3 inputDirection)
    {
        float targetSpeed = 0.0f;
        if (canMove)
        {
            switch (currentMovementMode)
            {
                case MovementMode.Move_Running:
                    {
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

    private bool CheckIfInWater()
    {
        bool detectWater = waterDetectionSphere.CheckSphere(transform.position);

        switch (currentMovementMode)
        {
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

    // Use a custom ground check instead of charactercontroller isGrounded as the built-in value
    // doen't seems to be reliable
    private void CheckIfGrounded()
    {
        bool detectGround = groundDetectionSphere.CheckSphere(transform.position);

        switch (currentMovementMode)
        {
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

            case MovementMode.Move_Swimming:
                {
                    currentMovementMode = detectGround ? MovementMode.Move_Running : MovementMode.Move_Falling;
                } break;

            default: break;
        }

        animationData.SetBool(animationData.animIDIsFalling, !detectGround);
    }

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

    private void ApplyGravity()
    {
        switch (currentMovementMode)
        {
            case MovementMode.Move_Falling:
                {
                    velocity.y += gravity * Time.deltaTime;

                    if (maxFallVelocity > 0.0f && velocity.y > maxFallVelocity)
                    {
                        velocity.y = maxFallVelocity;
                    }
                } break;

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

    public void EnableMovement(bool enabled)
    {
        canMove = enabled;
    }

    // Inputs

    public void OnMovementInput(InputAction.CallbackContext context)
    {
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
        if (context.started && (currentMovementMode == MovementMode.Move_Running))
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

    private void OnDrawGizmosSelected()
    {
        groundDetectionSphere.Draw(transform.position);
        waterDetectionSphere.Draw(transform.position);
    }
}
