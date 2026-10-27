using System.Text;
using UnityEngine;
using TMPro;

public class Scoreboard : MonoBehaviour
{
    [Header("Scoreboard Settings")]
    [SerializeField] private string _title = "BOWLING SCORE";
    [SerializeField] private int _stringBuffer = 200;
    [SerializeField] private TextMeshProUGUI _text;

    [Header("Official Bowling Scoring")]
    [SerializeField] private BowlingScoreManager _scoreManager;

    private StringBuilder _stringBuilder;

    // Keep this property for your BowlingDataLogger.cs
    public int CurrentScore =>
        _scoreManager != null ? _scoreManager.GetTotalScore() : 0;

    private void Awake()
    {
        _stringBuilder = new StringBuilder(_stringBuffer);
    }

    private void OnEnable()
    {
        if (_scoreManager != null)
            _scoreManager.OnScoreChanged += UpdateText;

        UpdateText();
    }

    private void OnDisable()
    {
        if (_scoreManager != null)
            _scoreManager.OnScoreChanged -= UpdateText;
    }

    private void UpdateText()
    {
        if (_text == null || _scoreManager == null)
            return;

        _stringBuilder.Clear();

        // Force left alignment for the score rows.
        _stringBuilder.Append("<align=left>");

        // Rounds 1–5 on the left.
        // Rounds 6–10 on the right.
        for (int i = 0; i < 5; i++)
        {
            _stringBuilder.Append("<pos=10%>");
            AppendRound(i);

            _stringBuilder.Append("<pos=55%>");
            AppendRound(i + 5);

            _stringBuilder.AppendLine();
        }

        _stringBuilder.Append("</align>");

        // Total score centered.
       // _stringBuilder.AppendLine();

        _stringBuilder.Append("<align=center>");
        _stringBuilder.Append("TOTAL SCORE: ");
        _stringBuilder.Append(CurrentScore);
        _stringBuilder.Append("</align>");

        if (_scoreManager.GameOver)
        {
            _stringBuilder.AppendLine();
            _stringBuilder.Append(
                "<align=center>GAME FINISHED!</align>"
            );
        }

        _text.text = _stringBuilder.ToString();
    }

    private void AppendRound(int i)
    {
        _stringBuilder.Append("Round ");
        _stringBuilder.Append(i + 1);
        _stringBuilder.Append(": ");

        var rolls = _scoreManager.GetFrameRolls(i);

        if (rolls.Count == 0)
        {
            _stringBuilder.Append("-");
        }
        else
        {
            for (int j = 0; j < rolls.Count; j++)
            {
                int pins = rolls[j];

                if (j > 0)
                    _stringBuilder.Append(" ");

                if (pins == 10)
                {
                    _stringBuilder.Append("X");
                }
                else if (j == 1 &&
                        rolls[0] != 10 &&
                        rolls[0] + pins == 10)
                {
                    _stringBuilder.Append("/");
                }
                else
                {
                    _stringBuilder.Append(pins);
                }
            }
        }

        _stringBuilder.Append(" | ");

        int? cumulativeScore =
            _scoreManager.GetCumulativeScoreIfKnown(i);

        if (cumulativeScore.HasValue)
            _stringBuilder.Append(cumulativeScore.Value);
        else
            _stringBuilder.Append("-");
    }
}