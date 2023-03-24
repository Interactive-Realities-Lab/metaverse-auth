using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class MorseControllerV2 : MonoBehaviour
{
    public InputActionReference inputDigit = null;
    [SerializeField] XRRayInteractor ray;
    [SerializeField] TOTP totp;
    [SerializeField] private UnityEvent OnMorseCodeCorrect;
    [SerializeField] private UnityEvent OnMorseCodeIncorrect;

    public bool IsInputHeld { get; private set; } = false;

    public float HeldValue { get; private set; }

    private float speed = 1f;
    [SerializeField] private AnimationCurve curve;

    public bool LockMorseInput { get; set; } = true;

    public string CurrentSegment
    {
        get
        {
            return listOfSegments[segmentIndex];
        }
        set
        {
            listOfSegments[segmentIndex] = value;
        }
    }
    public string Output
    {
        get
        {
            return string.Join(" ", listOfSegments).Trim();
        }

    }

    private int segmentIndex = 0;
    private string[] listOfSegments;

    [Range(0f, 1f)]
    [SerializeField] private float dotThreasholdValue;

    private void OnEnable()
    {
        Reset();
    }

    private void Awake()
    {
        inputDigit.action.started += OnPress;
        inputDigit.action.canceled += OnRelease;
    }

    private void Reset()
    {
        segmentIndex = 0;
        listOfSegments = new string[4] { "", "", "", "" };
        LockMorseInput = true;
        IsInputHeld = false;
        ClearCurrentSegment();
    }

    public void ClearCurrentSegment()
    {
        CurrentSegment = "";
    }

    private void OnPress(InputAction.CallbackContext context)
    {
        if (LockMorseInput) return;

        //Ignore morse input if clicking in UI Buttons.
        ray.TryGetCurrentRaycast(out var _hit, out var _index, out var ui_hit, out var ttt, out var isHitUI);
        if (ui_hit != null)
        {
            if (ui_hit.Value.gameObject.GetComponentInParent<Button>() != null)
                return;
        }

        if (CurrentSegment.Length > 3) return;

        if (!IsInputHeld)
        {
            IsInputHeld = true;
            StartCoroutine("IncreaseValue");
        }
    }
    private void OnRelease(InputAction.CallbackContext context)
    {
        if (LockMorseInput) return;
        if (!IsInputHeld) return;

        if (CurrentSegment.Length > 3)
        {
            IsInputHeld = false;
            return;
        }

        //StopAllCoroutines();
        StopCoroutine("IncreaseValue");

        CurrentSegment += HeldValue > dotThreasholdValue ? "-" : ".";

        //Debug.Log(CurrentSegment);

        HeldValue = 0;
        IsInputHeld = false;
    }

    public void AddSegment()
    {
        if (CurrentSegment.Length == 0) return;

        listOfSegments[segmentIndex] = CurrentSegment;

        if (segmentIndex < 3)
        {
            segmentIndex = Mathf.Min(3, ++segmentIndex);
            return;
        }

        if (!totp.checkCode(Output))
        {
            //Debug.Log("Wrong Code");
            OnMorseCodeIncorrect?.Invoke();
            return;
        }

        //Debug.Log("Correct Code");
        OnMorseCodeCorrect?.Invoke();
        
    }


    private IEnumerator IncreaseValue()
    {
        float currentVal = 0f;
        while (true)
        {
            currentVal += speed * Time.deltaTime;
            HeldValue = curve.Evaluate(currentVal);
            //Debug.Log(HeldValue);
            yield return null;
        }
    }

}
