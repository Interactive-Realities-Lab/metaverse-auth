using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class MorseInput : MonoBehaviour
{
    public float speedDenominator = 30;
    public float speed = 1;
    public string output = "";
    private bool isInputHeld = false;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        if (!slider)
        {
            gameObject.SetActive(false);
        }
    }

    public void OnInputPressed()
    {
        // Only run the first time input is held.
        if (!isInputHeld)
        {
            isInputHeld = true;
            StartCoroutine("IncreaseSliderValue");
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
        }
    }

    private IEnumerator IncreaseSliderValue()
    {
        while (true)
        {
            slider.value += speed * Time.deltaTime + slider.value / speedDenominator; // magic number
            yield return null;
        }
    }
}

[CustomEditor(typeof(MorseInput))]
public class MorseInputEditor : Editor
{
    private bool inputToggled = false;

    public override void OnInspectorGUI()
    {
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
        GUILayout.TextArea(((MorseInput)target).output);
        GUILayout.EndHorizontal();

    }
}
