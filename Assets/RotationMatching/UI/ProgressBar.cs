using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{

    [SerializeField] private MatchingAngles matchingAngles;
    [SerializeField] private Image image;

    private bool updateProgressBar = false;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!updateProgressBar) return;

        float t = (float)matchingAngles.currentMatchSamples / (float)matchingAngles.MatchMaxSamples;
        image.fillAmount = t;

    }

    public void StartUpdateProgressBar()
    {
        updateProgressBar = true;
    }
    public void StopUpdateProgressBar()
    {
        updateProgressBar = false;
    }

    public void ResetFillAmout()
    {
        image.fillAmount = 1f;
    }
}
