using System;
using System.Collections;
using UnityEngine;

public class Deck : Machine
{
    [Header(nameof(Deck))]
    [SerializeField] private float _closingDelay = 1.0f;
    [SerializeField] private float _openingDelay = 0.5f;

    [Header("Official Bowling Scoring")]
    [SerializeField] private BowlingScoreManager _scoreManager;
    [SerializeField] private Pin[] _pins;

    public static event Action OnDroppedDown;
    public static event Action OnLiftedUp;

    protected override void OnEnable()
    {
        base.OnEnable();

        Ball.OnDeckEvent += DropDown;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        Ball.OnDeckEvent -= DropDown;
    }

    protected override IEnumerator OnDropState()
    {
        yield return WaitForSeconds(_closingDelay);
        TriggerAnimation();
    }

    protected override IEnumerator OnLiftState()
    {
        yield return WaitForSeconds(_openingDelay);
        TriggerAnimation();
    }

    private void DropDown(Ball _)
    {
        if (_scoreManager != null && _scoreManager.GameOver)
            return;

        SetState(State.Drop);
    }

    /// <summary>
    /// Animation Event:
    /// Called when deck finishes dropping.
    /// </summary>
    public override void InvokeDropEvent()
    {
        base.InvokeDropEvent();

        OnDroppedDown?.Invoke();
    }

    /// <summary>
    /// Animation Event:
    /// Called when deck finishes lifting.
    /// </summary>
    public override void InvokeLiftEvent()
    {
        base.InvokeLiftEvent();

        OnLiftedUp?.Invoke();
    }

    private void ProcessBowlingRoll()
    {
        if (_scoreManager == null || _pins == null)
            return;

        int fallenPins = 0;

        foreach (Pin pin in _pins)
        {
            if (pin != null && pin.HasFallen)
                fallenPins++;
        }

        int knockedDownThisRoll =
            _scoreManager.PinsStanding - (10 - fallenPins);

        knockedDownThisRoll = Mathf.Clamp(
            knockedDownThisRoll,
            0,
            _scoreManager.PinsStanding
        );

        Debug.Log(
            "Frame: " + _scoreManager.CurrentFrame +
            " | Knocked down this roll: " + knockedDownThisRoll
        );

        _scoreManager.RegisterRoll(knockedDownThisRoll);
    }
}