using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIUpdateVibrationDelayMessage : MonoBehaviour
{
    [SerializeField] private MorseCodeGenerator morseCodeGenerator;
    [SerializeField] private TMP_Text textfield;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] string defaultText;
    private bool allowDisplayDelay = false;


    private void OnEnable()
    {
        canvasGroup.alpha = 0f;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!allowDisplayDelay) return;

        if (morseCodeGenerator.ActionDelay <= 1)
        {
            LockDisplayDelay();
            return;
        }

        if (!(canvasGroup.alpha == 1f))
            canvasGroup.alpha = 1f;

        UpdateUI();
    }

    public void LockDisplayDelay()
    {
        canvasGroup.alpha = 0f;
        allowDisplayDelay = false;
    }

    private void UpdateUI()
    {
        textfield.text = defaultText + " " + (int)morseCodeGenerator.ActionDelay;

    }

    public void AllowDisplayDelay()
    {
        allowDisplayDelay = true;
    }
}
