using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotation : MonoBehaviour
{
    private float degreesPerSecond = 10f;
    [SerializeField] private bool allowRotation = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
            allowRotation = !allowRotation;
        
        

        if(allowRotation)
            gameObject.transform.Rotate(new Vector3(0, degreesPerSecond, 0) * Time.deltaTime);
    }
}
