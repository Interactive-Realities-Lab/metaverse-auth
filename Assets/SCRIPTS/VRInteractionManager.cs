using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class VRInteractionManager : MonoBehaviour
{
    [Header("Ray Interactors")]
    public GameObject leftRay;
    public GameObject rightRay;

    [Header("Direct Grab Interactors")]
    public GameObject leftDirectInteractor;
    public GameObject rightDirectInteractor;

    [Header("Controller Managers")]
    public ActionBasedControllerManager leftControllerManager;
    public ActionBasedControllerManager rightControllerManager;

    [Header("Gameplay Scenes")]
    public string bowlingSceneName = "Bowling";
    public string shootingSceneName = "Shooting";
    public string archerySceneName = "Archery";

    private bool isMenuOpen = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Start()
    {
        UpdateInteractionMode();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateInteractionMode();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        UpdateInteractionMode();
    }

    public void SetMenuOpen(bool open)
    {
        isMenuOpen = open;

        Debug.Log("Menu Open: " + isMenuOpen);

        UpdateInteractionMode();
    }

    private void UpdateInteractionMode()
    {
        bool gameplaySceneLoaded =
            IsSceneLoaded(bowlingSceneName) ||
            IsSceneLoaded(shootingSceneName) ||
            IsSceneLoaded(archerySceneName);

        // During gameplay, rays are disabled.
        // When menu opens, rays are enabled.
        bool enableRay = !gameplaySceneLoaded || isMenuOpen;

        // Direct grabbing stays enabled during gameplay.
        bool enableDirectGrab = gameplaySceneLoaded;

        // Prevent automatic switching between
        // ray and direct interaction during gameplay.
        if (leftControllerManager != null)
            leftControllerManager.enabled = !gameplaySceneLoaded;

        if (rightControllerManager != null)
            rightControllerManager.enabled = !gameplaySceneLoaded;

        // Enable or disable rays.
        SetActiveSafe(leftRay, enableRay);
        SetActiveSafe(rightRay, enableRay);

        // Enable or disable direct grabbing.
        SetActiveSafe(leftDirectInteractor, enableDirectGrab);
        SetActiveSafe(rightDirectInteractor, enableDirectGrab);

        Debug.Log(
            "Interaction Mode | Gameplay: " + gameplaySceneLoaded +
            " | Menu: " + isMenuOpen +
            " | Ray: " + enableRay +
            " | Direct Grab: " + enableDirectGrab
        );
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded && scene.name == sceneName)
                return true;
        }

        return false;
    }

    private void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
        {
            obj.SetActive(active);
        }
    }
}