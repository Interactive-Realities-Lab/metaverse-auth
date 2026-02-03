using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class HeadsetMotion : MonoBehaviour
{
    [Header("IMU source (from FingerprintWsClient)")]
    public FingerprintWsClient imuSource; // drag Network object here (or auto-find)

    [Header("IMU zero command")]
    public string imuResetCommand = "ZERO";
    public bool appendNewline = true;

    [Header("Formatting")]
    public int decimals = 1;                   // UI only
    public bool questRelativeZero = true;

    [Header("(Auto) Row labels")]
    public TMP_Text rowQuestText;              // Motion/RowQuest/Text (TMP)
    public TMP_Text rowImuText;                // Motion/RowIMU/Text (TMP)
    public TMP_Text rowDeltaText;              // Motion/RowDelta/Text (TMP)

    [Header("Optional: hook the Reset button here to auto-wire on play")]
    public Button resetButton;                 // Motion/Reset (Button)

    // IMU parsed cache (fed by FingerprintWsClient)
    bool _haveP, _haveR, _haveY;
    float _p, _r, _y;

    // Quest zero
    Vector3 _questZeroEuler;
    bool _questHasZero;

    void Awake()
    {
        // Auto-wire TMP labels if not assigned
        if (rowQuestText == null)
            rowQuestText = transform.Find("RowQuest/Text (TMP)")?.GetComponent<TMP_Text>();
        if (rowImuText == null)
            rowImuText = transform.Find("RowIMU/Text (TMP)")?.GetComponent<TMP_Text>();
        if (rowDeltaText == null)
            rowDeltaText = transform.Find("RowDelta/Text (TMP)")?.GetComponent<TMP_Text>();
        if (resetButton == null)
            resetButton = transform.Find("Reset")?.GetComponent<Button>();
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        // Auto-find IMU source if not set
        if (imuSource == null)
            imuSource = FindObjectOfType<FingerprintWsClient>();

        // Initial UI
        if (rowQuestText) rowQuestText.text = "QUEST | Yaw: —  Pitch: —  Roll: —";
        if (rowImuText) rowImuText.text = "IMU   | Yaw: —  Pitch: —  Roll: —";
        if (rowDeltaText) rowDeltaText.text = "Delta | Yaw: —  Pitch: —  Roll: —";
    }

    void OnEnable()
    {
        // Subscribe to IMU updates
        if (imuSource != null)
            imuSource.OnImuYpr += OnImuYpr;
    }

    void OnDisable()
    {
        // Unsubscribe
        if (imuSource != null)
            imuSource.OnImuYpr -= OnImuYpr;
    }

    // Called by FingerprintWsClient whenever it parses an IMU message
    void OnImuYpr(float pitch, float roll, float yaw)
    {
        _p = pitch;
        _r = roll;
        _y = yaw;
        _haveP = _haveR = _haveY = true;
    }

    void Update()
    {
        // ----- QUEST row -----
        float questYaw = 0;
        float questPitch = 0;
        float questRoll = 0;

        if (TryGetHmdRotation(out var rot) || (Camera.main && (rot = Camera.main.transform.rotation) != Quaternion.identity))
        {
            Vector3 eul = rot.eulerAngles;
            eul = questRelativeZero && _questHasZero ? NormalizeEuler(eul - _questZeroEuler) : NormalizeEuler(eul);

            string f = $"F{Mathf.Clamp(decimals, 0, 5)}";

            questYaw = eul.y;
            questPitch = eul.x;
            questRoll = eul.z;

            if (rowQuestText)
                rowQuestText.text =
                    $"QUEST | Yaw: {questYaw.ToString(f)}°  Pitch: {questPitch.ToString(f)}°  Roll: {questRoll.ToString(f)}°";
        }

        // ----- IMU row (from FingerprintWsClient) -----
        string ff = $"F{Mathf.Clamp(decimals, 0, 5)}";
        string y = _haveY ? _y.ToString(ff) : "—";
        string p = _haveP ? _p.ToString(ff) : "—";
        string r = _haveR ? _r.ToString(ff) : "—";

        if (rowImuText)
            rowImuText.text = $"IMU   | Yaw: {y}°  Pitch: {p}°  Roll: {r}°";

        if (rowDeltaText)
        {
            float imuYaw = _haveY ? _y : 0f;
            float imuPitch = _haveP ? _p : 0f;
            float imuRoll = _haveR ? _r : 0f;

            rowDeltaText.text =
                $"Delta | Yaw: {(questYaw - imuYaw).ToString(ff)}°  Pitch: {(questPitch - imuPitch).ToString(ff)}°  Roll: {(questRoll - imuRoll).ToString(ff)}°";
        }
    }

    // ---------- Reset (button) ----------
    public void OnResetClicked()
    {
        // 1) zero QUEST row
        if (TryGetHmdRotation(out var rot) || (Camera.main && (rot = Camera.main.transform.rotation) != Quaternion.identity))
        {
            _questZeroEuler = NormalizeEuler(rot.eulerAngles);
            _questHasZero = true;
        }

        // 2) tell IMU to zero (send through FingerprintWsClient)
        if (imuSource != null)
            imuSource.SendImuCommand("BTN_B", true);
    }

    // ---------- helpers ----------
    static bool TryGetHmdRotation(out Quaternion q)
    {
        var dev = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (dev.isValid && dev.TryGetFeatureValue(CommonUsages.deviceRotation, out q))
            return true;

        q = Quaternion.identity;
        return false;
    }

    static Vector3 NormalizeEuler(Vector3 e)
    {
        e.x = NormalizeDeg(e.x);
        e.y = NormalizeDeg(e.y);
        e.z = NormalizeDeg(e.z);
        return e;
    }

    static float NormalizeDeg(float d)
    {
        d %= 360f;
        if (d >= 180f) d -= 360f;
        if (d < -180f) d += 360f;
        return d;
    }
}


