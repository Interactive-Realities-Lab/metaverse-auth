using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    public MenuToggle menuToggle;

    public void GoHome()
    {
        SceneFlow.Instance.OpenContentScene("Authentication");

        if (menuToggle != null)
            menuToggle.CloseMenu();
    }

    public void GoLobby()
    {
        SceneFlow.Instance.OpenContentScene("Lobby");

        if (menuToggle != null)
            menuToggle.CloseMenu();
    }
}
