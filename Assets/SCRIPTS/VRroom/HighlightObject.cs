using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script provides feedback to the player of what objects they are able to pick up.
/// Interactable objects will change color when the player is close enough to them and aiming
/// their controller at them.
/// </summary>
public class HighlightObject : MonoBehaviour
{
    /// <summary>
    /// The trigger to detect if an object is currently highlighted or not.
    /// </summary>
    Renderer touchCurrentRenderer;

    /// <summary>
    /// The normal, original color of the interactable object.
    /// </summary>
    Color touchOriginalColor;

    /// <summary>
    /// The color the interactable object changes to when a controller is aimed at it
    /// and the player is close enough.
    /// </summary>
    public Color touchHighlightColor;

/// <summary>
/// Method that is triggered when a controller highlights an interactable object.
/// <args> Collider other refers to the player's controller. </args>
/// </summary>
    void OnTriggerStay(Collider other)
    {
        // Objects that the player should be able to grab must have the tag "grabbable" in Unity
        if (touchCurrentRenderer == null && other.tag == "Grabbable")
        {
            touchCurrentRenderer = other.GetComponent<Renderer>();
            if (touchCurrentRenderer != null)
            {
                touchOriginalColor = touchCurrentRenderer.material.color;
                touchCurrentRenderer.material.color = touchHighlightColor;
            }
        }
    }

/// <summary>
/// Method that is triggered when a controller is no longer aimed at an interactable object.
/// <args> Collider other refers to the player's controller. </args>
/// </summary>
    void OnTriggerExit(Collider other)
    {
        touchCurrentRenderer.material.color = touchOriginalColor;
        touchCurrentRenderer = null;
    }
}
