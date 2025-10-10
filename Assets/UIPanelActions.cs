using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelActions : MonoBehaviour
{
    public GameObject logRegPanel;
    public GameObject motionPanel;

    public void ShowMotion()
    {
        if (logRegPanel) logRegPanel.SetActive(false);
        if (motionPanel) motionPanel.SetActive(true);
    }

    public void ShowLogin()
    {
        if (motionPanel) motionPanel.SetActive(false);
        if (logRegPanel) logRegPanel.SetActive(true);
    }
}
