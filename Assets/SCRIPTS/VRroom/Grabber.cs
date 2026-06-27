using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WIP: This script isn't currently used in the project.
/// This script allows the user to distance grab objects.
/// </summary>
public class Grabber : MonoBehaviour
{
    /// <summary>
    /// Refers to the hand of either the right or left controller depending
    /// on which the user is using to pick up an object.
    /// </summary>  
    public GameObject collidingObject;

    /// <summary>
    /// WIP
    /// </summary>  
    public GameObject objectInHand;

    /// <summary>
    /// Method that occurs when a player picks up an object.
    /// <args>Collider other refers to the controller the player is using to pick up
    /// the object. </args>
    /// </summary>  
    public void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<GameObject>())
                collidingObject = other.gameObject;
    }

    /// <summary>
    /// Method that occurs when a player drops an object.
    /// <args> Collider other refers to the controller the player is using to drop
    /// the object. </args>
    /// </summary>   
    public void OnTriggerExit(Collider other)
    {
        collidingObject = null;
    }
}
