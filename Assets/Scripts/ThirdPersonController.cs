using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Tooltip("Move speed of the character")]
    public float moveSpeed = 2.0f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float gravity = -9.81f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Min(0.0f)]
    public float rotationSmoothTime = 0.12f;

    private Vector2 moveInput = Vector2.zero;

    private float targetRotation = 0.0f;
    private float rotationVelocity;

    private CharacterController characterController;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

        if (moveInput.sqrMagnitude > 0.0f)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

            Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            Vector3 velocity = targetDirection.normalized * moveSpeed;
            velocity.y += gravity;

            characterController.Move(velocity * Time.deltaTime);
        }
    }

    public void OnMovementInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>(); 
    }
}
