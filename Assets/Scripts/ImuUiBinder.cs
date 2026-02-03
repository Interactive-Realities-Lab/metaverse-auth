using UnityEngine;
using TMPro;

public class ImuUiBinder : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text imuText; // drag your IMU text label here

    void OnEnable()
    {
        if (FingerprintWsClient.I != null)
            FingerprintWsClient.I.OnImuYpr += OnImu;
    }

    void OnDisable()
    {
        if (FingerprintWsClient.I != null)
            FingerprintWsClient.I.OnImuYpr -= OnImu;
    }

    private void OnImu(float pitch, float roll, float yaw)
    {
        if (imuText == null) return;
        imuText.text = $"IMU | Yaw: {yaw:F1}°  Pitch: {pitch:F1}°  Roll: {roll:F1}°";
    }
}
