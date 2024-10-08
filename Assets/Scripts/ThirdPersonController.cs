using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Walk speed of the character")]
    public float walkSpeed = 2.0f;

    [Tooltip("Run speed of the character")]
    public float runSpeed = 6.0f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Min(0.0f)]
    public float rotationSmoothTime = 0.12f;

    public bool shouldHoldRunKey = false;
    [Tooltip("Should the character snap to the ground ? Useful for walking down stairs for example. " +
        "\nSnapping is linked to step value from character controller.")]
    public bool snapToGround = true;

    [Tooltip("How many time need to reach the target speed if speed under it")]
    public float accelerationTime = 0.1f;
    [Tooltip("How many time need to reach the target speed if speed over it")]
    public float deccelerationTime = 0.1f;
    public float speedOffset = 0.1f;

    private float speedVelocity;

    [Header("Jump")]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Header("Gravity")]
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float gravity = -9.81f;
    [Tooltip("The maximum negative velocity we can reach. Set it to 0.0f to ignore it.")]
    public float maxFallVelocity = 50.0f;
    public float minDistanceForHardLanding = 3.0f;
    private Vector3 characterPositionBeforeFall;

    [Header("GroundDetection")]
    [Tooltip("Useful for rough ground")]
    public float groundCheckOffset = 0.14f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask groundLayers;

    [Header("Swim")]
    public float waterDetectionOffset = 0.0f;
    public float waterDetectionRadius = 0.01f;
    public float swimColliderRadius = 0.84f;
    public LayerMask waterLayers;

    [Header("Animator")]
    public Animator animator;
    public string moveSpeedParameterName = "MoveSpeed";
    public string jumpTriggerParameterName = "Jump";
    public string groundedParameterName = "Grounded";
    public string hardLandParameterName = "isHardLanding";
    public string isInWaterParameterName = "isInWater";

    private bool isRunning = false;
    private bool isGrounded = false;
    private bool mustAutoRun = false;
    private bool areMoveKeyReleasedWhileAutoRun = false;
    private bool inWater = false;
    private Vector2 moveInput = Vector2.zero;
    private bool jumpInput = false;

    private float targetRotation = 0.0f;
    private float rotationVelocity;
    public Vector3 velocity;
    private float groundedSphereRadius;

    // animation IDs
    private int animIDMoveSpeed;
    private int animIDJump;
    private int animIDGrounded;
    private int animIDHardLand;
    private int animIDIsInWater;

    private CharacterController characterController;
    private GameObject mainCamera;
    private bool canMove = true;
    private float baseColliderRadius;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();

        baseColliderRadius = characterController.radius;

        groundedSphereRadius = characterController.radius;

        AssignAnimationIDs();
    }

    // Update is called once per frame
    void Update()
    {
        CheckIfInWater();
        CheckIfGrounded();
        if (!inWater)
        {
            ApplyGravity();
            Jump();
        }
        Move();
    }

    private void Move()
    {
        Vector3 inputDirection = GetInputDirection();

        float speed = ComputeSpeed(inputDirection);
        
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

        characterController.Move(velocity * Time.deltaTime);

        if (animator)
        {
            animator.SetFloat(animIDMoveSpeed, speed);
        }
    }

    private Vector3 GetInputDirection()
    {
        if (mustAutoRun)
        {
            return new Vector3(transform.forward.x, 0.0f, transform.forward.z).normalized;
        }

        if (!canMove)
        {
            return Vector3.zero;
        }

        return new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;
    }

    private float ComputeSpeed(Vector3 inputDirection)
    {
        if (canMove)
        {
            float targetSpeed = isRunning ? runSpeed : walkSpeed;

            if (inputDirection.sqrMagnitude == 0.0f)
            {
                targetSpeed = 0.0f;
            }

            float currentHorizontalSpeed = new Vector3(velocity.x, 0.0f, velocity.z).magnitude;

            bool mustAccelerate = currentHorizontalSpeed < targetSpeed - speedOffset;
            bool mustDeccelerate = currentHorizontalSpeed > targetSpeed + speedOffset;

            if (mustAccelerate || mustDeccelerate) 
            {
                float speedChangeTime = mustAccelerate ? accelerationTime : deccelerationTime;

                float speed = Mathf.SmoothDamp(currentHorizontalSpeed, targetSpeed * inputDirection.magnitude, ref speedVelocity,
                    speedChangeTime);

                return speed;
            }

            return targetSpeed;
        }

        return 0.0f;
    }

    private void CheckIfInWater()
    {
        Vector3 spherePosition = transform.position + characterController.center;
        spherePosition.y += waterDetectionOffset;
        inWater = Physics.CheckSphere(spherePosition, waterDetectionRadius, waterLayers);
        if (inWater)
        {
            velocity.y = 0;
        }

        characterController.radius = inWater ? swimColliderRadius : baseColliderRadius;

        if (animator)
        {
            animator.SetBool(animIDIsInWater, inWater);
        }
    }

    // Use a custom ground check instead of charactercontroller isGrounded as the built-in value
    // doen't seems to be reliable
    private void CheckIfGrounded()
    {
        bool wasGrounded = isGrounded;

        Vector3 spherePosition = transform.position;
        spherePosition.y += groundCheckOffset;

        isGrounded = Physics.CheckSphere(spherePosition, groundedSphereRadius, groundLayers, QueryTriggerInteraction.Ignore);

        if (wasGrounded != isGrounded)
        {
            if (wasGrounded) 
            { 
                characterPositionBeforeFall = transform.position;
                // Cancel snap to ground velocity;
                velocity.y = 0.0f;
            }
            else
            {
                float distance = Mathf.Abs(transform.position.y - characterPositionBeforeFall.y);
                animator.SetBool(animIDHardLand, distance > minDistanceForHardLanding);
            }
        }

        if (animator)
        {
            animator.SetBool(animIDGrounded, isGrounded);
        }
    }

    private void Jump()
    {
        if (jumpInput && isGrounded)
        {
            // Formula came from https://docs.unity3d.com/ScriptReference/CharacterController.Move.html
            velocity.y = Mathf.Sqrt(JumpHeight * -2.0f * gravity);

            if (animator)
            {
                animator.SetTrigger(animIDJump);
            }
            jumpInput = false;
        }
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;

            if (maxFallVelocity > 0.0f && velocity.y > maxFallVelocity)
            {
                velocity.y = maxFallVelocity;
            }
        }
        else
        {
            velocity.y = snapToGround ? (-characterController.stepOffset / Time.deltaTime) : 0f;
        }
    }

    private void AssignAnimationIDs()
    {
        animIDMoveSpeed = Animator.StringToHash(moveSpeedParameterName);
        animIDJump = Animator.StringToHash(jumpTriggerParameterName);
        animIDGrounded = Animator.StringToHash(groundedParameterName);
        animIDHardLand = Animator.StringToHash(hardLandParameterName);
        animIDIsInWater = Animator.StringToHash(isInWaterParameterName);
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
        if (context.started && isGrounded && !inWater)
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
}
