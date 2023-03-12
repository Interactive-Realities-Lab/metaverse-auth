using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAcceptSegmentButton : MonoBehaviour
{


    private Button button;

    private void OnEnable()
    {
        ToogleButtonInteraction(false);
    }

    // Start is called before the first frame update
    void Awake()
    {
        button= GetComponent<Button>();
        
    }


    public void ToogleButtonInteraction(bool value)
    {
        button.interactable = value;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
