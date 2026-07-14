using UnityEngine;

/// <summary>
/// Spawns one target at a time at a random position inside a defined room volume.
/// When the current target is destroyed (shot), the next one spawns automatically.
/// Attach this to an empty GameObject (e.g. "TargetSpawner") in your scene.
/// </summary>
public class TargetSpawner : MonoBehaviour
{
    [Header("Target Prefab")]
    [Tooltip("The cube/target prefab to spawn. Must have the Target.cs script on it.")]
    public GameObject targetPrefab;

    [Header("Spawn Volume (room bounds)")]
    [Tooltip("Corner of the room with the smallest X, Y, Z.")]
    public Vector3 roomMin = new Vector3(-5f, 1f, -5f);

    [Tooltip("Corner of the room with the largest X, Y, Z.")]
    public Vector3 roomMax = new Vector3(5f, 3f, 5f);

    [Header("Options")]
    [Tooltip("If true, waits this many seconds before spawning the next target.")]
    public float delayBetweenSpawns = 0.5f;

    [Tooltip("Optional random rotation on spawn (visual only).")]
    public bool randomizeRotation = true;

    [Header("Spawn Sound")]
    [Tooltip("Sound played each time a new target spawns.")]
    public AudioClip spawnSound;

    [Tooltip("Volume for the spawn sound.")]
    [Range(0f, 1f)]
    public float spawnSoundVolume = 1f;

    [Header("Player Safety")]
    [Tooltip("Reference to the player/XR camera. If not set, it will try to find Camera.main automatically.")]
    public Transform player;

    [Tooltip("Targets will never spawn closer than this distance to the player.")]
    public float minDistanceFromPlayer = 1.5f;

    [Tooltip("Safety cap on how many times to retry finding a valid spawn point before giving up and using the last attempt anyway.")]
    public int maxSpawnAttempts = 30;

    private GameObject currentTarget;
    private AudioSource audioSource;

    void Awake()
    {
        // Add an AudioSource at runtime so you don't have to add one manually in the Inspector
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        if (player == null && Camera.main != null)
            player = Camera.main.transform;
    }

    void Start()
    {
        SpawnNextTarget();
    }

    public void SpawnNextTarget()
    {
        if (delayBetweenSpawns > 0f)
            Invoke(nameof(DoSpawn), delayBetweenSpawns);
        else
            DoSpawn();
    }

    private void DoSpawn()
    {
        if (targetPrefab == null)
        {
            Debug.LogError("TargetSpawner: 'Target Prefab' is not assigned (or its reference is broken). " +
                            "Drag your target prefab from the Project window into the Target Prefab slot.");
            return;
        }

        Vector3 spawnPos = GetRandomPointInRoom();

        Quaternion spawnRot = randomizeRotation
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : Quaternion.identity;

        currentTarget = Instantiate(targetPrefab, spawnPos, spawnRot);

        if (spawnSound != null)
            audioSource.PlayOneShot(spawnSound, spawnSoundVolume);

        // Hook the target's "OnHit" callback back to this spawner
        Target targetScript = currentTarget.GetComponent<Target>();
        if (targetScript != null)
        {
            targetScript.spawner = this;
        }
        else
        {
            Debug.LogWarning("Target prefab has no Target.cs component attached!");
        }
    }

    private Vector3 GetRandomPointInRoom()
    {
        // Random position anywhere inside the box defined by roomMin/roomMax,
        // retried until it's far enough from the player so targets never spawn
        // right in front of / on top of their face.
        Vector3 point = Vector3.zero;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float x = Random.Range(roomMin.x, roomMax.x);
            float y = Random.Range(roomMin.y, roomMax.y);
            float z = Random.Range(roomMin.z, roomMax.z);
            point = new Vector3(x, y, z);

            if (player == null)
                break; // no player reference, skip the distance check entirely

            float dist = Vector3.Distance(point, player.position);
            if (dist >= minDistanceFromPlayer)
                break; // good point, stop retrying
        }

        return point;
    }

    // Draws the spawn volume in the Scene view so you can line it up with your room
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = (roomMin + roomMax) / 2f;
        Vector3 size = roomMax - roomMin;
        Gizmos.DrawWireCube(center, size);
    }
}