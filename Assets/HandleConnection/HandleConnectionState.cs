using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleConnectionState : MonoBehaviour
{
    [SerializeField] private Animator stateMachine;


    public void GotoConnectedState()
    {
        if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Connected"))
            stateMachine.SetTrigger("GotoConnected");
    }


    public void GotoDisconnectedState()
    {
        if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Disconnected"))
            stateMachine.SetTrigger("GotoDisconnected");
    }

}
