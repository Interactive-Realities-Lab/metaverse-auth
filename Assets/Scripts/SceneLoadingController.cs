using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadingController : MonoBehaviour
{
    [SerializeField] private GameObject lobby;
    [SerializeField] private GameObject playArea;

    // Start is called before the first frame update
    void Start()
    {
        LoadLobby();
    }

    public void LoadLobby() 
    {
        lobby.SetActive(true);
        playArea.SetActive(false);
    }

    public void LoadPlayArea()
    {
        lobby.SetActive(false);
        playArea.SetActive(true);
    }


}
