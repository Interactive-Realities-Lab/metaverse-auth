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
    public GameObject userRowPrefab;

    [Header("Admin Remove")]
    public string adminUsername = "shaki";

    private string pendingRemoveUser = "";

    public AuthUIController authUIController;

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
        Debug.Log("HandleUsersList called. Count: " + (users == null ? 0 : users.Length));

        usersLoaded = true;
        ClearUserList();

        if (users == null || users.Length == 0)
            return;

        foreach (FingerprintWsClient.UserProfile user in users)
        {
            if (userRowPrefab == null)
            {
                Debug.LogError("userRowPrefab is not assigned.");
                return;
            }

            if (userListContent == null)
            {
                Debug.LogError("userListContent is not assigned.");
                return;
            }

            GameObject row = Instantiate(userRowPrefab, userListContent);
            row.SetActive(true);
            spawnedButtons.Add(row);

            Button nameBtn = row.transform.Find("UserButton").GetComponent<Button>();
            Button removeBtn = row.transform.Find("RemoveBtn").GetComponent<Button>();
            TMP_Text txt = row.transform.Find("UserButton/Text (TMP)").GetComponent<TMP_Text>();

            txt.text = user.name;

            string selectedName = user.name;

            nameBtn.onClick.RemoveAllListeners();
            nameBtn.onClick.AddListener(() =>
            {
                Debug.Log("NAME clicked: " + selectedName); 
                SelectUser(selectedName);
            });

            removeBtn.onClick.RemoveAllListeners();
            removeBtn.onClick.AddListener(() =>
            {
                Debug.Log("REMOVE clicked: " + selectedName); 
                AskAdminToRemoveUser(selectedName);
            });
        }
    }

    void AskAdminToRemoveUser(string usernameToRemove)
    {
        pendingRemoveUser = usernameToRemove;

        Debug.Log("Remove requested for: " + pendingRemoveUser);
        Debug.Log("Admin verification required: " + adminUsername);

        if (wsClient == null)
            wsClient = FingerprintWsClient.I;

        if (wsClient == null || !wsClient.IsConnected())
        {
            Debug.LogError("Device not connected. Cannot verify admin.");
            return;
        }

        // First verify admin fingerprint
        if (userInput != null)
            userInput.text = adminUsername;

        if (authUIController != null)
        {
            authUIController.BeginVerifyForSelectedUser(adminUsername, adminUsername);
        }
        else
        {
            wsClient.StartVerify(adminUsername);
        }
    }

    void ShowUserOptions(string username)
    {
        Debug.Log("Show dropdown for: " + username);

        // Later:
        // show panel with Edit and Remove buttons
    }

    void SelectUser(string username)
    {
        if (string.IsNullOrEmpty(username))
            return;

        /*if (userInput != null)
            userInput.text = username;*/

        CloseUserList();

        if (authUIController != null)
        {
            authUIController.BeginVerifyForSelectedUser(username, username);
        }
        else
        {
            Debug.LogError("AuthUIController is not assigned in UserListUI.");
        }
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

    public void OnAdminVerified(string verifiedUsername)
    {
        Debug.Log("OnAdminVerified called with: " + verifiedUsername);
        Debug.Log("pendingRemoveUser = " + pendingRemoveUser);
        Debug.Log("adminUsername = " + adminUsername);

        if (string.IsNullOrEmpty(pendingRemoveUser))
        {
            Debug.Log("No pending removal.");
            return;
        }

        if (verifiedUsername.Trim().ToLower() == adminUsername.Trim().ToLower())
        {
            Debug.Log("ADMIN VERIFIED. Sending delete command for: " + pendingRemoveUser);
            wsClient.DeleteUser(pendingRemoveUser);
        }
        else
        {
            Debug.Log("Admin verification failed. Verified user was: " + verifiedUsername);
        }

        pendingRemoveUser = "";
    }
}