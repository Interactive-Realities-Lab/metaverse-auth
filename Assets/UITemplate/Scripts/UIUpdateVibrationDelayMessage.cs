using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIUpdateVibrationDelayMessage : MonoBehaviour
{
    [SerializeField] private MorseCodeGenerator morseCodeGenerator;
    [SerializeField] private TMP_Text textfield;
    [SerializeField] string defaultText;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        textfield.text = defaultText + " " + morseCodeGenerator.ActionDelay;
    }
}
