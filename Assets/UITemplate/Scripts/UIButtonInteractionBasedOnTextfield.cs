using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]

public class UIButtonInteractionBasedOnTextfield : MonoBehaviour
{
    [SerializeField] TMP_Text textfield;
    private Button button;
    private string prevValue = "-1";

    //private void OnEnable()
    //{
    //    ToogleButtonInteraction(false);
    //}

    // Start is called before the first frame update
    void Awake()
    {
        button= GetComponent<Button>();
    }


    // Update is called once per frame
    void Update()
    {
        if (prevValue == textfield.text) return;

        //if (textfield.text.Length > 0)
        //    ToogleButtonInteraction(true);

        button.interactable = textfield.text.Length > 0 ? true : false;
        prevValue = textfield.text;      
    }
}
