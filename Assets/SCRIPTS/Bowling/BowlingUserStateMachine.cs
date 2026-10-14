using UnityEngine;

public class BowlingUserStateMachine : MonoBehaviour
{
    public enum UserState
    {
        Idle,
        Reaching,
        Grabbing,
        Holding,
        Throwing,
        PostThrow
    }

    [Header("Animator")]
    [SerializeField] private Animator animator;

    public UserState CurrentState { get; private set; } = UserState.Idle;

    private void Start()
    {
    CurrentState = UserState.Idle;
    animator.Play("Idle", 0, 0f);

    Debug.Log("USER STATE -> Idle");
    }

    public void ChangeState(UserState newState)
    {
    if (CurrentState == newState)
        return;

    CurrentState = newState;

    animator.Play(newState.ToString(), 0, 0f);

    Debug.Log($"USER STATE -> {CurrentState}");
    }

    // These public methods can be called by XR events/scripts

    public void StartReaching()
    {
        ChangeState(UserState.Reaching);
    }

    public void StartGrabbing()
    {
        ChangeState(UserState.Grabbing);
    }

    public void StartHolding()
    {
        ChangeState(UserState.Holding);
    }

    public void StartThrowing()
    {
        ChangeState(UserState.Throwing);
    }

    public void StartPostThrow()
    {
        ChangeState(UserState.PostThrow);
    }

    public void ReturnToIdle()
    {
        ChangeState(UserState.Idle);
    }

    // Useful for your data logger
    public string GetCurrentStateName()
    {
        return CurrentState.ToString();
    }
}