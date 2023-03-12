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
    public GameObject[] lights = new GameObject[4];

    private void OnEnable()
    {
        ResetLights();
    }


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

    public void ResetLights()
    {
        foreach(GameObject g in lights)
        {
            g.SetActive(false);
        }  
    }
}
