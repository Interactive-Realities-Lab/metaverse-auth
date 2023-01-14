using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IRLab.EventSystem.Event;


public class StateMatched : StateMachineBehaviour
{

    [Header("Events Channels")]
    [SerializeField] private VoidEventChannelSO OnRotationsMatchedEnter;
    [SerializeField] private VoidEventChannelSO OnRotationsMatchedExit;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnRotationsMatchedEnter.Broadcast();
    }


    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnRotationsMatchedExit.Broadcast();
    }

}
