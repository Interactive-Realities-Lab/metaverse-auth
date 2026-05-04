using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImuDebugListener : MonoBehaviour
{
    void OnEnable()
    {
        if (FingerprintWsClient.I != null)
        {
            FingerprintWsClient.I.OnImuYpr += OnImu;
        }
    }

    void OnDisable()
    {
        if (FingerprintWsClient.I != null)
        {
            FingerprintWsClient.I.OnImuYpr -= OnImu;
        }
    }

    void OnImu(float pitch, float roll, float yaw)
    {
        Debug.Log($"[IMU DATA] P:{pitch:F2} R:{roll:F2} Y:{yaw:F2}");
    }
}
