using System;
using UnityEngine;

public class Ball : Interactable
{
    [Header(nameof(Ball))]
    [SerializeField] private Tag _floorTag;
    [SerializeField] private float _maxVelocity = 20.0f;
    [SerializeField] private AudioClip _floorAudio;
    [SerializeField] private AnimationCurve _volumeCurve;
    [SerializeField] private AnimationCurve _pitchCurve;

    private int _collisionCount;

    public static event Action<Ball> OnDeckEvent;
    public static event Action<Ball> OnBlockerEvent;

    protected override void OnEnable()
    {
        base.OnEnable();
        Deck.OnDroppedDown += OnDeckDroppedDown;
        Station.OnDroppedDown += OnStationDroppedDown;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Deck.OnDroppedDown -= OnDeckDroppedDown;
        Station.OnDroppedDown -= OnStationDroppedDown;
    }

    protected override void FixedUpdate()
    {
        if (Disabled || _collisionCount == 0) return;
        base.FixedUpdate();
        float normalizedVelocity = Rigidbody.velocity.magnitude.Remap(0.0f, _maxVelocity, 0.0f, 1.0f);
        AudioSource.volume = _volumeCurve.Evaluate(normalizedVelocity);
        AudioSource.pitch = _pitchCurve.Evaluate(normalizedVelocity);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (collision.collider.tag == _floorTag.Value)
        {
            if (_collisionCount == 0) AudioSource.Play(_floorAudio, volume: 0.0f, loop: true);
            _collisionCount++;
        }
    }

    protected override void OnCollisionExit(Collision collision)
    {
        base.OnCollisionExit(collision);
        if (collision.collider.tag == _floorTag.Value)
        {
            _collisionCount--;
            if (_collisionCount == 0) AudioSource.Stop();
        }
    }

    protected override void OnTriggerEnter(Collider collider)
    {
        if (Disabled) return;
        base.OnTriggerEnter(collider);
        if (collider.TryGetComponent(out Trigger trigger))
        {
            Disabled = true;
            GetEventAction(trigger.EventType)?.Invoke();
        }
    }

    private Action GetEventAction(EventType eventType)
    {
        switch (eventType)
        {
            case EventType.Deck: return InvokeDeckEvent;
            case EventType.Blocker: return InvokeBlockerEvent;
            default: return null;
        }
    }

    private void InvokeDeckEvent()
    {
        OnDeckEvent?.Invoke(this);
    }

    private void InvokeBlockerEvent()
    {
        Renderer.enabled = false;
        Rigidbody.isKinematic = true;
        OnBlockerEvent?.Invoke(this);
    }

    private void OnDeckDroppedDown()
    {
        Rigidbody.isKinematic = true;
    }

    private void OnStationDroppedDown(Transform startPoint, float pushForce)
    {
        transform.position = startPoint.position;
        Disabled = false;
        Renderer.enabled = true;
        Rigidbody.isKinematic = false;
        Rigidbody.AddForce(Vector3.up * pushForce, ForceMode.Impulse);
    }
}
