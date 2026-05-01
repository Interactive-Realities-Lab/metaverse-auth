using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserListUI : MonoBehaviour
{
    [Header("References")]
    public FingerprintWsClient wsClient;
    public TMP_InputField userInput;
    public GameObject userListPanel;
    public Transform userListContent;
    public Button userButtonPrefab;

    private Coroutine requestRoutine;
    private bool usersLoaded = false;

    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    void OnEnable()
    {
        if (wsClient == null)
            wsClient = FingerprintWsClient.I;

        if (wsClient != null)
            wsClient.OnUsersList += HandleUsersList;

        if (userListPanel != null)
            userListPanel.SetActive(true);

        usersLoaded = false;

        if (requestRoutine != null)
            StopCoroutine(requestRoutine);

        requestRoutine = StartCoroutine(RequestUsersUntilLoaded());
    }

    void OnDisable()
    {
        if (requestRoutine != null)
        {
            StopCoroutine(requestRoutine);
            requestRoutine = null;
        }

        if (wsClient != null)
            wsClient.OnUsersList -= HandleUsersList;
    }

    private IEnumerator RequestUsersUntilLoaded()
    {
        float timeout = 8f;
        float elapsed = 0f;

        while (!usersLoaded && elapsed < timeout)
        {
            if (wsClient == null)
                wsClient = FingerprintWsClient.I;

            //ONLY request if connected
            if (wsClient != null && wsClient.IsConnected())
            {
                wsClient.RequestUsers();
            }

            yield return new WaitForSeconds(0.7f);
            elapsed += 0.7f;
        }

        requestRoutine = null;
    }

    public void OpenUserList()
    {
        if (userListPanel != null)
            userListPanel.SetActive(true);

        ClearUserList();

        if (wsClient != null)
            wsClient.RequestUsers();
    }

    public void CloseUserList()
    {
        if (userListPanel != null)
            userListPanel.SetActive(false);
    }

    void HandleUsersList(FingerprintWsClient.UserProfile[] users)
    {
        usersLoaded = true;

        ClearUserList();

        if (users == null || users.Length == 0)
            return;

        foreach (FingerprintWsClient.UserProfile user in users)
        {
            if (userButtonPrefab == null || userListContent == null)
                return;

            Button btn = Instantiate(userButtonPrefab, userListContent);
            spawnedButtons.Add(btn.gameObject);

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = user.name;

            string selectedName = user.name;
            btn.onClick.AddListener(() => SelectUser(selectedName));
        }
    }

    void SelectUser(string username)
    {
        if (userInput != null)
            userInput.text = username;

        CloseUserList();
    }

    void ClearUserList()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i]);
        }
        spawnedButtons.Clear();
    }

    public void OnLoginClicked()
    {
        string username = userInput.text.Trim();
        if (string.IsNullOrEmpty(username) || wsClient == null) return;

        wsClient.StartVerify(username);
    }

    public void OnRegisterClicked()
    {
        string username = userInput.text.Trim();
        if (string.IsNullOrEmpty(username) || wsClient == null) return;

        wsClient.StartEnroll(username);
    }
}