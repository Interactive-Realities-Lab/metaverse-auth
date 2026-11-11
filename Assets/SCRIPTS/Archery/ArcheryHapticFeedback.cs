using UnityEngine;
using UnityEngine.XR;

public class ArcheryHapticFeedback : MonoBehaviour
{
    [Header("Bowstring Reference")]
    [SerializeField] private Puller puller;

    [Header("Haptic Settings")]
    [Range(0f, 1f)]
    public float minimumIntensity = 0.10f;

    [Range(0f, 1f)]
    public float maximumIntensity = 0.45f;

    [Range(0f, 1f)]
    public float minimumPullAmount = 0.05f;

    public float pulseInterval = 0.05f;
    public float pulseDuration = 0.06f;

    [Header("Controllers")]
    public bool vibrateLeftController = true;
    public bool vibrateRightController = true;

    private InputDevice leftDevice;
    private InputDevice rightDevice;

    private float nextPulseTime;
    private bool wasDrawing;

    private void Start()
    {
        DetectControllers();

        if (puller == null)
        {
            puller = FindFirstObjectByType<Puller>();
        }
    }

    private void Update()
    {
        if (puller == null)
            return;

        // Only vibrate while the bowstring is selected
        // and pulled beyond the minimum threshold.
        bool isDrawing =
            puller.isSelected &&
            puller.PullAmount > minimumPullAmount;

        if (!isDrawing)
        {
            if (wasDrawing)
            {
                StopHaptics();
                wasDrawing = false;
            }

            return;
        }

        wasDrawing = true;

        if (Time.time < nextPulseTime)
            return;

        // Reconnect controllers if necessary.
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            DetectControllers();
        }

        float pull = Mathf.Clamp01(puller.PullAmount);

        float intensity = Mathf.Lerp(
            minimumIntensity,
            maximumIntensity,
            pull
        );

        if (vibrateLeftController)
        {
            SendHaptic(leftDevice, intensity);
        }

        if (vibrateRightController)
        {
            SendHaptic(rightDevice, intensity);
        }

        nextPulseTime = Time.time + pulseInterval;
    }

    private void DetectControllers()
    {
        leftDevice = InputDevices.GetDeviceAtXRNode(
            XRNode.LeftHand
        );

        rightDevice = InputDevices.GetDeviceAtXRNode(
            XRNode.RightHand
        );
    }

    private void SendHaptic(
        InputDevice device,
        float intensity)
    {
        if (!device.isValid)
            return;

        if (device.TryGetHapticCapabilities(
            out HapticCapabilities capabilities))
        {
            if (capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(
                    0,
                    intensity,
                    pulseDuration
                );
            }
        }
    }

    private void StopHaptics()
    {
        if (leftDevice.isValid)
            leftDevice.StopHaptics();

        if (rightDevice.isValid)
            rightDevice.StopHaptics();
    }

    private void OnDisable()
    {
        StopHaptics();
    }

    private void OnDestroy()
    {
        StopHaptics();
    }
}