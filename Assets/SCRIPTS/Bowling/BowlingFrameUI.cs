using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BowlingFrameUI : MonoBehaviour
{
    [Header("Frame UI")]
    [SerializeField] private TextMeshProUGUI frameNumberText;
    [SerializeField] private TextMeshProUGUI rollText;
    [SerializeField] private TextMeshProUGUI cumulativeScoreText;

    public void UpdateFrame(
        int frameIndex,
        List<int> rolls,
        int? cumulativeScore)
    {
        frameNumberText.text = (frameIndex + 1).ToString();

        rollText.text = FormatRolls(frameIndex, rolls);

        cumulativeScoreText.text =
            cumulativeScore.HasValue
                ? cumulativeScore.Value.ToString()
                : "-";
    }

    private string FormatRolls(int frameIndex, List<int> rolls)
    {
        if (rolls == null || rolls.Count == 0)
            return "- -";

        List<string> symbols = new List<string>();

        if (frameIndex < 9)
        {
            if (rolls[0] == 10)
            {
                return "X";
            }

            symbols.Add(FormatPinCount(rolls[0]));

            if (rolls.Count >= 2)
            {
                symbols.Add(
                    rolls[0] + rolls[1] == 10
                        ? "/"
                        : FormatPinCount(rolls[1])
                );
            }
            else
            {
                symbols.Add("-");
            }
        }
        else
        {
            // Tenth frame can contain up to three rolls.
            for (int i = 0; i < rolls.Count; i++)
            {
                int pins = rolls[i];

                if (i == 0)
                {
                    symbols.Add(FormatPinCount(pins));
                }
                else if (i == 1)
                {
                    if (rolls[0] != 10 &&
                        rolls[0] + pins == 10)
                    {
                        symbols.Add("/");
                    }
                    else
                    {
                        symbols.Add(FormatPinCount(pins));
                    }
                }
                else
                {
                    bool spareOnBonus =
                        rolls[0] == 10 &&
                        rolls[1] != 10 &&
                        rolls[1] + pins == 10;

                    symbols.Add(
                        spareOnBonus ? "/" : FormatPinCount(pins)
                    );
                }
            }

            while (symbols.Count < 3)
                symbols.Add("-");
        }

        return string.Join(" ", symbols);
    }

    private string FormatPinCount(int pins)
    {
        if (pins == 10)
            return "X";

        if (pins == 0)
            return "-";

        return pins.ToString();
    }
}