using System.Collections;
using System.Collections.Generic;
using UITemplate;
using UnityEngine;
using UnityEngine.InputSystem;

public class ResetConnectionHistory : MonoBehaviour
{

    [SerializeField] private List<UIUserContainer> usersContainers;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
{
    if (Keyboard.current != null &&
        Keyboard.current.rKey.wasPressedThisFrame)
    {
        // Reset connection history
    }
}
}
