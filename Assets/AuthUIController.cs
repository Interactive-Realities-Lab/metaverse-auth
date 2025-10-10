using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Attach to UIController
public class AuthUIController : MonoBehaviour
{
    [Header("Login panel (LogReg)")]
    [SerializeField] GameObject loginPanel;        // UIController/LogReg
    [SerializeField] TMP_InputField userNameField;     // LogReg/InputField (TMP)
    [SerializeField] Button registerButton;    // LogReg/Reg (Button)
    [SerializeField] Button loginButton;       // LogReg/Login (Button)


    [Header("Fingerprint prompt panel (RegFingerPrint)")]
    [SerializeField] GameObject fingerprintPanel;  // UIController/RegFingerPrint
    [SerializeField] TMP_Text fingerprintText;   // RegFingerPrint/Text (TMP)
    [SerializeField] Button okButton;
    [SerializeField] TMP_Text okButtonLabel;

    bool _busy;

    bool _subscribed;

    void EnsureWsSubscriptions()
    {
        var ws = FingerprintWsClient.I;
        if (ws == null || _subscribed) return;

        // EnsureWsSubscriptions()
        ws.OnEnrollSample += HandleEnrollSample;
        ws.OnDeviceMessage += HandleDeviceMsg;   

        _subscribed = true;
    }

    void OnDestroy()
    {
        // clean up to avoid leaks if the object is destroyed/reloaded
        var ws = FingerprintWsClient.I;
        if (ws != null && _subscribed)
        {
            ws.OnEnrollSample -= HandleEnrollSample;
            // ws.OnDeviceMessage -= HandleDeviceLine;
        }
        _subscribed = false;
    }

    void Awake()
    {
        _flow = Flow.None;

        registerButton.onClick.RemoveAllListeners();
        loginButton.onClick.RemoveAllListeners();
        registerButton.onClick.AddListener(OnPressRegister);
        loginButton.onClick.AddListener(OnPressLogin);

        // default: OK sends Button-A, but we’ll enable/disable it per message
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() => FingerprintWsClient.I?.PressA());
        if (okButtonLabel) okButtonLabel.text = "OK";

        SetPanels(true, "");
        EnsureWsSubscriptions();
    }

    enum Flow { None, Register, Login }
    Flow _flow = Flow.None;

    void OnEnable()
    {
        var ws = FingerprintWsClient.I;
        if (ws == null) return;

        ws.OnEnrollSample -= HandleEnrollSample;
        //ws.OnDeviceMessage -= HandleDeviceMsg;       // keep if you want human lines (not JSON)
        ws.OnEnrollSample += HandleEnrollSample;
        ws.OnDeviceMessage += HandleDeviceMsg;
    }

    void OnDisable()
    {
        var ws = FingerprintWsClient.I;
        if (ws == null) return;
        ws.OnEnrollSample -= HandleEnrollSample;
        ws.OnDeviceMessage -= HandleDeviceMsg;
    }

    // Buttons (no need to call EnsureWsSubscriptions here)
    public void OnPressRegister() { if (!_busy) _ = RegisterFlow(); }
    public void OnPressLogin() { if (!_busy) _ = LoginFlow(); }


    // Call it at the start of each flow (before SetPanels)
    async Task RegisterFlow()
    {
        _flow = Flow.Register;

        if (_busy) return;
        _busy = true;
        try
        {
            EnsureWsSubscriptions(); // <— add this line first

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
            EnsureWsSubscriptions();                      // <-- add
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

    // ==== OK button modes ====
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


    void HandleEnrollSample(int step, string pretty)
    {
        if (fingerprintPanel && fingerprintPanel.activeInHierarchy && fingerprintText)
            fingerprintText.text = pretty;

        var p = (pretty ?? "").ToLowerInvariant();

        // NEW: unknown user -> show message and Back
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
        if (msg.Length > 0 && (msg[0] == '{' || msg.Contains("sensorReady") || msg.Contains("\"op\""))) return;

        var p = (msg ?? "").ToLowerInvariant();

        // NEW: unknown user -> show message and Back
        if (p.Contains("user not found"))
        {
            if (fingerprintText) fingerprintText.text = "User not found. Please register first.";
            ShowOkBack();
            return;
        }

        fingerprintText.text = msg; // human-readable lines such as "sample saved", "press A…"
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
            okButton.interactable = false;     // disabled on initial "Hi ..." message
            if (okButtonLabel) okButtonLabel.text = "OK";
        }
    }

}
