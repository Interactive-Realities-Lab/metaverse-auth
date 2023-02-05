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

    [SerializeField] private Image background;
    [SerializeField] private TMP_Text text;

    public void Sampling()
    {
        background.color = colorSampling;
        text.text = "Stabilishing Parity ...";
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
    }

    public void Paused()
    {
        background.color = colorPaused;
        text.text = "Move to Continue ...";
    }


}
