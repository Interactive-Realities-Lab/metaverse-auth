using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ParityDebugPanel : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Object containing the HeadsetMotion component.")]
    [SerializeField] private HeadsetMotion headMotion;

    [Header("Text Output")]
    [SerializeField] private TMP_Text leftDebugText;
    [SerializeField] private TMP_Text frontDebugText;
    [SerializeField] private TMP_Text rightDebugText;

    [Header("Refresh Rate")]
    [Tooltip("0.1 means the panel updates 10 times per second.")]
    [Min(0.02f)]
    [SerializeField] private float refreshInterval = 0.1f;

    [Header("Debug Scene")]
    [SerializeField] private string debugSceneName = "DebugScene";

    private float _nextRefreshTime;
    private float _nextFindAttempt;

    private readonly StringBuilder _leftText =
        new StringBuilder(2500);

    private readonly StringBuilder _frontText =
        new StringBuilder(4000);

    private readonly StringBuilder _rightText =
        new StringBuilder(3500);


    private void Awake()
    {
        FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        RefreshDebugPanel();
    }

    private void Update()
    {
        if (headMotion == null ||
            !headMotion.isActiveAndEnabled)
        {
            if (Time.unscaledTime >= _nextFindAttempt)
            {
                _nextFindAttempt =
                    Time.unscaledTime + 1f;

                FindReferences();
            }
        }

        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime =
            Time.unscaledTime + refreshInterval;

        RefreshDebugPanel();
    }

    // FIND HEADSET MOTION
    // ============================================================

    private void FindReferences()
    {
        if (headMotion != null &&
            headMotion.isActiveAndEnabled)
        {
            return;
        }

        HeadsetMotion[] candidates =
            FindObjectsByType<HeadsetMotion>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

        foreach (HeadsetMotion candidate in candidates)
        {
            if (candidate == null ||
                !candidate.isActiveAndEnabled)
            {
                continue;
            }

            if (candidate.gameObject.scene.name == "Authentication")
            {
                headMotion = candidate;

                Debug.Log(
                    $"PARITY DEBUG | HeadsetMotion found in scene " +
                    $"{candidate.gameObject.scene.name} | " +
                    $"Object = {candidate.gameObject.name} | " +
                    $"Build Threshold = {candidate.buildThreshold:F2}"
                );

                return;
            }
        }

        headMotion = null;
    }

    // REFRESH ALL THREE PANELS
    // ============================================================

    private void RefreshDebugPanel()
    {
        if (leftDebugText == null ||
            frontDebugText == null ||
            rightDebugText == null)
            return;

        if (headMotion == null)
        {
            leftDebugText.text =
                "<b>PARITY DEBUG</b>\n\n" +
                "<color=#FF7777>HeadsetMotion not found</color>";

            frontDebugText.text =
                "<color=#FF7777>No calculation data</color>";

            rightDebugText.text =
                "Make sure the Authentication scene is loaded.";

            return;
        }

        _leftText.Clear();
        _frontText.Clear();
        _rightText.Clear();

        AddLeftConsole();
        AddFrontConsole();
        AddRightConsole();

        leftDebugText.text = _leftText.ToString();
        frontDebugText.text = _frontText.ToString();
        rightDebugText.text = _rightText.ToString();
    }

    // LEFT PANEL
    // RAW INPUT DATA
    // ============================================================

    private void AddLeftConsole()
    {
        _leftText.AppendLine("<b>========== INPUT DATA ==========</b>");
        _leftText.AppendLine();

        _leftText.Append("Parity status : ");
        _leftText.AppendLine(GetParityStatusText());

        AddLeftValue(
            "Confidence",
            headMotion.CurrentConfidence,
            "",
            2,
            8);

        AddLeftValue(
            "Initial build threshold",
            headMotion.buildThreshold,
            "",
            2,
            8);

        float progress =
            headMotion.buildThreshold > 0f
                ? headMotion.CurrentConfidence /
                  headMotion.buildThreshold
                : 0f;

        AddLeftValue(
            "Build progress",
            Mathf.Clamp01(progress) * 100f,
            " %",
            1,
            7);


        if (headMotion.ParityLostCountdownRunning)
        {
            AddLeftValue(
                "Logout remaining",
                headMotion.ParityLostTimeRemaining(),
                " s",
                2,
                7);
        }

        // CONFIDENCE
        // ========================================================

        _leftText.AppendLine();
        _leftText.AppendLine(
            "<b>CONFIDENCE UPDATE</b>");

        float nominalDelta =
            GetNominalConfidenceDelta();


        _leftText.Append("Current action: ");
        _leftText.AppendLine(
            GetConfidenceActionText());
        _leftText.AppendLine();

        _leftText.AppendLine(
            "Confidence change = Rate x Elapsed time");

        float selectedRate =
            GetCurrentConfidenceRate();

        AddLeftValue(
            "Rate",
            selectedRate,
            " /s",
            3,
            8);

        AddLeftValue(
            "Elapsed time",
            headMotion.gyroScoreEverySeconds,
            " s",
            3,
            8);

        _leftText.Append("Change = ");

        _leftText.Append(
            FixedNumber(
                selectedRate,
                3,
                8));

        _leftText.Append(" x ");

        _leftText.Append(
            FixedNumber(
                headMotion.gyroScoreEverySeconds,
                3,
                8));

        _leftText.Append(" = ");

        _leftText.Append(
            FixedSignedNumber(
                nominalDelta,
                3,
                9));

        _leftText.AppendLine();


        _leftText.AppendLine();
        _leftText.Append(
            "<b>ACTION: </b>");

        _leftText.AppendLine(
            GetAlgorithmAction());

        // QUEST
        // --------------------------------------------------------

        _leftText.AppendLine();
        _leftText.AppendLine("<b>QUEST HEADSET</b>");

        if (headMotion.HasQuestRotation)
        {
            _leftText.AppendLine(
            $"Yaw: {FixedNumber(headMotion.CurrentQuestYaw, 1)}°   " +
            $"Pitch: {FixedNumber(headMotion.CurrentQuestPitch, 1)}°   " +
            $"Roll: {FixedNumber(headMotion.CurrentQuestRoll, 1)}°"
           );
        }
        else
        {
            _leftText.AppendLine(
                "<color=#FF7777>" +
                "Quest orientation unavailable" +
                "</color>");
        }


        if (headMotion.HasQuestGyro)
        {
            Vector3 questGyro = headMotion.CurrentQuestGyro;

            float questInstantMagnitude =
                Mathf.Sqrt(
                    questGyro.x * questGyro.x +
                    questGyro.y * questGyro.y +
                    questGyro.z * questGyro.z
                );

            _leftText.AppendLine();
            _leftText.AppendLine("<b>QUEST MOTION MAGNITUDE</b>");

            _leftText.AppendLine(
                $"|gyro| = √(" +
                $"{FixedNumber(questGyro.x, 3)}² + " +
                $"{FixedNumber(questGyro.y, 3)}² + " +
                $"{FixedNumber(questGyro.z, 3)}²)"
            );

            _leftText.AppendLine(
                $"       = {FixedNumber(questInstantMagnitude, 3)} rad/s"
            );

            _leftText.AppendLine(
            $"Avg = Σ|gyroᵢ| / N,   " +
            $"N = {headMotion.CurrentQuestAverageSampleCount},   " +
            $"Avg = {FixedNumber(headMotion.CurrentQuestGyroMagnitude, 3)} rad/s"
          );

        }

        else
        {
            _leftText.AppendLine(
                "<color=#FF7777>Quest gyro unavailable</color>");
        }

        // IMU
        // --------------------------------------------------------

        _leftText.AppendLine();
        _leftText.AppendLine("<b>IMU SENSOR</b>");

        if (headMotion.HasImuOrientation)
        {
            _leftText.AppendLine(
            $"Yaw: {FixedNumber(headMotion.CurrentImuYaw, 1)}°   " +
            $"Pitch: {FixedNumber(headMotion.CurrentImuPitch, 1)}°   " +
            $"Roll: {FixedNumber(headMotion.CurrentImuRoll, 1)}°"
          );
        }

        else
        {
            _leftText.AppendLine(
                "<color=#FF7777>" +
                "IMU orientation unavailable" +
                "</color>");
        }

        if (headMotion.HasImuGyro)
        {
            Vector3 imuGyro = headMotion.CurrentImuGyro;

            float imuInstantMagnitude =
                Mathf.Sqrt(
                    imuGyro.x * imuGyro.x +
                    imuGyro.y * imuGyro.y +
                    imuGyro.z * imuGyro.z
                );

            _leftText.AppendLine();
            _leftText.AppendLine("<b>IMU MOTION MAGNITUDE</b>");

            _leftText.AppendLine(
                $"|gyro| = √(" +
                $"{FixedNumber(imuGyro.x, 3)}² + " +
                $"{FixedNumber(imuGyro.y, 3)}² + " +
                $"{FixedNumber(imuGyro.z, 3)}²)"
            );

            _leftText.AppendLine(
                $"       = {FixedNumber(imuInstantMagnitude, 3)} rad/s"
            );

            _leftText.AppendLine(
            $"Avg = Σ|gyroᵢ| / N,   " +
            $"N = {headMotion.CurrentImuAverageSampleCount},   " +
            $"Avg = {FixedNumber(headMotion.CurrentImuGyroMagnitude, 3)} rad/s"
        );
        }
        else
        {
            _leftText.AppendLine(
                "<color=#FF7777>IMU gyro unavailable</color>");
        }

        _leftText.AppendLine();
        _leftText.AppendLine(
                $"Avg Gyro, all the samples from last {FixedNumber(headMotion.gyroAverageWindowSeconds, 2)} seconds"
            );

        _leftText.AppendLine();
        _leftText.AppendLine("<b>PACKET INFORMATION</b>");

        AddLeftValue(
            "IMU packet age: Current time - time last IMU packet arrived",
            headMotion.CurrentImuGyroAge,
            " s",
            3,
            8);

        _leftText.Append("IMU packet fresh : ");
        _leftText.AppendLine(
            YesNo(headMotion.CurrentImuGyroFresh));

        AddLeftValue(
            "Fresh timeout",
            headMotion.imuGyroTimeoutSeconds,
            " s",
            1,
            8);
    }

    // FRONT PANEL
    // ============================================================

    private void AddFrontConsole()
    {
        _frontText.AppendLine(
            "<b>========== PARITY CALCULATION ==========</b>");

        // 1. MOTION EVIDENCE
        // ========================================================

        _frontText.AppendLine(
            "<b>1. MOVEMENT EVIDENCE</b>");


        _frontText.Append("Quest active: ");

        _frontText.Append(
            FixedNumber(
                headMotion.CurrentQuestGyroMagnitude,
                3,
                8));

        _frontText.Append(" >= ");

        _frontText.Append(
            FixedNumber(
                headMotion.gyroQuestActiveRadPerSec,
                3,
                8));

        _frontText.Append(" -> ");

        _frontText.AppendLine(
            TrueFalse(headMotion.CurrentQuestActive));


        _frontText.Append("IMU active  : ");

        _frontText.Append(
            FixedNumber(
                headMotion.CurrentImuGyroMagnitude,
                3,
                8));

        _frontText.Append(" >= ");

        _frontText.Append(
            FixedNumber(
                headMotion.gyroImuActiveRadPerSec,
                3,
                8));

        _frontText.Append(" -> ");

        _frontText.AppendLine(
            TrueFalse(headMotion.CurrentImuActive));


        bool enoughMotion =
            headMotion.CurrentQuestGyroMagnitude >=
            headMotion.minEvidenceMotion ||

            headMotion.CurrentImuGyroMagnitude >=
            headMotion.minEvidenceMotion;


        _frontText.AppendLine(
            "Enough motion formula:");

        _frontText.AppendLine(
            "Quest Avg >= minimum OR IMU Avg >= minimum");

        _frontText.Append("Minimum = ");

        _frontText.Append(
            FixedNumber(
                headMotion.minEvidenceMotion,
                3,
                8));

        _frontText.AppendLine(" rad/s");

        _frontText.Append("Result: ");
        _frontText.AppendLine(
            YesNo(enoughMotion));


        // 4. CORRELATION
        // ========================================================

        _frontText.AppendLine();
        _frontText.AppendLine(
            "<b>2. GYRO CORRELATION: Pearson Correlation Coefficient (PCC)</b>");

        _frontText.AppendLine(
            "PCC measures strength and direction of a linear relationship of two variables");

        _frontText.AppendLine(
            "Each axis uses Pearson correlation");

        _frontText.AppendLine();
        _frontText.AppendLine(
            "r = Sum((Q-Qavg)(I-Iavg))");

        _frontText.AppendLine(
            "    / sqrt(Sum(Q-Qavg)^2 * Sum(I-Iavg)^2)");

        AddFrontValue(
            "Correlation X",
            headMotion.CurrentGyroScoreX,
            "",
            3,
            8);

        AddFrontValue(
            "Correlation Y",
            headMotion.CurrentGyroScoreY,
            "",
            3,
            8);

        AddFrontValue(
            "Correlation Z",
            headMotion.CurrentGyroScoreZ,
            "",
            3,
            8);


        _frontText.AppendLine();
        _frontText.AppendLine(
            "Final score = (|X| + |Y| + |Z|) / 3");


        float calculatedScore =
            (
                Mathf.Abs(headMotion.CurrentGyroScoreX) +
                Mathf.Abs(headMotion.CurrentGyroScoreY) +
                Mathf.Abs(headMotion.CurrentGyroScoreZ)
            ) / 3f;


        _frontText.Append("            = (|");

        _frontText.Append(
            FixedNumber(
                headMotion.CurrentGyroScoreX,
                3,
                7));

        _frontText.Append("| + |");

        _frontText.Append(
            FixedNumber(
                headMotion.CurrentGyroScoreY,
                3,
                7));

        _frontText.Append("| + |");

        _frontText.Append(
            FixedNumber(
                headMotion.CurrentGyroScoreZ,
                3,
                7));

        _frontText.AppendLine("|) / 3");


        AddFrontValue(
            "Calculated score",
            calculatedScore,
            "",
            3,
            8);

        _frontText.AppendLine();

        AddFrontValue(
            "Build threshold",
            headMotion.gyroBuildThreshold,
            "",
            2,
            8);

        AddFrontValue(
            "Lose threshold",
            headMotion.gyroLoseThreshold,
            "",
            2,
            8);

        _frontText.Append("Score classification: ");
        _frontText.AppendLine(
            GetGyroScoreClassification());

    }

    // RIGHT PANEL
    // GATES + FINAL DECISION
    // ============================================================

    private void AddRightConsole()
    {
        _rightText.AppendLine(
            "<b>========== DECISION GATES ==========</b>");
        // 1. MOUNTING
        // ========================================================

        _rightText.AppendLine();
        _rightText.AppendLine(
            "<b>1. MOUNTING ORIENTATION</b>");

        bool pitchPass =
            headMotion.CurrentPitchOffsetError <=
            headMotion.attachmentPitchToleranceDeg;

        bool rollPass =
            headMotion.CurrentRollOffsetError <=
            headMotion.attachmentRollToleranceDeg;

        _rightText.Append("Pitch: ");

        _rightText.Append(
            FixedNumber(
                headMotion.CurrentPitchOffsetError,
                1,
                7));

        _rightText.Append(" <= ");

        _rightText.Append(
            FixedNumber(
                headMotion.attachmentPitchToleranceDeg,
                1,
                7));

        _rightText.Append(" deg -> ");

        _rightText.AppendLine(
            PassFail(pitchPass));


        _rightText.Append("Roll : ");

        _rightText.Append(
            FixedNumber(
                headMotion.CurrentRollOffsetError,
                1,
                7));

        _rightText.Append(" <= ");

        _rightText.Append(
            FixedNumber(
                headMotion.attachmentRollToleranceDeg,
                1,
                7));

        _rightText.Append(" deg -> ");

        _rightText.AppendLine(
            PassFail(rollPass));

        if (!headMotion.HasTrustedOrientationOffset)
        {
            _rightText.AppendLine(
                "Mount valid: <color=yellow>NOT CALIBRATED</color>");
        }
        else
        {
            _rightText.Append("Mount valid: ");
            _rightText.AppendLine(
                headMotion.CurrentMountingOrientationValid
                    ? "<color=green>YES</color>"
                    : "<color=red>NO</color>");
        }

        // 2. REATTACHMENT EVIDENCE
        // ========================================================

        _rightText.AppendLine();
        _rightText.AppendLine(
            "<b>2. ATTACHMENT EVIDENCE</b>");

        bool motionDifferenceValid =
            headMotion.CurrentMotionDifference <=
            headMotion.maxMotionDifference;

        bool attachmentEvidenceValid =
            headMotion.CurrentMountingOrientationValid &&
            motionDifferenceValid;

        _rightText.Append("Mount valid AND motion valid");
        _rightText.AppendLine();

        _rightText.Append(
            BoolWord(
                headMotion.CurrentMountingOrientationValid));

        _rightText.Append(" AND ");

        _rightText.Append(
            BoolWord(motionDifferenceValid));

        _rightText.Append(" = ");

        _rightText.AppendLine(
            PassFail(attachmentEvidenceValid));

        AddRightValue(
            "Required timer to consider attachment",
            headMotion.attachmentConfirmSeconds,
            " s",
            2,
            8);

        AddRightValue(
            "Confirm timer",
            headMotion.CurrentOrientationValidTimer,
            " s",
            2,
            8);

        // 3. DETACHMENT
        // ========================================================

        _rightText.AppendLine();
        _rightText.AppendLine(
            "<b>3. DETACHMENT CHECK</b>");

        bool questMoving =
        headMotion.CurrentQuestGyroMagnitude >=
        headMotion.gyroQuestActiveRadPerSec;

        bool imuMoving =
            headMotion.CurrentImuGyroMagnitude >=
            headMotion.gyroImuActiveRadPerSec;

        bool oneSidedMotion =
            questMoving != imuMoving;

        bool detachDifferenceCondition =
            headMotion.CurrentMotionDifference >=
            headMotion.detachedDifferenceThreshold;

        _rightText.Append("Quest moving      : ");
        _rightText.AppendLine(
            TrueFalse(questMoving));

        _rightText.Append("IMU moving        : ");
        _rightText.AppendLine(
            TrueFalse(imuMoving));

        _rightText.Append("One-sided motion  : ");
        _rightText.AppendLine(
            TrueFalse(oneSidedMotion));

        _rightText.Append("Difference large  : ");
        _rightText.AppendLine(
            TrueFalse(detachDifferenceCondition));

        _rightText.AppendLine();
        _rightText.AppendLine("One-Sided Motion Formula: QuestMoving != IMUMoving");
        _rightText.Append(
            BoolWord(questMoving));

        _rightText.Append(" != ");

        _rightText.Append(
            BoolWord(imuMoving));

        _rightText.Append(" = ");

        _rightText.AppendLine(
            BoolWord(oneSidedMotion));

        _rightText.AppendLine();
        _rightText.AppendLine(
            "Detached = OneSidedMotion AND DifferenceLarge");

        _rightText.Append(
            BoolWord(oneSidedMotion));

        _rightText.Append(" AND ");

        _rightText.Append(
            BoolWord(detachDifferenceCondition));

        _rightText.Append(" = ");

        _rightText.AppendLine(
            headMotion.CurrentDetachedMotion
                ? "<color=#FF5555><b>DETACHED</b></color>"
                : "<color=#55FF77><b>NOT DETACHED</b></color>");

        AddRightValue(
            "Difference limit",
            headMotion.detachedDifferenceThreshold,
            " rad/s",
            3,
            8);

        _rightText.Append("Detachment lockout: ");

        _rightText.AppendLine(
            headMotion.DetachmentLockout
                ? "<color=#FF5555>ACTIVE</color>"
                : "<color=#55FF77>OFF</color>");

        _rightText.AppendLine();

        _rightText.AppendLine(
            "Motion Difference Formula:");

        _rightText.Append("| Quest Avg - IMU Avg | = |");

        _rightText.Append(
            FixedNumber(
                headMotion.CurrentQuestGyroMagnitude,
                3,
                8));

        _rightText.Append(" - ");

        _rightText.Append(
            FixedNumber(
                headMotion.CurrentImuGyroMagnitude,
                3,
                8));

        _rightText.Append("| = ");

        _rightText.Append(
            FixedNumber(
                headMotion.CurrentMotionDifference,
                3,
                8));

        _rightText.AppendLine(" rad/s");

        // 4. FINAL BUILD DECISION
        // ========================================================

        _rightText.AppendLine();
        _rightText.AppendLine(
            "<b>4. FINAL PARITY DECISION</b>");


        bool attachmentReady =
            !headMotion.DetachmentLockout ||
            (
                headMotion.CurrentMountingOrientationValid &&
                motionDifferenceValid &&
                headMotion.CurrentOrientationValidTimer >=
                headMotion.attachmentConfirmSeconds
            );

        bool notBuilt =
            !headMotion.ParityBuilt;

        bool confidenceReady =
            headMotion.CurrentConfidence >=
            headMotion.buildThreshold;


        _rightText.Append("Not already built : ");
        _rightText.AppendLine(
            TrueFalse(notBuilt));

        _rightText.Append("Attachment ready  : ");
        _rightText.AppendLine(
            TrueFalse(attachmentReady));

        _rightText.Append("Confidence ready  : ");
        _rightText.AppendLine(
            TrueFalse(confidenceReady));


        _rightText.AppendLine();
        _rightText.AppendLine(
            "BUILD = !ParityBuilt AND AttachmentReady");

        _rightText.AppendLine(
            "        AND Confidence >= BuildThreshold");


        bool buildCondition =
            notBuilt &&
            attachmentReady &&
            confidenceReady;


        _rightText.Append(
            BoolWord(notBuilt));

        _rightText.Append(" AND ");

        _rightText.Append(
            BoolWord(attachmentReady));

        _rightText.Append(" AND ");

        _rightText.Append(
            BoolWord(confidenceReady));

        _rightText.Append(" = ");

        _rightText.AppendLine(
            TrueFalse(buildCondition));

        // LOSS CONDITION
        // ========================================================

        bool lossCondition =
            headMotion.ParityBuilt &&
            headMotion.CurrentConfidence <=
            headMotion.loseThreshold;


        _rightText.AppendLine();
        _rightText.AppendLine(
            "LOSS = ParityBuilt AND Confidence <= LoseThreshold");


        _rightText.Append(
            BoolWord(headMotion.ParityBuilt));

        _rightText.Append(" AND ");

        _rightText.Append(
            BoolWord(
                headMotion.CurrentConfidence <=
                headMotion.loseThreshold));

        _rightText.Append(" = ");

        _rightText.AppendLine(
            TrueFalse(lossCondition));

        // CURRENT RESULT
        // ========================================================

        _rightText.AppendLine();
        _rightText.AppendLine(
            "<b>CURRENT RESULT</b>");

        _rightText.AppendLine(
            GetFinalDecisionText());


        // LAST LOSS REASON
        // ========================================================

        if (!headMotion.ParityBuilt)
        {
            _rightText.AppendLine();
            _rightText.AppendLine(
                "<b>LAST LOSS / STATUS REASON</b>");

            _rightText.AppendLine(
                headMotion.CurrentParityLostReason);

            _rightText.AppendLine();
            _rightText.AppendLine(
                GetCurrentReestablishReason());
        }
    }


    // CONFIDENCE CALCULATION HELPERS
    // ============================================================

    private float GetCurrentConfidenceRate()
    {
        float qMag =
            headMotion.CurrentQuestGyroMagnitude;

        float iMag =
            headMotion.CurrentImuGyroMagnitude;


        // Same too-fast check used by HeadsetMotion
        bool tooFastMotion =
            qMag > 2.5f ||
            iMag > 2.5f;


        if (!headMotion.CurrentImuGyroFresh)
            return 0f;


        if (tooFastMotion)
            return 0f;


        if (headMotion.DetachmentLockout)
            return 0f;


        bool enoughMotion =
            qMag >= headMotion.minEvidenceMotion ||
            iMag >= headMotion.minEvidenceMotion;


        if (!enoughMotion)
        {
            return -headMotion.noMotionLossPerSecond;
        }


        if (headMotion.CurrentGyroScore >=
            headMotion.gyroBuildThreshold)
        {
            return headMotion.gyroGainPerSecond;
        }


        if (headMotion.CurrentGyroScore <
            headMotion.gyroLoseThreshold)
        {
            return -headMotion.gyroLossPerSecond;
        }


        if (!headMotion.ParityBuilt)
        {
            return -headMotion.gyroUncertainLossPerSecond;
        }


        return 0f;
    }


    private float GetNominalConfidenceDelta()
    {
        return
            GetCurrentConfidenceRate() *
            headMotion.gyroScoreEverySeconds;
    }


    private string GetConfidenceActionText()
    {
        float qMag =
            headMotion.CurrentQuestGyroMagnitude;

        float iMag =
            headMotion.CurrentImuGyroMagnitude;


        bool tooFastMotion =
            qMag > 2.5f ||
            iMag > 2.5f;


        if (!headMotion.CurrentImuGyroFresh)
        {
            return
                "<color=#FF5555>" +
                "RESET / LOST - IMU DATA STALE" +
                "</color>";
        }


        if (tooFastMotion)
        {
            return
                "<color=#FFD45A>" +
                "HOLD - MOTION TOO FAST" +
                "</color>";
        }


        if (headMotion.DetachmentLockout)
        {
            return
                "<color=#FF5555>" +
                "BLOCKED - DETACHMENT LOCKOUT" +
                "</color>";
        }


        bool enoughMotion =
            qMag >= headMotion.minEvidenceMotion ||
            iMag >= headMotion.minEvidenceMotion;


        if (!enoughMotion)
        {
            return
                "<color=#FFD45A>" +
                "NO MOTION - MAY DECREASE AFTER GRACE PERIOD" +
                "</color>";
        }


        if (headMotion.CurrentGyroScore >=
            headMotion.gyroBuildThreshold)
        {
            return
                "<color=#55FF77>" +
                "INCREASE CONFIDENCE" +
                "</color>";
        }


        if (headMotion.CurrentGyroScore <
            headMotion.gyroLoseThreshold)
        {
            return
                "<color=#FF7777>" +
                "DECREASE CONFIDENCE" +
                "</color>";
        }


        if (!headMotion.ParityBuilt)
        {
            return
                "<color=#FFD45A>" +
                "UNCERTAIN - DECREASE SLOWLY" +
                "</color>";
        }


        return
            "<color=#FFD45A>" +
            "UNCERTAIN - HOLD CONFIDENCE" +
            "</color>";
    }


    private string GetAlgorithmAction()
    {
        if (!headMotion.CurrentImuGyroFresh)
        {
            return
                "<color=#FF5555>" +
                "IMU packet is stale. Gyro parity cannot be evaluated." +
                "</color>";
        }


        if (headMotion.CurrentDetachedMotion &&
            headMotion.ParityBuilt)
        {
            return
                "<color=#FF5555>" +
                "Possible IMU detachment detected. " +
                "Detachment warning/loss logic is active." +
                "</color>";
        }


        float qMag =
            headMotion.CurrentQuestGyroMagnitude;

        float iMag =
            headMotion.CurrentImuGyroMagnitude;


        if (qMag > 2.5f ||
            iMag > 2.5f)
        {
            return
                "<color=#FFD45A>" +
                "Motion is too fast for reliable scoring. " +
                "Confidence is being held." +
                "</color>";
        }


        if (headMotion.DetachmentLockout)
        {
            return
                "<color=#FF5555>" +
                "Parity rebuild is blocked by detachment lockout." +
                "</color>";
        }

        bool enoughMotion =
            qMag >= headMotion.minEvidenceMotion ||
            iMag >= headMotion.minEvidenceMotion;

        if (!enoughMotion)
        {
            return
                "<color=#FFD45A>" +
                "There is not enough movement for reliable scoring." +
                "</color>";
        }

        if (headMotion.CurrentGyroScore >=
            headMotion.gyroBuildThreshold)
        {
            return
                "<color=#55FF77>" +
                "Gyro score is good. Confidence is increasing." +
                "</color>";
        }

        if (headMotion.CurrentGyroScore <
            headMotion.gyroLoseThreshold)
        {
            return
                "<color=#FF7777>" +
                "Gyro score is low. Confidence is decreasing." +
                "</color>";
        }

        if (!headMotion.ParityBuilt)
        {
            return
                "<color=#FFD45A>" +
                "Gyro score is uncertain. Confidence is decreasing slowly." +
                "</color>";
        }

        return
            "<color=#FFD45A>" +
            "Gyro score is uncertain, but parity is already established. " +
            "Confidence is held." +
            "</color>";
    }

    // STATUS / DECISION HELPERS
    // ============================================================

    private string GetGyroScoreClassification()
    {
        if (headMotion.CurrentGyroScore >=
            headMotion.gyroBuildThreshold)
        {
            return
                "<color=#55FF77>" +
                "<b>GOOD MATCH</b>" +
                "</color>";
        }


        if (headMotion.CurrentGyroScore <
            headMotion.gyroLoseThreshold)
        {
            return
                "<color=#FF7777>" +
                "<b>BAD MATCH</b>" +
                "</color>";
        }


        return
            "<color=#FFD45A>" +
            "<b>UNCERTAIN</b>" +
            "</color>";
    }

    private string GetFinalDecisionText()
    {
        if (headMotion.ParityBuilt)
        {
            return
                "<color=#55FF77>" +
                "<b>PARITY ESTABLISHED</b>" +
                "</color>";
        }


        if (!headMotion.CurrentImuGyroFresh)
        {
            return
                "<color=#FF5555>" +
                "<b>PARITY BLOCKED: IMU DATA STALE</b>" +
                "</color>";
        }


        if (headMotion.DetachmentLockout)
        {
            return
                "<color=#FF5555>" +
                "<b>PARITY BLOCKED: DETACHMENT LOCKOUT</b>" +
                "</color>";
        }


        if (headMotion.CurrentConfidence <
            headMotion.buildThreshold)
        {
            return
                "<color=#FFD45A>" +
                "<b>ESTABLISHING PARITY</b>\n" +
                "Waiting for confidence to reach build threshold." +
                "</color>";
        }


        return
            "<color=#FFD45A>" +
            "<b>WAITING FOR NEXT PARITY EVALUATION</b>" +
            "</color>";
    }

    private string GetParityStatusText()
    {
        if (headMotion.ParityBuilt)
        {
            return
                "<color=#55FF77>" +
                "<b>BUILT / MATCHED</b>" +
                "</color>";
        }


        if (headMotion.ReauthRequired)
        {
            return
                "<color=#FF5555>" +
                "<b>LOST</b>" +
                "</color>";
        }


        return
            "<color=#FFD45A>" +
            "<b>ESTABLISHING</b>" +
            "</color>";
    }

    private string GetCurrentReestablishReason()
    {
        if (headMotion.ParityBuilt)
        {
            return
                "<color=#55FF77>" +
                "Parity is established" +
                "</color>";
        }


        if (!headMotion.CurrentImuGyroFresh)
        {
            return
                "<color=#FF5555>" +
                "IMU data is stale or disconnected" +
                "</color>\n" +
                "Last gyro packet age: " +
                FixedNumber(
                    headMotion.CurrentImuGyroAge,
                    2,
                    7) +
                " s";
        }


        if (headMotion.DetachmentLockout)
        {
            return
                "<color=#FF5555>" +
                "Rebuild blocked by detachment lockout" +
                "</color>\n" +
                "Reattach the IMU in the correct orientation.";
        }


        if (!headMotion.CurrentMountingOrientationValid)
        {
            return
                "<color=#FF7777>" +
                "Mounting orientation is invalid" +
                "</color>";
        }


        if (headMotion.CurrentConfidence <
            headMotion.buildThreshold)
        {
            return
                "<color=#FFD45A>" +
                "Confidence has not reached the build threshold." +
                "</color>";
        }


        return
            "<color=#FFD45A>" +
            "All current conditions look valid. " +
            "Waiting for next parity evaluation." +
            "</color>";
    }

    // FIXED-WIDTH OUTPUT
    // IMPORTANT: THIS PREVENTS VALUES FROM SHAKING
    // ============================================================

    private void AddLeftValue(
        string label,
        float value,
        string unit,
        int decimalPlaces,
        int width = 8)
    {
        _leftText.Append(label);
        _leftText.Append(": ");

        _leftText.Append(
            FixedNumber(
                value,
                decimalPlaces,
                width));

        _leftText.AppendLine(unit);
    }


    private void AddFrontValue(
        string label,
        float value,
        string unit,
        int decimalPlaces,
        int width = 8)
    {
        _frontText.Append(label);
        _frontText.Append(": ");

        _frontText.Append(
            FixedNumber(
                value,
                decimalPlaces,
                width));

        _frontText.AppendLine(unit);
    }

    private void AddRightValue(
        string label,
        float value,
        string unit,
        int decimalPlaces,
        int width = 8)
    {
        _rightText.Append(label);
        _rightText.Append(": ");

        _rightText.Append(
            FixedNumber(
                value,
                decimalPlaces,
                width));

        _rightText.AppendLine(unit);
    }


    private static string FixedNumber(
        float value,
        int decimalPlaces,
        int width = 8)
    {
        string formatted =
            value.ToString($"F{decimalPlaces}");

        return
            $"<mspace=0.6em>{formatted.PadLeft(width)}</mspace>";
    }


    private static string FixedSignedNumber(
        float value,
        int decimalPlaces,
        int width = 8)
    {
        string formatted =
            value >= 0f
                ? "+" + value.ToString($"F{decimalPlaces}")
                : value.ToString($"F{decimalPlaces}");

        return
            $"<mspace=0.6em>{formatted.PadLeft(width)}</mspace>";
    }

    // COLOR HELPERS
    // ============================================================

    private static string YesNo(bool value)
    {
        return value
            ? "<color=#55FF77>YES</color>"
            : "<color=#FF7777>NO</color>";
    }


    private static string TrueFalse(bool value)
    {
        return value
            ? "<color=#55FF77>TRUE</color>"
            : "<color=#FF7777>FALSE</color>";
    }


    private static string BoolWord(bool value)
    {
        return value
            ? "<color=#55FF77>TRUE</color>"
            : "<color=#FF7777>FALSE</color>";
    }


    private static string PassFail(bool value)
    {
        return value
            ? "<color=#55FF77><b>PASS</b></color>"
            : "<color=#FF7777><b>FAIL</b></color>";
    }
}