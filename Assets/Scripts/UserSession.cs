using UnityEngine;

public class UserSession : MonoBehaviour
{
    public static UserSession Instance;

    public string userName;
    public string deviceUserId;
    public bool isAuthenticated;
    public bool parityBuiltOnce;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartSession(string name, string id)
    {
        userName = name;
        deviceUserId = id;
        isAuthenticated = true;
        parityBuiltOnce = true;
    }

    public void ClearSession()
    {
        userName = "";
        deviceUserId = "";
        isAuthenticated = false;
        parityBuiltOnce = false;
    }
}