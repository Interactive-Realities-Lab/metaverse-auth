using System;
using UnityEngine;

public class Pin : Interactable
{
    [Header(nameof(Pin))]
    [SerializeField] private AudioClip _pointAudio;
    [SerializeField] private Tag[] _collisionTags;

    [Header("Official Bowling Scoring")]
    [SerializeField] private float _fallAngleThreshold = 45f;
    [SerializeField] private float _fallDistanceThreshold = 0.3f;

    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private bool _hasFallen;

    public bool HasFallen => _hasFallen;

    // Kept for compatibility with existing scripts.
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
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        if (Disabled ||
            collision.relativeVelocity.sqrMagnitude <
            MinCollisionVelocity.Pow2() ||
            !IsCollisionTagValid(collision))
            return;

        base.OnCollisionEnter(collision);

        Disabled = true;

        if (AudioSource != null && _pointAudio != null)
        {
            AudioSource.Play(
                _pointAudio,
                pitch: UnityEngine.Random.Range(0.9f, 1.1f)
            );
        }

        // Do not increment score here.
        // OnCollisionEntered?.Invoke();
    }

    private void Update()
    {
        if (_hasFallen)
            return;

        CheckIfFallen();
    }

    private void CheckIfFallen()
    {
        // Angle between the pin's current up direction
        // and the world's vertical direction.
        float angle = Vector3.Angle(
            transform.up,
            Vector3.up
        );

        // Horizontal displacement from starting position.
        Vector3 displacement = transform.position - _startPosition;
        displacement.y = 0f;

        float horizontalDistance = displacement.magnitude;

        if (angle >= _fallAngleThreshold ||
            horizontalDistance >= _fallDistanceThreshold)
        {
            _hasFallen = true;

            Debug.Log($"{gameObject.name} has fallen.");
        }
    }

    private bool IsCollisionTagValid(Collision collision)
    {
        for (int i = _collisionTags.Length - 1; i >= 0; i--)
        {
            if (_collisionTags[i].Value == collision.collider.tag)
                return true;
        }

        return false;
    }

    public void ResetPin()
    {
        Disabled = false;

        _hasFallen = false;

        Rigidbody.angularVelocity = Vector3.zero;
        Rigidbody.linearVelocity = Vector3.zero;

        transform.position = _startPosition;
        transform.rotation = _startRotation;
    }
}