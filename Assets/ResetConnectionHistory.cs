using System.Collections;
using System.Collections.Generic;
using UITemplate;
using UnityEngine;

public class ResetConnectionHistory : MonoBehaviour
{

    [SerializeField] private List<UIUserContainer> usersContainers;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            foreach(var obj in usersContainers)
                obj.hasConnected = false;
        }
    }
}
