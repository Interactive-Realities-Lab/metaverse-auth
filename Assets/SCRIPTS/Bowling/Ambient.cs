using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Ambient : MonoBehaviour
{
    [SerializeField] private float _targetVolume = 0.2f;
    [SerializeField] private float _smoothTime = 1.0f;
    [SerializeField] private float _epsilon = 0.02f;

    private bool _started;
    private float _velocity;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.volume = 0.0f;
        _audioSource.loop = true;
        _audioSource.Play();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        //Loader.OnFullFade += StartTransition;
    }

    private void OnDisable()
    {
        //Loader.OnFullFade -= StartTransition;
    }

    private IEnumerator Start()
    {
        while (!_started) yield return null;
        yield return OnTransition();
    }

    public void StartTransition()
    {
        _started = true;
    }

    private IEnumerator OnTransition()
    {
        _audioSource.time = 0.0f;
        while (Mathf.Abs(_audioSource.volume - _targetVolume) > _epsilon)
        {
            _audioSource.volume = Mathf.SmoothDamp(_audioSource.volume, _targetVolume, ref _velocity, _smoothTime);
            yield return null;
        }
        _audioSource.volume = _targetVolume;
    }
}
