using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Tooltip("Move speed of the character")]
    public float moveSpeed = 2.0f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float gravity = -9.81f;

    private Vector3 moveInput = Vector3.zero;

    private CharacterController characterController;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 velocity = moveInput.normalized * moveSpeed;
        velocity.y += gravity;

        characterController.Move(velocity * Time.deltaTime);
    }

    public void OnMovementInput(InputAction.CallbackContext context)
    {
        Vector2 inputValue = context.ReadValue<Vector2>();
        moveInput.Set(inputValue.x, 0.0f, inputValue.y);
    }
}
