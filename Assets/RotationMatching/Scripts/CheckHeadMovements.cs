using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckHeadMovements : MonoBehaviour
{
    [SerializeField] private Animator stateMachine;
    [SerializeField] private Transform head;
    private float rotDiff;

    [Range(0f, 1f)]
    [SerializeField] private float threashold = 1;
    private Quaternion prevRot;

    private int currSample;
    private int samplingRate = 200;
    private bool isEnabled = false;

    [SerializeField] public bool IsRotating { get; private set; }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isEnabled) return;

        var currentRot = head.rotation;

        rotDiff = Mathf.Abs( Quaternion.Dot(currentRot, prevRot));

        if (rotDiff < threashold)
        {
            if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Sampling"))
                stateMachine.SetTrigger("GotoSampling");
        }
        else
        {
            if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Paused"))
                stateMachine.SetTrigger("GotoPaused");
        }

        if (currSample % samplingRate == 0)
            prevRot = currentRot;

        currSample++;
    }

    public void DisableCheck()
    {
        isEnabled = false;
    }

    public void EnableCheck()
    {
        isEnabled = true;
    }

}
