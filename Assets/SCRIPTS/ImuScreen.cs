using TMPro;
using UnityEngine;

public class ImuScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;

    [TextArea]
    [SerializeField] private string currentMessage;

    private void OnEnable()
    {
        if (FingerprintWsClient.I != null)
        {
            FingerprintWsClient.I.OnDeviceMessage += HandleDeviceMessage;
        }
    }

    private void OnDisable()
    {
        if (FingerprintWsClient.I != null)
        {
            FingerprintWsClient.I.OnDeviceMessage -= HandleDeviceMessage;
        }
    }

    void HandleDeviceMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            return;

        currentMessage = msg;

        Debug.Log("[IMU SCREEN] " + msg);

        if (debugText != null)
        {
            debugText.text = msg;
        }
    }
}