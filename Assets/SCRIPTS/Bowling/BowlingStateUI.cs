using UnityEngine;
using TMPro;

public class BowlingStateUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("State Machine")]
    [SerializeField] private BowlingUserStateMachine stateMachine;

    private void Start()
    {
        if (stateMachine == null)
        {
            stateMachine = FindFirstObjectByType<BowlingUserStateMachine>();
        }
    }

    private void LateUpdate()
    {
        if (stateText == null || stateMachine == null)
            return;

        string currentState = stateMachine.CurrentState.ToString();

        int currentFrame = Time.frameCount;

        float currentTime = Time.time;

        stateText.text =
            $"BOWLING STATE MONITOR\n\n" +
            $"State: {currentState}\n" +
            $"Frame: {currentFrame}\n" +
            $"Time: {currentTime:F4} s";
    }
}