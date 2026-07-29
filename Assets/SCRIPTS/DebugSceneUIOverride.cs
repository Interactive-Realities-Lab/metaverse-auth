using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug-scene-only UI override.
///
/// While the additive Debug scene is loaded:
/// 1. Keeps the debug panel active.
/// 2. Hides the parity-lost panel from the Authentication scene.
///
/// Attach this to a separate, always-active GameObject in DebugScene.
/// Do not attach it to DebugPanel itself.
/// </summary>
public class DebugSceneUIOverride : MonoBehaviour
{
    [Header("Debug Scene")]
    [SerializeField] private string debugSceneName = "DebugScene";

    [Header("Debug UI")]
    [SerializeField] private GameObject debugPanelRoot;

    [Header("Authentication UI")]
    [Tooltip("Exact GameObject name of the panel shown when parity is lost.")]
    [SerializeField] private string parityLostPanelName =
        "ParityLostPanel";

    [Header("Optional")]
    [Tooltip("The Authentication scene name. Leave empty to search every loaded scene.")]
    [SerializeField] private string authenticationSceneName =
        "Authentication";

    private GameObject _parityLostPanel;

    private void Awake()
    {
        // Keep this controller active even if another script disables
        // objects inside the debug canvas.
        FindParityLostPanel();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindParityLostPanel();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindParityLostPanel();
    }

    private void LateUpdate()
    {
        if (!IsDebugSceneLoaded())
            return;

        // Force the debug panel to remain visible.
        if (debugPanelRoot != null &&
            !debugPanelRoot.activeSelf)
        {
            debugPanelRoot.SetActive(true);
        }

        // The Authentication script may enable this panel during Update.
        // Disabling it in LateUpdate makes this debug-scene override win
        // before the frame is rendered.
        if (_parityLostPanel == null)
            FindParityLostPanel();

        if (_parityLostPanel != null &&
            _parityLostPanel.activeSelf)
        {
            _parityLostPanel.SetActive(false);
        }
    }

    private bool IsDebugSceneLoaded()
    {
        Scene debugScene =
            SceneManager.GetSceneByName(debugSceneName);

        return debugScene.IsValid() &&
               debugScene.isLoaded;
    }

    private void FindParityLostPanel()
    {
        _parityLostPanel = null;

        for (int i = 0;
             i < SceneManager.sceneCount;
             i++)
        {
            Scene scene =
                SceneManager.GetSceneAt(i);

            if (!scene.isLoaded)
                continue;

            if (!string.IsNullOrWhiteSpace(
                    authenticationSceneName) &&
                scene.name != authenticationSceneName)
            {
                continue;
            }

            GameObject[] roots =
                scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                Transform[] children =
                    root.GetComponentsInChildren<Transform>(
                        true);

                foreach (Transform child in children)
                {
                    if (child.gameObject.name ==
                        parityLostPanelName)
                    {
                        _parityLostPanel =
                            child.gameObject;

                        Debug.Log(
                            "DebugSceneUIOverride found " +
                            $"'{parityLostPanelName}' in " +
                            $"scene '{scene.name}'.");

                        return;
                    }
                }
            }
        }
    }
}