using IRLab.Tools.Timer;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlUIFeedback : MonoBehaviour
{

    [SerializeField] private Color colorSampling;
    [SerializeField] private Color colorMatching;
    [SerializeField] private Color colorNotMatching;
    [SerializeField] private Color colorPaused;

    [SerializeField] private List<Image> background;
    [SerializeField] private TMP_Text text;

    [SerializeField] private Image fillImage1;
    [SerializeField] private Image fillImage2;

    public void Sampling()
    {
        foreach (var image in background)
            image.color = colorSampling;
        text.text = "Establishing Parity .....";
    }

    public void NotMatching()
    {
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

        text.text = "Parity Lost!";
    }

    public void ParityLosing()
    {
        foreach (var image in background)
            image.color = colorNotMatching;

        if (fillImage1 != null)
        {
            fillImage1.fillAmount = 0.5f;
            fillImage1.color = colorNotMatching;
        }

        if (fillImage2 != null)
        {
            fillImage2.fillAmount = 0.5f;
            fillImage2.color = colorNotMatching;
        }

        text.text = "Parity Losing.....";
    }

    public void Matched()
    {
        foreach (var image in background)
            image.color = colorMatching;
        text.text = "Parity Established!";
    }

    public void Paused()
    {
        foreach (var image in background)
            image.color = colorPaused;
        text.text = "Move to Continue ...";
    }


}
