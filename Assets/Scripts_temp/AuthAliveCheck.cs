using UnityEngine;

public class AuthAliveCheck : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.Log("Auth Continuous object enabled");
    }

    private void OnDisable()
    {
        Debug.Log("Auth Continuous object disabled");
    }

    private void OnDestroy()
    {
        Debug.Log("Auth Continuous object destroyed");
    }
}