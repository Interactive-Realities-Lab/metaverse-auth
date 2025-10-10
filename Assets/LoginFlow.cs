using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginFlow : MonoBehaviour
{
    public TMP_InputField userField;
    public Button registerBtn;
    public Button loginBtn;

    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject fingerprintPanel;
    [SerializeField] TMP_Text statusLabel;

    void ShowLogin() { if (loginPanel) loginPanel.SetActive(true); if (fingerprintPanel) fingerprintPanel.SetActive(false); }
    void ShowFP(string msg) { if (loginPanel) loginPanel.SetActive(false); if (fingerprintPanel) fingerprintPanel.SetActive(true); if (statusLabel) statusLabel.text = msg; }



    void Start()
    {
        registerBtn.onClick.AddListener(() => DoOp("register"));
        loginBtn.onClick.AddListener(() => DoOp("login"));
        ShowLogin();
    }

    async void DoOp(string op)
    {
        var name = userField.text.Trim();
        if (string.IsNullOrEmpty(name)) { ShowFP("Enter a user name."); return; }

        var ok = await FingerprintWsClient.I.EnsureConnectedAsync();
        if (!ok) { ShowFP("Device not found. Check M5 IP and that WS is on :82."); return; }

        if (op == "register")
        {
            FingerprintWsClient.I.StartEnroll(name);
            ShowFP($"Registering '{name}'… Place finger for sample 1.");
        }
        else
        {
            FingerprintWsClient.I.StartVerify(name);
            ShowFP($"Verifying '{name}'… Place finger.");
        }
    }
}
