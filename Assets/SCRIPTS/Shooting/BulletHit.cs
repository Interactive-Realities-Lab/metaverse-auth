using UnityEngine;

public class BulletHit : MonoBehaviour
{
    [Header("Hit Effect (any GameObject/prefab, no Particle System required)")]
    [Tooltip("Any prefab you want to appear on hit — a sprite, a flash quad, an animated object, etc. Leave empty for no visual effect.")]
    public GameObject hitEffectPrefab;

    [Tooltip("How long the hit effect stays before being destroyed (seconds).")]
    public float hitEffectLifetime = 1f;

    [Header("Hit Sound (optional)")]
    public AudioClip hitSound;
    [Range(0f, 1f)] public float hitSoundVolume = 1f;

    private GameObject xrCamera;

    void Start()
    {
        // Finds the XR camera automatically from the main scene
        xrCamera = Camera.main.gameObject;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            Debug.Log("Bullet!!");

            Vector3 hitPos = other.gameObject.transform.position;
            Destroy(other.gameObject);

            // Spawn the visual hit effect, if one is assigned
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);

                if (xrCamera != null)
                    effect.transform.LookAt(xrCamera.transform);

                Destroy(effect, hitEffectLifetime);
            }

            // Play the hit sound, if one is assigned
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, hitPos, hitSoundVolume);
            }
        }
    }
}