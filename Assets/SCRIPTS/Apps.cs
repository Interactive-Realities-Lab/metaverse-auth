using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Apps : MonoBehaviour
{
    public void OpenScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            if (SceneFlow.Instance != null)
            {
                SceneFlow.Instance.OpenContentScene(sceneName);
            }
            else
            {
                Debug.LogError("SceneFlow.Instance is null.");
            }
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