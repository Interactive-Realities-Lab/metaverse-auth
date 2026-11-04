using System;
using UnityEngine;

public class ShootingUserStateMachine : MonoBehaviour
{
    public enum ShootingState
    {
        Idle,
        Reaching,
        Holding,
        Searching,
        TargetLocated,
        Aiming,
        Firing,
        PostFiring,
        GunReleased
    }

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Current State")]
    [SerializeField] private ShootingState currentState = ShootingState.Idle;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    public ShootingState CurrentState => currentState;

    public string CurrentStateName => currentState.ToString();

    public event Action<ShootingState> OnStateChanged;

    private float stateStartTime;

    public float StateDuration =>
        Time.time - stateStartTime;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        currentState = ShootingState.Idle;
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

        if (showDebugLogs)
        {
            Debug.Log(
                "[Shooting State] " +
                previousState +
                " -> " +
                currentState +
                " | Time: " +
                Time.time.ToString("F3") +
                " | Frame: " +
                Time.frameCount
            );
        }

        OnStateChanged?.Invoke(currentState);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.Play(currentState.ToString(), 0, 0f);
    }

    public bool IsInState(ShootingState state)
    {
        return currentState == state;
    }

    public void ResetStateMachine()
    {
        ChangeState(ShootingState.Idle);
    }
}