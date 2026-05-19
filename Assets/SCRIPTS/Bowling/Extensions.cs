using UnityEngine;

public static class Extensions
{
    public static float Pow2(this float value)
    {
        return Mathf.Pow(value, 2);
    }

    public static float Remap(this float value, float inputFrom, float inputTo, float outputFrom, float outputTo)
    {
        return (value - inputFrom) / (inputTo - inputFrom) * (outputTo - outputFrom) + outputFrom;
    }

    public static void Play(this AudioSource audioSource, AudioClip audioClip, float volume = 1.0f, bool loop = false, float pitch = 1.0f, float delay = 0.0f, float spatialBlend = 0.0f)
    {
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = spatialBlend;
        audioSource.PlayDelayed(delay);
    }
}
