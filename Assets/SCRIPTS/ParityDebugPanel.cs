using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows live Quest, IMU, parity, attachment, and sensor values
/// exposed by HeadsetMotion.
///
/// Attach this script to the debug Canvas or its background panel.
/// </summary>
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

    private readonly StringBuilder _leftText =
        new StringBuilder(1800);

    private readonly StringBuilder _frontText =
        new StringBuilder(1200);

    private readonly StringBuilder _rightText =
        new StringBuilder(1800);

    private bool IsDebugSceneLoaded()
    {
        Scene debugScene =
            SceneManager.GetSceneByName(debugSceneName);

        return debugScene.IsValid() &&
            debugScene.isLoaded;
    }

    private void Awake()
    {
        FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        RefreshDebugPanel();
    }

    private float _nextFindAttempt;

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

    private void FindReferences()
    {
        if (headMotion == null ||
            !headMotion.isActiveAndEnabled)
        {
            HeadsetMotion[] candidates =
                FindObjectsByType<HeadsetMotion>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            foreach (HeadsetMotion candidate in candidates)
            {
                if (candidate != null &&
                    candidate.isActiveAndEnabled)
                {
                    headMotion = candidate;
                    break;
                }
            }
        }
    }

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
                "<color=#FF7777>No data</color>";

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

    private void AddLeftConsole()
    {       
        _leftText.Append("Status: ");
        _leftText.AppendLine(GetParityStatusText());

        _leftText.Append("Confidence: ");

        _leftText.Append(
            FixedNumber(
                headMotion.CurrentConfidence,
                2,
                5));

        _leftText.Append(" / ");

        _leftText.Append(
            FixedNumber(
                headMotion.buildThreshold,
                2,
                5));

        _leftText.AppendLine();

        float progress =
            headMotion.buildThreshold > 0f
                ? headMotion.CurrentConfidence /
                headMotion.buildThreshold
                : 0f;

        _leftText.AppendLine(
            $"Build progress: {Mathf.Clamp01(progress) * 100f:F1}%");

        if (headMotion.ParityLostCountdownRunning)
        {
            _leftText.Append(
                "<color=#FF7777>Logout remaining: ");

            _leftText.Append(
                headMotion.ParityLostTimeRemaining()
                    .ToString("F1"));

            _leftText.AppendLine(" s</color>");
        }

        _leftText.AppendLine();
        _leftText.AppendLine("<b>QUEST HEADSET</b>");

        if (headMotion.HasQuestRotation)
        {
            _leftText.Append("Yaw:");
            _leftText.Append(
                FixedNumber(
                    headMotion.CurrentQuestYaw,
                    1,
                    6));
            _leftText.Append("°  ");

            _leftText.Append("Pitch:");
            _leftText.Append(
                FixedNumber(
                    headMotion.CurrentQuestPitch,
                    1,
                    6));
            _leftText.Append("°  ");

            _leftText.Append("Roll:");
            _leftText.Append(
                FixedNumber(
                    headMotion.CurrentQuestRoll,
                    1,
                    6));
            _leftText.AppendLine("°");
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
            Vector3 gyro =
                headMotion.CurrentQuestGyro;

            AddLeftValue("Gyro X", gyro.x, " rad/s", 3);
            AddLeftValue("Gyro Y", gyro.y, " rad/s", 3);
            AddLeftValue("Gyro Z", gyro.z, " rad/s", 3);

            AddLeftValue(
                "Average magnitude",
                headMotion.CurrentQuestGyroMagnitude,
                " rad/s",
                3);
        } 
            
        _leftText.AppendLine();
        _leftText.AppendLine("<b>IMU SENSOR</b>");

        if (headMotion.HasImuOrientation)
        {
            _leftText.Append("Yaw:");
            _leftText.Append(
                FixedNumber(
                    headMotion.CurrentImuYaw,
                    1,
                    6));
            _leftText.Append("°  ");

            _leftText.Append("Pitch:");
            _leftText.Append(
                FixedNumber(
                    headMotion.CurrentImuPitch,
                    1,
                    6));
            _leftText.Append("°  ");

            _leftText.Append("Roll:");
            _leftText.Append(
                FixedNumber(
                    headMotion.CurrentImuRoll,
                    1,
                    6));
            _leftText.AppendLine("°");
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
            Vector3 gyro =
                headMotion.CurrentImuGyro;

            AddLeftValue("Gyro X", gyro.x, " rad/s", 3);
            AddLeftValue("Gyro Y", gyro.y, " rad/s", 3);
            AddLeftValue("Gyro Z", gyro.z, " rad/s", 3);

            AddLeftValue(
                "Average magnitude",
                headMotion.CurrentImuGyroMagnitude,
                " rad/s",
                3);
        }
    }

    private void AddFrontConsole()
    {
        AddFrontValue(
            "Delta yaw",
            headMotion.CurrentDeltaYaw,
            "°",
            1);

        AddFrontValue(
            "Delta pitch",
            headMotion.CurrentDeltaPitch,
            "°",
            1);

        AddFrontValue(
            "Delta roll",
            headMotion.CurrentDeltaRoll,
            "°",
            1);

        _frontText.AppendLine();

        _frontText.Append("Gyro score: ");
        _frontText.AppendLine(
            ScoreText(headMotion.CurrentGyroScore));

        _frontText.Append("Score X/Y/Z: ");

        _frontText.Append(
            FixedNumber(
                headMotion.CurrentGyroScoreX,
                2,
                6));

        _frontText.Append(" / ");

        _frontText.Append(
            FixedNumber(
                headMotion.CurrentGyroScoreY,
                2,
                6));

        _frontText.Append(" / ");

        _frontText.Append(
            FixedNumber(
                headMotion.CurrentGyroScoreZ,
                2,
                6));

        _frontText.AppendLine();

        AddFrontValue(
            "Motion ratio",
            headMotion.CurrentMotionRatio,
            "",
            3);

        _frontText.Append("Both quiet: ");
        _frontText.AppendLine(
            YesNo(headMotion.CurrentBothQuiet));

        _frontText.AppendLine();

        _frontText.Append("Quest active: ");
        _frontText.AppendLine(
            YesNo(headMotion.CurrentQuestActive));

        _frontText.Append("IMU active: ");
        _frontText.AppendLine(
            YesNo(headMotion.CurrentImuActive));
    }

    private void AddRightConsole()
    {  
        _rightText.Append("Mount orientation valid: ");
        _rightText.AppendLine(
            YesNo(
                headMotion.CurrentMountingOrientationValid));

        AddRightValue(
            "Pitch offset error",
            headMotion.CurrentPitchOffsetError,
            "°",
            1);

        AddRightValue(
            "Roll offset error",
            headMotion.CurrentRollOffsetError,
            "°",
            1);

        _rightText.AppendLine();

        _rightText.Append("Detached motion: ");

        if (headMotion.CurrentDetachedMotion)
        {
            _rightText.AppendLine(
                "<color=#FF5555>DETECTED</color>");
        }
        else
        {
            _rightText.AppendLine(
                "<color=#55FF77>NO</color>");
        }

        AddRightValue(
            "Detach timer",
            headMotion.CurrentDetachTimer,
            " s",
            2);

        _rightText.Append("Detachment lockout: ");

        if (headMotion.DetachmentLockout)
        {
            _rightText.AppendLine(
                "<color=#FF5555>ACTIVE</color>");
        }
        else
        {
            _rightText.AppendLine(
                "<color=#55FF77>OFF</color>");
        }

        _rightText.AppendLine();

        _rightText.AppendLine(
            "<b>SENSOR CONNECTION</b>");

        _rightText.Append("IMU gyro packet age: ");

        _rightText.Append(
            FixedNumber(
                headMotion.CurrentImuGyroAge,
                2,
                6));

        _rightText.AppendLine(" s");

        _rightText.Append("IMU gyro fresh: ");
        _rightText.AppendLine(
            YesNo(headMotion.CurrentImuGyroFresh));

        // Only show these when parity is lost
        if (!headMotion.ParityBuilt)
        {
            _rightText.AppendLine();
            _rightText.AppendLine(
                "<b>LAST LOSS REASON</b>");

            _rightText.AppendLine(
                headMotion.CurrentParityLostReason);

            _rightText.AppendLine();
            _rightText.AppendLine(
                "<b>CURRENT STATUS</b>");

            _rightText.AppendLine(
                GetCurrentReestablishReason());
        }
        else
        {
            _rightText.AppendLine();
            _rightText.AppendLine(
                "<b>CURRENT STATUS</b>");

            _rightText.AppendLine(
                "<color=#55FF77>Parity is established</color>");
        }
    }
    
    // HELPERS
    // =====================================

    private void AddLeftValue(
    string label,
    float value,
    string unit,
    int decimalPlaces)
    {
        _leftText.Append(label);
        _leftText.Append(": ");
        _leftText.Append(
            FixedNumber(value, decimalPlaces));
        _leftText.AppendLine(unit);
    }

    private void AddFrontValue(
        string label,
        float value,
        string unit,
        int decimalPlaces)
    {
        _frontText.Append(label);
        _frontText.Append(": ");
        _frontText.Append(
            FixedNumber(value, decimalPlaces));
        _frontText.AppendLine(unit);
    }

    private void AddRightValue(
        string label,
        float value,
        string unit,
        int decimalPlaces)
    {
        _rightText.Append(label);
        _rightText.Append(": ");
        _rightText.Append(
            FixedNumber(value, decimalPlaces));
        _rightText.AppendLine(unit);
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

    private string ScoreText(float score)
    {
        string value =
            FixedNumber(score, 3, 7);

        if (score >= headMotion.gyroBuildThreshold)
        {
            return
                $"<color=#55FF77>{value}</color>";
        }

        if (score < headMotion.gyroLoseThreshold)
        {
            return
                $"<color=#FF7777>{value}</color>";
        }

        return
            $"<color=#FFD45A>{value}</color>";
    }

    private static string YesNo(bool value)
    {
        return value
            ? "<color=#55FF77>YES</color>"
            : "<color=#FF7777>NO</color>";
    }

    private static string AgeText(float age)
    {
        if (age < 0f)
        {
            return
                "<color=#FF7777>NO PACKET</color>";
        }

        if (age <= 0.75f)
        {
            return
                $"<color=#55FF77>{age:F3} s</color>";
        }

        return
            $"<color=#FF7777>{age:F3} s</color>";
    }

    private string GetCurrentReestablishReason()
    {
        if (headMotion.ParityBuilt)
            return "<color=#55FF77>Parity is established</color>";

        if (!headMotion.CurrentImuGyroFresh)
        {
            return
                "<color=#FF5555>IMU data is stale or disconnected</color>\n" +
                $"Last gyro packet age: {headMotion.CurrentImuGyroAge:F2} s";
        }

        if (headMotion.DetachmentLockout)
        {
            return
                "<color=#FF5555>Rebuild blocked by detachment lockout</color>\n" +
                "Reattach the IMU in the correct orientation.";
        }

        if (!headMotion.CurrentMountingOrientationValid)
        {
            return
                "<color=#FF7777>Mounting orientation is invalid</color>\n" +
                $"Pitch error: {headMotion.CurrentPitchOffsetError:F1}°\n" +
                $"Roll error: {headMotion.CurrentRollOffsetError:F1}°";
        }

        if (headMotion.CurrentConfidence <
            headMotion.buildThreshold)
        {
            return
                "<color=#FFD45A>Confidence is not high enough</color>\n" +
                $"Confidence: {headMotion.CurrentConfidence:F2}" +
                $" / {headMotion.buildThreshold:F2}";
        }

        if (headMotion.CurrentGyroScore <
            headMotion.gyroBuildThreshold)
        {
            return
                "<color=#FFD45A>Gyro movements are not matching</color>\n" +
                $"Gyro score: {headMotion.CurrentGyroScore:F3}" +
                $" / {headMotion.gyroBuildThreshold:F3}";
        }

        if (headMotion.CurrentBothQuiet)
        {
            return
                "<color=#FFD45A>Waiting for matching movement</color>\n" +
                "Move the headset normally.";
        }

        return
            "<color=#FFD45A>All conditions look valid</color>\n" +
            "Waiting for the next parity evaluation.";
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
}