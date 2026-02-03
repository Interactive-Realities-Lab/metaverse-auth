using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserListView : MonoBehaviour
{
    [Header("Links")]
    public FingerprintWsClient ws;          // drag Network → FingerprintWsClient, or leave null to use singleton
    public ScrollRect scrollRect;           // drag UserLogin/Scroll View (ScrollRect)
    public RectTransform content;           // drag UserLogin/Scroll View/Viewport/Content
    public GameObject userButtonPrefab;     // a Button prefab with a TMP label child
    public TMP_Text statusText;             // optional “Loading…” text
    public AuthUIController authUI;         // drag your AuthUIController (on UIController)

    void OnEnable()
    {
        if (ws == null) ws = FingerprintWsClient.I;  // use the global instance if not assigned
        if (authUI == null) authUI = GetComponentInParent<AuthUIController>();

        if (ws != null) ws.OnUsersList += HandleUsers;

        // UI setup safety
        if (scrollRect)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.horizontalScrollbar = null;
        }

        Clear();
        SetStatus("Loading users…");
        _ = InitAndRequestAsync();
    }

    void OnDisable()
    {
        if (ws != null) ws.OnUsersList -= HandleUsers;
    }

    async System.Threading.Tasks.Task InitAndRequestAsync()
    {
        if (ws == null) { SetStatus("WS client missing."); return; }
        await ws.EnsureConnectedAsync();
        ws.RequestUsers(); // sends "list" (change in FingerprintWsClient if needed)
    }

    void HandleUsers(UserProfile[] users)
    {
        Clear();

        if (users == null || users.Length == 0)
        {
            SetStatus("No users registered.");
            return;
        }
        SetStatus("");

        foreach (var u in users)
        {
            var go = Instantiate(userButtonPrefab, content);
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = string.IsNullOrWhiteSpace(u.displayName) ? u.username : u.displayName;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                ws.StartVerify(u.username); // your existing verify
                authUI?.GoToFingerprintPrompt(u.displayName ?? u.username);
            });
        }
    }

    void Clear()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }
}
