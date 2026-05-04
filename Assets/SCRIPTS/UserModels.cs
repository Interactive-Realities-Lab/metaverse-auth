using System;

[Serializable]
public class UserProfile
{
    public string username;     // unique id
    public string displayName;  // shown in UI
}

[Serializable]
public class UsersResponse
{
    public string type;         // e.g., "USERS"
    public UserProfile[] users; // array of users
}
