using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelActions : MonoBehaviour
{
    public GameObject userLoginPanel;
    public GameObject logRegPanel;
    public GameObject motionPanel;
    public GameObject rotationMatchingUI;

    private void Start()
    {
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowMotion()
    {
        Debug.Log("ShowMotion called");
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (motionPanel) motionPanel.SetActive(true);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowLogin()
    {
       
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(true);
        if (motionPanel) motionPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowUserLogin()
    {
        if (userLoginPanel) userLoginPanel.SetActive(true);
        if (motionPanel) motionPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowRotationUI()
    {
        Debug.Log("ShowRotationUI called");

        if (motionPanel) motionPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(true);
    }

    public void HideRotationUI()
    {
        Debug.Log("HideRotationUI called");

        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }
}
