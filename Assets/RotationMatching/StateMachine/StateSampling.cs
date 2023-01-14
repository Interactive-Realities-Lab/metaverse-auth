using IRLab.EventSystem.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateSampling : StateMachineBehaviour
{
    [Header("Events Channels")]
    [SerializeField] private VoidEventChannelSO OnRotationsSamplingEnter;
    [SerializeField] private VoidEventChannelSO OnRotationsSamplingExit;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnRotationsSamplingEnter.Broadcast();
    }


    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnRotationsSamplingExit.Broadcast();
    }
}
