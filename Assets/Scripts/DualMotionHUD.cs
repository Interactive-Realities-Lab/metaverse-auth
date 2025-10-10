using UnityEngine;
using UnityEngine.XR;
using TMPro;

public class DualMotionHUD : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text questText;   // top row
    public TMP_Text imuText;     // bottom row

    [Header("Display")]
    [Range(0f, 1f)] public float smoothing = 0.2f;
    public int decimals = 1;

    [Header("Zeroing behavior")]
    public bool questZeroOnStart = true;     // zero Quest on first frame
    public bool imuZeroOnFirstPacket = true; // zero IMU when first packet arrives

    // --- Quest state ---
    private Vector3 qSmoothed;
    private float qYaw0, qPitch0, qRoll0;
    private bool qZeroed;

    // --- IMU state (incoming, degrees) ---
    private float iYawRaw, iPitchRaw, iRollRaw;
    private Vector3 iSmoothed;
    private float iYaw0, iPitch0, iRoll0;
    private bool iZeroed;                 // has baseline been captured?
    private bool iHasPacket;              // have we received at least one packet?

    void Awake()
    {
        if (questText) { questText.enableWordWrapping = false; questText.overflowMode = TextOverflowModes.Overflow; }
        if (imuText) { imuText.enableWordWrapping = false; imuText.overflowMode = TextOverflowModes.Overflow; }
    }

    void Update()
    {
        string fmt = "F" + decimals;
        float t = 1f - Mathf.Pow(1f - smoothing, Time.deltaTime * 60f);

        // ---------- QUEST ----------
        var head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        if (head.isValid && head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q))
        {
            Vector3 e = q.eulerAngles;

            if (questZeroOnStart && !qZeroed)
            {
                qYaw0 = Wrap180(e.y);
                qPitch0 = Wrap180(e.x);
                qRoll0 = Wrap180(e.z);
                qZeroed = true;
            }

            float qPitch = Wrap180(e.x) - qPitch0;
            float qYaw = Wrap180(e.y) - qYaw0;
            float qRoll = Wrap180(e.z) - qRoll0;

            qSmoothed.x = Mathf.LerpAngle(qSmoothed.x, qPitch, t);
            qSmoothed.y = Mathf.LerpAngle(qSmoothed.y, qYaw, t);
            qSmoothed.z = Mathf.LerpAngle(qSmoothed.z, qRoll, t);

            if (questText)
                questText.text = $"QUEST  |  Pitch: {qSmoothed.x.ToString(fmt)}°  " +
                                 $"Yaw: {qSmoothed.y.ToString(fmt)}°  ";
                                 //$"Roll: {qSmoothed.z.ToString(fmt)}°";
        }

        // Quest's YAW = IMU's Roll

        // ---------- IMU ----------
        // If we want IMU to start at 0, capture baseline AFTER first packet arrives
        if (imuZeroOnFirstPacket && iHasPacket && !iZeroed)
        {
            iYaw0 = iYawRaw; iPitch0 = iPitchRaw; iRoll0 = iRollRaw;
            iZeroed = true;
        }

        // If no packet yet, show zeros to avoid flicker
        float iPitchZ = iHasPacket ? Wrap180(iPitchRaw - iPitch0) : 0f;
        float iYawZ = iHasPacket ? Wrap180(iYawRaw - iYaw0) : 0f;
        float iRollZ = iHasPacket ? Wrap180(iRollRaw - iRoll0) : 0f;

        float t2 = t;
        iSmoothed.x = Mathf.LerpAngle(iSmoothed.x, iPitchZ, t2);
        iSmoothed.y = Mathf.LerpAngle(iSmoothed.y, iYawZ, t2);
        iSmoothed.z = Mathf.LerpAngle(iSmoothed.z, iRollZ, t2);

        if (imuText)
            imuText.text = $"IMU     |  Pitch: {iSmoothed.x.ToString(fmt)}°  " +
                           $"Yaw: {iSmoothed.y.ToString(fmt)}°";

       // $"IMU     |  Yaw: {iSmoothed.y.ToString(fmt)}°  "
    }

    // Called by your WebSocket script whenever a new IMU sample arrives (degrees)
    public void OnImuEulerDegrees(float pitchDeg, float yawDeg, float rollDeg)
    {
        iPitchRaw = pitchDeg;
        iYawRaw = rollDeg;
        iRollRaw = yawDeg;
        iHasPacket = true;                  // tells Update() we can capture baseline now
    }

    // Buttons / UI hooks
    public void ZeroQuestNow()
    {
        var head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        if (head.isValid && head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q))
        {
            Vector3 e = q.eulerAngles;
            qYaw0 = Wrap180(e.y);
            qPitch0 = Wrap180(e.x);
            qRoll0 = Wrap180(e.z);
            qZeroed = true;
        }
    }
    public void ZeroImuNow()
    {
        // zero to current IMU reading (if no packet yet, it will zero on first packet)
        iYaw0 = iYawRaw; iPitch0 = iPitchRaw; iRoll0 = iRollRaw;
        iZeroed = true;
    }
    public void ZeroBoth() { ZeroQuestNow(); ZeroImuNow(); }

    static float Wrap180(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }
}
