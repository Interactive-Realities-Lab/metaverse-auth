using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using NativeWebSocket;

public class ImuAndQuestHud : MonoBehaviour
{
    [Header("IMU Connection")]
    [Tooltip("e.g., 192.168.4.1")]
    public string deviceIp = "192.168.4.1";
    public int port = 82;

    [Header("IMU Command on Reset")]
    [Tooltip("What your firmware expects to zero (same as Btn B). Common: ZERO or BTN_B.")]
    public string imuResetCommand = "ZERO";
    public bool appendNewline = true;

    [Header("IMU UI (TMP_Text)")]
    public TMP_Text imuStatus;   // "IMU: Connected/Disconnected…"
    public TMP_Text imuRate;     // "XX Hz"
    public TMP_Text imuRaw;      // exact line received
    public TMP_Text imuPitch;    // parsed pitch if present
    public TMP_Text imuRoll;     // parsed roll if present
    public TMP_Text imuYaw;      // parsed yaw if present

    [Header("Quest UI (TMP_Text)")]
    public TMP_Text questPitch;
    public TMP_Text questRoll;
    public TMP_Text questYaw;

    [Header("Formatting")]
    public int decimals = 1;
    public bool showDashWhenMissing = true;

    [Header("Quest Zeroing")]
    public bool questRelativeZero = true;

    [Header("CSV Order (when device sends 3 comma numbers)")]
    public CsvOrder csvOrder = CsvOrder.P_R_Y; // your sketch prints Pitch,Roll,Yaw

    [Header("Debug")]
    public bool showDebugLogs = false;

    public enum CsvOrder { P_R_Y, P_Y_R }

    // ---- internal state ----
    WebSocket _ws;
    string _wsUrl;
    int _msgCount;
    float _lastRateTs;

    // IMU parsed cache
    bool _haveP, _haveR, _haveY;
    float _p, _r, _y;

    // Quest zero
    Vector3 _questZeroEuler;
    bool _questHasZero;

    // line assembly (if your firmware ever sends partial frames; NativeWebSocket usually delivers whole frames)
    readonly StringBuilder _lineBuf = new StringBuilder(256);

    // --------------- Unity lifecycle ---------------
    async void Start()
    {
        _wsUrl = $"ws://{deviceIp}:{port}";
        await Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif
        // update IMU rate once per second
        if (Time.unscaledTime - _lastRateTs >= 1f)
        {
            if (imuRate) imuRate.SetText($"{_msgCount} Hz");
            _msgCount = 0;
            _lastRateTs = Time.unscaledTime;
        }

        // show Quest Euler every frame
        if (TryGetHmdRotation(out var rot) || (Camera.main && (rot = Camera.main.transform.rotation) != Quaternion.identity))
        {
            var eul = rot.eulerAngles;
            eul = questRelativeZero && _questHasZero ? NormalizeEuler(eul - _questZeroEuler) : NormalizeEuler(eul);
            string fmt = $"F{Mathf.Clamp(decimals, 0, 5)}";
            if (questYaw) questYaw.text = eul.y.ToString(fmt);
            if (questPitch) questPitch.text = eul.x.ToString(fmt);
            if (questRoll) questRoll.text = eul.z.ToString(fmt);
        }

        // reflect parsed IMU values (already updated in OnMessage)
        string f = $"F{Mathf.Clamp(decimals, 0, 5)}";
        if (imuPitch) imuPitch.text = _haveP ? _p.ToString(f) : (showDashWhenMissing ? "—" : "");
        if (imuRoll) imuRoll.text = _haveR ? _r.ToString(f) : (showDashWhenMissing ? "—" : "");
        if (imuYaw) imuYaw.text = _haveY ? _y.ToString(f) : (showDashWhenMissing ? "—" : "");
    }

    async void OnApplicationQuit()
    {
        await CloseSocket();
    }

    // --------------- Public UI hooks ---------------
    // Hook your Reset button to this method.
    public async void OnResetQuestZero()
    {
        // 1) Zero Quest (capture current HMD as origin)
        if (TryGetHmdRotation(out var rot) || (Camera.main && (rot = Camera.main.transform.rotation) != Quaternion.identity))
        {
            _questZeroEuler = NormalizeEuler(rot.eulerAngles);
            _questHasZero = true;
        }

        // 2) Zero IMU by sending your command (Btn B equivalent)
        await SendImuCommand(imuResetCommand);
    }

    public async void StreamOn() { await SendImuCommand("STREAM:ON"); }
    public async void StreamOff() { await SendImuCommand("STREAM:OFF"); }
    public async void SendYawRef(float yawDeg)
    {
        await SendImuCommand("YAW_REF:" + yawDeg.ToString(CultureInfo.InvariantCulture));
    }

    // --------------- WebSocket ---------------
    async Task Connect()
    {
        try
        {
            if (imuStatus) imuStatus.SetText("IMU: Connecting…");
            _ws = new WebSocket(_wsUrl);

            _ws.OnOpen += () => { if (imuStatus) imuStatus.SetText("IMU: Connected"); if (showDebugLogs) Debug.Log("[IMU] Connected " + _wsUrl); };
            _ws.OnError += (e) => { if (imuStatus) imuStatus.SetText("IMU: Error"); Debug.LogError("[IMU] " + e); };
            _ws.OnClose += (c) => { if (imuStatus) imuStatus.SetText($"IMU: Closed ({c})"); if (showDebugLogs) Debug.LogWarning("[IMU] Closed: " + c); ReconnectSoon(); };

            _ws.OnMessage += HandleWsMessage;

            await _ws.Connect();
        }
        catch (Exception ex)
        {
            if (imuStatus) imuStatus.SetText("IMU: Connect failed");
            Debug.LogError("[IMU] Connect exception: " + ex.Message);
            ReconnectSoon();
        }
    }

    void ReconnectSoon()
    {
        // simple backoff: try again in 2 seconds
        Invoke(nameof(_Reconnect), 2f);
    }
    async void _Reconnect()
    {
        await CloseSocket();
        await Connect();
    }

    async Task CloseSocket()
    {
        try
        {
            if (_ws != null)
            {
                if (_ws.State == WebSocketState.Open)
                    await _ws.Close();
                _ws = null;
            }
        }
        catch { /* ignore */ }
    }

    void HandleWsMessage(byte[] bytes)
    {
        _msgCount++;
        string text = Encoding.UTF8.GetString(bytes);
        if (imuRaw) imuRaw.SetText(text); // EXACT line as sent by device

        // Accept JSON or CSV
        if (text.Length > 0 && text[0] == '{')
        {
            // JSON: allow keys pitch/roll/yaw or p/r/y
            _haveP = TryExtractJsonFloat(text, "\"pitch\":", "\"p\":", out _p);
            _haveR = TryExtractJsonFloat(text, "\"roll\":", "\"r\":", out _r);
            _haveY = TryExtractJsonFloat(text, "\"yaw\":", "\"y\":", out _y);
        }
        else
        {
            // CSV: tolerate spaces and CRLF; use configured order
            if (TryParseCsv(text, csvOrder, out float p, out float r, out float y))
            {
                _p = p; _r = r; _y = y;
                _haveP = _haveR = _haveY = true;
            }
            else
            {
                _haveP = _haveR = _haveY = false;
            }
        }
    }

    async Task SendImuCommand(string cmd)
    {
        if (_ws == null || _ws.State != WebSocketState.Open) return;
        if (appendNewline) cmd += "\n";
        try
        {
            await _ws.SendText(cmd);
            if (imuStatus) imuStatus.SetText($"IMU: Sent '{cmd.Trim()}'");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[IMU] Send failed: " + ex.Message);
        }
    }

    // --------------- Helpers ---------------
    static bool TryExtractJsonFloat(string s, string primaryKey, string altKey, out float value)
    {
        if (TryExtractAfterKey(s, primaryKey, out value)) return true;
        if (!string.IsNullOrEmpty(altKey)) return TryExtractAfterKey(s, altKey, out value);
        return false;
    }

    static bool TryExtractAfterKey(string s, string key, out float value)
    {
        value = 0f;
        int i = s.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return false;
        i += key.Length;
        int j = i;
        while (j < s.Length && " -+.0123456789eE".IndexOf(s[j]) >= 0) j++;
        var num = s.Substring(i, j - i).Trim();
        return float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    static bool TryParseCsv(string line, CsvOrder order, out float p, out float r, out float y)
    {
        p = r = y = 0f;
        if (string.IsNullOrWhiteSpace(line)) return false;
        var parts = line.Trim().Split(',');
        if (parts.Length < 3) return false;

        bool ok0 = float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float a0);
        bool ok1 = float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float a1);
        bool ok2 = float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float a2);
        if (!(ok0 && ok1 && ok2)) return false;

        if (order == CsvOrder.P_R_Y) { p = a0; r = a1; y = a2; }  // matches your M5 sketch
        else { p = a0; y = a1; r = a2; }  // alternate (pitch,yaw,roll)
        return true;
    }

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
        e.x = NormalizeDeg(e.x); e.y = NormalizeDeg(e.y); e.z = NormalizeDeg(e.z);
        return e;
    }
    static float NormalizeDeg(float d)
    {
        d %= 360f;
        if (d >= 180f) d -= 360f;
        if (d < -180f) d += 360f;
        return d;
    }

    // === External feed API (called by ImuWebSocket) ===
    public void OnImuEulerDegrees(float pitch, float yaw, float roll)
    {
        _p = pitch; _y = yaw; _r = roll;
        _haveP = _haveR = _haveY = true;
    }

    // Back-compat alias if any old code calls ResetQuestZero()
    public void ResetQuestZero() => OnResetQuestZero();

}
