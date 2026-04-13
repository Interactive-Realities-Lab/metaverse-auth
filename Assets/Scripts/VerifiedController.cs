using System.Collections;
using UnityEngine;
using TMPro;

public class VerifiedController : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float verifiedDelay = 2f;
    [SerializeField] private UIPanelActions panelActions;

    private Coroutine verifiedRoutine;
    private string currentUserName;

    private void OnEnable()
    {
        if (FingerprintWsClient.I != null)
            FingerprintWsClient.I.OnDeviceMessage += HandleDeviceMessage;
    }

    private void OnDisable()
    {
        if (FingerprintWsClient.I != null)
            FingerprintWsClient.I.OnDeviceMessage -= HandleDeviceMessage;
    }

    private void HandleDeviceMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;

        string lower = msg.Trim().ToLowerInvariant();
        Debug.Log("Device message received: " + lower);

        if (lower.Contains("verified"))
        {
            Debug.Log("Verified detected");

            if (verifiedRoutine != null)
                StopCoroutine(verifiedRoutine);

            verifiedRoutine = StartCoroutine(SwitchToContinuousUI());
        }
    }

    private IEnumerator SwitchToContinuousUI()
    {
        Debug.Log("Waiting before switching UI...");
        yield return new WaitForSeconds(verifiedDelay);

        Debug.Log("Calling ShowMotion()");
        if (panelActions != null)
        {
            panelActions.ShowPairDeviceUI();
        }
        else
        {
            Debug.LogWarning("panelActions is not assigned!");
        }

        if (FingerprintWsClient.I != null && !string.IsNullOrEmpty(currentUserName))
        {
            Debug.Log("Starting continuous for: " + currentUserName);
            FingerprintWsClient.I.StartContinuous(currentUserName);
        }
        else
        {
            Debug.LogWarning("Current user name is empty, so StartContinuous was not called.");
        }
    }

    public void SetCurrentUser(string userName)
    {
        currentUserName = userName;
        Debug.Log("Current user set: " + currentUserName);
    }
}