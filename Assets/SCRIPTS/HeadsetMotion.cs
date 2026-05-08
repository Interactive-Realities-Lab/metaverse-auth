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

    [Tooltip("How often to recompute score (seconds).")]
    public float gyroScoreEverySeconds = 0.5f;

    [Tooltip("If score stays below this for N checks -> mismatch (logic lives elsewhere).")]
    public float gyroScoreWarnThreshold = 0.6f;

    float _lastParityCos = 0f;

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
    float _gyroScore = 0f;
    float _gyroScoreX = 0f, _gyroScoreY = 0f, _gyroScoreZ = 0f;

    float _lastImuYprAt = -999f;
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
        Debug.Log("Parity lost timeout reached. Returning to login panel.");

        // Force full logout
        SetAuthVerified(false);
        FingerprintWsClient.I?.StopContinuous();

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
        _espGyro = new Vector3(gx, gy, gz);
        _haveG = true;

        float t = Time.time;
        _espGyroBuf.Add(new Sample(t, _espGyro));
        TrimOld(_espGyroBuf, t - gyroWindowSeconds);

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

            if (rowQuestText)
            {
                rowQuestText.text =
                    $"QUEST | Yaw: {questYaw.ToString(f)}°  Pitch: {questPitch.ToString(f)}°  Roll: {questRoll.ToString(f)}°";
            }
        }

        // ----- IMU row -----
        string ff = $"F{Mathf.Clamp(decimals, 0, 5)}";

        float imuRawPitch = _p;
        float imuRawRoll = _r;
        float imuRawYaw = _y;

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
                            good = cosSim >= parityCosThreshold;

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

                     
                    if (!_parityBuilt && _confidence >= buildThreshold)
                        {
                            _confidence = buildThreshold; // make sure progress reports full
                            _parityBuilt = true;
                            _parityEverBuilt = true;
                            _reauthRequired = false;   // stop re-auth timeout once parity is built again

                            Debug.Log("HeadsetMotion: MATCHED");

                            if (SceneFlow.Instance != null)
                                SceneFlow.Instance.RestoreLobbyUI();
                            else if (uiPanelActions != null)
                                uiPanelActions.HideContinuousAuthVisuals();

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

        if (_pendingMatchedUi && _parityBuilt && Time.time >= _pendingMatchedUiAt)
        {
            _pendingMatchedUi = false;

            if (uiPanelActions != null)
                uiPanelActions.ShowRotationUI();

            SetState(MotionState.Matched);

            if (SceneFlow.Instance != null)
                SceneFlow.Instance.OpenContentScene("Lobby");
        }

        // ---------------- QUEST gyro capture + score ----------------
        if (TryGetHmdAngularVelocity(out Vector3 questW))
        {
            float t = Time.time;
            _questGyroBuf.Add(new Sample(t, questW));
            TrimOld(_questGyroBuf, t - gyroWindowSeconds);
        }

        // Recompute score periodically
        if (Time.time >= _nextGyroScoreAt)
        {
            _nextGyroScoreAt = Time.time + Mathf.Max(0.05f, gyroScoreEverySeconds);

            if (_haveG && _questGyroBuf.Count > 20 && _espGyroBuf.Count > 20)
            {
                ComputeGyroScore(out _gyroScore, out _gyroScoreX, out _gyroScoreY, out _gyroScoreZ);
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
        if (csvLogger != null && csvLogger.ShouldWriteRow(Time.time))
        {
            bool haveQuestNow = haveQuest;
            bool haveImuNow = _haveP && _haveR && _haveY;

            if (haveQuestNow && haveImuNow)
            {
                float imuYawQ = -_y;
                float imuPitchQ = -_r;
                float imuRollQ = -_p;

                float dy = Mathf.DeltaAngle(imuYawQ, questYaw);
                float dp = Mathf.DeltaAngle(imuPitchQ, questPitch);
                float dr = Mathf.DeltaAngle(imuRollQ, questRoll);

                float trialTime = _trialRunning ? (Time.time - _trialStartTime) : 0f;

                csvLogger.WriteRow(
                    trialTime,
                    questYaw, questPitch, questRoll,
                    _p, _r, _y,
                    imuPitchQ, imuYawQ, imuRollQ,
                    dy, dp, dr,
                    _parityBuilt,
                    _lastParityCos
                );
            }
        }

        // -------- parity re-establish timeout logic --------
        if (_reauthRequired && !_parityBuilt)
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
        if (timeoutText != null)
        {
            if (_reauthRequired && !_parityBuilt)
                timeoutText.text = $"Re-authenticate in {Mathf.Max(0f, parityLostTimeoutSeconds - _parityLostTimer):F1}s";
            else
                timeoutText.text = "";
        }
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

        // parity reset
        _parityBuilt = false;
        _confidence = 0f;
        _lastParityCos = 0f;
        _havePrevQuest = _havePrevImu = false;
        _imuYprUpdated = false;
        if (parityText) parityText.text = "PARITY | —";

        // start new CSV logging session
        if (csvLogger != null)
        {
            csvLogger.StopCsvLogging();
            csvLogger.StartCsvLogging();
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

            if (parityText) parityText.text = "PARITY | —";

            SetState(MotionState.Paused, true);

            if (uiPanelActions != null)
                uiPanelActions.HideContinuousAuthVisuals();
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