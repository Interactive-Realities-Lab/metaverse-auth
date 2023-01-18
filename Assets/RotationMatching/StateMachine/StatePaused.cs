using IRLab.EventSystem.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatePaused : StateMachineBehaviour
{
    [Header("Events Channels")]
    [SerializeField] private VoidEventChannelSO OnSamplingPausedEnter;
    [SerializeField] private VoidEventChannelSO OnSamplingPausedExit;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnSamplingPausedEnter.Broadcast();
    }


    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnSamplingPausedExit.Broadcast();
    }
}
