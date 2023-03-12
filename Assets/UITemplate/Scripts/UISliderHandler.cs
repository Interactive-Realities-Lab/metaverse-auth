using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISliderHandler : MonoBehaviour
{

    [SerializeField] private Image image;
    [SerializeField] private AnimationCurve sampleCurve;
    [SerializeField] private ColorVariable dotColor, DashColor;

    private void OnEnable()
    {
        image.fillAmount = 0;
    }

    void Update()
    {
        if (image == null && image.fillAmount == 0) return;

        var sampledValue = sampleCurve.Evaluate(image.fillAmount);
        image.color = Color.Lerp(dotColor.color, DashColor.color, sampledValue);        
    }
}
