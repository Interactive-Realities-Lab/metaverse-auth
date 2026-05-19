using System;
using System.Collections;
using UnityEngine;

public class Station : Machine
{
    [Header(nameof(Station))]
    [SerializeField] private float _pushForce = 50.0f;
    [SerializeField] private float _breakTime = 0.1f;
    [SerializeField][Range(0.1f, 1.0f)] private float _ballHideVolume = 0.2f;
    [SerializeField] private AudioClip _ballHideAudio;
    [SerializeField] private AudioClip _ballPushAudio;
    [SerializeField] private ParticleSystem _ballHideEffect;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private AudioSource _audioSource;

    public static event Action<Transform, float> OnDroppedDown;

    protected override void OnEnable()
    {
        base.OnEnable();
        Deck.OnLiftedUp += OnBallInDeck;
        Ball.OnBlockerEvent += OnBallBlocked;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Deck.OnLiftedUp += OnBallInDeck;
        Ball.OnBlockerEvent -= OnBallBlocked;
    }

    private void OnBallBlocked(Ball ball)
    {
        OnBallInDeck();
        _ballHideEffect.transform.position = ball.transform.position;
        _ballHideEffect.Play(true);
        _audioSource.Play(_ballHideAudio, _ballHideVolume);
    }

    private void OnBallInDeck()
    {
        SetState(State.Drop);
    }

    protected override IEnumerator OnDropState()
    {
        TriggerAnimation();
        yield break;
    }

    protected override IEnumerator OnLiftState()
    {
        yield return WaitForSeconds(_breakTime);
        TriggerAnimation();
    }

    /// <summary> Animation Event </summary>
    public override void InvokeDropEvent()
    {
        base.InvokeDropEvent();
        _audioSource.Play(_ballPushAudio);
        OnDroppedDown?.Invoke(_startPoint, _pushForce);
    }
}
