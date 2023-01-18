using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class NotifyParityLost : MonoBehaviour
{
    [SerializeField] private ActionBasedController controller;

    public void SendNotification() 
    {
        controller?.SendHapticImpulse(.5f, .2f);
    }
}
