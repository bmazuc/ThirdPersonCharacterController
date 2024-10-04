using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Gravity")]
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float gravity = -9.81f;

    [Header("Animator")]
    public string moveSpeedParameterName = "MoveSpeed";

    private bool isSprinting = false;

    private Vector2 moveInput = Vector2.zero;

    private float targetRotation = 0.0f;
    private float rotationVelocity;

    // animation IDs
    private int _animIDMoveSpeed;

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
        Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

        float inputSqrMagnitude = moveInput.sqrMagnitude;

        float speed = isSprinting ? sprintSpeed : moveSpeed;

        if (inputSqrMagnitude > 0.0f)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

            Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            Vector3 velocity = targetDirection.normalized * speed;
            velocity.y += gravity;

            characterController.Move(velocity * Time.deltaTime);
        }

        if (animator)
        {
            animator.SetFloat(_animIDMoveSpeed, inputSqrMagnitude * speed);
        }
    }

    public void OnMovementInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>(); 
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

    private void AssignAnimationIDs()
    {
        _animIDMoveSpeed = Animator.StringToHash(moveSpeedParameterName);
    }
}
