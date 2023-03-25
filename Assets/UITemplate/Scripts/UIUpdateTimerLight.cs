using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UIUpdateTimerLight : MonoBehaviour
{
    [SerializeField] private Image[] timerLights = new Image[4];
    [SerializeField] private MorseCodeGenerator morseCodeGenerator;
    //[SerializeField] private Image timerLight;

    private bool allowDisplayDelay = false;
    private int currentIndex;

    private void OnEnable()
    {
        Reset();
    }

    private void Reset()
    {
        currentIndex = 0;
        foreach (var light in timerLights)
        {
            light.fillAmount = 0f;
            light.transform.GetChild(0).gameObject.SetActive(false);
            light.gameObject.SetActive(false);
        }
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

        UpdateUI();
    }

    public void SetLights(int index)
    {
        if (index >= timerLights.Length) return;

        if (index > 0)
            SetFinished(index - 1);

        SetCurrent(index);
    }

    public void SetCurrent(int index)
    {
        timerLights[index].gameObject.SetActive(true);
        currentIndex = index;
    }

    public void SetFinished(int index)
    { 
       timerLights[index].gameObject.SetActive(false);
    }

    public void LockDisplayDelay()
    {
        timerLights[currentIndex].fillAmount = 0f;
        timerLights[currentIndex].transform.GetChild(0).gameObject.SetActive(false);        
        allowDisplayDelay = false;
    }
    private void UpdateUI()
    {
        timerLights[currentIndex].transform.GetChild(0).gameObject.SetActive(true);
        timerLights[currentIndex].fillAmount = Mathf.InverseLerp(1f, morseCodeGenerator.RepeatDelay, morseCodeGenerator.ActionDelay);

    }

    public void AllowDisplayDelay()
    {
        allowDisplayDelay = true;
    }

}
