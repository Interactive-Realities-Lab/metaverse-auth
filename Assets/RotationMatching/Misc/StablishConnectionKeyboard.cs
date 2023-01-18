using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StablishConnectionKeyboard : MonoBehaviour, IInputStablishConnection
{
    [SerializeField] private MatchingAngles matchingAngles;

    public void Connect()
    {
        if (matchingAngles.isDisconnected)
            matchingAngles.isDisconnected = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Connect();
        }        
    }
}
