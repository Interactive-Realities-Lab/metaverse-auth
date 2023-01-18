using IRLab.EventSystem.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateDisconnected : StateMachineBehaviour
{
    [Header("Events Channels")]
    [SerializeField] private VoidEventChannelSO OnDisconnectedEnter;
    [SerializeField] private VoidEventChannelSO OnDisconnectedExit;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnDisconnectedEnter.Broadcast();
    }


    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnDisconnectedExit.Broadcast();
    }
}
