using UnityEngine;
using TMPro;

public class HeadOrientationHUD : MonoBehaviour
{
    [Header("References")]
    public Transform head;                 
    public TMP_Text anglesText;            

    [Header("Display")]
    [Range(0f, 1f)] public float smoothing = 0.2f;    // 0=no smoothing, 1=more smoothing
    public int decimals = 1;                          // number of decimals to show
    public bool zeroOnStart = true;                   // start all axes at 0

    // internal
    private float yawZero, pitchZero, rollZero;
    private Vector3 smoothedEuler;                    // smoothed (pitch, yaw, roll) in deg
    private bool baselineCaptured = false;

    void Awake()
    {
        if (head == null && Camera.main != null)
            head = Camera.main.transform;
    }

    void Update()
    {
        if (head == null || anglesText == null) return;

        Vector3 e = head.rotation.eulerAngles;

        // Capture a baseline once so values start at 0
        if (zeroOnStart && !baselineCaptured)
        {
            yawZero = Wrap180(e.y);
            pitchZero = Wrap180(e.x);
            rollZero = Wrap180(e.z);
            baselineCaptured = true;
        }

        float pitch = Wrap180(e.x) - pitchZero; // X
        float yaw = Wrap180(e.y) - yawZero;   // Y
        float roll = Wrap180(e.z) - rollZero;  // Z

        // Smooth (exponential, framerate-normalized)
        float t = 1f - Mathf.Pow(1f - smoothing, Time.deltaTime * 60f);
        smoothedEuler.x = Mathf.LerpAngle(smoothedEuler.x, pitch, t);
        smoothedEuler.y = Mathf.LerpAngle(smoothedEuler.y, yaw, t);
        smoothedEuler.z = Mathf.LerpAngle(smoothedEuler.z, roll, t);

        string fmt = "F" + decimals;

        
        anglesText.text =
            $"Yaw: {smoothedEuler.y.ToString(fmt)}°\n" +
            $"Pitch: {smoothedEuler.x.ToString(fmt)}°\n" +
            $"Roll: {smoothedEuler.z.ToString(fmt)}°\n";
    }

    public void ZeroAll()   
    {
        if (head == null) return;
        Vector3 e = head.rotation.eulerAngles;
        yawZero = Wrap180(e.y);
        pitchZero = Wrap180(e.x);
        rollZero = Wrap180(e.z);
        baselineCaptured = true;
    }

    // Helpers
    static float Wrap180(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}
