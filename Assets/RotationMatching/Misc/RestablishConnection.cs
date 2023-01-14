using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestablishConnection : MonoBehaviour
{
    [SerializeField] private MatchingAngles matchingAngles;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (matchingAngles.connectionLost)
                matchingAngles.connectionLost = false;
        }        
    }
}
