using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class BowlingInteractionManager : MonoBehaviour
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

    [Header("Scene Names")]
    public string bowlingSceneName = "Bowling";

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

    // Called when menu opens or closes
    public void SetMenuOpen(bool open)
    {
        isMenuOpen = open;

        UpdateInteractionMode();
    }

    private void UpdateInteractionMode()
    {
        bool bowlingLoaded = IsSceneLoaded(bowlingSceneName);

        bool enableRay = !bowlingLoaded || isMenuOpen;
        bool enableDirectGrab = bowlingLoaded;

        // Disable automatic controller interaction switching
        // while Bowling is loaded.
        if (leftControllerManager != null)
            leftControllerManager.enabled = !bowlingLoaded;

        if (rightControllerManager != null)
            rightControllerManager.enabled = !bowlingLoaded;

        // Control ray visibility
        SetActiveSafe(leftRay, enableRay);
        SetActiveSafe(rightRay, enableRay);

        // Control direct grabbing
        SetActiveSafe(leftDirectInteractor, enableDirectGrab);
        SetActiveSafe(rightDirectInteractor, enableDirectGrab);
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);

            if (s.isLoaded && s.name == sceneName)
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