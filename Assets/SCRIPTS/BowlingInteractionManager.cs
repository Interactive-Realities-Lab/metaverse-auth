using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BowlingInteractionManager : MonoBehaviour
{
   [Header("Ray Interactors")]
    public GameObject leftRay;
    public GameObject rightRay;

    [Header("Direct Grab Interactors")]
    public GameObject leftDirectInteractor;
    public GameObject rightDirectInteractor;

    [Header("Scene Names")]
    public string bowlingSceneName = "Bowling";

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

    private void UpdateInteractionMode()
    {
        bool bowlingLoaded = IsSceneLoaded(bowlingSceneName);

        // Bowling: no ray, direct grab only
        SetActiveSafe(leftRay, !bowlingLoaded);
        SetActiveSafe(rightRay, !bowlingLoaded);

        SetActiveSafe(leftDirectInteractor, bowlingLoaded);
        SetActiveSafe(rightDirectInteractor, bowlingLoaded);
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
            obj.SetActive(active);
    }
}
