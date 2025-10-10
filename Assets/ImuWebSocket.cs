using UnityEngine;
using NativeWebSocket;
using System.Globalization;
using System.Text;

public class ImuWebSocket : MonoBehaviour
{
    [Header("Device")]
    public string deviceIp = "10.51.121.118";
    public int port = 82;

    [Header("HUD target")]
    public ImuAndQuestHud hud;   // <-- NOTE: ImuAndQuestHud (not DualMotionHUD)

    [Header("Options")]
    public bool showDebugLogs = false;

    WebSocket ws;

    async void Start()
    {
        if (hud == null) hud = FindObjectOfType<ImuAndQuestHud>();

        string url = $"ws://{deviceIp}:{port}";
        ws = new WebSocket(url);

        ws.OnOpen += () => { if (showDebugLogs) Debug.Log("[IMU] Connected " + url); };
        ws.OnError += (e) => Debug.LogError("[IMU] " + e);
        ws.OnClose += (e) => Debug.LogWarning("[IMU] Closed: " + e);

        ws.OnMessage += (bytes) =>
        {
            string text = Encoding.UTF8.GetString(bytes);

            // JSON: {"pitch":-1.23,"yaw":5.67,"roll":0.45}
            // CSV : -1.23,5.67,0.45  (pitch,yaw,roll)  OR  (pitch,roll,yaw) if your HUD is set accordingly
            if (text.Length > 0 && text[0] == '{')
            {
                float pitch = Extract(text, "\"pitch\":");
                float yaw = Extract(text, "\"yaw\":");
                float roll = Extract(text, "\"roll\":");
                if (hud) hud.OnImuEulerDegrees(pitch, yaw, roll);
            }
            else
            {
                if (TryParseCsvPYR(text, out float p, out float y, out float r))
                    if (hud) hud.OnImuEulerDegrees(p, y, r);
            }
        };

        await ws.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif
    }

    async void OnApplicationQuit()
    {
        if (ws != null)
        {
            await ws.Close();
            ws = null;
        }
    }

    // ---------- Public buttons ----------
    public async void ResetBoth()
    {
        // 1) Zero Quest line in HUD
        if (hud != null) hud.OnResetQuestZero();

        // 2) Tell IMU to zero (Btn B equivalent)
        if (ws != null && ws.State == WebSocketState.Open)
            await ws.SendText("ZERO");
    }

    public async void StreamOn() { if (ws != null && ws.State == WebSocketState.Open) await ws.SendText("STREAM:ON"); }
    public async void StreamOff() { if (ws != null && ws.State == WebSocketState.Open) await ws.SendText("STREAM:OFF"); }

    // ---------- Helpers ----------
    static float Extract(string s, string key)
    {
        int i = s.IndexOf(key);
        if (i < 0) return 0f;
        i += key.Length;
        int j = i;
        while (j < s.Length && " -+.0123456789eE".IndexOf(s[j]) >= 0) j++;
        var num = s.Substring(i, j - i).Trim();
        if (float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) return v;
        return 0f;
    }

    static bool TryParseCsvPYR(string line, out float pitch, out float yaw, out float roll)
    {
        pitch = yaw = roll = 0f;
        if (string.IsNullOrWhiteSpace(line)) return false;
        var parts = line.Trim().Split(',');
        if (parts.Length < 3) return false;

        bool okP = float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out pitch);
        bool okY = float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out yaw);
        bool okR = float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out roll);
        return okP && okY && okR;
    }
}
