using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using NativeWebSocket;

public class MotionHud : MonoBehaviour
{
    [Header("IMU connection")]
    public string deviceIp = "10.51.121.118";
    public int port = 82;

    [Header("IMU zero command")]
    public string imuResetCommand = "ZERO";
    public bool appendNewline = true;

    [Header("CSV order when device sends 3 numbers")]
    public CsvOrder csvOrder = CsvOrder.P_R_Y; // your sketch prints Pitch,Roll,Yaw

    [Header("Formatting")]
    public int decimals = 1;                   // UI only
    public bool questRelativeZero = true;

    [Header("(Auto) Row labels")]
    public TMP_Text rowQuestText;              // Motion/RowQuest/Text (TMP)
    public TMP_Text rowImuText;                // Motion/RowIMU/Text (TMP)
    public TMP_Text rowDeltaText;              // Motion/RowDelta/Text (TMP)

    [Header("Optional: hook the Reset button here to auto-wire on play")]
    public Button resetButton;                 // Motion/Reset (Button)

    public enum CsvOrder { P_R_Y, P_Y_R }

    // ---- internals ----
    WebSocket _ws;
    string _wsUrl;

    // IMU parsed cache
    bool _haveP, _haveR, _haveY;
    float _p, _r, _y;

    // Quest zero
    Vector3 _questZeroEuler;
    bool _questHasZero;

    // rate (optional, not shown but kept for future)
    int _msgCount;
    float _lastRateTs;

    // ---------- lifecycle ----------
    async void Awake()
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

        // Initial UI
        if (rowQuestText) rowQuestText.text = "QUEST | Yaw: —  Pitch: —  Roll: —";
        if (rowImuText) rowImuText.text = "IMU   | Yaw: —  Pitch: —  Roll: —";
        if (rowDeltaText) rowDeltaText.text = "IMU   | Yaw: —  Pitch: —  Roll: —";
    }

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
                rowQuestText.text = $"QUEST | Yaw: {questYaw.ToString(f)}°  Pitch: {questPitch.ToString(f)}°  Roll: {questRoll.ToString(f)}°";
        }

        // ----- IMU row (from last parsed values) -----
        if (rowImuText)
        {
            string f = $"F{Mathf.Clamp(decimals, 0, 5)}";
            string y = _haveY ? _y.ToString(f) : "—";
            string p = _haveP ? _p.ToString(f) : "—";
            string r = _haveR ? _r.ToString(f) : "—";
            rowImuText.text = $"IMU   | Yaw: {y}°  Pitch: {r}°  Roll: {p}°";

            rowDeltaText.text = $"Delta   | Yaw: {(questYaw - (_haveY ? _y : 0)).ToString(f)}°  Pitch: {(questPitch - (_haveR ? _r : 0)).ToString(f)}°  Roll: {(questRoll - (_haveP ? _p : 0)).ToString(f)}°";
        }

        // (optional) message rate if you want: uncomment next 5 lines and add a TMP if needed
        // if (Time.unscaledTime - _lastRateTs >= 1f)
        // {
        //     _msgCount = 0;
        //     _lastRateTs = Time.unscaledTime;
        // }
    }

    async void OnDestroy()
    {
        try { if (_ws != null && _ws.State == WebSocketState.Open) await _ws.Close(); } catch { }
        _ws = null;
    }

    // ---------- Reset (button) ----------
    public async void OnResetClicked()
    {
        // 1) zero QUEST row
        if (TryGetHmdRotation(out var rot) || (Camera.main && (rot = Camera.main.transform.rotation) != Quaternion.identity))
        {
            _questZeroEuler = NormalizeEuler(rot.eulerAngles);
            _questHasZero = true;
        }
        // 2) tell IMU to zero (Btn B equivalent in your sketch)
        await SendImuCommand(imuResetCommand);
    }

    // ---------- WebSocket ----------
    async Task Connect()
    {
        try
        {
            _ws = new WebSocket(_wsUrl);

            _ws.OnOpen += () => Debug.Log("[IMU] Connected " + _wsUrl);
            _ws.OnError += (e) => Debug.LogError("[IMU] " + e);
            _ws.OnClose += (c) => { Debug.LogWarning("[IMU] Closed: " + c); ReconnectSoon(); };

            _ws.OnMessage += HandleImuMessage;

            await _ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError("[IMU] Connect exception: " + ex.Message);
            ReconnectSoon();
        }
    }

    void ReconnectSoon() => Invoke(nameof(_Reconnect), 2f);
    async void _Reconnect()
    {
        try { if (_ws != null && _ws.State == WebSocketState.Open) await _ws.Close(); } catch { }
        _ws = null;
        await Connect();
    }

    void HandleImuMessage(byte[] bytes)
    {
        _msgCount++;
        string text = Encoding.UTF8.GetString(bytes).Trim();

        // Accept JSON: {"pitch":-1.23,"yaw":5.67,"roll":0.45}
        // OR CSV  : pitch,roll,yaw  (default)  or pitch,yaw,roll (set via csvOrder)
        if (text.Length > 0 && text[0] == '{')
        {
            _haveP = TryExtractJsonFloat(text, "\"pitch\":", "\"p\":", out _p);
            _haveR = TryExtractJsonFloat(text, "\"roll\":", "\"r\":", out _r);
            _haveY = TryExtractJsonFloat(text, "\"yaw\":", "\"y\":", out _y);
        }
        else
        {
            if (TryParseCsv(text, csvOrder, out float p, out float r, out float y))
            {
                _p = -p; _r = -r; _y = y;
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
        try { await _ws.SendText(cmd); }
        catch (Exception ex) { Debug.LogWarning("[IMU] send failed: " + ex.Message); }
    }

    // ---------- helpers ----------
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

        if (order == CsvOrder.P_R_Y) { p = a0; r = a1; y = a2; }  // your sketch default
        else { p = a0; y = a1; r = a2; }  // alternate
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
}
