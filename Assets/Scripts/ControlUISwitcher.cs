using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlUISwitcher : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject keyboardText;
    [SerializeField] private string keyboardControlScheme;
    [SerializeField] private GameObject gamepadText;
    [SerializeField] private string gamepadControlScheme;

    public void OnControlsChanged(PlayerInput inPlayerInput)
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

    private void DisplayKeyboardText(bool displayMouseText)
    {
        keyboardText.SetActive(displayMouseText);
        gamepadText.SetActive(!displayMouseText);
    }
}
