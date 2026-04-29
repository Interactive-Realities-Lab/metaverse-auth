using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelActions : MonoBehaviour
{
    public GameObject userLoginPanel;
    public GameObject logRegPanel;
    public GameObject RegFingerPrintPanel;
    public GameObject ContinuousAuthPanel;

    [SerializeField] private UIFeedbackPlayArea tinyRotationFeedback;
    [SerializeField] private CanvasGroup rotationMatchingUI;
    [SerializeField] private GameObject canvas0; // pair device
    [SerializeField] private GameObject canvas1; // parity 
    [SerializeField] private GameObject canvas2; // lobby

    private void Start()
    {
        SetCanvasGroup(rotationMatchingUI, false);
    }

    private void SetCanvasGroup(CanvasGroup cg, bool show)
    {
        if (cg == null) return;

        cg.alpha = show ? 1f : 0f;
        cg.interactable = show;
        cg.blocksRaycasts = show;
    }

    public void ShowUserLogin()
    {
        if (userLoginPanel) userLoginPanel.SetActive(true);
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        SetCanvasGroup(rotationMatchingUI, false);
    }

    public void ShowLogin()
    {
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(true);
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
        SetCanvasGroup(rotationMatchingUI, false);
    }

    // Initial auth screen: Pair Device
    public void ShowPairDeviceUI()
    {
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (RegFingerPrintPanel) RegFingerPrintPanel.SetActive(false);

        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(true);
        SetCanvasGroup(rotationMatchingUI, false);

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
        SetCanvasGroup(rotationMatchingUI, false);

        if (tinyRotationFeedback != null)
            tinyRotationFeedback.HideNow();

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
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
       
        SetCanvasGroup(rotationMatchingUI, true);
    }

    public void HideContinuousAuthVisuals()
    {
        if (canvas0) canvas0.SetActive(false);
        if (canvas1) canvas1.SetActive(false);
        if (canvas2) canvas2.SetActive(false);
    }
}