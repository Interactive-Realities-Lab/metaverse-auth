using IRLab.Tools.Timer;
using System.Collections;
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

    private float desiredAlpha;
    private float currentAlpha;

    private bool allowFade;



    public void Sampling()
    {
        if (!allowFade) return;
        foreach (var image in background)
            image.color = colorSampling;
        text.text = "Establishing Parity ...";
        desiredAlpha = 1;
        currentAlpha = 0;

        allowFade = true;
    }

    public void NotMaching()
    {
        foreach (var image in background)
            image.color = colorNotMatching;
        text.text = "Parity Lost.";
    }

    public void Mached()
    {
        foreach (var image in background)
            image.color = colorMatching;
        text.text = "Parity Etablished";
        desiredAlpha = 0;
        currentAlpha = 1;

        Timer.Create(() => allowFade = true, 2f);
        
    }

    void Update()
    {
        if (!allowFade) return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, desiredAlpha, Time.deltaTime);



        canvas.alpha = fadeCurve.Evaluate(currentAlpha);
    }

}
