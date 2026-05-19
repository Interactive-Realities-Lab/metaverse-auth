using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public abstract class Machine : MonoBehaviour
{
    [Header(nameof(Machine))]
    [SerializeField] private float _speed = 1.0f;

    private State _state;
    private Animator _animator;

    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.speed = _speed;
    }

    protected virtual void OnEnable() { }

    protected virtual void OnDisable() { }

    private IEnumerator Start()
    {
        while (true)
        {
            switch (_state)
            {
                case State.Drop:
                    yield return OnDropState();
                    break;
                case State.Lift:
                    yield return OnLiftState();
                    break;
                default:
                    yield return null;
                    break;
            }
        }
    }

    protected virtual IEnumerator OnDropState() { yield break; }

    protected virtual IEnumerator OnLiftState() { yield break; }

    protected void SetState(State state) => _state = state;

    protected void TriggerAnimation() => _animator.SetInteger(nameof(State), (int)_state);

    protected IEnumerator WaitForSeconds(float time)
    {
        for (float i = 0.0f; i < time; i += Time.deltaTime)
            yield return null;
    }

    /// <summary> Animation Event </summary>
    public virtual void InvokeDropEvent()
    {
        SetState(State.Lift);
    }

    /// <summary> Animation Event </summary>
    public virtual void InvokeLiftEvent()
    {
        SetState(State.Idle);
    }

    protected enum State
    {
        Idle = 0,
        Drop = 1,
        Lift = 2,
    }
}
