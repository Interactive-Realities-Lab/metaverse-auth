using UnityEngine;
using TMPro;

public class Scoreboard : MonoBehaviour
{
    [Header("Official Bowling Scoring")]
    [SerializeField] private BowlingScoreManager _scoreManager;

    [Header("Frame UI")]
    [SerializeField] private BowlingFrameUI[] _frameUI;

    [Header("Optional Total Score")]
    [SerializeField] private TextMeshProUGUI _totalScoreText;

    // Preserved for BowlingDataLogger.cs
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

        if (_totalScoreText != null)
        {
            _totalScoreText.text =
                "TOTAL: " + CurrentScore;
        }
    }
}