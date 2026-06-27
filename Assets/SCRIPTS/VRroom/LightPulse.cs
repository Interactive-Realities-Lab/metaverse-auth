using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WIP
/// Abandoned script that was going to pulse the light of the button.
/// </summary>
public class LightPulse : MonoBehaviour
{
    /// <summary>
    /// The light of the button.
    /// </summary>  
    Light buttonLight;

    /// <summary>
    /// Instantiates the variable.
    /// </summary>  
    void Awake()
    {
        buttonLight = GetComponent<Light>();
    }

    /// <summary>
    /// The code that would have caused the light to pulsate.
    /// </summary>  
    void FixedUpdate()
    {
        buttonLight.intensity = Mathf.PingPong(Time.deltaTime, 6);
    }
}
