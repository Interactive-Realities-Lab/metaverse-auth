using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using IRLab.EventSystem.Event;

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

    [Header("Parity status UI (optional)")]
    public TMP_Text parityText; // add a TMP text in the UI if you want

    [SerializeField] private UIOTPHandler otpHandler;

    [Header("Parity logic (angles)")]
    //public float parityBuildSeconds = 1.0f;       // kept for tuning/reference
    public float parityCheckEverySeconds = 0.15f; // how often to evaluate
    //public float parityDeltaToleranceDeg = 4.5f;  // kept for tuning/reference
    //public int parityBadChecksToLose = 3;         // kept for tuning/reference
    public float parityCosThreshold = 0.3f;
    public float parityMinMotionDeg = 2.0f;

    [Header("One-sided motion tolerance")]
    public float oneSidedMotionDeg = 4.0f;
    public int oneSidedBadChecksToPunish = 3;
    public float oneSidedStepDown = 0.03f;
    public float oneSidedDifferenceThreshold = 0.20f;

    [Header("Attachment Gate")]
    public float maxMotionDifference = 0.20f; // rad/s

    [Header("Pitch/Roll Attachment Check")]
    public float attachmentPitchToleranceDeg = 8f;
    public float attachmentRollToleranceDeg = 8f;
    public float attachmentConfirmSeconds = 0.75f;

    private float _trustedPitchOffset = 0f;
    private float _trustedRollOffset = 0f;
    private bool _haveTrustedOrientationOffset = false;
    private float _orientationValidTimer = 0f;

    private int _oneSidedBadCount = 0;
    bool _parityBuilt = false;
    float _nextParityCheckAt = 0f;
    float _confidence = 0f;
    bool _parityEverBuilt = false;                //tracking whether parity was ever built once

    // previous samples (to compute change)
    bool _havePrevQuest = false, _havePrevImu = false;
    Vector3 _prevQuestPYR; // (pitch,yaw,roll)
    Vector3 _prevImuPYR;

    bool _pendingMatchedUi = false;
    float _pendingMatchedUiAt = 0f;

    [Header("UI timing")]
    public float matchedUiDelaySeconds = 0.4f;

    [Header("Parity hysteresis")]
    public float buildThreshold = 1.0f;
    public float loseThreshold = 0.2f;
    public float stepUp = 0.15f;
    public float stepDown = 0.075f;

    [Header("Auth Gate")]
    [SerializeField] private bool authVerified = false;

    [Header("Continuous Auth UI")]
    public ControlUIFeedback controlUIFeedback;
    public ProgressBar progressBar;
    

    [Header("Parity Warning")]
    public float almostLostThreshold = 0.45f;
    private bool _almostLostShowing = false;

    [Header("Parity Lost Timeout")]
    public float parityLostTimeoutSeconds = 10f;
    public TMP_Text timeoutText;

    bool _reauthRequired = false;

    [Header("UI Panel Actions")]
    public UIPanelActions uiPanelActions;

    [Header("State Event Channels")]
    public VoidEventChannelSO onSamplingEnter;
    public VoidEventChannelSO onMatchedEnter;
    public VoidEventChannelSO onNotMatchingEnter;
    public VoidEventChannelSO onSamplingPausedEnter;

    public VoidEventChannelSO onSamplingExit;
    public VoidEventChannelSO onMatchedExit;
    public VoidEventChannelSO onNotMatchingExit;
    public VoidEventChannelSO onSamplingPausedExit;

    [Header("CSV Logger")]
    public HeadsetMotionCsvLogger csvLogger;
    public ParityEventLogger parityEventLogger;
    public XRDeviceLogger xrDeviceLogger;

    private float _parityLostTimer = 0f;
    private bool _parityLostCountdownRunning = false;

    public enum MotionState
    {
        Paused = 0,
        Sampling = 1,
        Matched = 2,
        NotMatched = 3
    }

    [Header("Animator / FSM")]
    public Animator uiAnimator;

    MotionState _state = MotionState.Sampling;

    [Header("NEW: Gyro row (optional)")]
    public TMP_Text rowGyroText;               // optional extra row for gyro score

    [Header("Optional: hook the Reset button here to auto-wire on play")]
    public Button resetButton;                 // Motion/Reset (Button)

    // IMU parsed cache (fed by FingerprintWsClient)
    bool _haveP, _haveR, _haveY;
    float _p, _r, _y; // pitch, roll, yaw (as received)

    // NEW: ESP gyro cache (fed by FingerprintWsClient)
    bool _haveG;
    Vector3 _espGyro; // (deg/s or rad/s - just be consistent)

    // Quest zero (store as quaternion for stable relative rotation)
    Quaternion _questZeroRot = Quaternion.identity;
    bool _questHasZero;

    // Alignment offset: Quest = offset * IMU (kept; not used unless you choose to later)
    Quaternion _imuToQuestOffset = Quaternion.identity;
    bool _haveOffset = false;

    public float imuSlerp = 0.15f; // 0..1
    Quaternion _imuQuatSmoothed = Quaternion.identity;
    bool _imuSmoothedInit = false;

    // ---------------- NEW: Sliding window gyro matching ----------------
    [Header("NEW: Gyro matching")]
    [Tooltip("Seconds of data used for correlation (2-5s recommended).")]
    public float gyroWindowSeconds = 3.0f;

    [Header("Gyro-Based Parity Decision")]
    public bool useGyroForParity = true;
    public float gyroBuildThreshold = 0.55f;
    public float gyroLoseThreshold = 0.35f;

    [Header("Time-Based Gyro Confidence")]
    [Tooltip("Confidence gained per real second while gyro evidence is good.")]
    public float gyroGainPerSecond = 0.32f;

    [Tooltip("Confidence lost per real second while gyro evidence is bad.")]
    public float gyroLossPerSecond = 0.08f;

    [Tooltip("Stronger confidence loss per real second for one-sided gyro mismatch.")]
    public float gyroMismatchLossPerSecond = 0.32f;

    [Tooltip("Small confidence loss per real second while score is uncertain and parity is not yet built.")]
    public float gyroUncertainLossPerSecond = 0.02f;

    [Header("Head Movement Speed Feedback")]
    public TMP_Text speedFeedbackText;

    public float minGoodSpeed = 0.1f;
    public float maxGoodSpeed = 2.5f;

    [Header("IMU Freshness")]
    public float imuGyroTimeoutSeconds = 0.75f;
    public float gyroQuestActiveRadPerSec = 0.1f;
    public float gyroImuActiveRadPerSec = 0.1f;
    public float minEvidenceMotion = 0.08f;

    [Header("IMU Detach Detection")]
    public float detachedLoseSeconds = 2.0f;
    public float detachedDifferenceThreshold = 0.20f;
    public float detachWarningSeconds = 0.75f;
    private float _detachWarningStartTime = -1f;
    private float _detachedTimer = 0f;
    private bool _detachmentLockout = false;

    [Header("Motion Detection")]
    public float noMotionGraceSeconds = 1.0f;
    public float noMotionLossPerSecond = 0.05f;

    private float _noMotionStartTime = -1f;

    [Tooltip("How often to recompute score (seconds).")]
    public float gyroScoreEverySeconds = 0.25f;

    // [Tooltip("If score stays below this for N checks -> mismatch (logic lives elsewhere).")]
    // public float gyroScoreWarnThreshold = 0.6f;

    float _lastParityCos = 0f;

    float _currentQuestYaw = 0f;
    float _currentQuestPitch = 0f;
    float _currentQuestRoll = 0f;

    // Values exposed to XRDeviceLogger
    public float CurrentQuestYaw => _currentQuestYaw;
    public float CurrentQuestPitch => _currentQuestPitch;
    public float CurrentQuestRoll => _currentQuestRoll;

    // ============================================================

    public Quaternion CurrentQuestRotation { get; private set; }
    public Vector3 CurrentQuestGyro { get; private set; }

    public bool HasQuestRotation { get; private set; }
    public bool HasQuestGyro { get; private set; }

    public bool ParityBuilt => _parityBuilt;
    public float CurrentConfidence => _confidence;
    public float CurrentGyroScore => _gyroScore;

    // ============================================================
    // Values exposed to the live debug panel
    // ============================================================

    // IMU mapped orientation
    private float _currentImuYaw;
    private float _currentImuPitch;
    private float _currentImuRoll;

    // Difference between Quest and IMU orientation
    private float _currentDeltaYaw;
    private float _currentDeltaPitch;
    private float _currentDeltaRoll;

    // Averaged gyro values used by parity logic
    private float _currentQuestGyroMagnitude;
    private float _currentImuGyroMagnitude;
    private float _currentMotionDifference;

    // Current decisions
    private bool _currentQuestActive;
    private bool _currentImuActive;
    private bool _currentBothQuiet;
    private bool _currentDetachedMotion;
    private bool _currentMountingOrientationValid;
    private bool _currentImuGyroFresh;

    // Mounting errors
    private float _currentPitchOffsetError;
    private float _currentRollOffsetError;

    // Last important parity transition
    private string _lastParityEvent = "Waiting";
    private string _lastParityReason = "Parity has not been evaluated yet";
    private string _lastParityEventTime = "--:--:--";


    // ---------------- IMU orientation ----------------

    public bool HasImuOrientation =>
        _haveP && _haveR && _haveY;

    public float CurrentImuRawPitch => _p;
    public float CurrentImuRawRoll => _r;
    public float CurrentImuRawYaw => _y;

    public float CurrentImuYaw => _currentImuYaw;
    public float CurrentImuPitch => _currentImuPitch;
    public float CurrentImuRoll => _currentImuRoll;


    // ---------------- Orientation differences ----------------

    public float CurrentDeltaYaw => _currentDeltaYaw;
    public float CurrentDeltaPitch => _currentDeltaPitch;
    public float CurrentDeltaRoll => _currentDeltaRoll;


    // ---------------- IMU gyro ----------------

    public bool HasImuGyro => _haveG;
    public Vector3 CurrentImuGyro => _espGyro;

    public float CurrentQuestGyroMagnitude =>
        _currentQuestGyroMagnitude;

    public float CurrentImuGyroMagnitude =>
        _currentImuGyroMagnitude;


    // ---------------- Gyro comparison ----------------

    public float CurrentGyroScoreX => _gyroScoreX;
    public float CurrentGyroScoreY => _gyroScoreY;
    public float CurrentGyroScoreZ => _gyroScoreZ;

    public float CurrentMotionDifference => _currentMotionDifference;

    public bool CurrentQuestActive => _currentQuestActive;
    public bool CurrentImuActive => _currentImuActive;
    public bool CurrentBothQuiet => _currentBothQuiet;
    public bool CurrentDetachedMotion => _currentDetachedMotion;
    public bool CurrentImuGyroFresh => _currentImuGyroFresh;


    // ---------------- Attachment check ----------------

    public bool CurrentMountingOrientationValid =>
        _currentMountingOrientationValid;

    public float CurrentPitchOffsetError =>
        _currentPitchOffsetError;

    public float CurrentRollOffsetError =>
        _currentRollOffsetError;

    public float CurrentOrientationValidTimer =>
        _orientationValidTimer;

    public float CurrentDetachTimer =>
        _detachedTimer;

    public bool DetachmentLockout =>
        _detachmentLockout;

    public int CurrentOneSidedBadCount =>
        _oneSidedBadCount;


    // ---------------- Parity information ----------------

    public MotionState CurrentMotionState => _state;
    public bool ParityEverBuilt => _parityEverBuilt;
    public bool ReauthRequired => _reauthRequired;

    public float CurrentLastParityCos => _lastParityCos;

    public bool ParityLostCountdownRunning =>
        _parityLostCountdownRunning;

    public float CurrentParityLostTimer =>
        _parityLostTimer;

    public string LastParityEvent =>
        _lastParityEvent;

    public string LastParityReason =>
        _lastParityReason;

    public string LastParityEventTime =>
        _lastParityEventTime;

    public string CurrentParityLostReason => 
        _lastParityReason;


    // ---------------- Sensor packet ages ----------------

    public float CurrentImuGyroAge
    {
        get
        {
            if (_lastImuGyroAt < 0f)
                return -1f;

            return Time.time - _lastImuGyroAt;
        }
    }

    public float CurrentImuOrientationAge
    {
        get
        {
            if (_lastImuYprAt < 0f)
                return -1f;

            return Time.time - _lastImuYprAt;
        }
    }

    // -------- Trial Timer --------
    float _trialStartTime = 0f;
    bool _trialRunning = false;

    struct Sample
    {
        public float t;
        public Vector3 v;
        public Sample(float t, Vector3 v) { this.t = t; this.v = v; }
    }

    readonly List<Sample> _questGyroBuf = new List<Sample>(512);
    readonly List<Sample> _espGyroBuf = new List<Sample>(512);
    float _nextGyroScoreAt = 0f;

    // Actual elapsed-time tracking for confidence updates.
    // This makes confidence gain/loss independent of frame rate and
    // independent of gyroScoreEverySeconds.
    float _lastGyroConfidenceUpdateAt = -1f;

    float _gyroScore = 0f;
    float _gyroScoreX = 0f, _gyroScoreY = 0f, _gyroScoreZ = 0f;

    float _lastImuYprAt = -999f;
    float _lastImuGyroAt = -999f;
    float _lastBothQuietAt = -999f;
    bool _imuYprUpdated = false;

    void Awake()
    {
        // Auto-wire TMP labels if not assigned
        if (rowQuestText == null)
            rowQuestText = transform.Find("RowQuest/Text (TMP)")?.GetComponent<TMP_Text>();
        if (rowImuText == null)
            rowImuText = transform.Find("RowIMU/Text (TMP)")?.GetComponent<TMP_Text>();
        if (rowDeltaText == null)
            rowDeltaText = transform.Find("RowDelta/Text (TMP)")?.GetComponent<TMP_Text>();
        if (rowGyroText == null)
            rowGyroText = transform.Find("RowGyro/Text (TMP)")?.GetComponent<TMP_Text>();
        if (resetButton == null)
            resetButton = transform.Find("Reset")?.GetComponent<Button>();
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
        if (parityText == null)
            parityText = transform.Find("RowParity/Text (TMP)")?.GetComponent<TMP_Text>();

        // Auto-find IMU source if not set
        if (imuSource == null)
            imuSource = FindObjectOfType<FingerprintWsClient>();

        // Initial UI
        if (rowQuestText) rowQuestText.text = "QUEST | Yaw: —  Pitch: —  Roll: —";
        if (rowImuText) rowImuText.text = "IMU   | Pitch: —  Roll: —  Yaw: —";
        if (rowDeltaText) rowDeltaText.text = "Delta | Yaw: —  Pitch: —  Roll: —";
        if (rowGyroText) rowGyroText.text = "GYRO  | Score: — (x:— y:— z:—)";
        if (parityText) parityText.text = "PARITY | —";

        // If you want stepUp/stepDown tied to check rate, keep this.
        stepUp = parityCheckEverySeconds;
        stepDown = parityCheckEverySeconds * 0.5f;

        SetState(MotionState.Sampling, true);

        Debug.Log("Animator assigned to: " + uiAnimator.name);
    }

    void OnEnable()
    {
        if (imuSource != null)
            imuSource.OnImuYpr += OnImuYpr;

        if (imuSource != null)
            imuSource.OnImuGyro += OnImuGyro;
    }

    void OnDisable()
    {
        if (imuSource != null)
            imuSource.OnImuYpr -= OnImuYpr;

        if (imuSource != null)
            imuSource.OnImuGyro -= OnImuGyro;
    }

    void SetState(MotionState s, bool force = false)
    {
        if (!force && _state == s) return;

        MotionState previousState = _state;

        if (!force)
        {
            switch (previousState)
            {
                case MotionState.Sampling:
                    if (onSamplingExit != null) onSamplingExit.Broadcast();
                    break;
                case MotionState.Matched:
                    if (onMatchedExit != null) onMatchedExit.Broadcast();
                    break;
                case MotionState.NotMatched:
                    if (onNotMatchingExit != null) onNotMatchingExit.Broadcast();
                    break;
                case MotionState.Paused:
                    if (onSamplingPausedExit != null) onSamplingPausedExit.Broadcast();
                    break;
            }
        }

        _state = s;

        Debug.Log($"Setting Animator State = {(int)_state}");

        if (uiAnimator != null)
            uiAnimator.SetInteger("State", (int)_state);

        switch (_state)
        {
            case MotionState.Sampling:
                if (controlUIFeedback != null) controlUIFeedback.Sampling();
                if (onSamplingEnter != null) onSamplingEnter.Broadcast();
                break;

            case MotionState.Matched:
                if (controlUIFeedback != null) controlUIFeedback.Matched();
                if (onMatchedEnter != null) onMatchedEnter.Broadcast();
                break;

            case MotionState.NotMatched:
                //if (controlUIFeedback != null) controlUIFeedback.NotMatching();
                if (onNotMatchingEnter != null) onNotMatchingEnter.Broadcast();
                break;

            case MotionState.Paused:
                if (controlUIFeedback != null) controlUIFeedback.Paused();
                if (onSamplingPausedEnter != null) onSamplingPausedEnter.Broadcast();
                break;
        }
    }

    public float ParityProgress01()
    {
        if (buildThreshold <= 0f) return 0f;
        return Mathf.Clamp01(_confidence / buildThreshold);
    }

    public bool IsParityBuilt()
    {
        return _parityBuilt;
    }

    public MotionState CurrentState()
    {
        return _state;
    }

    public float LastParityCos()
    {
        return _lastParityCos;
    }

    public float ParityLostTimeRemaining()
    {
        return Mathf.Max(0f, parityLostTimeoutSeconds - _parityLostTimer);
    }

    void HandleParityLostTimeout()
    {
        _lastParityEvent = "REAUTHENTICATION TIMEOUT";
        _lastParityReason =
            $"Parity was not regained within " +
            $"{parityLostTimeoutSeconds:F1} seconds";

        _lastParityEventTime =
            DateTime.Now.ToString("HH:mm:ss.fff");
                
        Debug.Log("Parity lost timeout reached. Returning to login panel.");

        if (parityEventLogger != null)
        {
            float trialTime = _trialRunning ? (Time.time - _trialStartTime) : 0f;

            float imuYawQ = -_y;
            float imuPitchQ = -_r;
            float imuRollQ = -_p;

           parityEventLogger.LogEvent(
            "REAUTH_TIMEOUT",
            trialTime,
            _currentQuestYaw, _currentQuestPitch, _currentQuestRoll,
            imuYawQ, imuPitchQ, imuRollQ,
            _confidence,
            _lastParityCos,
            _gyroScore,
            _lastParityReason
          );
        }

        // Force full logout
        SetAuthVerified(false);
        FingerprintWsClient.I?.StopContinuous();
        FingerprintWsClient.I?.Logout();

        _trialRunning = false;
        _parityLostCountdownRunning = false;
        _parityLostTimer = 0f;
        _reauthRequired = false;

        SetState(MotionState.Paused, true);

        if (SceneFlow.Instance != null)
            SceneFlow.Instance.OnAuthFailed();
        else if (uiPanelActions != null)
            uiPanelActions.ShowLogin();
    }

    void OnImuYpr(float pitch, float roll, float yaw)
    {
        _p = pitch;
        _r = roll;
        _y = yaw;
        _haveP = _haveR = _haveY = true;

        _lastImuYprAt = Time.time;
        _imuYprUpdated = true;
    }

    //Called by FingerprintWsClient whenever it parses ESP gyro
    void OnImuGyro(float gx, float gy, float gz)
    {
        _espGyro = new Vector3(gx, gy, gz) * Mathf.Deg2Rad;
        _haveG = true;
        _lastImuGyroAt = Time.time;

        float t = Time.time;
        _espGyroBuf.Add(new Sample(t, _espGyro));
        TrimOld(_espGyroBuf, t - gyroWindowSeconds);

        Debug.Log($"IMU RAW GYRO = {_espGyro.magnitude:F3}");

/*#if UNITY_EDITOR
        Debug.Log($"ESP GYRO: gx={gx:F3}, gy={gy:F3}, gz={gz:F3}");
#endif*/
    }

    static Quaternion ImuYprToQuat(float pitch, float roll, float yaw)
    {
        return Quaternion.Euler(pitch, yaw, roll);
    }

    void Update()
    {
        // ----- QUEST row -----
        float questYaw = 0f;
        float questPitch = 0f;
        float questRoll = 0f;

        Quaternion hmdRot;
        bool haveQuest = TryGetHmdRotation(out hmdRot) ||
                         (Camera.main && (hmdRot = Camera.main.transform.rotation) != Quaternion.identity);

        if (haveQuest)
        {
            Quaternion rel = (questRelativeZero && _questHasZero) ? (_questZeroRot * hmdRot) : hmdRot;
            Vector3 eul = NormalizeEuler(rel.eulerAngles);

            string f = $"F{Mathf.Clamp(decimals, 0, 5)}";

            questYaw = eul.y;
            questPitch = eul.x;
            questRoll = eul.z;

            _currentQuestYaw = questYaw;
            _currentQuestPitch = questPitch;
            _currentQuestRoll = questRoll;

            CurrentQuestRotation = rel;
            HasQuestRotation = true;

            if (rowQuestText)
            {
                rowQuestText.text =
                    $"QUEST | Yaw: {questYaw.ToString(f)}°  Pitch: {questPitch.ToString(f)}°  Roll: {questRoll.ToString(f)}°";
            }
        }

        else
        {
            HasQuestRotation = false;
        }

        // ----- IMU row -----
        string ff = $"F{Mathf.Clamp(decimals, 0, 5)}";

        float imuRawPitch = _p;
        float imuRawRoll = _r;
        float imuRawYaw = _y;

        // Same mapping currently used by CSV and parity event logging
        _currentImuYaw = -_y;
        _currentImuPitch = -_r;
        _currentImuRoll = -_p;

        // Current Quest versus IMU orientation differences
        _currentDeltaYaw = haveQuest
            ? Mathf.DeltaAngle(_currentImuYaw, questYaw)
            : 0f;

        _currentDeltaPitch = haveQuest
            ? Mathf.DeltaAngle(_currentImuPitch, questPitch)
            : 0f;

        _currentDeltaRoll = haveQuest
            ? Mathf.DeltaAngle(_currentImuRoll, questRoll)
            : 0f;

        // -------- mapping ---------
        // QuestYaw   <- -IMU yaw
        // QuestPitch <- -IMU roll
        // QuestRoll  <- -IMU pitch

        string imuYawMappedStr = (_haveR ? (-imuRawRoll).ToString(ff) : "—");
        string imuPitchMappedStr = (_haveY ? (-imuRawYaw).ToString(ff) : "—");
        string imuRollMappedStr = (_haveP ? (-imuRawPitch).ToString(ff) : "—");

        if (rowImuText != null)
        {
            rowImuText.text =
                $"IMU | Yaw: {imuPitchMappedStr}°  Pitch: {imuYawMappedStr}°  Roll: {imuRollMappedStr}°";
        }

        // ----- Delta row -----
        if (rowDeltaText)
        {
            // Match EXACT same mapping as the IMU row being displayed
            float imuYawQ = -_y; // same as UI Yaw
            float imuPitchQ = -_r; // same as UI Pitch
            float imuRollQ = -_p; // same as UI Roll

            float dy = haveQuest ? Mathf.DeltaAngle(imuYawQ, questYaw) : 0f;
            float dp = haveQuest ? Mathf.DeltaAngle(imuPitchQ, questPitch) : 0f;
            float dr = haveQuest ? Mathf.DeltaAngle(imuRollQ, questRoll) : 0f;

            rowDeltaText.text =
                $"Delta | Yaw: {dy.ToString(ff)}°  Pitch: {dp.ToString(ff)}°  Roll: {dr.ToString(ff)}°";
        }

        if (!authVerified)
        {
            if (timeoutText != null) timeoutText.text = "";
            return;
        }

        // ---------------- PARITY (angles: change should match) ----------------
        // ---------------- PARITY (angles: change should match) ----------------
        if (!useGyroForParity)
        {
            if (Time.time >= _nextParityCheckAt)
            {
            _nextParityCheckAt = Time.time + Mathf.Max(0.05f, parityCheckEverySeconds);

            bool haveImuNow = _haveP && _haveR && _haveY;
            bool haveQuestNow = haveQuest;

            if (haveQuestNow && haveImuNow)
            {
                Vector3 questNow = new Vector3(questPitch, questYaw, questRoll);
                Vector3 imuNow = new Vector3(-_r, -_y, -_p);

                if (_havePrevQuest && _havePrevImu)
                {
                    float dQuestPitch = Mathf.DeltaAngle(_prevQuestPYR.x, questNow.x);
                    float dQuestYaw = Mathf.DeltaAngle(_prevQuestPYR.y, questNow.y);
                    float dQuestRoll = Mathf.DeltaAngle(_prevQuestPYR.z, questNow.z);

                    float dImuPitch = Mathf.DeltaAngle(_prevImuPYR.x, imuNow.x);
                    float dImuYaw = Mathf.DeltaAngle(_prevImuPYR.y, imuNow.y);
                    float dImuRoll = Mathf.DeltaAngle(_prevImuPYR.z, imuNow.z);

                    Vector3 dq = new Vector3(dQuestPitch, dQuestYaw, dQuestRoll);
                    Vector3 di = new Vector3(dImuPitch, dImuYaw, dImuRoll);

                    bool imuUpdatedSinceLastCheck = _imuYprUpdated;
                    _imuYprUpdated = false;

                    float cosSim = 0f;
                    bool good = false;

                    float questMag = dq.magnitude;
                    float imuMag = di.magnitude;

                    bool questMoved = questMag >= parityMinMotionDeg;
                    bool imuMoved = imuMag >= parityMinMotionDeg;

                    // Evaluate if at least one moved
                    bool evaluated = imuUpdatedSinceLastCheck && (questMoved || imuMoved);

                    if (!evaluated)
                    {
                        Debug.Log("ParityCheck SKIPPED (no movement on both devices)");
                    }
                    else
                    {
                        Debug.Log($"ParityCheck running | conf={_confidence:F2} | built={_parityBuilt} | ever={_parityEverBuilt}");

                        bool shouldApplyConfidence = true;

                        if (questMoved && imuMoved)
                        {
                            cosSim = Vector3.Dot(dq.normalized, di.normalized);

                            float motionDifference =
                                Mathf.Abs(questMag - imuMag);

                            bool similarAmount =
                                motionDifference <= maxMotionDifference;

                            good = cosSim >= parityCosThreshold && similarAmount;

                            if (!similarAmount)
                            {
                                Debug.Log(
                                    $"ATTACHMENT GATE FAILED | " +
                                    $"qMag={questMag:F2} imuMag={imuMag:F2} " +
                                    $"difference={motionDifference:F2}");
                            }

                            _oneSidedBadCount = 0;
                        }
                        else
                        {
                            float oneSidedAmount = Mathf.Max(questMag, imuMag);

                            if (oneSidedAmount >= oneSidedMotionDeg)
                            {
                                _oneSidedBadCount++;

                                cosSim = -1f;
                                good = false;

                                if (_oneSidedBadCount < oneSidedBadChecksToPunish)
                                {
                                    Debug.Log($"One-sided motion detected but waiting: {_oneSidedBadCount}/{oneSidedBadChecksToPunish}");
                                    shouldApplyConfidence = false;
                                }
                            }
                            else
                            {
                                Debug.Log("Small one-sided motion ignored.");
                                shouldApplyConfidence = false;
                            }
                        }

                        _lastParityCos = cosSim;

                        if (shouldApplyConfidence)
                        {
                            if (good)
                                _confidence += stepUp;
                            else
                            {
                                if (_oneSidedBadCount >= oneSidedBadChecksToPunish)
                                    _confidence -= oneSidedStepDown;
                                else
                                    _confidence -= stepDown;
                            }

                            _confidence = Mathf.Clamp(_confidence, 0f, buildThreshold);

                            Debug.Log($"AlmostLost check | built={_parityBuilt} conf={_confidence:F2}");

                            if (_parityBuilt && _confidence <= almostLostThreshold && _confidence > loseThreshold)
                            {
                                if (!_almostLostShowing)
                                {
                                    _almostLostShowing = true;

                                    if (uiPanelActions != null)
                                        uiPanelActions.ShowParityLosingUI();
                                }
                            }
                            else
                            {
                                if (_almostLostShowing)
                                {
                                    _almostLostShowing = false;

                                    if (uiPanelActions != null)
                                        uiPanelActions.HideParityLosingUI();
                                }
                            }
                        }


                        if (!_parityBuilt &&
                            !_detachmentLockout &&
                            _confidence >= buildThreshold)
                        {
                            bool wasReestablishing = _parityEverBuilt;

                            _confidence = buildThreshold;               

                            _parityBuilt = true;
                            _parityEverBuilt = true;
                            _reauthRequired = false;

                            XRDeviceLogger.Instance?.SetParityEstablished(true);

                            Debug.Log("Parity established. XR data recording started.");

                            Debug.Log("HeadsetMotion: MATCHED");

                            if (SceneFlow.Instance != null)
                            {
                                if (wasReestablishing)
                                    SceneFlow.Instance.RestoreCurrentSceneAfterParity();
                                else
                                    SceneFlow.Instance.OpenContentScene("Lobby");
                            }

                            _pendingMatchedUi = true;
                            _pendingMatchedUiAt = Time.time + matchedUiDelaySeconds;
                        }
                        else if (_parityBuilt && _confidence <= loseThreshold)
                        {
                            _parityBuilt = false;
                            _pendingMatchedUi = false;
                            _reauthRequired = true;   // starting re-auth timeout behavior

                            Debug.Log("HeadsetMotion: LOST");

                            if (SceneFlow.Instance != null)
                                SceneFlow.Instance.ShowContinuousAuthUI();
                            else if (uiPanelActions != null)
                                uiPanelActions.ShowContinuousAuth();
                        }
                    }

                    /*#if UNITY_EDITOR
                                        if (Time.frameCount % 30 == 0)
                                        {
                                            Debug.Log(
                                                $"dq(Q): P{dq.x:F2} Y{dq.y:F2} R{dq.z:F2} | " +
                                                $"di(IMU): P{di.x:F2} Y{di.y:F2} R{di.z:F2} | " +
                                                $"updated:{imuUpdatedSinceLastCheck} eval:{evaluated} cos:{cosSim:F2} built:{_parityBuilt} conf:{_confidence:F2}");
                                        }
                    #endif*/

                    if (parityText)
                    {
                        if (_parityBuilt)
                            parityText.text = $"PARITY | BUILT (cos:{cosSim:F2})";
                        else if (_confidence <= loseThreshold)
                            parityText.text = $"PARITY | LOST (cos:{cosSim:F2})";
                        else
                            parityText.text = $"PARITY | SAMPLING (cos:{cosSim:F2})";
                    }

                    if (_parityBuilt)
                    {
                        SetState(MotionState.Matched);
                    }
                    else if (_parityEverBuilt && _confidence <= loseThreshold)
                    {
                        SetState(MotionState.NotMatched);
                    }
                    else
                    {
                        SetState(MotionState.Sampling);
                    }

                // ALWAYS update previous values
                _prevQuestPYR = questNow;
                _prevImuPYR = imuNow;
                _havePrevQuest = _havePrevImu = true;
            }
            else
            {
                _lastParityCos = 0f;
                _confidence = 0f;
                _parityBuilt = false;
                if (parityText) parityText.text = "PARITY | —";
                _havePrevQuest = _havePrevImu = false;

                SetState(MotionState.Sampling);
            }
        }
      }
    }
        if (_pendingMatchedUi && _parityBuilt && Time.time >= _pendingMatchedUiAt)
        {
            _pendingMatchedUi = false;

            if (uiPanelActions != null)
                uiPanelActions.ShowRotationUI();

            SetState(MotionState.Matched);

            if (SceneFlow.Instance != null)
                SceneFlow.Instance.RestoreCurrentSceneAfterParity();
        }

        // ---------------- QUEST gyro capture + score ----------------
        if (TryGetHmdAngularVelocity(out Vector3 questW))
        {
            CurrentQuestGyro = questW;
            HasQuestGyro = true;

            float t = Time.time;
            _questGyroBuf.Add(new Sample(t, questW));
            TrimOld(_questGyroBuf, t - gyroWindowSeconds);

            Debug.Log($"QUEST RAW GYRO = {questW.magnitude:F3}");
        }
        else
        {
            HasQuestGyro = false;
        }

        // Recompute score periodically
        if (Time.time >= _nextGyroScoreAt)
{
    _nextGyroScoreAt = Time.time + Mathf.Max(0.05f, gyroScoreEverySeconds);

    if (_haveG && _questGyroBuf.Count > 20 && _espGyroBuf.Count > 20)
    {
        ComputeGyroScore(out _gyroScore, out _gyroScoreX, out _gyroScoreY, out _gyroScoreZ);

        if (useGyroForParity && authVerified)
        {
            // TIME-BASED CONFIDENCE:
            // Measure actual elapsed real time since the previous gyro decision.
            // The first evaluation uses the configured score interval as a
            // reasonable initial delta.
            float gyroEvalNow = Time.unscaledTime;
            float gyroElapsedSeconds =
                (_lastGyroConfidenceUpdateAt < 0f)
                    ? Mathf.Max(0.05f, gyroScoreEverySeconds)
                    : Mathf.Max(0f, gyroEvalNow - _lastGyroConfidenceUpdateAt);

            _lastGyroConfidenceUpdateAt = gyroEvalNow;
        //    if (_detachmentLockout)
        //    {
        //         _confidence = 0f;
        //         _parityBuilt = false;
        //         _pendingMatchedUi = false;
        //         _reauthRequired = true;

        //         Debug.Log("GYRO PARITY | DETACHMENT LOCKOUT ACTIVE - BUILD BLOCKED");

        //         UpdateParityUIState();
        //         return;
        //     }    
           
            bool imuGyroFresh = Time.time - _lastImuGyroAt <= imuGyroTimeoutSeconds;
            _currentImuGyroFresh = imuGyroFresh;

            if (!imuGyroFresh)
            {
                float imuAge = Time.time - _lastImuGyroAt;

                if (_parityBuilt)
                {
                    _confidence = 0f;
                    _parityBuilt = false;

                    XRDeviceLogger.Instance?.SetParityEstablished(false);

                    _pendingMatchedUi = false;
                    _reauthRequired = true;
                    //_detachmentLockout = true;

                    _lastParityEvent = "PARITY LOST BY IMU DISCONNECTION";

                    _lastParityReason =
                        $"No IMU gyro packet received for {imuAge:F2} seconds";

                    _lastParityEventTime =
                        DateTime.Now.ToString("HH:mm:ss.fff");

                    Debug.Log(
                        $"HeadsetMotion: LOST by stale IMU data. Age={imuAge:F2}s");

                    if (SceneFlow.Instance != null)
                        SceneFlow.Instance.ShowContinuousAuthUI();
                    else if (uiPanelActions != null)
                        uiPanelActions.ShowContinuousAuth();

                    UpdateParityUIState();
                }

                return;
            }

            else
            {
               float qGyroMag = AvgGyroMag(_questGyroBuf, 0.7f);
               float iGyroMag = AvgGyroMag(_espGyroBuf, 0.7f);

               UpdateSpeedFeedback(qGyroMag);

               _currentQuestGyroMagnitude = qGyroMag;
               _currentImuGyroMagnitude = iGyroMag;

               float motionDifference =
                    Mathf.Abs(qGyroMag - iGyroMag);

               _currentMotionDifference = motionDifference;

               float currentPitchOffset =
                    Mathf.DeltaAngle(-_r, questPitch);

               float currentRollOffset =
                    Mathf.DeltaAngle(-_p, questRoll);

               float pitchOffsetError = Mathf.Abs(
                    Mathf.DeltaAngle(_trustedPitchOffset, currentPitchOffset)
                );

               float rollOffsetError = Mathf.Abs(
                    Mathf.DeltaAngle(_trustedRollOffset, currentRollOffset)
                );

               bool mountingOrientationValid =
                    !_haveTrustedOrientationOffset ||
                    (pitchOffsetError <= attachmentPitchToleranceDeg &&
                    rollOffsetError <= attachmentRollToleranceDeg);

               _currentPitchOffsetError = pitchOffsetError;
               _currentRollOffsetError = rollOffsetError;
               _currentMountingOrientationValid =
                    mountingOrientationValid;

               if (!_parityBuilt)
               {
                    bool attachmentEvidenceValid =
                        mountingOrientationValid &&
                        motionDifference <= maxMotionDifference;

                    if (attachmentEvidenceValid)
                    {
                        _orientationValidTimer += gyroScoreEverySeconds;
                    }
                    else
                    {
                        _orientationValidTimer = 0f;
                    }
               }

               else
                {
                    _orientationValidTimer = 0f;
                }
                
               Debug.Log(
                    $"MOUNT CHECK | valid={mountingOrientationValid} " +
                    $"pitchErr={pitchOffsetError:F1} " +
                    $"rollErr={rollOffsetError:F1} " +
                    $"timer={_orientationValidTimer:F1}");

               if (_detachmentLockout &&
                mountingOrientationValid &&
                _orientationValidTimer >= attachmentConfirmSeconds)
               {
                _detachmentLockout = false;
                _confidence = 0f;

                Debug.Log(
                    "DETACHMENT LOCKOUT CLEARED by stable pitch/roll alignment");
                }

               float imuAge = Time.time - _lastImuGyroAt;

               Debug.Log(
                $"GYRO RAW | age={imuAge:F2}s " +
                $"score={_gyroScore:F2} " +
                $"qMag={qGyroMag:F2} " +
                $"iMag={iGyroMag:F2}");

               bool questActive = qGyroMag >= gyroQuestActiveRadPerSec;
               bool imuActive = iGyroMag >= gyroImuActiveRadPerSec;

               bool enoughMotionForScoring =
                    qGyroMag >= minEvidenceMotion ||
                    iGyroMag >= minEvidenceMotion;

               _currentQuestActive = questActive;
               _currentImuActive = imuActive;

               Debug.Log(
                $"ACTIVE? quest={questActive} imu={imuActive} " +
                $"qMag={qGyroMag:F2} iMag={iGyroMag:F2}");

            //    bool bothQuiet =
            //     qGyroMag < gyroQuestActiveRadPerSec &&
            //     iGyroMag < gyroImuActiveRadPerSec;

            //    _currentBothQuiet = bothQuiet;
               
            //    bool oneSidedGyroMismatch =
            //         qGyroMag > 0.15f &&
            //         motionDifference > oneSidedDifferenceThreshold;

            bool oneSidedGyroMismatch = false;

               Debug.Log(
                    $"GYRO PARITY | score={_gyroScore:F2} " +
                    $"qMag={qGyroMag:F2} iMag={iGyroMag:F2} " +
                    $"diff={motionDifference:F2} " +
                    $"conf={_confidence:F2}"); 

            bool detachedMotion =
                    qGyroMag >= gyroQuestActiveRadPerSec &&
                    iGyroMag < gyroImuActiveRadPerSec &&
                    motionDifference >= detachedDifferenceThreshold;
              
                _currentDetachedMotion = detachedMotion;

            if (_parityBuilt && detachedMotion)
               {
                    if (_detachWarningStartTime < 0f)
                    {
                        _detachWarningStartTime = Time.unscaledTime;

                        // Put confidence into warning range
                        _confidence = Mathf.Min(_confidence, almostLostThreshold);

                        if (uiPanelActions != null)
                            uiPanelActions.ShowParityLosingUI();
                    }

                    float warningElapsed =
                        Time.unscaledTime - _detachWarningStartTime;

                    if (warningElapsed >= detachWarningSeconds)
                    {
                        _confidence = 0f;
                        _parityBuilt = false;

                        XRDeviceLogger.Instance?.SetParityEstablished(false);

                        _pendingMatchedUi = false;
                        _reauthRequired = true;

                        if (uiPanelActions != null)
                            uiPanelActions.HideParityLosingUI();

                        if (SceneFlow.Instance != null)
                            SceneFlow.Instance.ShowContinuousAuthUI();
                        else if (uiPanelActions != null)
                            uiPanelActions.ShowContinuousAuth();

                        UpdateParityUIState();
                    }
                }
            else
                {
                    _detachWarningStartTime = -1f;
                }

            bool tooFastMotion = qGyroMag > 2.5f || iGyroMag > 2.5f;
            bool bothQuiet = false;
                _currentBothQuiet = false;
                                      
            if (tooFastMotion)
               {
                    Debug.Log(
                        $"GYRO PARITY | too fast for reliable scoring | " +
                        $"score={_gyroScore:F2} conf={_confidence:F2}");

                    // Hold confidence
                    _oneSidedBadCount = 0;
               }
            else if (bothQuiet)
               {
                    Debug.Log("GYRO PARITY | both quiet, holding confidence");
                    _lastBothQuietAt = Time.time;
                    _oneSidedBadCount = 0;
               }
            else if (oneSidedGyroMismatch)
               {
                    bool justStartedMoving = Time.time - _lastBothQuietAt < 0.25f;

                    if (justStartedMoving)
                    {
                        Debug.Log("GYRO PARITY | movement just started, ignoring first mismatch");
                        _oneSidedBadCount = 0;
                    }
                    else
                    {
                        _oneSidedBadCount++;

                        if (_oneSidedBadCount >= 1)
                        {
                            _confidence -= gyroMismatchLossPerSecond * gyroElapsedSeconds;
                            _confidence = Mathf.Clamp(_confidence, 0f, buildThreshold);
                            UpdateParityUIState();
                        }
                    }
                }
            else
                {
                    _oneSidedBadCount = 0;

                    if (_detachmentLockout)
                    {
                        // IMU was previously detached.
                        // Do not allow parity to rebuild from matching hand movement.
                        _confidence = 0f;

                        Debug.Log(
                            "GYRO PARITY | rebuild blocked because IMU detach lockout is active");
                    }

                    else
                    {
                            if (!enoughMotionForScoring)
                            {
                                if (_noMotionStartTime < 0f)
                                    _noMotionStartTime = Time.unscaledTime;

                                float noMotionElapsed =
                                    Time.unscaledTime - _noMotionStartTime;

                                if (noMotionElapsed >= noMotionGraceSeconds)
                                {
                                    _confidence -= noMotionLossPerSecond * gyroElapsedSeconds;
                                }
                            }
                            else
                            {
                                // Motion has resumed, so reset inactivity timer
                                _noMotionStartTime = -1f;

                                if (_gyroScore >= gyroBuildThreshold)
                                {
                                    _confidence += gyroGainPerSecond * gyroElapsedSeconds;
                                }
                                else if (_gyroScore < gyroLoseThreshold)
                                {
                                    _confidence -= gyroLossPerSecond * gyroElapsedSeconds;
                                }
                                else
                                {
                                    if (!_parityBuilt)
                                    {
                                        _confidence -=
                                            gyroUncertainLossPerSecond * gyroElapsedSeconds;
                                    }
                                }
                            }
                    }
                }

               _confidence = Mathf.Clamp(_confidence, 0f, buildThreshold);
               UpdateParityUIState();

            //    if ( !_parityBuilt &&
            //         !_detachmentLockout &&
            //         mountingOrientationValid &&
            //         _orientationValidTimer >= attachmentConfirmSeconds &&
            //         _confidence >= buildThreshold)

               bool motionDifferenceValid =
                    _currentMotionDifference <= maxMotionDifference;

               bool attachmentReady =
                    !_detachmentLockout ||
                    (
                        mountingOrientationValid &&
                        motionDifferenceValid &&
                        _orientationValidTimer >= attachmentConfirmSeconds
                    );

               if (!_parityBuilt &&
                   attachmentReady &&
                   _confidence >= buildThreshold)
               {
                bool wasReestablishing = _parityEverBuilt;
                _confidence = buildThreshold;

                // Save the mounting orientation only during the first trusted build.
                if (!_haveTrustedOrientationOffset)
                {
                    _trustedPitchOffset =
                        Mathf.DeltaAngle(-_r, questPitch);

                    _trustedRollOffset =
                        Mathf.DeltaAngle(-_p, questRoll);

                    _haveTrustedOrientationOffset = true;

                    Debug.Log(
                        $"TRUSTED MOUNT OFFSET SAVED | " +
                        $"pitch={_trustedPitchOffset:F1} " +
                        $"roll={_trustedRollOffset:F1}");
                }

                _parityBuilt = true;
                _parityEverBuilt = true;
                _reauthRequired = false;

                // Parity is restored — clear any warning UI
                _almostLostShowing = false;
                _detachWarningStartTime = -1f;

                if (uiPanelActions != null)
                {
                    uiPanelActions.HideParityLosingUI();
                }

                XRDeviceLogger.Instance?.SetParityEstablished(true);

                Debug.Log("Parity established. XR data recording started.");

                Debug.Log("HeadsetMotion: MATCHED by GYRO");

                _lastParityEvent = "PARITY ESTABLISHED";
                _lastParityReason =
                    $"Gyro score {_gyroScore:F3}, " +
                    $"confidence {_confidence:F3}, " +
                    $"mount valid={mountingOrientationValid}";

                _lastParityEventTime =
                    DateTime.Now.ToString("HH:mm:ss.fff");

                if (parityEventLogger != null)
                {
                    float trialTime = _trialRunning ? (Time.time - _trialStartTime) : 0f;

                    float imuYawQ = -_y;
                    float imuPitchQ = -_r;
                    float imuRollQ = -_p;

                    parityEventLogger.LogEvent(
                        "BUILT",
                        trialTime,
                        questYaw, questPitch, questRoll,
                        imuYawQ, imuPitchQ, imuRollQ,
                        _confidence,
                        _lastParityCos,
                        _gyroScore,
                        _lastParityReason
                    );
                }

                if (SceneFlow.Instance != null)
                {
                    if (wasReestablishing)
                        SceneFlow.Instance.RestoreCurrentSceneAfterParity();
                    else
                        SceneFlow.Instance.OpenContentScene("Lobby");
                }

                _pendingMatchedUi = true;
                _pendingMatchedUiAt = Time.time + matchedUiDelaySeconds;
               }
               else if (_parityBuilt && _confidence <= loseThreshold)
               {
                _parityBuilt = false;
                XRDeviceLogger.Instance?.SetParityEstablished(false);
                _pendingMatchedUi = false;
                _reauthRequired = true;

                Debug.Log("HeadsetMotion: LOST by GYRO");

                _lastParityEvent = "PARITY LOST";
                _lastParityReason =
                    $"Confidence {_confidence:F3} reached " +
                    $"lose threshold {loseThreshold:F3}; " +
                    $"gyro score={_gyroScore:F3}";

                _lastParityEventTime =
                    DateTime.Now.ToString("HH:mm:ss.fff");

                if (parityEventLogger != null)
                {
                    float trialTime = _trialRunning ? (Time.time - _trialStartTime) : 0f;

                    float imuYawQ = -_y;
                    float imuPitchQ = -_r;
                    float imuRollQ = -_p;

                    parityEventLogger.LogEvent(
                        "LOST",
                        trialTime,
                        questYaw, questPitch, questRoll,
                        imuYawQ, imuPitchQ, imuRollQ,
                        _confidence,
                        _lastParityCos,
                        _gyroScore,
                        _lastParityReason
                    );
                }

                if (SceneFlow.Instance != null)
                    SceneFlow.Instance.ShowContinuousAuthUI();
                else if (uiPanelActions != null)
                    uiPanelActions.ShowContinuousAuth();

                UpdateParityUIState();
               }
            
            }
       }             
    }
}
        if (rowGyroText)
        {
            string f2 = $"F{Mathf.Clamp(decimals, 0, 5)}";
            string s = (_questGyroBuf.Count > 20 && _espGyroBuf.Count > 20) ? _gyroScore.ToString(f2) : "—";
            rowGyroText.text =
                $"GYRO  | Score: {s}   (x:{_gyroScoreX.ToString(f2)} y:{_gyroScoreY.ToString(f2)} z:{_gyroScoreZ.ToString(f2)})";
        }

        // ---------------- CSV row write ----------------
        // if (csvLogger != null && csvLogger.ShouldWriteRow(Time.time))
        // {
        //     bool haveQuestNow = haveQuest;
        //     bool haveImuNow = _haveP && _haveR && _haveY;

        //     if (haveQuestNow && haveImuNow)
        //     {
        //         float imuYawQ = -_y;
        //         float imuPitchQ = -_r;
        //         float imuRollQ = -_p;

        //         float dy = Mathf.DeltaAngle(imuYawQ, questYaw);
        //         float dp = Mathf.DeltaAngle(imuPitchQ, questPitch);
        //         float dr = Mathf.DeltaAngle(imuRollQ, questRoll);

        //         float trialTime = _trialRunning ? (Time.time - _trialStartTime) : 0f;

        //         csvLogger.WriteRow(
        //             trialTime,
        //             questYaw, questPitch, questRoll,
        //             _p, _r, _y,
        //             imuPitchQ, imuYawQ, imuRollQ,
        //             dy, dp, dr,
        //             _parityBuilt,
        //             _lastParityCos
        //         );
        //     }
        // }

        if (parityEventLogger != null)
        {
            float trialTime = _trialRunning ? (Time.time - _trialStartTime) : 0f;

            float imuYawQ = -_y;
            float imuPitchQ = -_r;
            float imuRollQ = -_p;

            parityEventLogger.TickStillLostHeartbeat(
                trialTime,
                questYaw, questPitch, questRoll,
                imuYawQ, imuPitchQ, imuRollQ,
                _confidence,
                _lastParityCos,
                _gyroScore
            );
        }

        // -------- parity re-establish timeout logic --------
        bool trulyLost = _reauthRequired && !_parityBuilt && _state == MotionState.NotMatched;

        if (trulyLost)
        {
            if (!_parityLostCountdownRunning)
            {
                _parityLostCountdownRunning = true;
                _parityLostTimer = 0f;
            }

            _parityLostTimer += Time.deltaTime;

            if (_parityLostTimer >= parityLostTimeoutSeconds)
            {
                HandleParityLostTimeout();
            }
        }
        else
        {
            _parityLostCountdownRunning = false;
            _parityLostTimer = 0f;
        }
     
       // -------- timeout countdown text --------
        if (trulyLost)
        {
            if (_reauthRequired && !_parityBuilt)
                timeoutText.text = $"Re-authenticate in {Mathf.Max(0f, parityLostTimeoutSeconds - _parityLostTimer):F1}s";
            else
                timeoutText.text = "";
        }
    }

    void UpdateParityUIState()
    {
      if (_parityBuilt && _confidence <= almostLostThreshold && _confidence > loseThreshold)
     {
        if (!_almostLostShowing)
        {
            _almostLostShowing = true;

            if (uiPanelActions != null)
                uiPanelActions.ShowParityLosingUI();
        }
     }
     else
     {
        if (_almostLostShowing)
        {
            _almostLostShowing = false;

            if (uiPanelActions != null)
                uiPanelActions.HideParityLosingUI();
        }
     }

     if (parityText)
     {
        if (_parityBuilt)
            parityText.text = $"PARITY | BUILT (gyro:{_gyroScore:F2})";
        else if (_confidence <= loseThreshold)
            parityText.text = $"PARITY | LOST (gyro:{_gyroScore:F2})";
        else
            parityText.text = $"PARITY | SAMPLING (gyro:{_gyroScore:F2})";
     }

     if (_parityBuilt)
        SetState(MotionState.Matched);
     else if (_parityEverBuilt && _confidence <= loseThreshold)
        SetState(MotionState.NotMatched);
     else
        SetState(MotionState.Sampling);
    }
    
    // ---------- Reset (button) ----------
    public void OnResetClicked()
    {
        // 1) zero QUEST row
        if (TryGetHmdRotation(out var rot) || (Camera.main && (rot = Camera.main.transform.rotation) != Quaternion.identity))
        {
            _questZeroRot = Quaternion.Inverse(rot);
            _questHasZero = true;
        }

        // 2) tell IMU to zero
        if (imuSource != null)
            imuSource.SendImuCommand("BTN_B", true);

        // clear gyro buffers
        _questGyroBuf.Clear();
        _espGyroBuf.Clear();
        _gyroScore = _gyroScoreX = _gyroScoreY = _gyroScoreZ = 0f;
        _lastGyroConfidenceUpdateAt = -1f;

        // parity reset
        _parityBuilt = false;
        _confidence = 0f;
        _lastParityCos = 0f;
        _havePrevQuest = _havePrevImu = false;
        _imuYprUpdated = false;

        // User should press Reset only after physically attaching the IMU again. // DO not have a reset button now
        //_detachmentLockout = false; 
        _detachedTimer = 0f;

        _haveTrustedOrientationOffset = false;
        _trustedPitchOffset = 0f;
        _trustedRollOffset = 0f;
        _orientationValidTimer = 0f;

        _lastParityEvent = "RESET PRESSED";
        _lastParityReason =
                    "Waiting for Quest and IMU motion comparison";

        _lastParityEventTime =
                    DateTime.Now.ToString("HH:mm:ss.fff");

        if (parityText) parityText.text = "PARITY | —";

        // start new CSV logging session
        // if (csvLogger != null)
        // {
        //     csvLogger.StopCsvLogging();
        //     csvLogger.StartCsvLogging();
        // }
        if (parityEventLogger != null)
        {
        parityEventLogger.StopLogging();   // closes previous file if any
        parityEventLogger.StartLogging();
        }
        // Start trial timer
        _trialStartTime = Time.time;
        _trialRunning = true;  

        if (progressBar != null)
        {
            progressBar.ResetFillAmount();
            progressBar.StartUpdateProgressBar();
        }

        SetState(MotionState.Sampling);

        _parityLostTimer = 0f;
        _parityLostCountdownRunning = false;
        _parityEverBuilt = false;
        _reauthRequired = false;
    }

    public void PauseFSM()
    {
        SetState(MotionState.Paused);
    }

    public void ResumeFSM()
    {
        SetState(MotionState.Sampling);
    }

    public void SetAuthVerified(bool value)
    {
        authVerified = value;

        if (!authVerified)
        {
            _parityBuilt = false;
            XRDeviceLogger.Instance?.SetParityEstablished(false);
            _confidence = 0f;
            _lastParityCos = 0f;
            _havePrevQuest = false;
            _havePrevImu = false;
            _imuYprUpdated = false;
            _pendingMatchedUi = false;
            _parityEverBuilt = false;
            _reauthRequired = false;
            _parityLostTimer = 0f;
            _parityLostCountdownRunning = false;
            _lastGyroConfidenceUpdateAt = -1f;

            if (parityText) parityText.text = "PARITY | —";

            SetState(MotionState.Paused, true);

            if (uiPanelActions != null)
                uiPanelActions.HideContinuousAuthVisuals();
        }
    }

    void UpdateSpeedFeedback(float qGyroMag)
    {
        if (speedFeedbackText == null)
            return;

        if (qGyroMag < minGoodSpeed)
        {
            speedFeedbackText.text = "MOVE FASTER";
        }
        else if (qGyroMag > maxGoodSpeed)
        {
            speedFeedbackText.text = "MOVE SLOWER";
        }
        else
        {
            speedFeedbackText.text = "GOOD SPEED";
        }
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

    // NEW: Quest angular velocity (gyro)
    static bool TryGetHmdAngularVelocity(out Vector3 w)
    {
        var dev = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (dev.isValid && dev.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out w))
            return true;

        w = Vector3.zero;
        return false;
    }

    float AvgGyroMag(List<Sample> buf, float seconds)
    {
    float now = Time.time;
    float sum = 0f;
    int count = 0;

    for (int i = buf.Count - 1; i >= 0; i--)
    {
        if (now - buf[i].t > seconds)
            break;

        sum += buf[i].v.magnitude;
        count++;
    }

    if (count == 0) return 0f;
    return sum / count;
    }

    static void TrimOld(List<Sample> buf, float tMin)
    {
        int removeCount = 0;
        for (int i = 0; i < buf.Count; i++)
        {
            if (buf[i].t >= tMin) break;
            removeCount++;
        }
        if (removeCount > 0)
            buf.RemoveRange(0, removeCount);
    }

    // Computes correlation on synchronized-by-time sampling (simple nearest-neighbor)
    void ComputeGyroScore(out float score, out float sx, out float sy, out float sz)
    {
        var Q = _questGyroBuf;
        var E = _espGyroBuf;
        if (Q.Count < 10 || E.Count < 10) { score = sx = sy = sz = 0f; return; }

        float t0 = Mathf.Max(Q[0].t, E[0].t);
        float t1 = Mathf.Min(Q[Q.Count - 1].t, E[E.Count - 1].t);
        if (t1 <= t0) { score = sx = sy = sz = 0f; return; }

        List<Vector3> qList = new List<Vector3>(Q.Count);
        List<Vector3> eList = new List<Vector3>(Q.Count);

        int ei = 0;
        for (int qi = 0; qi < Q.Count; qi++)
        {
            float tq = Q[qi].t;
            if (tq < t0 || tq > t1) continue;

            while (ei + 1 < E.Count && E[ei + 1].t <= tq) ei++;

            int best = ei;
            if (ei + 1 < E.Count)
            {
                float d0 = Mathf.Abs(E[ei].t - tq);
                float d1 = Mathf.Abs(E[ei + 1].t - tq);
                if (d1 < d0) best = ei + 1;
            }

            qList.Add(Q[qi].v);
            eList.Add(E[best].v);
        }

        if (qList.Count < 20) { score = sx = sy = sz = 0f; return; }

        sx = CorrAxis(qList, eList, 0);
        sy = CorrAxis(qList, eList, 1);
        sz = CorrAxis(qList, eList, 2);

        score = (Mathf.Abs(sx) + Mathf.Abs(sy) + Mathf.Abs(sz)) / 3f;
    }

    static float CorrAxis(List<Vector3> a, List<Vector3> b, int axis)
    {
        int n = Mathf.Min(a.Count, b.Count);
        if (n < 5) return 0f;

        float meanA = 0f, meanB = 0f;
        for (int i = 0; i < n; i++)
        {
            meanA += Axis(a[i], axis);
            meanB += Axis(b[i], axis);
        }
        meanA /= n;
        meanB /= n;

        float num = 0f, denA = 0f, denB = 0f;
        for (int i = 0; i < n; i++)
        {
            float da = Axis(a[i], axis) - meanA;
            float db = Axis(b[i], axis) - meanB;
            num += da * db;
            denA += da * da;
            denB += db * db;
        }

        float den = Mathf.Sqrt(denA * denB);
        if (den < 1e-6f) return 0f;
        return num / den;
    }

    static float Axis(Vector3 v, int axis)
    {
        if (axis == 0) return v.x;
        if (axis == 1) return v.y;
        return v.z;
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