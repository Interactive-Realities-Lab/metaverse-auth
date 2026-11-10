using UnityEngine;
using TMPro;

public class Scoreboard : MonoBehaviour
{
    [Header("Official Bowling Scoring")]
    [SerializeField] private BowlingScoreManager _scoreManager;

    [Header("Frame UI")]
    [SerializeField] private BowlingFrameUI[] _frameUI;

    [Header("Game Completion Message")]
    [SerializeField] private TextMeshProUGUI _gameCompletedText;

    // Keep this for BowlingDataLogger.cs
    public int CurrentScore =>
        _scoreManager != null
            ? _scoreManager.GetTotalScore()
            : 0;

    private void Start()
    {
        UpdateScoreboard();
    }

    private void OnEnable()
    {
        if (_scoreManager != null)
            _scoreManager.OnScoreChanged += UpdateScoreboard;
    }

    private void OnDisable()
    {
        if (_scoreManager != null)
            _scoreManager.OnScoreChanged -= UpdateScoreboard;
    }

    private void UpdateScoreboard()
    {
        if (_scoreManager == null)
            return;

        // Update all 10 frames
        for (int i = 0; i < 10; i++)
        {
            if (_frameUI == null ||
                i >= _frameUI.Length ||
                _frameUI[i] == null)
                continue;

            _frameUI[i].UpdateFrame(
                i,
                _scoreManager.GetFrameRolls(i),
                _scoreManager.GetCumulativeScoreIfKnown(i)
            );
        }

        // Show completion message only when game finishes
        if (_gameCompletedText != null)
        {
            _gameCompletedText.text =
                _scoreManager.GameOver
                    ? "GAME COMPLETED!"
                    : "";

        }
    }
}