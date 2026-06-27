using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script shows the player how to interact with the world and what actions
/// are mapped to which buttons. It is togglable.
/// It's also a WIP. I'd like to spend some more time on the UI to make it more user
/// friendly and elegant.
/// </summary>
public class InstructionCanvas : MonoBehaviour
{
    /// <summary>
    /// The Canvas that shows the instructions on how to play the game.
    /// </summary>  
    public GameObject InstructionsPanel;

    /// <summary>
    /// Method that constantly checks to see if the instructions canvas is being toggled
    /// on or off.
    /// </summary>  
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInstructions();
    }

    /// <summary>
    /// Method that does the actual toggling of the instructions canvas.
    /// </summary>  
    void ToggleInstructions()
    {
        if (InstructionsPanel.activeSelf)
            InstructionsPanel.SetActive(false);
        else
            InstructionsPanel.SetActive(true);
    }
}
