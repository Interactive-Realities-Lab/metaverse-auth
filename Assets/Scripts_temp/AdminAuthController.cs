using UnityEngine;

public class AdminAuthController : MonoBehaviour
{
    [SerializeField] private string adminUserName = "admin"; // change from inspector

    private bool adminVerified = false;
    private string pendingDeleteUser = "";

    public void RequestDelete(string userToDelete)
    {
        pendingDeleteUser = userToDelete;

        // Step 1: verify admin
        Debug.Log("Verifying admin: " + adminUserName);
        FingerprintWsClient.I?.StartVerify(adminUserName);
    }

    // Call this when device sends "verified"
    public void OnDeviceVerified(string verifiedUser)
    {
        if (verifiedUser == adminUserName)
        {
            Debug.Log("Admin verified!");

            adminVerified = true;

            // Step 2: perform delete
            if (!string.IsNullOrEmpty(pendingDeleteUser))
            {
                FingerprintWsClient.I?.DeleteUser(pendingDeleteUser);
                Debug.Log("Deleted user: " + pendingDeleteUser);
                pendingDeleteUser = "";
            }
        }
        else
        {
            Debug.Log("Non-admin verified → ignore");
        }
    }

    public void ResetAdmin()
    {
        adminVerified = false;
        pendingDeleteUser = "";
    }
}