using System;
using System.Collections;
using UnityEngine;

public class Deck : Machine
{
    [Header(nameof(Deck))]
    [SerializeField] private float _closingDelay = 1.0f;
    [SerializeField] private float _openingDelay = 0.5f;

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
        SetState(State.Drop);
    }

    /// <summary> Animation Event </summary>
    public override void InvokeDropEvent()
    {
        base.InvokeDropEvent();
        OnDroppedDown?.Invoke();
    }

    /// <summary> Animation Event </summary>
    public override void InvokeLiftEvent()
    {
        base.InvokeLiftEvent();
        OnLiftedUp?.Invoke();
    }
}
