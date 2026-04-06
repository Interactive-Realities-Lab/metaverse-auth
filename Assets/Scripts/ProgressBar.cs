using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private HeadsetMotion headsetMotion;
    [SerializeField] private Image image;
    [SerializeField] private AnimationCurve curvedProgress;
    [SerializeField] private float smoothSpeed = 6f;

    private bool updateProgressBar = true;
    private float currentFill = 0f;  

    void Update()
    {
        if (!updateProgressBar) return;
        if (headsetMotion == null || image == null) return;

        float target = curvedProgress.Evaluate(headsetMotion.ParityProgress01());

        currentFill = Mathf.Lerp(currentFill, target, Time.deltaTime * smoothSpeed);

        image.fillAmount = Mathf.Clamp01(currentFill);
    }

    public void ResetFillAmount()
    {
        currentFill = 0f;
        if (image != null)
            image.fillAmount = 0f;
    }

    public void StartUpdateProgressBar()
    {
        updateProgressBar = true;
    }

    public void StopUpdateProgressBar()
    {
        updateProgressBar = false;
    }
}