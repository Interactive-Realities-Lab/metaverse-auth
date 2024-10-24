using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class MorseController : MonoBehaviour
{


    [Header("References")]
    [SerializeField] XRRayInteractor ray;
    [SerializeField] TOTP totp;
    public Slider slider;
    public MorseCodeDisplayer display;
    public InputActionReference inputDigit = null;

    [Header("Input Config")]
    public bool simulateInput = false;
    public bool simulateDelete = false;
    public bool simulateGo = false;
    [Range(0, 1)] public float exponentialSpeed = 0.3f;
    public float linearSpeed = 1;

    [SerializeField] private UnityEvent OnMorseCodeCorrect;
    [SerializeField] private UnityEvent OnMorseCodeIncorrect;

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
    private string[] output = new string[4] { "", "", "", "" };
    private bool isInputHeld = false;


    public bool LockMorseInput { get; set; }

    private void OnEnable()
    {
        LockMorseInput = true;
        ResetAll();
    }

    private void Awake()
    {
        gameObject.SetActive(false);

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
        Debug.Log(Segment);
        /*        Debug.Log(Output);


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
                }*/
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

    public void ClearSegment()
    {
        Segment = "";
        display.morseString = Segment;
    }

    public void ResetAll()
    {
        ClearSegment();
        segmentIndex = 0;
    }

    public void AcceptSegment()
    {
        if (Segment.Length > 0) // continue here
        {
            if (segmentIndex >= 3)
            {
                if (!totp.checkCode(Output))
                {
                    OnMorseCodeIncorrect?.Invoke();
                    return;
                }
                OnMorseCodeCorrect?.Invoke();

            }

            //display.lights[Mathf.Min(segmentIndex, 3)].SetActive(true);
            segmentIndex = Mathf.Min(3, segmentIndex + 1);


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
        if (LockMorseInput) return;

        ray.TryGetCurrentRaycast(out var _hit, out var _index, out var ui_hit, out var ttt, out var isHitUI);

        if (isHitUI) return;

        // If max digit limit reached don't execute.
        if (Segment.Length > 3)
        {
            return;
        }

        // Only run the first time input is held.
        if (!isInputHeld)
        {
            isInputHeld = true;
            StartCoroutine("IncreaseSliderValue");
            slider.transform.localScale = Vector3.one * 2f;
        }
    }

    public void OnInputReleased()
    {
 
        if (LockMorseInput) return;

        ray.TryGetCurrentRaycast(out var _hit, out var _index, out var ui_hit, out var ttt, out var isHitUI);

        if (isHitUI) return;
        // If max digit limit reached don't execute.
        if (Segment.Length > 3)
        {
            isInputHeld = false;
            return;
        }

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



//[CustomEditor(typeof(MorseController))]
//public class newMorseInputEditor : Editor
//{
//    private bool inputToggled = false;

//    public override void OnInspectorGUI()
//    {
//        GUILayout.BeginHorizontal();
//        GUILayout.Label("Exponential Speed");
//        ((MorseController)target).exponentialSpeed = EditorGUILayout.FloatField(((MorseController)target).exponentialSpeed);
//        GUILayout.EndHorizontal();



//        GUILayout.BeginHorizontal();
//        GUILayout.Label("Linear Speed");
//        ((MorseController)target).linearSpeed = EditorGUILayout.FloatField(((MorseController)target).linearSpeed);
//        GUILayout.EndHorizontal();



//        GUILayout.BeginHorizontal();
//        if (GUILayout.Button("Simulate Input"))
//        {
//            if (inputToggled)
//            {
//                ((MorseController)target).OnInputReleased();
//                inputToggled = false;
//            }
//            else
//            {
//                ((MorseController)target).OnInputPressed();
//                inputToggled = true;
//            }

//        }
//        GUILayout.EndHorizontal();



//        GUILayout.BeginHorizontal();
//        GUILayout.TextArea(((MorseController)target).Output);
//        GUILayout.EndHorizontal();

//    }
//}