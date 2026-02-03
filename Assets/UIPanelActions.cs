using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelActions : MonoBehaviour
{
    public GameObject userLoginPanel;
    public GameObject logRegPanel;
    public GameObject motionPanel;


    public void ShowMotion()
    {
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
        if (motionPanel) motionPanel.SetActive(true);
    }

    public void ShowLogin()
    {
       
        if (userLoginPanel) userLoginPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(true);
        if (motionPanel) motionPanel.SetActive(false);
    }

    public void ShowUserLogin()
    {
        if (userLoginPanel) userLoginPanel.SetActive(true);
        if (motionPanel) motionPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(false);
    }
}
