using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecideAuthPhase : MonoBehaviour
{

    [SerializeField] private GameObject totpPhase;
    [SerializeField] private GameObject rotationMatchingPhase;
    [SerializeField] private HandleConnectionUser connectionUser;


    public void GotoPhase()
    {
        if(!connectionUser.CurrentLoadedUser.hasConnected)
            totpPhase.SetActive(true);
        else
            rotationMatchingPhase.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
