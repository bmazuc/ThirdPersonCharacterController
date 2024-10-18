using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/**
 * A class to switch the text displaying commands when changing controls.
 */
public class ControlUISwitcher : MonoBehaviour
{
    [Tooltip("The text to display when using keyboard & mouse.")]
    [SerializeField] private GameObject keyboardText;
    [Tooltip("The name of the control scheme associated to keyboard & mouse.")]
    [SerializeField] private string keyboardControlScheme;
    [Tooltip("The text to display when using gamepad.")]
    [SerializeField] private GameObject gamepadText;
    [Tooltip("The name of the control scheme associated to gamepad.")]
    [SerializeField] private string gamepadControlScheme;

    // Function called by player input control changed event
    public void OnControlsChanged(PlayerInput playerInput)
    {
        if (playerInput.currentControlScheme == keyboardControlScheme)
        {
            DisplayKeyboardText(true);
        }
        else if (playerInput.currentControlScheme == gamepadControlScheme)
        {
            DisplayKeyboardText(false);
        }
    }

    // Helper function to switch the texts
    private void DisplayKeyboardText(bool displayMouseText)
    {
        keyboardText.SetActive(displayMouseText);
        gamepadText.SetActive(!displayMouseText);
    }
}
