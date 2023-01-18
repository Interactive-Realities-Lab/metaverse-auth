using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IRLab.EventSystem.Event;


public class StateConnected : StateMachineBehaviour
{

    [Header("Events Channels")]
    [SerializeField] private VoidEventChannelSO OnConnectedEnter;
    [SerializeField] private VoidEventChannelSO OnConnectedExit;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnConnectedEnter.Broadcast();
    }


    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnConnectedExit.Broadcast();
    }

}
