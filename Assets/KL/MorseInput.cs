using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class MorseInput : MonoBehaviour
{
    public TextMeshProUGUI displayText;

    public MorseSegment[] segments;
    [Space()]
    [Range(0, 1)] public float exponentialSpeed = 0.3f;
    public float linearSpeed = 1;
    public string Output
    {
        get
        {
            return output;
        }

    }
    private string output = "";

    private bool isInputHeld = false;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        if (!slider)
        {
            gameObject.SetActive(false);
        }

        slider.transform.localScale = Vector3.zero;

    }

    public void ResetSegment()
    {
        output = StringExtension.RemoveLastWord(output);
    }

    public void AcceptSegment()
    {
        if(output.Split(" ").Length == 4)
        {
           Debug.Log( CheckOutput());   
        }

        output += " ";
    }

    public bool CheckOutput()
    {
        return output == "..-  -.  -.-.  --.";
    }

    public void OnInputPressed()
    {
        // Only run the first time input is held.
        if (!isInputHeld)
        {
            isInputHeld = true;
            StartCoroutine("IncreaseSliderValue");
            slider.transform.localScale = Vector3.one;
        }
    }

    public void OnInputReleased()
    {
        if (isInputHeld)
        {
            StopAllCoroutines();

            if(slider.value > 0.99)
            {
                output += "-";
            }
            else
            {
                output += ".";

            }

            slider.value = 0;
            isInputHeld = false;
            slider.transform.localScale = Vector3.zero;
        }
    }

    private IEnumerator IncreaseSliderValue()
    {
        while (true)
        {
            slider.value += linearSpeed * Time.deltaTime + slider.value * exponentialSpeed / 10;
            yield return null;
        }
    }

    private void Update()
    {
        displayText.text = output;
    }
}

[CustomEditor(typeof(MorseInput))]
public class MorseInputEditor : Editor
{
    private bool inputToggled = false;

    public override void OnInspectorGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Exponential Speed");
        ((MorseInput)target).exponentialSpeed = EditorGUILayout.FloatField(((MorseInput)target).exponentialSpeed);
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        GUILayout.Label("Linear Speed");
        ((MorseInput)target).linearSpeed = EditorGUILayout.FloatField(((MorseInput)target).linearSpeed);
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Simulate Input"))
        {
            if (inputToggled)
            {
                ((MorseInput)target).OnInputReleased();
                inputToggled = false;
            }
            else
            {
                ((MorseInput)target).OnInputPressed();
                inputToggled = true;
            }

        }
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        GUILayout.TextArea(((MorseInput)target).Output);
        GUILayout.EndHorizontal();

    }
}


public static class StringExtension
{
    public static string RemoveLastWord(string s)
    {
        // remove the space from the start
        // and at the end of the string
        s = s.Trim();

        string newStr = "";
        if (s.Contains(" "))
        {
            newStr = s.Substring(0, s.LastIndexOf(' ')).TrimEnd();
            return newStr;
        }
        else
        {
            return s;
        }
    }
}