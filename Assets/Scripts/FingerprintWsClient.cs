using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using UnityEngine;
using NativeWebSocket;

public class FingerprintWsClient : MonoBehaviour
{
    [Header("Debug")]
    public bool devEchoToUI = false;  // TEMP: echo every non-JSON line to the UI

    // ---------- Singleton ----------
    public static FingerprintWsClient I { get; private set; }

    // ---------- Inspector ----------
    [Header("ESP32 Device")]
    public string deviceIp = "192.168.43.57";
    public int port = 83;
    [Tooltip("Keep '/' for root. Use '/ws' only if your sketch serves WS at that path.")]
    public string wsPath = "/";

    [Header("IMU (motion)")]
    public bool enableImu = true;

    // IMPORTANT CHANGE:
    // We keep these fields so your Inspector doesn't break,
    // but we will NOT create a second WebSocket anymore.
    // IMU data is expected to come over the SAME ws connection as Fingerprint.
    [Tooltip("IGNORED now. IMU shares the same WS as fingerprint.")]
    public int imuPort = 82;        // kept for inspector compatibility (ignored)
    public string imuWsPath = "/";  // kept for inspector compatibility (ignored)

    [Header("Connection")]
    public bool autoConnectOnStart = true;
    public float connectTimeoutSec = 4f;

    // ---------- Events ----------
    public event Action<string> OnDeviceMessage;             // raw device text (JSON or plain)
    public event Action<int, string> OnEnrollSample;         // parsed progress: (step 0..6, pretty text)
    public event Action<UserProfile[]> OnUsersList;          // fired when device returns the registered users list

    public event Action<string> OnImuRawMessage;             // optional
    public event Action<float, float, float> OnImuYpr;       // pitch, roll, yaw
    public event Action<float, float, float> OnImuGyro;

    [Serializable]
    public class ImuJson { public float pitch; public float roll; public float yaw; }

    // ---------- Internals ----------
    WebSocket ws;        // fingerprint WS (83) - also receives mirrored IMU data
    WebSocket wsImu;     // IMU command/ACK WS (82)
    bool isConnecting;
    bool isImuConnecting;

    int enrollStep = 0; // 0..6, advanced only when we detect a "sample saved" line

    // ========= Unity lifecycle =========
    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        // Keep your fix for DontDestroyOnLoad warning:
        DontDestroyOnLoad(transform.root.gameObject);
    }

    async void Start()
    {
        await Task.Yield();

        if (!autoConnectOnStart)
            return;

        try
        {
            Debug.Log($"[WS] Target = ws://{deviceIp}:{port}{wsPath}");
            bool ok = await EnsureConnectedAsync();
            if (!ok)
            {
                Debug.LogWarning("[WS] Connection failed on Start()");
                return;
            }
            Debug.Log("[WS] Connected.");

            // IMU data is mirrored onto WS83 as "IMU:..."
            // IMU commands + ACK (ZERO/HOLD) use WS82 (wsIMU).

            if (enableImu)
            {
                string imuPath = string.IsNullOrEmpty(imuWsPath) ? "/" : imuWsPath;
                if (!imuPath.StartsWith("/")) imuPath = "/" + imuPath;

                bool imuOk = await EnsureImuConnectedAsync();
                Debug.Log("[IMU] Command socket connected=" + imuOk + $" (ws://{deviceIp}:{imuPort}{imuWsPath})");
            }

            else
                Debug.Log("[IMU] enableImu is OFF (ignoring IMU parsing).");
        }
        catch (Exception ex)
        {
            Debug.LogError("[WS] Connect exception: " + ex);
        }
    }

    void Update()
    {
        if (ws != null) ws.DispatchMessageQueue();
        if (wsImu != null) wsImu.DispatchMessageQueue();
    }

    async void OnApplicationQuit()
    {
        await CloseAsync();
    }

    // ========= Public API used by UI =========

    public async Task<bool> EnsureConnectedAsync()
    {
        if (ws != null && ws.State == WebSocketState.Open) return true;

        if (isConnecting)
        {
            float t0 = Time.unscaledTime;
            while (isConnecting && Time.unscaledTime - t0 < connectTimeoutSec)
                await Task.Yield();
            return ws != null && ws.State == WebSocketState.Open;
        }

        return await ConnectAsync();
    }

    // Backwards-compatible wrapper: other scripts might call this.
    // Now it simply ensures the main WS is connected.
    public async Task<bool> EnsureImuConnectedAsync()
    {
        if (!enableImu) return false;
        if (wsImu != null && wsImu.State == WebSocketState.Open) return true;

        if (isImuConnecting)
        {
            float t0 = Time.unscaledTime;
            while (isImuConnecting && Time.unscaledTime - t0 < connectTimeoutSec)
                await Task.Yield();
            return wsImu != null && wsImu.State == WebSocketState.Open;
        }

        return await ConnectImuAsync();
    }

    async Task<bool> ConnectImuAsync()
    {
        isImuConnecting = true;

        string path = string.IsNullOrEmpty(imuWsPath) ? "/" : imuWsPath;
        if (!path.StartsWith("/")) path = "/" + path;
        string url = $"ws://{deviceIp}:{imuPort}{path}";

        if (wsImu != null)
        {
            try { await wsImu.Close(); } catch { }
            wsImu = null;
        }

        Debug.Log("[IMU] Connecting " + url);
        wsImu = new WebSocket(url);

        wsImu.OnOpen += () => Debug.Log("[IMU] Open");
        wsImu.OnError += e => Debug.LogError("[IMU] Error " + e);
        wsImu.OnClose += c => Debug.LogWarning("[IMU] Closed " + c);

        // Only ACK / IMU control replies should arrive here (ACK:ZERO, etc.)
        wsImu.OnMessage += (bytes) =>
        {
            var msg = Encoding.UTF8.GetString(bytes).Trim();
            Debug.Log("[IMU <=] " + msg);

            if (msg.StartsWith("ACK:", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("ERR:", StringComparison.OrdinalIgnoreCase) ||
                msg.Equals("PONG", StringComparison.OrdinalIgnoreCase))
            {
                OnImuRawMessage?.Invoke(msg);
            }
        };

        var _ = wsImu.Connect();
        float start = Time.unscaledTime;
        while (wsImu.State != WebSocketState.Open && Time.unscaledTime - start < connectTimeoutSec)
            await Task.Yield();

        isImuConnecting = false;

        if (wsImu.State == WebSocketState.Open) return true;

        Debug.LogWarning("[IMU] Connect timeout/fail (state=" + wsImu.State + ")");
        return false;
    }

    public async void StartEnroll(string name) => await SendText($"register:{Escape(name)}");
    public async void StartVerify(string name) => await SendText($"verify:{Escape(name)}");

    public async void QueryState() => await SendText("state");
    public async void StartContinuous(string name) => await SendText($"cont_start:{Escape(name)}");
    public async void StopContinuous() => await SendText("cont_stop");
    public async void DeleteUser(string name) => await SendText($"delete:{Escape(name)}");
    public async void Ping() => await SendText("ping");
    public async void PressA() => await SendText("btnA");
    public async void RequestUsers() => await SendText("list");

    // ========= Connection =========

    async Task<bool> ConnectAsync()
    {
        isConnecting = true;

        string path = string.IsNullOrEmpty(wsPath) ? "/" : wsPath;
        if (!path.StartsWith("/")) path = "/" + path;
        string url = $"ws://{deviceIp}:{port}{path}";

        // Dispose any previous socket
        if (ws != null)
        {
            try { await ws.Close(); } catch { }
            ws = null;
        }

        Debug.Log("[WS] Connecting " + url);
        ws = new WebSocket(url);

        ws.OnOpen += () => Debug.Log("[WS] Open");
        ws.OnError += e => Debug.LogError("[WS] Error " + e);
        ws.OnClose += c => Debug.Log("[WS] Closed " + c);

        ws.OnMessage += HandleWsMessage;

        // Connect with soft timeout
        var _ = ws.Connect();
        float start = Time.unscaledTime;
        while (ws.State != WebSocketState.Open && Time.unscaledTime - start < connectTimeoutSec)
            await Task.Yield();

        isConnecting = false;

        if (ws.State == WebSocketState.Open) return true;

        Debug.LogWarning("[WS] Connect timeout/fail (state=" + ws.State + ")");
        return false;
    }

    public async Task CloseAsync()
    {
        if (ws != null)
        {
            try { await ws.Close(); } catch { }
            ws = null;
        }
        if (wsImu != null)
        {
            try { await wsImu.Close(); } catch { }
            wsImu = null;
        }
    }

    // ========= Sending =========

    public async Task SendText(string s)
    {
        if (ws == null || ws.State != WebSocketState.Open)
        {
            Debug.LogWarning("[WS] send skipped (state=" + (ws?.State.ToString() ?? "null") + "): " + s);
            return;
        }
        Debug.Log("[WS] => " + s);
        await ws.SendText(s);
    }

    public async void SendImuCommand(string cmd, bool addNewline)
    {
        if (!enableImu) return;

        if (string.IsNullOrWhiteSpace(cmd)) cmd = "BTN_B";

        bool ok = await EnsureImuConnectedAsync();
        if (!ok)
        {
            Debug.LogWarning("[IMU CMD] Cannot send, wsImu not connected.");
            return;
        }

        if (addNewline) cmd += "\n";
        Debug.Log("[IMU CMD] => " + cmd.Trim());

        await wsImu.SendText(cmd);
    }

    // -------- Message handling & parsing ------------

    void HandleWsMessage(byte[] bytes)
    {
        var raw = Encoding.UTF8.GetString(bytes);
        
        var trimmed = raw.Trim();
        Debug.Log("[RAW WS] " + trimmed);
        var lower = trimmed.ToLowerInvariant();

        // ----- IMU parsing (over SAME WS) -----
        // We support:
        //  A) "IMU:pitch,roll,yaw"   (recommended)
        //  B) "pitch,roll,yaw"       (legacy)
        //  C) {"type":"IMU","pitch":..,"roll":..,"yaw":..} (optional)
        if (enableImu)
        {
            if (trimmed.StartsWith("ACK:", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[ESP32 ACK] " + trimmed);
                OnImuRawMessage?.Invoke(trimmed);   // IMU channel
                return;
            }

            // A0) Tagged CSV: GYRO:gx,gy,gz   (recommended)
            if (trimmed.StartsWith("GYRO:", StringComparison.OrdinalIgnoreCase))
            {
                string csv = trimmed.Substring(5).Trim();
                OnImuRawMessage?.Invoke(trimmed);

                if (TryParseCsv3(csv, out var gx, out var gy, out var gz))
                {
                    OnImuGyro?.Invoke(gx, gy, gz);
                    return;
                }
            }

            // A) Tagged CSV: IMU:1.23,4.56,7.89
            if (trimmed.StartsWith("IMU:", StringComparison.OrdinalIgnoreCase))
            {
                string csv = trimmed.Substring(4).Trim();
                OnImuRawMessage?.Invoke(trimmed);

                if (TryParseCsvYpr(csv, out var p, out var r, out var y))
                {
                    OnImuYpr?.Invoke(p, r, y);
                    return;
                }

                // If it's tagged but not parseable, still continue to fingerprint parsing
                // (maybe you used a different IMU format)
            }
            // C) JSON IMU (only if you send it from ESP)
            if (trimmed.StartsWith("{") && trimmed.Contains("\"pitch\"") && trimmed.Contains("\"roll\"") && trimmed.Contains("\"yaw\""))
            {
                try
                {
                    var imu = JsonUtility.FromJson<ImuJson>(trimmed);
                    OnImuRawMessage?.Invoke(trimmed);
                    OnImuYpr?.Invoke(imu.pitch, imu.roll, imu.yaw);
                    return;
                }
                catch { /* ignore and fall through */ }
            }
            // B) Plain CSV (legacy): "1.23,4.56,7.89"
            if (LooksLikePlainCsvYpr(trimmed) && TryParseCsvYpr(trimmed, out var p2, out var r2, out var y2))
            {
                OnImuRawMessage?.Invoke(trimmed);
                OnImuYpr?.Invoke(p2, r2, y2);
                return;
            }
        }

        // TEMP: echo all non-JSON device lines straight to the UI
        if (devEchoToUI)
        {
            bool isJson = trimmed.Length > 0 && trimmed[0] == '{';
            if (!isJson)
            {
                OnEnrollSample?.Invoke(Mathf.Clamp(enrollStep, 0, 6), trimmed);
            }
        }

        Debug.Log("[WS] <= " + trimmed);

        // --- JSON handling (users list, etc.) ---
        if (trimmed.Length > 0 && trimmed[0] == '{')
        {
            try
            {
                var maybeUsers = JsonUtility.FromJson<UsersResponse>(trimmed);
                if (maybeUsers != null && !string.IsNullOrEmpty(maybeUsers.type))
                {
                    if (maybeUsers.type.Equals("USERS", StringComparison.OrdinalIgnoreCase))
                    {
                        OnUsersList?.Invoke(maybeUsers.users ?? new UserProfile[0]);
                        OnDeviceMessage?.Invoke(trimmed);
                        return;
                    }
                }
            }
            catch
            {
                // Not a UsersResponse; fall through
            }
        }

        // --- Enrollment / progress parsing (text) ---

        if (lower.Contains("registration done") || lower.Contains("enroll done") || lower.Contains("complete"))
        {
            enrollStep = 6;
            Debug.Log("[WS] PARSED: done");
            OnEnrollSample?.Invoke(6, "Registration done!");
            OnDeviceMessage?.Invoke(trimmed);
            return;
        }

        var mSavedS = Regex.Match(lower, @"\bs([1-6])\s*saved\b");
        if (mSavedS.Success)
        {
            enrollStep = mSavedS.Groups[1].Value[0] - '0';
            string msg = (enrollStep < 6)
                ? $"Sample {enrollStep}/6 saved.\nLift your finger and press A for sample {enrollStep + 1}."
                : "Sample 6/6 saved.\nRegistration done!";
            Debug.Log($"[WS] PARSED: saved step={enrollStep}");
            OnEnrollSample?.Invoke(enrollStep, msg);
            OnDeviceMessage?.Invoke(trimmed);
            return;
        }

        var mStartS = Regex.Match(lower, @"\bstart\s*s([1-6])");
        if (mStartS.Success)
        {
            int next = mStartS.Groups[1].Value[0] - '0';
            Debug.Log($"[WS] PARSED: next start S{next}");
            OnEnrollSample?.Invoke(Mathf.Clamp(enrollStep, 0, 6), $"Start S{next}: Place your Finger, press OK and lift your finger.");
            OnDeviceMessage?.Invoke(trimmed);
            return;
        }

        if (lower.Contains("lift finger"))
        {
            Debug.Log("[WS] PARSED: lift finger");
            OnEnrollSample?.Invoke(Mathf.Clamp(enrollStep, 0, 6), "Lift your finger…");
            OnDeviceMessage?.Invoke(trimmed);
            return;
        }

        if (lower.Contains("user exist"))
        {
            OnEnrollSample?.Invoke(Mathf.Clamp(enrollStep, 0, 6), "user exist.");
            OnDeviceMessage?.Invoke(trimmed);
            return;
        }

        // Fallback
        OnDeviceMessage?.Invoke(trimmed);
    }

    // ========= Helpers =========

    static bool TryParseCsvYpr(string csv, out float pitch, out float roll, out float yaw)
    {
        pitch = roll = yaw = 0f;
        var parts = csv.Split(',');
        if (parts.Length != 3) return false;

        return float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out pitch) &&
               float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out roll) &&
               float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out yaw);
    }

    static bool TryParseCsv3(string csv, out float a, out float b, out float c)
    {
        a = b = c = 0f;
        var parts = csv.Split(',');
        if (parts.Length != 3) return false;

        return float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out a) &&
               float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b) &&
               float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out c);
    }

    static bool LooksLikePlainCsvYpr(string s)
    {
        // fast check to avoid treating fingerprint strings as IMU:
        // require 2 commas and that the first non-space char is [-0-9.]
        int commaCount = 0;
        for (int i = 0; i < s.Length; i++) if (s[i] == ',') commaCount++;
        if (commaCount != 2) return false;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) continue;
            return (c == '-' || c == '+' || c == '.' || (c >= '0' && c <= '9'));
        }
        return false;
    }

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace(":", " ");
    }

    static int ExtractDigit1to6(string t)
    {
        foreach (char c in t) if (c >= '1' && c <= '6') return c - '0';
        return -1;
    }

    static bool IsSampleSaved(string lower, out int step)
    {
        step = -1;

        var m = Regex.Match(lower, @"(?:sample\s*#?\s*([1-6])\s*)?(saved|store|stored)");
        if (m.Success)
        {
            if (m.Groups[1].Success) step = m.Groups[1].Value[0] - '0';
            if (lower.Contains("sample")) return true;
        }

        if (lower.Contains("sample") && (lower.Contains("saved") || lower.Contains("store")))
            return true;

        return false;
    }
}
