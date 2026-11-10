using System;
using UnityEngine;

public class ShootingUserStateMachine : MonoBehaviour
{
    public enum ShootingState
    {
        Idle,
        Reaching,
        HoldingIdle,
        Searching,
        TargetLocated,
        Aiming,
        Firing,
        PostFiring
    }

    [Header("Animator")]
    [SerializeField] private Animator animator;

    private ShootingState currentState = ShootingState.Idle;
    private float stateStartTime;

    public ShootingState CurrentState => currentState;
    public string CurrentStateName => currentState.ToString();
    public float StateDuration => Time.time - stateStartTime;

    public event Action<ShootingState> OnStateChanged;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        stateStartTime = Time.time;
    }

    private void Start()
    {
        UpdateAnimator();
    }

    public void ChangeState(ShootingState newState)
    {
        if (currentState == newState)
            return;

        ShootingState previousState = currentState;

        currentState = newState;
        stateStartTime = Time.time;

        UpdateAnimator();

        OnStateChanged?.Invoke(currentState);

        Debug.Log(
            "[SHOOTING STATE] " +
            previousState + " -> " + currentState +
            " | Frame: " + Time.frameCount
        );
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        string statePath;

        switch (currentState)
        {
            case ShootingState.Idle:
                statePath = "Base Layer.GunNotHeld.Idle";
                break;

            case ShootingState.Reaching:
                statePath = "Base Layer.GunNotHeld.Reaching";
                break;

            case ShootingState.HoldingIdle:
                statePath = "Base Layer.GunHeld.HoldingIdle";
                break;

            case ShootingState.Searching:
                statePath = "Base Layer.GunHeld.Searching";
                break;

            case ShootingState.TargetLocated:
                statePath = "Base Layer.GunHeld.TargetLocated";
                break;

            case ShootingState.Aiming:
                statePath = "Base Layer.GunHeld.Aiming";
                break;

            case ShootingState.Firing:
                statePath = "Base Layer.GunHeld.Firing";
                break;

            case ShootingState.PostFiring:
                statePath = "Base Layer.GunHeld.PostFiring";
                break;

            default:
                return;
        }

        animator.Play(statePath, 0, 0f);
    }
}