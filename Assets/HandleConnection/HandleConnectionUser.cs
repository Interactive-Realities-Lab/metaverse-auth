using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UITemplate; 
public class HandleConnectionUser : MonoBehaviour
{
    private UIUserContainer currentLoadedUser;

    public UIUserContainer CurrentLoadedUser { get => currentLoadedUser; private set => currentLoadedUser = value; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void StoreCurrentUser(UIUserContainer userContainer)
    {
        CurrentLoadedUser = userContainer;
    }

    public void UserChangeState(bool value)
    {
        CurrentLoadedUser.hasConnected = value;
    }
}
