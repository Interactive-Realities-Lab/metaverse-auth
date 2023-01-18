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

    [SerializeField] private Image background;
    [SerializeField] private TMP_Text text;

    private float desiredAlpha;
    private float currentAlpha;

    private bool allowFade;



    public void Sampling()
    {
        if (!allowFade) return;
        background.color = colorSampling;
        text.text = "Stabilishing Parity ...";
        desiredAlpha = 1;
        currentAlpha = 0;

        allowFade = true;
    }

    public void NotMaching()
    {
        background.color = colorNotMatching;
        text.text = "Parity Lost. \n Press Space to start over.";
    }

    public void Mached()
    {
        background.color = colorMatching;
        text.text = "Parity Stablished";
        desiredAlpha = 0;
        currentAlpha = 1;

        Timer.Create(() => allowFade = true, 2f);
        
    }

    void Update()
    {
        if (!allowFade) return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, desiredAlpha, 2.0f * Time.deltaTime);
        canvas.alpha = currentAlpha;
    }

}
