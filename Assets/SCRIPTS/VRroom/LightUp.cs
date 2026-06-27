using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightUp : MonoBehaviour
{
    MeshRenderer oldMaterial;
    MeshRenderer newMaterial;
    void Awake()
    {
        oldMaterial = GetComponent<MeshRenderer>();
        newMaterial = GetComponent<MeshRenderer>();

    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<GameObject>().name == "RightHandAnchor" ||
            other.gameObject.GetComponent<GameObject>().name == "LeftHandAnchor")
                Debug.Log("HI");
    }
    void OnTriggerExt(Collider other)
    {
        if (other.gameObject.GetComponent<GameObject>().name == "RightHandAnchor" ||
            other.gameObject.GetComponent<GameObject>().name == "LeftHandAnchor")
                Debug.Log("OK BYE");
    }
    // void OnCollisionEnter(Collision other)
    // {
    //     if (other.gameObject.GetComponent<GameObject>().name == "RightHandAnchor" ||
    //         other.gameObject.GetComponent<GameObject>().name == "LeftHandAnchor")
    //             Debug.Log("HI");

            // gameObject.GetComponent<MeshRenderer>().materials[0] = newMaterial;
    // }
    // void OnCollisionExit(Collision other)
    // {
    //     Debug.Log("Bye");
        // gameObject.GetComponent<MeshRenderer>().materials[0] = oldMaterial;

    // }
}
