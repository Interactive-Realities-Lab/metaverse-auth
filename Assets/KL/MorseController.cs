using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.InputSystem;

public class MorseController : MonoBehaviour
{
    [Header("References")]
    public Slider slider;
    public MorseCodeDisplayer display;
    public InputActionReference inputDigit = null;

    [Header("Input Config")]
    public bool simulateInput = false;
    public bool simulateDelete = false;
    public bool simulateGo = false;
    [Range(0, 1)] public float exponentialSpeed = 0.3f;
    public float linearSpeed = 1;

    // Runtime Variables
    public string Segment
    {
        get
        {
            return output[segmentIndex];
        }
        set
        {
            output[segmentIndex] = value;
        }
    }
    public string Output
    {
        get
        {
            return string.Join(" ", output).Trim();
        }

    }
    private int segmentIndex = 0;
    private string[] output = new string[4] {"", "", "", ""};
    private bool isInputHeld = false;

    private void Awake()
    {
        if (!slider)
        {
            slider = GetComponent<Slider>();

        }
        slider.transform.localScale = Vector3.zero;

        inputDigit.action.started += InputDigit;
        inputDigit.action.canceled += InputDigitRelease;
    }

    private void Update()
    {
        Debug.Log(Output);


        if (simulateInput)
        {
            if(!isInputHeld)
            {
                OnInputPressed();
                isInputHeld = true;
            }
        }
        else
        {
            if (isInputHeld)
            {
                OnInputReleased();
                isInputHeld = false;
            }
        }

        if (simulateDelete)
        {
            ResetSegment();
            simulateDelete = false;
        }

        if (simulateGo)
        {
            AcceptSegment();
            simulateGo = false;
        }
    }

    private void InputDigit(InputAction.CallbackContext context)
    {
        OnInputPressed();
    }

    private void InputDigitRelease(InputAction.CallbackContext context)
    {
        OnInputReleased();
    }

    public void ResetOutput()
    {
        output = new string[4] { "", "", "", "" };
    }

    public void ResetSegment()
    {
        Segment = "";
        display.morseString = Segment;
    }

    public void AcceptSegment()
    {
        if (Segment.Length > 0) // continue here
        {
            display.lights[Mathf.Min(segmentIndex, 3)].SetActive(true);
            segmentIndex++;

            if (segmentIndex >= 3)
            {
                //display.lights[segmentIndex].SetActive(true);
                CheckOutput();
            }
        }

        display.morseString = Segment;
    }

    // ADD CONFIRMATION CODE HERE
    public bool CheckOutput()
    {
        //return Output == confirmationString;
        return false;
    }

    public void OnInputPressed()
    {
        // Only run the first time input is held.
        if (!isInputHeld)
        {
            isInputHeld = true;
            StartCoroutine("IncreaseSliderValue");
            slider.transform.localScale = Vector3.one * 0.01f;
        }
    }

    public void OnInputReleased()
    {
        if (!isInputHeld)
        {
            return;
        }

        // Stop coroutine first to halt slider movement.
        StopAllCoroutines();

        // Add dot or dash to output string.
        if (slider.value > 0.99)
        {
            Segment += "-";
        }
        else
        {
            Segment += ".";

        }

        // Update display
        display.morseString = Segment;

        // Reset slider.
        isInputHeld = false;
        slider.value = 0;
        slider.transform.localScale = Vector3.zero;
    }

    private IEnumerator IncreaseSliderValue()
    {
        while (true)
        {
            slider.value += linearSpeed * Time.deltaTime + slider.value * exponentialSpeed / 10;
            yield return null;
        }
    }
}

// [CustomEditor(typeof(MorseController))]
public class newMorseInputEditor : Editor
{
    private bool inputToggled = false;

    public override void OnInspectorGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Exponential Speed");
        ((MorseController)target).exponentialSpeed = EditorGUILayout.FloatField(((MorseController)target).exponentialSpeed);
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        GUILayout.Label("Linear Speed");
        ((MorseController)target).linearSpeed = EditorGUILayout.FloatField(((MorseController)target).linearSpeed);
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Simulate Input"))
        {
            if (inputToggled)
            {
                ((MorseController)target).OnInputReleased();
                inputToggled = false;
            }
            else
            {
                ((MorseController)target).OnInputPressed();
                inputToggled = true;
            }

        }
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        GUILayout.TextArea(((MorseController)target).Output);
        GUILayout.EndHorizontal();

    }
}