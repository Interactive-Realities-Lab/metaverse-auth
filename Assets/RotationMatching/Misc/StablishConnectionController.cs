using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StablishConnectionController : MonoBehaviour, IInputStablishConnection
{
    [SerializeField] private MatchingAngles matchingAngles;
    [SerializeField] private InputActionReference controllerTrigger;

    public void Connect()
    {
        if (matchingAngles.isDisconnected)
            matchingAngles.isDisconnected = false;
    }

    private void Awake()
    {
        controllerTrigger.action.performed += GripPress;
    }

    private void GripPress(InputAction.CallbackContext obj)
    {
        Connect();
    }

}
