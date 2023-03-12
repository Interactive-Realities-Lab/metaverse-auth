using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MorseCodeDisplayer : MonoBehaviour
{
    /// <summary>
    /// THIS SCRIPT UPDATES A TEXT MESH PRO TEXT FIELD.
    /// IT TAKES IN A MORSE CODE STRING AS INPUT (morseString) AND CONVERTS IT INTO AN EASY TO READ VERSION.
    /// </summary>

    public TextMeshProUGUI tmp;
    [HideInInspector] public string morseString;


    void Update()
    {
        if (morseString == "")
        {
            tmp.text = "";
            return;
        }

        string newString = "";
        foreach (char c in morseString)
        {
            newString += (c == '.') ? "-" : "----";
            newString += " ";
        }

        tmp.text = newString;
    }


}
