using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFlow : MonoBehaviour
{
    public static SceneFlow Instance;
    public GameObject currentLobbyUIRoot;

    [Header("Scene Names")]
    public string authenticationSceneName = "Authentication";

    private string currentContentScene = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void OpenContentScene(string sceneName)
    {
        StartCoroutine(OpenContentSceneRoutine(sceneName));
    }

    IEnumerator OpenContentSceneRoutine(string sceneName)
    {
        DebugLoadedScenes("Before opening " + sceneName);

        if (!string.IsNullOrEmpty(currentContentScene) &&
            SceneManager.GetSceneByName(currentContentScene).isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(currentContentScene);
        }

        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        currentContentScene = sceneName;

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        // Find Lobby UI root (IMPORTANT: set tag or name)
        currentLobbyUIRoot = GameObject.FindWithTag("LobbyUI"); 

        DebugLoadedScenes("After opening " + sceneName);
    }

    public void ShowContinuousAuthUI()
    {
        Debug.Log("Showing parity lost UI, hiding lobby");

        // Hide lobby UI
        if (currentLobbyUIRoot != null)
            currentLobbyUIRoot.SetActive(false);

        // Show auth UI
        var ui = FindObjectOfType<UIPanelActions>(true);
        if (ui != null)
            ui.ShowContinuousAuth();
    }

    public void RestoreLobbyUI()
    {
        Debug.Log("Restoring lobby UI, hiding parity UI");

        // Hide auth visuals
        var ui = FindObjectOfType<UIPanelActions>(true);
        if (ui != null)
            ui.HideContinuousAuthVisuals();

        // Show lobby UI
        if (currentLobbyUIRoot != null)
            currentLobbyUIRoot.SetActive(true);
    }

    IEnumerator ReturnToAuthenticationRoutine()
    {
        if (!string.IsNullOrEmpty(currentContentScene) &&
            SceneManager.GetSceneByName(currentContentScene).isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(currentContentScene);
        }

        currentContentScene = "";

        Scene authScene = SceneManager.GetSceneByName(authenticationSceneName);
        if (authScene.IsValid() && authScene.isLoaded)
        {
            SceneManager.SetActiveScene(authScene);
        }

        var ui = FindObjectOfType<UIPanelActions>(true);
        if (ui != null)
            ui.ShowLogin();
    }

    public void OnAuthFailed()
    {
        StartCoroutine(ReturnToAuthenticationRoutine());
    }

    private void DebugLoadedScenes(string label)
    {
        Debug.Log("==== " + label + " ====");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            Debug.Log($"Scene {i}: {s.name}, loaded={s.isLoaded}");
        }
        Debug.Log("Active Scene: " + SceneManager.GetActiveScene().name);
    }
}