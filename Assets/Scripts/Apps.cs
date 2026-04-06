using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class Apps : MonoBehaviour
{
    public void OpenScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is empty.");
        }
    }

    public void QuitApp()
    {
        Application.Quit();
        Debug.Log("Quit pressed");
    }
}
