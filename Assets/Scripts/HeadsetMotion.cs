using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.IO;
using System.Text;

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

    [Header("Parity logic (angles)")]
    public float parityBuildSeconds = 1.0f;     // need this long “good” to build
    public float parityCheckEverySeconds = 0.15f; // how often to evaluate
    public float parityDeltaToleranceDeg = 4.5f; // tolerance on delta-changes
    public int parityBadChecksToLose = 3;       // consecutive bad checks => loss

    bool _parityBuilt = false;
    float _goodAccum = 0f;
    int _badStreak = 0;
    float _nextParityCheckAt = 0f;

    // previous samples (to compute change)
    bool _havePrevQuest = false, _havePrevImu = false;
    Vector3 _prevQuestPYR; // (pitch,yaw,roll) or however you store it
    Vector3 _prevImuPYR;


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

    // ---------------- CSV LOGGING ----------------
    [Header("CSV Logging")]
    public bool enableCsvLogging = true;

    [Tooltip("How often to write a row (seconds). 0.05=20Hz, 0.1=10Hz")]
    public float logEverySeconds = 1f;

    [Tooltip("Label for the IMU placement (e.g., 'TopFront', 'LeftStrap', 'BackCenter').")]
    public string imuPlacementLabel = "TopFront";

    [Tooltip("Optional: participant/session id.")]
    public string sessionId = "S01";

    [Tooltip("Write file in persistentDataPath (recommended for Quest/PC).")]
    public bool usePersistentDataPath = true;

    StreamWriter _csv;
    StringBuilder _sb = new StringBuilder(512);
    float _nextLogAt = 0f;
    bool _isLogging = false;
    string _csvPath = "";

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
            rowGyroText = transform.Find("RowGyro/Text (TMP)")?.GetComponent<TMP_Text>(); // optional
        if (resetButton == null)
            resetButton = transform.Find("Reset")?.GetComponent<Button>();
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
        if (parityText == null)
            parityText = transform.Find("RowParity/Text (TMP)")?.GetComponent<TMP_Text>(); // optional row

        // Auto-find IMU source if not set
        if (imuSource == null)
            imuSource = FindObjectOfType<FingerprintWsClient>();

        // Initial UI
        if (rowQuestText) rowQuestText.text = "QUEST | Yaw: —  Pitch: —  Roll: —";
        if (rowImuText) rowImuText.text = "IMU   | Pitch: —  Roll: —  Yaw: —";
        if (rowDeltaText) rowDeltaText.text = "Delta | Yaw: —  Pitch: —  Roll: —";
        if (rowGyroText) rowGyroText.text = "GYRO  | Score: — (x:— y:— z:—)";
        if (parityText) parityText.text = "PARITY | —";
    }

    void OnEnable()
    {
        // Subscribe to IMU angle updates
        if (imuSource != null)
            imuSource.OnImuYpr += OnImuYpr;

        // NEW: Subscribe to ESP gyro updates (you will add this event in FingerprintWsClient)
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

    float _lastImuYprAt = -999f;
    bool _imuYprUpdated = false;

    // Called by FingerprintWsClient whenever it parses an IMU message
   /* void OnImuYpr(float pitch, float roll, float yaw)
    {
        _p = pitch;
        _r = roll;
        _y = yaw;
        _haveP = _haveR = _haveY = true;
    }*/

    void OnImuYpr(float pitch, float roll, float yaw)
    {
        _p = pitch; _r = roll; _y = yaw;
        _haveP = _haveR = _haveY = true;

        _lastImuYprAt = Time.time;
        _imuYprUpdated = true;
    }

    // NEW: Called by FingerprintWsClient whenever it parses ESP gyro
    void OnImuGyro(float gx, float gy, float gz)
    {
        _espGyro = new Vector3(gx, gy, gz);
        _haveG = true;

        // store into buffer with timestamp
        float t = Time.time;
        _espGyroBuf.Add(new Sample(t, _espGyro));
        TrimOld(_espGyroBuf, t - gyroWindowSeconds);

        #if UNITY_EDITOR
        Debug.Log($"ESP GYRO: gx={gx:F3}, gy={gy:F3}, gz={gz:F3}");
        #endif
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
                rowQuestText.text =
                    $"QUEST | Yaw: {questYaw.ToString(f)}°  Pitch: {questPitch.ToString(f)}°  Roll: {questRoll.ToString(f)}°";
        }

        // ----- IMU row (from FingerprintWsClient) -----
        string ff = $"F{Mathf.Clamp(decimals, 0, 5)}";

        // raw numbers as floats (so we can negate)
        float imuRawPitch = _p; // IMU pitch
        float imuRawRoll = _r; // IMU roll
        float imuRawYaw = _y; // IMU yaw

        // YOUR Quest mapping:
        // QuestYaw  <- -IMU 
        // QuestPitch<-  IMU 
        // QuestRoll <- -IMU 
        string imuYawMappedStr = (_haveR ? (-imuRawRoll).ToString(ff) : "—");  // -_r
        string imuPitchMappedStr = (_haveY ? (-imuRawYaw).ToString(ff) : "—");  // - _y
        string imuRollMappedStr = (_haveP ? (imuRawPitch).ToString(ff) : "—"); // _p

        rowImuText.text =
           $"IMU | Yaw: {imuPitchMappedStr}°  Pitch: {imuRollMappedStr}°  Roll: {imuYawMappedStr}°";

        // ----- Delta row -----
       
        if (rowDeltaText)
        {
            // IMU mapped into Quest axes (KEEP THIS SAME everywhere: IMU row, Delta, Parity)
            float imuYawQ = -_y;
            float imuPitchQ = _p;
            float imuRollQ = -_r;

            Vector3 imuNow = new Vector3(imuPitchQ, imuYawQ, imuRollQ);
            Vector3 questNow = new Vector3(questPitch, questYaw, questRoll);

            float dy = haveQuest ? Mathf.DeltaAngle(imuYawQ, questYaw) : 0f;
            float dp = haveQuest ? Mathf.DeltaAngle(imuPitchQ, questPitch) : 0f;
            float dr = haveQuest ? Mathf.DeltaAngle(imuRollQ, questRoll) : 0f;

            rowDeltaText.text =
                $"Delta | Yaw: {dy.ToString(ff)}°  Pitch: {dp.ToString(ff)}°  Roll: {dr.ToString(ff)}°";
        }

        // ---------------- PARITY (angles: change should match) ----------------
        if (Time.time >= _nextParityCheckAt)
        {
            _nextParityCheckAt = Time.time + Mathf.Max(0.05f, parityCheckEverySeconds);

            // We need Quest angles + IMU angles available
            bool haveImuNow = _haveP && _haveR && _haveY;
            bool haveQuestNow = haveQuest;

            if (haveQuestNow && haveImuNow)
            {
                // IMPORTANT: use the same mapping you already use in Delta row
                // Quest uses: (Pitch=eul.x, Yaw=eul.y, Roll=eul.z)
                Vector3 questNow = new Vector3(questPitch, questYaw, questRoll);

                // Your current mapping (must match Delta mapping)
                Vector3 imuNow = new Vector3(_p, -_y, -_r);

                if (_havePrevQuest && _havePrevImu)
                {
                    // change since last sample (wrap-aware)
                    float dQuestPitch = Mathf.DeltaAngle(_prevQuestPYR.x, questNow.x);
                    float dQuestYaw = Mathf.DeltaAngle(_prevQuestPYR.y, questNow.y);
                    float dQuestRoll = Mathf.DeltaAngle(_prevQuestPYR.z, questNow.z);

                    float dImuPitch = Mathf.DeltaAngle(_prevImuPYR.x, imuNow.x);
                    float dImuYaw = Mathf.DeltaAngle(_prevImuPYR.y, imuNow.y);
                    float dImuRoll = Mathf.DeltaAngle(_prevImuPYR.z, imuNow.z);

                    Vector3 dq = new Vector3(dQuestPitch, dQuestYaw, dQuestRoll);
                    Vector3 di = new Vector3(dImuPitch, dImuYaw, dImuRoll);

                    // Gate evaluation on whether IMU actually updated since last parity check
                    bool imuUpdatedSinceLastCheck = _imuYprUpdated;
                    _imuYprUpdated = false;

                    float cosSim = 0f;
                    bool good = false;

                    // Only evaluate when:
                    // 1) IMU updated
                    // 2) motion is not almost still
                    // 3) vectors are not tiny
                    bool evaluated =
                        imuUpdatedSinceLastCheck &&
                        dq.sqrMagnitude >= 1e-6f &&
                        di.sqrMagnitude >= 1e-6f &&
                        dq.magnitude >= 0.2f;

                    if (evaluated)
                    {
                        cosSim = Vector3.Dot(dq.normalized, di.normalized);
                        good = cosSim >= 0.65f;

                        _lastParityCos = cosSim;

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
                    // else: neutral (do not build, do not punish)
#if UNITY_EDITOR

                    if (Time.frameCount % 30 == 0)
                    {
                        Debug.Log(
                            $"dq(Q): P{dq.x:F2} Y{dq.y:F2} R{dq.z:F2} | " +
                            $"di(IMU): P{di.x:F2} Y{di.y:F2} R{di.z:F2} | " +
                            $"updated:{imuUpdatedSinceLastCheck} eval:{evaluated} cos:{cosSim:F2} built:{_parityBuilt}");
                    }
#endif

                    if (parityText)
                    {
                        parityText.text = _parityBuilt
                            ? $"PARITY | BUILT (cos:{cosSim:F2})"
                            : $"PARITY | LOST (cos:{cosSim:F2})";
                    }
                }

                // ALWAYS update previous values (even if we didn't evaluate)
                _prevQuestPYR = questNow;
                _prevImuPYR = imuNow;
                _havePrevQuest = _havePrevImu = true;
            }
            else
            {
                // missing data -> treat as not built
                _lastParityCos = 0f;
                _goodAccum = 0f;
                _badStreak = 0;
                _parityBuilt = false;
                if (parityText) parityText.text = "PARITY | —";
                _havePrevQuest = _havePrevImu = false;
            }
        }


        // ---------------- QUEST gyro capture + score ----------------
        // Capture Quest angular velocity each frame
        if (TryGetHmdAngularVelocity(out Vector3 questW))
        {
            float t = Time.time;
            _questGyroBuf.Add(new Sample(t, questW));
            TrimOld(_questGyroBuf, t - gyroWindowSeconds);
        }

        // Recompute score periodically (does not affect your existing logic)
        if (Time.time >= _nextGyroScoreAt)
        {
            _nextGyroScoreAt = Time.time + Mathf.Max(0.05f, gyroScoreEverySeconds);

            if (_haveG && _questGyroBuf.Count > 20 && _espGyroBuf.Count > 20)
            {
                // Compare on magnitude first (robust even if axis mapping imperfect)
                // and also per-axis with correlation.
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
        if (_isLogging && Time.time >= _nextLogAt)
        {
            _nextLogAt = Time.time + Mathf.Max(0.01f, logEverySeconds);

            bool haveQuestNow = haveQuest;
            bool haveImuNow = _haveP && _haveR && _haveY;

            if (haveQuestNow && haveImuNow)
            {
                // Use EXACT SAME mapping you already use in Delta/Parity:
                float imuYawQ = -_y;
                float imuPitchQ = _p;
                float imuRollQ = -_r;

                float dy = Mathf.DeltaAngle(imuYawQ, questYaw);
                float dp = Mathf.DeltaAngle(imuPitchQ, questPitch);
                float dr = Mathf.DeltaAngle(imuRollQ, questRoll);

                // Parity cos is local inside your parity block right now.
                // So we store a field for it (see step 6 below).
                float parityCos = _lastParityCos;

                string utc = DateTime.UtcNow.ToString("o"); // ISO 8601

                _sb.Length = 0;
                _sb.Append(utc).Append(',');
                float trialTime = _trialRunning ? (Time.time - _trialStartTime) : 0f;
                _sb.Append(trialTime.ToString("F2")).Append(',');
                _sb.Append(imuPlacementLabel).Append(',');

                _sb.Append(questYaw.ToString("F2")).Append(',');
                _sb.Append(questPitch.ToString("F2")).Append(',');
                _sb.Append(questRoll.ToString("F2")).Append(',');

                _sb.Append(_p.ToString("F2")).Append(',');
                _sb.Append(_r.ToString("F2")).Append(',');
                _sb.Append(_y.ToString("F2")).Append(',');

                _sb.Append(imuPitchQ.ToString("F2")).Append(',');
                _sb.Append(imuYawQ.ToString("F2")).Append(',');
                _sb.Append(imuRollQ.ToString("F2")).Append(',');

                _sb.Append(dy.ToString("F2")).Append(',');
                _sb.Append(dp.ToString("F2")).Append(',');
                _sb.Append(dr.ToString("F2")).Append(',');

                _sb.Append(_parityBuilt ? "1" : "0").Append(',');
                _sb.Append(parityCos.ToString("F2")).Append(',');

                _csv.WriteLine(_sb.ToString());

                // Flush occasionally (not every row) to avoid performance hit
                if (Time.frameCount % 60 == 0) _csv.Flush();
            }
        }
    }

    // ---------- Reset (button) ----------
    public void OnResetClicked()
    {
        // 1) zero QUEST row (store as quaternion)
        if (TryGetHmdRotation(out var rot) || (Camera.main && (rot = Camera.main.transform.rotation) != Quaternion.identity))
        {
            _questZeroRot = Quaternion.Inverse(rot);
            _questHasZero = true;
        }

        // 2) tell IMU to zero
        if (imuSource != null)
            imuSource.SendImuCommand("BTN_B", true);

        // NEW: clear gyro buffers so score restarts clean
        _questGyroBuf.Clear();
        _espGyroBuf.Clear();
        _gyroScore = _gyroScoreX = _gyroScoreY = _gyroScoreZ = 0f;

        // parity reset
        _parityBuilt = false;
        _goodAccum = 0f;
        _badStreak = 0;
        _havePrevQuest = _havePrevImu = false;
        if (parityText) parityText.text = "PARITY | —";
 
        // ---- START NEW CSV LOGGING SESSION ----
        StopCsvLogging();     // close previous file (if any)
        StartCsvLogging();    // start fresh file for this trial

        // Start trial timer
        _trialStartTime = Time.time;
        _trialRunning = true;
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
        // Remove oldest samples outside the window
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
        // Build paired samples by time: for each Quest sample find nearest ESP sample
        var Q = _questGyroBuf;
        var E = _espGyroBuf;
        if (Q.Count < 10 || E.Count < 10) { score = sx = sy = sz = 0f; return; }

        // Work within the overlap time range
        float t0 = Mathf.Max(Q[0].t, E[0].t);
        float t1 = Mathf.Min(Q[Q.Count - 1].t, E[E.Count - 1].t);
        if (t1 <= t0) { score = sx = sy = sz = 0f; return; }

        // Collect paired vectors
        List<Vector3> qList = new List<Vector3>(Q.Count);
        List<Vector3> eList = new List<Vector3>(Q.Count);

        int ei = 0;
        for (int qi = 0; qi < Q.Count; qi++)
        {
            float tq = Q[qi].t;
            if (tq < t0 || tq > t1) continue;

            // advance ei until E[ei].t is close to tq
            while (ei + 1 < E.Count && E[ei + 1].t <= tq) ei++;

            // choose nearer of ei and ei+1
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

        // OPTIONAL: If your ESP gyro axis mapping differs, map here.
        // Right now: keep as-is. If scores are low, we can add a mapping matrix later.

        sx = CorrAxis(qList, eList, 0);
        sy = CorrAxis(qList, eList, 1);
        sz = CorrAxis(qList, eList, 2);

        // overall score: mean absolute correlation
        score = (Mathf.Abs(sx) + Mathf.Abs(sy) + Mathf.Abs(sz)) / 3f;
    }

    static float CorrAxis(List<Vector3> a, List<Vector3> b, int axis)
    {
        int n = Mathf.Min(a.Count, b.Count);
        if (n < 5) return 0f;

        // Extract + mean remove (kills gyro bias)
        float meanA = 0f, meanB = 0f;
        for (int i = 0; i < n; i++)
        {
            meanA += Axis(a[i], axis);
            meanB += Axis(b[i], axis);
        }
        meanA /= n; meanB /= n;

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

    public void StartCsvLogging()
    {
        if (!enableCsvLogging) return;
        if (_isLogging) return;

        string dir = usePersistentDataPath ? Application.persistentDataPath : Application.dataPath;
        Directory.CreateDirectory(dir);

        string fileName = $"HeadsetIMU_{sessionId}_{imuPlacementLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        _csvPath = Path.Combine(dir, fileName);

        _csv = new StreamWriter(_csvPath, false, Encoding.UTF8);

        // Header
        _csv.WriteLine(
            "utc_iso,unity_time,imu_placement," +
            "questYaw,questPitch,questRoll," +
            "imuPitch,imuRoll,imuYaw," +
            "imu_map_pitchQ,imu_map_yawQ,imu_map_rollQ," +
            "deltaYaw,deltaPitch,deltaRoll," +
            "parity_built,parity_cos"            
        );

        _csv.Flush();
        _nextLogAt = Time.time;
        _isLogging = true;

        Debug.Log($"CSV logging STARTED: {_csvPath}");
    }

    public void StopCsvLogging()
    {
        if (!_isLogging) return;

        _isLogging = false;

        try
        {
            _csv?.Flush();
            _csv?.Close();
            _csv = null;
        }
        catch { /* ignore */ }

        Debug.Log($"CSV logging STOPPED: {_csvPath}");
    }

    void OnApplicationQuit()
    {
        StopCsvLogging();
    }
}
