using System.Collections.Generic;
using UnityEngine;

public class BowlingRollController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BowlingScoreManager scoreManager;
    [SerializeField] private Pin[] pins;

    // Tracks pins already counted during the current rack.
    private readonly HashSet<Pin> countedPins = new();

    private bool rackWasReset;

    private void OnEnable()
    {
        Deck.OnDroppedDown += HandleRollEnded;
    }

    private void OnDisable()
    {
        Deck.OnDroppedDown -= HandleRollEnded;
    }

    private void Start()
    {
        if (scoreManager != null)
        {
            scoreManager.OnRackResetRequired += ResetAllPins;

            // Prepare the initial rack.
            ResetAllPins();
        }
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnRackResetRequired -= ResetAllPins;
        }
    }

    private void HandleRollEnded()
    {
        if (scoreManager == null || scoreManager.GameOver)
            return;

        int newlyFallen = 0;

        foreach (Pin pin in pins)
        {
            if (pin == null)
                continue;

            if (pin.HasFallen && !countedPins.Contains(pin))
            {
                countedPins.Add(pin);
                newlyFallen++;
            }
        }

        Debug.Log(
            "BOWLING ROLL COMPLETE | Frame: " +
            scoreManager.CurrentFrame +
            " | Newly Fallen: " + newlyFallen
        );

        rackWasReset = false;

        // Record this roll using official bowling rules.
        scoreManager.RegisterRoll(newlyFallen);

        // If the frame is not finished, remove fallen pins
        // and keep the remaining pins for the next roll.
        if (!scoreManager.GameOver && !rackWasReset)
        {
            RemoveFallenPins();
        }
    }

    private void RemoveFallenPins()
    {
        foreach (Pin pin in pins)
        {
            if (pin != null && pin.HasFallen)
            {
                pin.gameObject.SetActive(false);
            }
        }

        Debug.Log("BOWLING | Fallen pins removed");
    }

    public void ResetAllPins()
    {
        rackWasReset = true;

        foreach (Pin pin in pins)
        {
            if (pin == null)
                continue;

            pin.gameObject.SetActive(true);
            pin.ResetPin();
        }

        countedPins.Clear();

        Debug.Log("BOWLING | Full rack reset");
    }
}