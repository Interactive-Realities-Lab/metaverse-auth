using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MorseControllerV2 : MonoBehaviour
{
    public InputActionReference inputDigit = null;

    public bool IsInputHeld { get; private set; } = false;
 
    public float HeldValue { get; private set; }

    private float speed = 1f;
    [SerializeField] private AnimationCurve curve;

    private void Awake()
    {
        inputDigit.action.started += OnPress;
        inputDigit.action.canceled += OnRelease;
    }

    private void OnPress(InputAction.CallbackContext context) 
    {
        //Replace this
        if (!gameObject.activeInHierarchy) return;
        
        Debug.Log("Button Pressed!");

        if(!IsInputHeld)
        {
            IsInputHeld = true;
            StartCoroutine("IncreaseValue");
        }
    }
    private void OnRelease(InputAction.CallbackContext context) 
    {
        
        StopAllCoroutines();


        HeldValue = 0;
        IsInputHeld = false;
    }

    
    private IEnumerator IncreaseValue()
    {
        float currentVal = 0f;
        while (true)
        {
            currentVal += speed * Time.deltaTime;
            HeldValue = curve.Evaluate(currentVal);
            Debug.Log(HeldValue);
            yield return null;
        }
    }

}
