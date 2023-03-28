using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class HandleConnectionState : MonoBehaviour
{
    [SerializeField] private Animator stateMachine;
    public InputActionReference inputDisconnect = null;

    public bool IsInputHeld { get; private set; } = false;

    public float HeldValue { get; private set; }



    [SerializeField] public float HeldThreasholdValue { get; private set; } = 4f;



    private void Awake()
    {
        inputDisconnect.action.started += OnPress;
        inputDisconnect.action.canceled += OnRelease;


    }



    private void OnPress(InputAction.CallbackContext obj)
    {
        //if (!IsInputHeld)
        //{
        //    IsInputHeld = true;
        StartCoroutine("IncreaseValue");

        //}
    }

    private void OnRelease(InputAction.CallbackContext obj)
    {
        //if (!IsInputHeld) return;

        StopCoroutine("IncreaseValue");

        HeldValue = 0;
        //IsInputHeld = false;
    }

    private void Update()
    {


        if (!inputDisconnect.action.IsPressed()) return;

        if (HeldValue >= HeldThreasholdValue)
        {
            GotoDisconnectedState();
            StopCoroutine("IncreaseValue");
            HeldValue = 0;
        }
    }

    private IEnumerator IncreaseValue()
    {
        while (true)
        {
            HeldValue += Time.deltaTime;

            //Debug.Log(HeldValue);
            yield return null;
        }
    }

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
