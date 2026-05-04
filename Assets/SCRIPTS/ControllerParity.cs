using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class ControllerParity : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text leftText;
    public TMP_Text rightText;
    public TMP_Text parityText;

    [Header("Parity settings")]
    public float parityCheckEverySeconds = 0.03f;   // ~33 Hz (good for 100 Hz sensors too)
    public float parityBuildSeconds = 1.0f;
    public int parityBadChecksToLose = 3;

    [Header("Cosine settings")]
    public float cosThreshold = 0.65f;             // start here
    public float minMotion = 0.25f;                // ignore tiny motion (tune 0.15–0.5)

    bool _parityBuilt = false;
    float _goodAccum = 0f;
    int _badStreak = 0;
    float _nextCheckAt = 0f;

    void Update()
    {
        if (Time.time < _nextCheckAt) return;
        _nextCheckAt = Time.time + Mathf.Max(0.01f, parityCheckEverySeconds);

        // Read angular velocity from both controllers
        bool haveL = TryGetAngularVelocity(XRNode.LeftHand, out Vector3 wL);
        bool haveR = TryGetAngularVelocity(XRNode.RightHand, out Vector3 wR);

        if (leftText) leftText.text = haveL ? $"LEFT  w=({wL.x:F2},{wL.y:F2},{wL.z:F2})" : "LEFT  w=—";
        if (rightText) rightText.text = haveR ? $"RIGHT w=({wR.x:F2},{wR.y:F2},{wR.z:F2})" : "RIGHT w=—";

        if (!haveL || !haveR)
        {
            // tracking missing -> reset parity
            _parityBuilt = false;
            _goodAccum = 0f;
            _badStreak = 0;
            if (parityText) parityText.text = "PARITY | — (tracking missing)";
            return;
        }

        float magL = wL.magnitude;
        float magR = wR.magnitude;

        // Only evaluate when BOTH are moving enough
        bool evaluate = (magL >= minMotion && magR >= minMotion);

        float cosSim = 0f;
        bool good = false;

        if (evaluate)
        {
            cosSim = Vector3.Dot(wL.normalized, wR.normalized);  // [-1..1]
            good = cosSim >= cosThreshold;

            if (good)
            {
                _badStreak = 0;
                _goodAccum += parityCheckEverySeconds;
                if (!_parityBuilt && _goodAccum >= parityBuildSeconds)
                    _parityBuilt = true;
            }
            else
            {
                _goodAccum = 0f;
                _badStreak++;
                if (_parityBuilt && _badStreak >= parityBadChecksToLose)
                    _parityBuilt = false;
            }
        }
        // else: neutral (don’t punish, don’t build)

        if (parityText)
        {
            string evalStr = evaluate ? $"cos:{cosSim:F2}" : "neutral (still)";
            parityText.text = _parityBuilt
                ? $"PARITY | BUILT ({evalStr})"
                : $"PARITY | LOST  ({evalStr})";
        }
    }

    static bool TryGetAngularVelocity(XRNode node, out Vector3 w)
    {
        var dev = InputDevices.GetDeviceAtXRNode(node);
        if (dev.isValid && dev.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out w))
            return true;

        w = Vector3.zero;
        return false;
    }
}