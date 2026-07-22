using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class XRDeviceLogger : MonoBehaviour
{
    public static XRDeviceLogger Instance { get; private set; }

    [Header("Logging")]
    [Tooltip("Enable or disable CSV recording.")]
    public bool enableLogging = true;

    [Header("HeadsetMotion Source")]
    public HeadsetMotion headsetMotion;

    [Tooltip("Samples collected per second. Use 50 to match your IMU frequency.")]
    [Range(1f, 120f)]
    public float sampleRateHz = 50f;

    [Tooltip("When true, recording stops whenever parity is lost.")]
    public bool stopLoggingWhenParityLost = true;

    [Header("Runtime Information")]
    [SerializeField] private bool parityEstablished;
    [SerializeField] private string currentSceneName;
    [SerializeField] private string csvFilePath;
    [SerializeField] private int sampleNumber;

    private StreamWriter writer;
    private float nextSampleTime;
    private string sessionId;

    private InputDevice headset;
    private InputDevice leftController;
    private InputDevice rightController;

    private static readonly CultureInfo CsvCulture =
        CultureInfo.InvariantCulture;

    private void Awake()
    {
        // Prevent duplicate loggers when scenes change.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (headsetMotion == null)
            headsetMotion = FindObjectOfType<HeadsetMotion>();

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        nextSampleTime = Time.unscaledTime;

        FindXRDevices();

        if (enableLogging)
            OpenCsvFile();
    }

    private void Update()
    {
        if (!enableLogging)
            return;

        // Recording begins only after parity has been established.
        if (!parityEstablished)
            return;

        if (sampleRateHz <= 0f)
            return;

        float sampleInterval = 1f / sampleRateHz;

        if (Time.unscaledTime < nextSampleTime)
            return;

        /*
         * Increment by the sample interval instead of setting it directly
         * to Time.unscaledTime. This produces a steadier sampling schedule.
         */
        nextSampleTime += sampleInterval;

        // Prevent a large backlog after a pause or frame freeze.
        if (nextSampleTime < Time.unscaledTime - sampleInterval)
            nextSampleTime = Time.unscaledTime + sampleInterval;

        RefreshDevicesIfNeeded();
        RecordSample();
    }

    /// <summary>
    /// Call this from your parity script whenever parity is built or lost.
    /// </summary>
    public void SetParityEstablished(bool established)
    {
        bool previousState = parityEstablished;
        parityEstablished = established;

        if (established && !previousState)
        {
            // Start sampling immediately when parity becomes established.
            nextSampleTime = Time.unscaledTime;

            WriteEventRow("PARITY_BUILT");
        }
        else if (!established && previousState)
        {
            WriteEventRow("PARITY_LOST");

            if (!stopLoggingWhenParityLost)
            {
                // Continue logging even though the parity column becomes 0.
                nextSampleTime = Time.unscaledTime;
            }
        }
    }

    /// <summary>
    /// Optional method for manually adding events such as button presses,
    /// teleportation, authentication, trial start, or trial end.
    /// </summary>
    public void LogEvent(string eventName)
    {
        if (!enableLogging)
            return;

        WriteEventRow(eventName);
    }

    private void RecordSample()
    {
        if (writer == null)
            OpenCsvFile();

        if (writer == null)
            return;

        sampleNumber++;

        DeviceData hmdData = ReadDeviceData(headset);

        if (headsetMotion != null)
        {
            if (headsetMotion.HasQuestRotation)
            {
                hmdData.rotation =
                    headsetMotion.CurrentQuestRotation;

                hmdData.eulerAngles = new Vector3(
                    headsetMotion.CurrentQuestPitch,
                    headsetMotion.CurrentQuestYaw,
                    headsetMotion.CurrentQuestRoll
                );
            }

            if (headsetMotion.HasQuestGyro)
            {
                hmdData.angularVelocity =
                    headsetMotion.CurrentQuestGyro;
            }
        }
        DeviceData leftData = ReadDeviceData(leftController);
        DeviceData rightData = ReadDeviceData(rightController);

        StringBuilder row = new StringBuilder(2048);

        // Session and timing information
        Add(row, sessionId);
        //Add(row, sessionLabel);
        Add(row, sampleNumber);
        //Add(row, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        Add(row, Time.realtimeSinceStartupAsDouble);
        //Add(row, Time.unscaledTimeAsDouble);
        Add(row, Time.frameCount);
        Add(row, currentSceneName);
        Add(row, parityEstablished ? 1 : 0);
        Add(row, "SAMPLE");

        // Headset
        AddDeviceColumns(row, hmdData);

        // Left controller
        AddControllerColumns(row, leftData, leftController);

        // Right controller
        AddControllerColumns(row, rightData, rightController);

        RemoveFinalComma(row);
        writer.WriteLine(row.ToString());

        /*
         * Flushing every row is safer if the headset application closes
         * unexpectedly. For very long sessions, you can flush less often.
         */
        writer.Flush();
    }

    private void WriteEventRow(string eventName)
    {
        if (!enableLogging)
            return;

        if (writer == null)
            OpenCsvFile();

        if (writer == null)
            return;

        StringBuilder row = new StringBuilder(2048);

        Add(row, sessionId);
        //Add(row, sessionLabel);
        Add(row, sampleNumber);
        //Add(row, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        Add(row, Time.realtimeSinceStartupAsDouble);
        //Add(row, Time.unscaledTimeAsDouble);
        Add(row, Time.frameCount);
        Add(row, currentSceneName);
        Add(row, parityEstablished ? 1 : 0);
        Add(row, eventName);

        /*
         * Keep event rows the same length as sample rows.
         * The remaining device columns are left empty.
         */
        int deviceColumnCount = GetDeviceColumnCount();
        int controllerExtraColumnCount = GetControllerExtraColumnCount();

        int emptyColumns =
            deviceColumnCount +                          // Headset
            deviceColumnCount + controllerExtraColumnCount + // Left
            deviceColumnCount + controllerExtraColumnCount;  // Right

        for (int i = 0; i < emptyColumns; i++)
            Add(row, string.Empty);

        RemoveFinalComma(row);
        writer.WriteLine(row.ToString());
        writer.Flush();
    }

    private void FindXRDevices()
    {
        headset = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    private void RefreshDevicesIfNeeded()
    {
        if (!headset.isValid)
            headset = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);

        if (!leftController.isValid)
            leftController =
                InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (!rightController.isValid)
            rightController =
                InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private DeviceData ReadDeviceData(InputDevice device)
    {
        DeviceData data = new DeviceData
        {
            //deviceValid = device.isValid
        };

        if (!device.isValid)
            return data;

        device.TryGetFeatureValue(
            CommonUsages.isTracked,
            out data.isTracked);

        // device.TryGetFeatureValue(
        //     CommonUsages.trackingState,
        //     out data.trackingState);

        device.TryGetFeatureValue(
            CommonUsages.devicePosition,
            out data.position);

        device.TryGetFeatureValue(
            CommonUsages.deviceRotation,
            out data.rotation);

        device.TryGetFeatureValue(
            CommonUsages.deviceVelocity,
            out data.velocity);

        device.TryGetFeatureValue(
            CommonUsages.deviceAngularVelocity,
            out data.angularVelocity);

        device.TryGetFeatureValue(
            CommonUsages.deviceAcceleration,
            out data.acceleration);

        device.TryGetFeatureValue(
            CommonUsages.deviceAngularAcceleration,
            out data.angularAcceleration);

        Vector3 eul = data.rotation.eulerAngles;

            data.eulerAngles = new Vector3(
                NormalizeAngle(eul.x),
                NormalizeAngle(eul.y),
                NormalizeAngle(eul.z)
            );

        return data;
    }

    private void AddDeviceColumns(
        StringBuilder row,
        DeviceData data)
    {
        //Add(row, data.deviceValid ? 1 : 0);
        Add(row, data.isTracked ? 1 : 0);
        //Add(row, (int)data.trackingState);

        // Position in meters
        AddVector3(row, data.position);

        // Quaternion rotation
        AddQuaternion(row, data.rotation);

        // Euler rotation in degrees
        AddVector3(row, data.eulerAngles);

        // Linear velocity in meters per second
        AddVector3(row, data.velocity);

        // Angular velocity, normally radians per second
        AddVector3(row, data.angularVelocity);

        // Linear acceleration
        AddVector3(row, data.acceleration);

        // Angular acceleration
        AddVector3(row, data.angularAcceleration);
    }

    private void AddControllerColumns(
        StringBuilder row,
        DeviceData deviceData,
        InputDevice controller)
    {
        AddDeviceColumns(row, deviceData);

        bool primaryButton = false;
        bool secondaryButton = false;
        bool menuButton = false;
        bool primaryTouch = false;
        bool secondaryTouch = false;
        bool triggerButton = false;
        bool gripButton = false;
        bool thumbstickClick = false;
        bool thumbstickTouch = false;

        float trigger = 0f;
        float grip = 0f;
        Vector2 thumbstick = Vector2.zero;
        float batteryLevel = -1f;

        if (controller.isValid)
        {
            controller.TryGetFeatureValue(
                CommonUsages.primaryButton,
                out primaryButton);

            controller.TryGetFeatureValue(
                CommonUsages.secondaryButton,
                out secondaryButton);

            controller.TryGetFeatureValue(
                CommonUsages.menuButton,
                out menuButton);

            controller.TryGetFeatureValue(
                CommonUsages.primaryTouch,
                out primaryTouch);

            controller.TryGetFeatureValue(
                CommonUsages.secondaryTouch,
                out secondaryTouch);

            controller.TryGetFeatureValue(
                CommonUsages.triggerButton,
                out triggerButton);

            controller.TryGetFeatureValue(
                CommonUsages.gripButton,
                out gripButton);

            controller.TryGetFeatureValue(
                CommonUsages.primary2DAxisClick,
                out thumbstickClick);

            controller.TryGetFeatureValue(
                CommonUsages.primary2DAxisTouch,
                out thumbstickTouch);

            controller.TryGetFeatureValue(
                CommonUsages.trigger,
                out trigger);

            controller.TryGetFeatureValue(
                CommonUsages.grip,
                out grip);

            controller.TryGetFeatureValue(
                CommonUsages.primary2DAxis,
                out thumbstick);

            controller.TryGetFeatureValue(
                CommonUsages.batteryLevel,
                out batteryLevel);
        }

        Add(row, primaryButton ? 1 : 0);
        Add(row, secondaryButton ? 1 : 0);
        Add(row, menuButton ? 1 : 0);
        Add(row, primaryTouch ? 1 : 0);
        Add(row, secondaryTouch ? 1 : 0);
        Add(row, triggerButton ? 1 : 0);
        Add(row, gripButton ? 1 : 0);
        Add(row, thumbstickClick ? 1 : 0);
        Add(row, thumbstickTouch ? 1 : 0);

        Add(row, trigger);
        Add(row, grip);
        Add(row, thumbstick.x);
        Add(row, thumbstick.y);
        Add(row, batteryLevel);
    }

    private void OpenCsvFile()
    {
        try
        {
            string folder =
                Path.Combine(
                    Application.persistentDataPath,
                    "XRDataLogs");

            Directory.CreateDirectory(folder);

            //string safeLabel = MakeFileNameSafe(sessionLabel);

            csvFilePath = Path.Combine(
                folder,
                $"{sessionId}.csv");

            bool fileAlreadyExists = File.Exists(csvFilePath);

            writer = new StreamWriter(
                csvFilePath,
                append: true,
                Encoding.UTF8);

            if (!fileAlreadyExists)
            {
                writer.WriteLine(BuildHeader());
                writer.Flush();
            }

            Debug.Log(
                $"XR CSV logging file:\n{csvFilePath}");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Could not open XR CSV file: {exception.Message}");

            writer = null;
        }
    }

    private string BuildHeader()
    {
        StringBuilder header = new StringBuilder(4096);

        Add(header, "SessionID");
        Add(header, "SessionLabel");
        Add(header, "SampleNumber");
        //Add(header, "LocalDateTime");
        Add(header, "RealtimeSeconds");
        //Add(header, "UnityUnscaledTime");
        Add(header, "FrameNumber");
        Add(header, "SceneName");
        Add(header, "ParityEstablished");
        //Add(header, "EventType");

        AddDeviceHeader(header, "HMD");
        AddControllerHeader(header, "Left");
        AddControllerHeader(header, "Right");

        RemoveFinalComma(header);
        return header.ToString();
    }

    private void AddDeviceHeader(
        StringBuilder header,
        string prefix)
    {
        //Add(header, $"{prefix}_DeviceValid");
        Add(header, $"{prefix}_IsTracked");
       // Add(header, $"{prefix}_TrackingState");

        Add(header, $"{prefix}_PosX");
        Add(header, $"{prefix}_PosY");
        Add(header, $"{prefix}_PosZ");

        Add(header, $"{prefix}_RotQX");
        Add(header, $"{prefix}_RotQY");
        Add(header, $"{prefix}_RotQZ");
        Add(header, $"{prefix}_RotQW");

        Add(header, $"{prefix}_Pitch");
        Add(header, $"{prefix}_Yaw");
        Add(header, $"{prefix}_Roll");

        Add(header, $"{prefix}_VelX");
        Add(header, $"{prefix}_VelY");
        Add(header, $"{prefix}_VelZ");

        Add(header, $"{prefix}_AngVelX");
        Add(header, $"{prefix}_AngVelY");
        Add(header, $"{prefix}_AngVelZ");

        Add(header, $"{prefix}_AccelX");
        Add(header, $"{prefix}_AccelY");
        Add(header, $"{prefix}_AccelZ");

        Add(header, $"{prefix}_AngAccelX");
        Add(header, $"{prefix}_AngAccelY");
        Add(header, $"{prefix}_AngAccelZ");
    }

    private void AddControllerHeader(
        StringBuilder header,
        string prefix)
    {
        AddDeviceHeader(header, prefix);

        Add(header, $"{prefix}_PrimaryButton");
        Add(header, $"{prefix}_SecondaryButton");
        Add(header, $"{prefix}_MenuButton");
        Add(header, $"{prefix}_PrimaryTouch");
        Add(header, $"{prefix}_SecondaryTouch");
        Add(header, $"{prefix}_TriggerButton");
        Add(header, $"{prefix}_GripButton");
        Add(header, $"{prefix}_ThumbstickClick");
        Add(header, $"{prefix}_ThumbstickTouch");

        Add(header, $"{prefix}_TriggerValue");
        Add(header, $"{prefix}_GripValue");
        Add(header, $"{prefix}_ThumbstickX");
        Add(header, $"{prefix}_ThumbstickY");
        Add(header, $"{prefix}_BatteryLevel");
    }

    private void OnActiveSceneChanged(
        Scene previousScene,
        Scene newScene)
    {
        currentSceneName = newScene.name;

        // Devices can temporarily become invalid during scene changes.
        FindXRDevices();

        if (enableLogging && parityEstablished)
            WriteEventRow($"SCENE_ENTER_{newScene.name}");
    }

    private static string MakeFileNameSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "XRSession";

        foreach (char invalidCharacter
                 in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(
                invalidCharacter,
                '_');
        }

        return value;
    }

    private static void AddVector3(
        StringBuilder row,
        Vector3 value)
    {
        Add(row, value.x);
        Add(row, value.y);
        Add(row, value.z);
    }

    private static void AddQuaternion(
        StringBuilder row,
        Quaternion value)
    {
        Add(row, value.x);
        Add(row, value.y);
        Add(row, value.z);
        Add(row, value.w);
    }

    private static void Add(
        StringBuilder row,
        string value)
    {
        if (value == null)
            value = string.Empty;

        bool requiresQuotes =
            value.Contains(",") ||
            value.Contains("\"") ||
            value.Contains("\n");

        if (requiresQuotes)
        {
            value = "\"" +
                    value.Replace("\"", "\"\"") +
                    "\"";
        }

        row.Append(value);
        row.Append(',');
    }

    private static void Add(
        StringBuilder row,
        int value)
    {
        row.Append(value);
        row.Append(',');
    }

    private static void Add(
        StringBuilder row,
        float value)
    {
        row.Append(value.ToString("F6", CsvCulture));
        row.Append(',');
    }

    private static void Add(
        StringBuilder row,
        double value)
    {
        row.Append(value.ToString("F6", CsvCulture));
        row.Append(',');
    }

    private static void RemoveFinalComma(StringBuilder row)
    {
        if (row.Length > 0 &&
            row[row.Length - 1] == ',')
        {
            row.Length--;
        }
    }

    private static int GetDeviceColumnCount()
    {
        /*
         * Valid + tracked + tracking state = 3
         * Position = 3
         * Quaternion = 4
         * Euler angles = 3
         * Velocity = 3
         * Angular velocity = 3
         * Acceleration = 3
         * Angular acceleration = 3
         */
        return 25;
    }

    private static int GetControllerExtraColumnCount()
    {
        // Nine digital values and five analog values.
        return 14;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            writer?.Flush();
    }

    private void OnApplicationQuit()
    {
        CloseWriter();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        CloseWriter();
        Instance = null;
    }

    private void CloseWriter()
    {
        if (writer == null)
            return;

        writer.Flush();
        writer.Close();
        writer.Dispose();
        writer = null;
    }

    private struct DeviceData
    {
        //public bool deviceValid;
        public bool isTracked;
        //public InputTrackingState trackingState;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 eulerAngles;

        public Vector3 velocity;
        public Vector3 angularVelocity;
        public Vector3 acceleration;
        public Vector3 angularAcceleration;
    }
}