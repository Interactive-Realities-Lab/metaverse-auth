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
        Debug.Log("AAAA");
        if (matchingAngles.connectionLost)
            matchingAngles.connectionLost = false;
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
