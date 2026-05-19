using System;
using UnityEngine;

public class Pin : Interactable
{
    [Header(nameof(Pin))]
    [SerializeField] private AudioClip _pointAudio;
    [SerializeField] private Tag[] _collisionTags;

    private Vector3 _startPosition;
    private Quaternion _startRotation;

    public static event Action OnCollisionEntered;

    protected override void Awake()
    {
        base.Awake();
        _startPosition = transform.position;
        _startRotation = transform.rotation;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Deck.OnDroppedDown += OnDeckDroppedDown;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Deck.OnDroppedDown -= OnDeckDroppedDown;
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (Disabled || collision.relativeVelocity.sqrMagnitude < MinCollisionVelocity.Pow2() || !IsCollisionTagValid(collision)) return;
        base.OnCollisionEnter(collision);
        Disabled = true;
        AudioSource.Play(_pointAudio, pitch: UnityEngine.Random.Range(0.9f, 1.1f));
        OnCollisionEntered?.Invoke();
    }

    private bool IsCollisionTagValid(Collision collision)
    {
        for (int i = _collisionTags.Length - 1; i >= 0; i--)
        {
            if (_collisionTags[i].Value == collision.collider.tag) return true;
        }
        return false;
    }

    private void OnDeckDroppedDown()
    {
        Disabled = false;
        Rigidbody.angularVelocity = Rigidbody.velocity = Vector3.zero;
        transform.position = _startPosition;
        transform.rotation = _startRotation;
    }
}
