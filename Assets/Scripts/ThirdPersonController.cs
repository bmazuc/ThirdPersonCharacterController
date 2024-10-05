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
    [Tooltip("Move speed of the character")]
    public float moveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character")]
    public float sprintSpeed = 6.0f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Min(0.0f)]
    public float rotationSmoothTime = 0.12f;

    public bool shouldHoldSprintKey = false;

    [Header("Jump")]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Header("Gravity")]
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float gravity = -9.81f;
    [Tooltip("The maximum negative velocity we can reach. Set it to 0.0f to ignore it.")]
    public float maxFallVelocity = 50.0f;

    [Header("Animator")]
    public string moveSpeedParameterName = "MoveSpeed";
    public string jumpTriggerParameterName = "Jump";
    public string groundedParameterName = "Grounded";
    public string yVelocityParameterName = "YVelocity";

    private bool isSprinting = false;

    private Vector2 moveInput = Vector2.zero;
    private bool jumpInput = false;

    private float targetRotation = 0.0f;
    private float rotationVelocity;
    private Vector3 velocity;

    // animation IDs
    private int animIDMoveSpeed;
    private int animIDJump;
    private int animIDGrounded;

    private CharacterController characterController;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        AssignAnimationIDs();
    }

    // Update is called once per frame
    void Update()
    {
        if (animator)
        {
            animator.SetBool(animIDGrounded, characterController.isGrounded);
        }

        ApplyGravity();
        Jump();
        Move();
    }

    private void Move()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

        float inputSqrMagnitude = moveInput.sqrMagnitude;

        float speed = isSprinting ? sprintSpeed : moveSpeed * moveInput.magnitude;

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

    private void Jump()
    {
        if (jumpInput && characterController.isGrounded)
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
        if (!characterController.isGrounded)
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
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpInput = true;
        }
    }

    public void OnSprintInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isSprinting = shouldHoldSprintKey ? true : !isSprinting;
        }
        else if (context.canceled && shouldHoldSprintKey)
        {
            isSprinting = false;
        }
    }
}
