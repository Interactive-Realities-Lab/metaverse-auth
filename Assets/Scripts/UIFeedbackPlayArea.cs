using IRLab.Tools.Timer;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFeedbackPlayArea : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvas;
    [SerializeField] private Color colorSampling;
    [SerializeField] private Color colorMatching;
    [SerializeField] private Color colorNotMatching;

    [SerializeField] private AnimationCurve fadeCurve;

    [SerializeField] private List<Image> background;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Image fillImage1;
    [SerializeField] private Image fillImage2;
    //[SerializeField] private ProgressBar fillProgressBar;

    private float desiredAlpha;
    private float currentAlpha;
    private bool allowFade;

    public void Sampling()
    {
        Debug.Log("Tiny UIFeedbackPlayArea.Sampling called");

        foreach (var image in background)
            image.color = colorSampling;

        text.text = "Establishing Parity...";
        currentAlpha = 1f;
        desiredAlpha = 1f;
        canvas.alpha = 1f;
        allowFade = false;
    }

    public void NotMatching()
    {
        /*if (fillProgressBar != null)
            fillProgressBar.StopUpdateProgressBar();*/

        foreach (var image in background)
            image.color = colorNotMatching;

        if (fillImage1 != null)
        {
            fillImage1.fillAmount = 1f;
            fillImage1.color = colorNotMatching;
        }

        if (fillImage2 != null)
        {
            fillImage2.fillAmount = 1f;
            fillImage2.color = colorNotMatching;
        }

        text.text = "Parity Lost.";
        currentAlpha = 1f;
        desiredAlpha = 1f;
        canvas.alpha = 1f;
        allowFade = false;
    }

    public void Matched()
    {
        Debug.Log("Tiny UIFeedbackPlayArea.Matched called");

        foreach (var image in background)
            image.color = colorMatching;

        text.text = "Parity Established";
        currentAlpha = 1f;
        desiredAlpha = 1f;
        canvas.alpha = 1f;
        allowFade = false;

        Timer.Create(() =>
        {
            desiredAlpha = 0f;
            allowFade = true;
        }, 2f);
    }

    void Update()
    {
        if (!allowFade) return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, desiredAlpha, Time.deltaTime);
        canvas.alpha = fadeCurve.Evaluate(currentAlpha);
    }
}
