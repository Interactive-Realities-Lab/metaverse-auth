using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISliderHandler : MonoBehaviour
{

    [SerializeField] private Image image;
    [SerializeField] private AnimationCurve sampleCurve;
    [SerializeField] private ColorVariable dotColor, DashColor;
    [SerializeField] private MorseControllerV2 morseController;

    [Range(0f, 1f)]
    [SerializeField] private float startFillValue;

    private void OnEnable()
    {
        image.fillAmount = 0;
    }

    void Update()
    {
        if (!morseController.IsInputHeld)
        {
            image.fillAmount = 0;
            return;
        }

        image.fillAmount = morseController.HeldValue + startFillValue;

        var sampledValue = sampleCurve.Evaluate(image.fillAmount);
        image.color = Color.Lerp(dotColor.color, DashColor.color, sampledValue);        
    }
}
