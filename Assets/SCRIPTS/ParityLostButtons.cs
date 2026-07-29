using UnityEngine;

public class ParityLostButtons : MonoBehaviour
{
    [SerializeField] private GameObject continuousAuthPanel;

    public void OpenDebugScene()
    {
        if (SceneFlow.Instance != null)
        {
            SceneFlow.Instance.OpenContentScene("Debug");
        }

        // Hide the blocking parity-loss panel in DebugScene
        if (continuousAuthPanel != null)
        {
            continuousAuthPanel.SetActive(false);
        }
    }
}