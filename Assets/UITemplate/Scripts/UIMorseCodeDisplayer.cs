using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIMorseCodeDisplayer : MonoBehaviour
{
    /// <summary>
    /// THIS SCRIPT UPDATES A TEXT MESH PRO TEXT FIELD.
    /// IT TAKES IN A MORSE CODE STRING AS INPUT (morseString) AND CONVERTS IT INTO AN EASY TO READ VERSION.
    /// </summary>

    public TextMeshProUGUI tmp;
    [SerializeField] private MorseControllerV2 morseController;


    void Update()
    {
        if (morseController.CurrentSegment == "")
        {
            tmp.text = "";
            return;
        }

        string newString = "";
        foreach (char c in morseController.CurrentSegment)
        {
            newString += (c == '.') ? "-" : "----";
            newString += " ";
        }

        tmp.text = newString;
    }


}
