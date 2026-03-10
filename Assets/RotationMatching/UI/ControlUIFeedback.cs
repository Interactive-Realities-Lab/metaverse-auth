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

    public void Sampling()
    {
        foreach (var image in background)
            image.color = colorSampling;
        text.text = "Establishing Parity ...";
    }

    public void NotMatching()
    {
        foreach (var image in background)
            image.color = colorNotMatching;
        text.text = "Parity Lost.";
    }

    public void Matched()
    {
        foreach (var image in background)
            image.color = colorMatching;
        text.text = "Parity Established";
    }

    public void Paused()
    {
        foreach (var image in background)
            image.color = colorPaused;
        text.text = "Move to Continue ...";
    }


}
