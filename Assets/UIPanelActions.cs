using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelActions : MonoBehaviour
{
    public GameObject userLoginPanel;
    public GameObject logRegPanel;
    public GameObject RegFingerPrintPanel;
    public GameObject ContinuousAuthPanel;
    public GameObject rotationMatchingUI;

    [SerializeField] private GameObject canvas0; // pair device
    [SerializeField] private GameObject canvas1; // parity 
    [SerializeField] private GameObject canvas2; // lobby

    private void Start()
    {
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowUserLogin()
    {
        if (userLoginPanel) userLoginPanel.SetActive(true);
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowLogin()
    {
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(true);
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    // Initial auth screen: Pair Device
    public void ShowPairDeviceUI()
    {
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (RegFingerPrintPanel) RegFingerPrintPanel.SetActive(false);

        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(true);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);

        if (canvas0) canvas0.SetActive(true);
        if (canvas1) canvas1.SetActive(false);
        if (canvas2) canvas2.SetActive(false);
    }

    // Lost parity screen
    public void ShowContinuousAuth()
    {
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (RegFingerPrintPanel) RegFingerPrintPanel.SetActive(false);

        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(true);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);

        if (canvas0) canvas0.SetActive(false);
        if (canvas1) canvas1.SetActive(true);
        if (canvas2) canvas2.SetActive(false);
    }

    // Matched / rotation established
    public void ShowRotationUI()
    {
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (RegFingerPrintPanel) RegFingerPrintPanel.SetActive(false);

        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(true);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(true);

        if (canvas0) canvas0.SetActive(false);
        if (canvas1) canvas1.SetActive(false);
        if (canvas2) canvas2.SetActive(true);
    }

    public void HideContinuousAuthVisuals()
    {
        if (canvas0) canvas0.SetActive(false);
        if (canvas1) canvas1.SetActive(false);
        if (canvas2) canvas2.SetActive(false);
    }
}