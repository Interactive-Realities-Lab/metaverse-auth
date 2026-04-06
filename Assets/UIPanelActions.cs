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
    public GameObject LobbyPanel;

    private void Start()
    {
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowContinuousAuth()
    {
        //Debug.Log("ShowMotion called");
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(true);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
        if (RegFingerPrintPanel) RegFingerPrintPanel.SetActive(false);
    }

    public void ShowLogin()
    {
       
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(true);
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowUserLogin()
    {
        if (userLoginPanel) userLoginPanel.SetActive(true);
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    public void ShowRotationUI()
    {
        //Debug.Log("ShowRotationUI called");

        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(true);
        if (LobbyPanel) LobbyPanel.SetActive(true);
    }

    public void HideRotationUI()
    {
        //Debug.Log("HideRotationUI called");

        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
    }

    /*public void ShowLobby()
    {
        //Debug.Log("ShowLobby called");
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (ContinuousAuthPanel) ContinuousAuthPanel.SetActive(false);
        if (rotationMatchingUI) rotationMatchingUI.SetActive(false);
        if (RegFingerPrintPanel) RegFingerPrintPanel.SetActive(false);
        if (LobbyPanel) LobbyPanel.SetActive(true);
    }*/
}
