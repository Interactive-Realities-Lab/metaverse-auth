using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;

public class FingerprintWsClient : MonoBehaviour
{
    [Header("Debug")]
    public bool devEchoToUI = true;  // TEMP: echo every non-JSON line to the UI


    // ---------- Singleton ----------
    public static FingerprintWsClient I { get; private set; }

    // ---------- Inspector ----------
    [Header("ESP32 Device")]
    public string deviceIp = "192.168.43.57";
    public int port = 82;
    [Tooltip("Keep '/' for root. Use '/ws' only if your sketch serves WS at that path.")]
    public string wsPath = "/";

    [Header("Connection")]
    public bool autoConnectOnStart = false;
    public float connectTimeoutSec = 4f;

    // ---------- Events (UI may subscribe to these) ----------
    public event Action<string> OnDeviceMessage;       // raw device line (JSON or text)
    public event Action<int, string> OnEnrollSample;        // parsed progress: (step 0..6, pretty text)

    // ---------- Internals ----------
    WebSocket ws;
    bool isConnecting;
    int enrollStep = 0; // 0..6, advanced only when we detect a "sample saved" line

    // ========= Unity lifecycle =========

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        if (autoConnectOnStart)
            await EnsureConnectedAsync();
    }

    void Update()
    {
        ws?.DispatchMessageQueue(); // required on Quest/Android & WebGL
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

    public async void StartEnroll(string name) => await SendText($"register:{Escape(name)}");
    public async void StartVerify(string name) => await SendText($"verify:{Escape(name)}");

    public async void QueryState() => await SendText("state");
    public async void StartContinuous(string name) => await SendText($"cont_start:{Escape(name)}");
    public async void StopContinuous() => await SendText("cont_stop");
    public async void DeleteUser(string name) => await SendText($"delete:{Escape(name)}");
    public async void Ping() => await SendText("ping");

    public async void PressA() => await SendText("btnA");


    // ========= Connection =========

    async Task<bool> ConnectAsync()
    {
        isConnecting = true;

        // Build URL
        string path = string.IsNullOrEmpty(wsPath) ? "/" : wsPath;
        if (!path.StartsWith("/")) path = "/" + path;
        string url = $"ws://{deviceIp}:{port}{path}";

        // Dispose any previous socket
        if (ws != null)
        {
            try { await ws.Close(); } catch { /* ignore */ }
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
        if (ws == null) return;
        try { await ws.Close(); } catch { /* ignore */ }
        ws = null;
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

    // ========= Message handling & parsing =========

    void HandleWsMessage(byte[] bytes)
    {
        var raw = Encoding.UTF8.GetString(bytes);
        var lower = raw.ToLowerInvariant();

        // TEMP: echo all non-JSON device lines straight to the UI to prove the path
        if (devEchoToUI)
        {
            // ignore JSON/status like {"sensorReady":...}
            bool isJson = raw.Length > 0 && raw[0] == '{';
            if (!isJson)
            {
                // show exactly what the device sent (e.g., "S1 saved", "Start S2: press A")
                OnEnrollSample?.Invoke(Mathf.Clamp(enrollStep, 0, 6), raw);
            }
        }


        Debug.Log("[WS] <= " + raw);          // already there

        // --- put the new code BELOW here (inside this method) ---

        // Completed
        if (lower.Contains("registration done") || lower.Contains("enroll done") || lower.Contains("complete"))
        {
            enrollStep = 6;
            Debug.Log("[WS] PARSED: done");
            OnEnrollSample?.Invoke(6, "Registration done!");
            return;
        }

        // "S1 saved" .. "S6 saved"
        var mSavedS = Regex.Match(lower, @"\bs([1-6])\s*saved\b");
        if (mSavedS.Success)
        {
            enrollStep = mSavedS.Groups[1].Value[0] - '0';
            string msg = (enrollStep < 6)
                ? $"Sample {enrollStep}/6 saved.\nLift your finger and press A for sample {enrollStep + 1}."
                : "Sample 6/6 saved.\nRegistration done!";
            Debug.Log($"[WS] PARSED: saved step={enrollStep}");
            OnEnrollSample?.Invoke(enrollStep, msg);
            return;
        }

        var mStartS = Regex.Match(lower, @"\bstart\s*s([1-6])");
        if (mStartS.Success)
        {
            int next = mStartS.Groups[1].Value[0] - '0';
            Debug.Log($"[WS] PARSED: next start S{next}");
            OnEnrollSample?.Invoke(Mathf.Clamp(enrollStep, 0, 6), $"Start S{next}: Place your Finger, press OK and lift your finger.");
            return;
        }

        if (lower.Contains("lift finger"))
        {
            Debug.Log("[WS] PARSED: lift finger");
            OnEnrollSample?.Invoke(Mathf.Clamp(enrollStep, 0, 6), "Lift your finger…");
            return;
        }

        if (lower.Contains("user exist"))
        {
            OnEnrollSample?.Invoke(Mathf.Clamp(enrollStep, 0, 6), "user exist.");
            return;
        }

    }



    // ========= Helpers =========

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // keep the simple "command:name" protocol happy
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

        // Try common forms:
        // "sample 1 saved", "sample 1 stored", "saved sample 1",
        // "sample #1 saved", "sample1 saved"
        var m = System.Text.RegularExpressions.Regex.Match(
            lower, @"(?:sample\s*#?\s*([1-6])\s*)?(saved|store|stored)");
        if (m.Success)
        {
            if (m.Groups[1].Success) step = m.Groups[1].Value[0] - '0';
            // Make sure it really refers to a sample; require the word 'sample' somewhere.
            if (lower.Contains("sample")) return true;
        }

        // Fallback: both 'sample' and a save verb somewhere in the line
        if (lower.Contains("sample") && (lower.Contains("saved") || lower.Contains("store")))
            return true;

        return false;
    }

}
