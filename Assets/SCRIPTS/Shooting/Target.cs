using UnityEngine;

/// <summary>
/// Attach this to your target cube prefab.
/// Call Hit() from your shooting/raycast script when the player shoots this target,
/// OR let it auto-detect bullet collisions (see OnCollisionEnter / OnTriggerEnter below).
/// </summary>
public class Target : MonoBehaviour
{
    [HideInInspector]
    public TargetSpawner spawner;

    [Header("Optional effects")]
    //public ParticleSystem hitVfx;       // small particle burst
    public AudioClip hitSound;         

    private bool alreadyHit = false;

    /// <summary>
    /// Call this from your gun/raycast script when this target is shot.
    /// Example (in your shooting script):
    ///   if (Physics.Raycast(ray, out hit)) {
    ///       Target t = hit.collider.GetComponent<Target>();
    ///       if (t != null) t.Hit();
    ///   }
    /// </summary>
    public void Hit()
    {
        if (alreadyHit) return; // prevent double-triggering
        alreadyHit = true;

        // if (hitVfx != null)
        //     Instantiate(hitVfx, transform.position, Quaternion.identity);

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position);

        // Tell the spawner to spawn the next target
        if (spawner != null)
            spawner.SpawnNextTarget();

        Destroy(gameObject);
    }

    // --- OPTIONAL AUTO-DETECTION ---
    // If your bullets are physical projectiles tagged "Bullet" with a Rigidbody,
    // this will trigger Hit() automatically on collision. Delete if you're using
    // raycast shooting and calling Hit() manually instead.

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Bullet"))
        {
            Hit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Hit();
        }
    }
}
