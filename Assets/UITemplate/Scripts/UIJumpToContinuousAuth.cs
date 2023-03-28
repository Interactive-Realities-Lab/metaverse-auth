using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIJumpToContinuousAuth : MonoBehaviour
{
    public UnityEvent OnJumpToContinuousAuth;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.J))
            OnJumpToContinuousAuth?.Invoke();
            
    }
}
