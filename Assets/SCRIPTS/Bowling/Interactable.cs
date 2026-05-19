using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Renderer))]
public abstract class Interactable : MonoBehaviour
{
    [Header(nameof(Interactable))]
    [SerializeField] private float _minCollisionVelocity = 1.0f;
    [SerializeField] private float _maxAngularVelocity = 7.0f;
    [SerializeField] private AudioSource _audioSource;

    protected bool Disabled { get; set; }
    protected Renderer Renderer { get; private set; }
    protected Rigidbody Rigidbody { get; private set; }
    protected AudioSource AudioSource => _audioSource;
    protected float MinCollisionVelocity => _minCollisionVelocity;

    protected virtual void Awake()
    {
        Renderer = GetComponent<Renderer>();
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.maxAngularVelocity = _maxAngularVelocity;
    }

    protected virtual void OnEnable() { }

    protected virtual void OnDisable() { }

    protected virtual void Start() { }

    protected virtual void FixedUpdate() { }

    protected virtual void OnTriggerEnter(Collider _) { }

    protected virtual void OnCollisionEnter(Collision _) { }

    protected virtual void OnCollisionExit(Collision _) { }

    protected virtual void OnCollisionStay(Collision _) { }
}
