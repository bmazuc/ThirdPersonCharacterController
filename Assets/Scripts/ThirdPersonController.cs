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

    [Header("Jump")]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Header("Gravity")]
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float gravity = -9.81f;
    [Tooltip("The maximum negative velocity we can reach. Set it to 0.0f to ignore it.")]
    public float maxFallVelocity = 50.0f;

    [Header("GroundDetection")]
    [Tooltip("Useful for rough ground")]
    public float groundCheckOffset = -0.14f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask groundLayers;

    [Header("Animator")]
    public Animator animator;
    public string moveSpeedParameterName = "MoveSpeed";
    public string jumpTriggerParameterName = "Jump";
    public string groundedParameterName = "Grounded";

    private bool isRunning = false;
    private bool isGrounded = false;
    public bool mustAutoRun = false;
    private bool areMoveKeyReleasedWhileAutoRun = false;

    private Vector2 currentMoveInput = Vector2.zero;
    private Vector2 moveInput = Vector2.zero;
    private bool jumpInput = false;

    private float targetRotation = 0.0f;
    private float rotationVelocity;
    private Vector3 velocity;
    private float groundedSphereRadius;

    // animation IDs
    private int animIDMoveSpeed;
    private int animIDJump;
    private int animIDGrounded;

    private CharacterController characterController;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();

        groundedSphereRadius = characterController.radius;

        AssignAnimationIDs();
    }

    // Update is called once per frame
    void Update()
    {
        CheckIfGrounded();
        ApplyGravity();
        Jump();
        Move();
    }

    private void Move()
    {
        currentMoveInput = moveInput;
        if (mustAutoRun)
        {
            currentMoveInput.Set(transform.forward.x, transform.forward.z);
        }

        Vector3 inputDirection = new Vector3(currentMoveInput.x, 0.0f, currentMoveInput.y).normalized;

        float inputSqrMagnitude = currentMoveInput.sqrMagnitude;

        float speed = isRunning ? runSpeed : walkSpeed;
        speed *= currentMoveInput.magnitude;

        if (inputSqrMagnitude > 0.0f)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

        Vector3 moveVelocity = targetDirection.normalized * speed;
        velocity.Set(moveVelocity.x, velocity.y, moveVelocity.z);

        characterController.Move(velocity * Time.deltaTime);

        if (animator)
        {
            animator.SetFloat(animIDMoveSpeed, inputSqrMagnitude * speed);
        }
    }

    // Use a custom ground check instead of charactercontroller isGrounded as the built-in value
    // doen't seems to be reliable
    private void CheckIfGrounded()
    {
        Vector3 spherePosition = transform.position;
        spherePosition.y -= groundCheckOffset;

        isGrounded = Physics.CheckSphere(spherePosition, groundedSphereRadius, groundLayers, QueryTriggerInteraction.Ignore);

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
            velocity.y += Mathf.Sqrt(JumpHeight * -2.0f * gravity);

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
        else if (velocity.y < 0)
        {
            velocity.y = 0f;
        }
    }

    private void AssignAnimationIDs()
    {
        animIDMoveSpeed = Animator.StringToHash(moveSpeedParameterName);
        animIDJump = Animator.StringToHash(jumpTriggerParameterName);
        animIDGrounded = Animator.StringToHash(groundedParameterName);
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
        if (context.started)
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
