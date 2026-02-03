using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Attach to UIController
public class AuthUIController : MonoBehaviour
{

    [Header("UserSelection panel (UserSelection)")]
    [SerializeField] GameObject userSelectionPanel;           // UIController/UserSelection
    [SerializeField] Button register;                   // Reg (Button)

    [Header("Login panel (LogReg)")]
    [SerializeField] GameObject loginPanel;           // UIController/LogReg
    [SerializeField] TMP_InputField userNameField;       // LogReg/InputField (TMP)
    [SerializeField] Button registerButton;       // LogReg/Reg (Button)
    [SerializeField] Button loginButton;          // LogReg/Login (Button)

    [Header("Fingerprint prompt panel (RegFingerPrint)")]
    [SerializeField] GameObject fingerprintPanel;     // UIController/RegFingerPrint
    [SerializeField] TMP_Text fingerprintText;      // RegFingerPrint/Text (TMP)
    [SerializeField] Button okButton;
    [SerializeField] TMP_Text okButtonLabel;

    bool _busy;
    bool _subscribed;

    enum Flow { None, Register, Login }
    Flow _flow = Flow.None;

    void Awake()
    {
        _flow = Flow.None;

        registerButton.onClick.RemoveAllListeners();
        loginButton.onClick.RemoveAllListeners();
        registerButton.onClick.AddListener(OnPressRegister);
        loginButton.onClick.AddListener(OnPressLogin);

        // default: OK sends Button-A (device prompt will say when to press)
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() => FingerprintWsClient.I?.PressA());
        if (okButtonLabel) okButtonLabel.text = "OK";

        SetPanels(true, "");
        EnsureWsSubscriptions();
    }

    void OnEnable()
    {
        var ws = FingerprintWsClient.I;
        if (ws == null) return;

        // Make sure we don't double-subscribe
        ws.OnEnrollSample -= HandleEnrollSample;
        ws.OnDeviceMessage -= HandleDeviceMsg;

        ws.OnEnrollSample += HandleEnrollSample;
        ws.OnDeviceMessage += HandleDeviceMsg;
        _subscribed = true;
    }

    void OnDisable()
    {
        var ws = FingerprintWsClient.I;
        if (ws == null) return;

        ws.OnEnrollSample -= HandleEnrollSample;
        ws.OnDeviceMessage -= HandleDeviceMsg;
        _subscribed = false;
    }

    void OnDestroy()
    {
        var ws = FingerprintWsClient.I;
        if (ws != null && _subscribed)
        {
            ws.OnEnrollSample -= HandleEnrollSample;
            ws.OnDeviceMessage -= HandleDeviceMsg;
        }
        _subscribed = false;
    }

    void EnsureWsSubscriptions()
    {
        var ws = FingerprintWsClient.I;
        if (ws == null || _subscribed) return;

        ws.OnEnrollSample += HandleEnrollSample;
        ws.OnDeviceMessage += HandleDeviceMsg;
        _subscribed = true;
    }

    // ===== Buttons (manual name typed) =====
    public void OnPressRegister() { if (!_busy) _ = RegisterFlow(); }
    public void OnPressLogin() { if (!_busy) _ = LoginFlow(); }

    async Task RegisterFlow()
    {
        _flow = Flow.Register;
        if (_busy) return;
        _busy = true;
        try
        {
            EnsureWsSubscriptions();

            var ws = FingerprintWsClient.I;
            if (ws == null) { SetPanels(false, "WS client not found in scene."); return; }

            var name = userNameField?.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name)) { SetPanels(false, "Please enter your name first."); return; }

            SetPanels(false, $"Hi {name},\nPlease place your fingerprint to REGISTER.");

            if (!await ws.EnsureConnectedAsync()) { SetPanels(false, "Device not found."); return; }

            ws.StartEnroll(name);
        }
        finally { _busy = false; }
    }

    async Task LoginFlow()
    {
        _flow = Flow.Login;
        if (_busy) return;
        _busy = true;
        try
        {
            EnsureWsSubscriptions();
            var ws = FingerprintWsClient.I;
            if (ws == null) { SetPanels(false, "WS client not found in scene."); return; }

            var name = userNameField?.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name)) { SetPanels(false, "Please enter your name first."); return; }

            SetPanels(false, "Please place your fingerprint to LOGIN.");

            if (!await ws.EnsureConnectedAsync()) { SetPanels(false, "Device not found."); return; }

            ws.StartVerify(name);
        }
        finally { _busy = false; }
    }

    // ===== Public entry points for the User List =====

    /// <summary>
    /// Called by your UserList (when a profile button is tapped).
    /// Ensures connection, switches UI, and starts verify with the given username.
    /// </summary>
    public async void BeginVerifyForSelectedUser(string username, string displayName)
    {
        _flow = Flow.Login;

        // reflect the selection into the input field (nice for consistency)
        if (userNameField) userNameField.text = username ?? "";

        SetPanels(false, $"User: {displayName ?? username}\nPlease place your fingerprint to LOGIN.");

        var ws = FingerprintWsClient.I;
        if (ws == null) { SetPanels(false, "WS client not found in scene."); return; }

        EnsureWsSubscriptions();

        if (!await ws.EnsureConnectedAsync())
        {
            SetPanels(false, "Device not found.");
            return;
        }

        ws.StartVerify(username);
    }

    /// <summary>
    /// Lightweight UI switch if some other script already sent the verify command.
    /// </summary>
    public void GoToFingerprintPrompt(string displayName)
    {
        _flow = Flow.Login;
        SetPanels(false, $"User: {displayName}\nPlace your finger on the sensor…");
        DisableOk(); // will be enabled when device tells to press A
    }

    // ===== OK button modes =====
    void ShowOkPressA()
    {
        if (!okButton) return;
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() => FingerprintWsClient.I?.PressA());
        if (okButtonLabel) okButtonLabel.text = "OK";
        okButton.interactable = true;
    }

    void ShowOkBack()
    {
        if (!okButton) return;
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(BackToLogin);
        if (okButtonLabel) okButtonLabel.text = "Back";
        okButton.interactable = true;
    }

    void DisableOk()
    {
        if (!okButton) return;
        okButton.interactable = false;
    }

    // ===== Incoming messages =====
    void HandleEnrollSample(int step, string pretty)
    {
        if (fingerprintPanel && fingerprintPanel.activeInHierarchy && fingerprintText)
            fingerprintText.text = pretty;

        var p = (pretty ?? "").ToLowerInvariant();

        // Unknown user
        if (p.Contains("user not found"))
        {
            if (fingerprintText) fingerprintText.text = "User not found. Please register first.";
            ShowOkBack();
            return;
        }

        if (p.Contains("user exist"))
        {
            ShowOkBack();
            return;
        }

        if (p.Contains("press a") || p.StartsWith("start s"))
        {
            ShowOkPressA();
            return;
        }

        if (step >= 6 || p.Contains("registration done") || p.Contains("verified"))
        {
            ShowOkBack();
            return;
        }

        DisableOk();
    }

    void HandleDeviceMsg(string msg)
    {
        if (_flow == Flow.None || !fingerprintPanel || !fingerprintPanel.activeInHierarchy) return;
        if (string.IsNullOrEmpty(msg)) return;

        // ignore generic JSON/status lines
        if (msg.Length > 0 && (msg[0] == '{' || msg.Contains("sensorReady") || msg.Contains("\"op\""))) return;

        var p = msg.ToLowerInvariant();

        if (p.Contains("user not found"))
        {
            if (fingerprintText) fingerprintText.text = "User not found. Please register first.";
            ShowOkBack();
            return;
        }

        // Show raw human-readable device line (e.g., “Sample 1 saved”, “Press A…”)
        if (fingerprintText) fingerprintText.text = msg;
    }

    public void BackToLogin()
    {
        _flow = Flow.None;
        SetPanels(true, "");
        if (userNameField) userNameField.text = "";
    }

    // ---------- UI helper ----------
    void SetPanels(bool showLogin, string message)
    {
        if (loginPanel) loginPanel.SetActive(showLogin);
        if (fingerprintPanel) fingerprintPanel.SetActive(!showLogin);
        if (fingerprintText) fingerprintText.text = message ?? "";

        if (okButton)
        {
            okButton.gameObject.SetActive(!showLogin);
            okButton.interactable = false;     // disabled on initial message
            if (okButtonLabel) okButtonLabel.text = "OK";
        }
    }
}
