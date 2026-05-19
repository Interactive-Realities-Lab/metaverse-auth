using System.Text;
using UnityEngine;
using TMPro;

public class Scoreboard : MonoBehaviour
{
    [SerializeField] private string _title;
    [SerializeField] private int _stringBuffer = 50;
    [SerializeField] private TextMeshProUGUI _text;

    private int _score;
    private StringBuilder _stringBuilder;

    private void Awake()
    {
        _stringBuilder = new StringBuilder(_stringBuffer);
        UpdateText();
    }

    private void OnEnable()
    {
        Pin.OnCollisionEntered += UpdateScore;
    }

    private void OnDisable()
    {
        Pin.OnCollisionEntered -= UpdateScore;
    }

    private void UpdateScore()
    {
        _score++;
        UpdateText();
    }

    private void UpdateText()
    {
        _text.text = _stringBuilder.Clear().Append(_title).Append('\n').Append(_score).ToString();
    }
}
