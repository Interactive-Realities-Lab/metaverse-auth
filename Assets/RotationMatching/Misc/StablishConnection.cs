using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StablishConnection : MonoBehaviour, IInputStablishConnection
{
    [SerializeField] private MatchingAngles matchingAngles;

    public void Connect()
    {
        if (matchingAngles.isDisconnected)
            matchingAngles.isDisconnected = false;
    }

}
