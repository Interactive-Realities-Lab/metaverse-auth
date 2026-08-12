using UnityEngine;
using UnityEngine.UI;

public class UnlockDoor : MonoBehaviour
{
    [Header("Knight Puzzle")]
    public Image knightImage;
    public GameObject knightPiece;

    [Header("Battery Puzzle")]
    public GameObject battery;
    public Collider batteryTargetCollider;

    public Renderer rotorRenderer;
    public Color batteryCompletedColor = Color.green;
    public AudioClip batteryCompletedSound;

    [Header("UI")]
    public GameObject lockedText;
    public GameObject unlockedText;

    [Header("Door")]
    public GameObject door;

    [Header("Sound")]
    public AudioClip unlockSound;

    private Collider imageCollider;
    private Collider knightCollider;
    private Collider batteryCollider;
    private BoxCollider doorCollider;

    private bool knightCompleted = false;
    private bool batteryCompleted = false;
    private bool doorUnlocked = false;

    void Awake()
    {
        imageCollider = knightImage.GetComponent<Collider>();
        knightCollider = knightPiece.GetComponent<Collider>();

        batteryCollider = battery.GetComponent<Collider>();

        doorCollider = door.GetComponent<BoxCollider>();
    }

    void FixedUpdate()
    {
        // Check knight puzzle
        if (!knightCompleted &&
            knightCollider.bounds.Intersects(imageCollider.bounds))
        {
            knightCompleted = true;

            Debug.Log("Knight task completed!");

            knightImage.enabled = false;
        }

        // Check battery puzzle
        if (!batteryCompleted &&
            batteryCollider.bounds.Intersects(batteryTargetCollider.bounds))
    {
        batteryCompleted = true;

        Debug.Log("Battery task completed!");

        // Change Rotor color to green
        if (rotorRenderer != null)
        {
            rotorRenderer.material.color = batteryCompletedColor;
        }

        // Play battery success sound
        if (batteryCompletedSound != null)
        {
            AudioSource.PlayClipAtPoint(
                batteryCompletedSound,
                batteryTargetCollider.transform.position
            );
        }
    }

        // Only unlock when BOTH are complete
        if (knightCompleted && batteryCompleted && !doorUnlocked)
        {
            UnlockTheDoor();
        }
    }

    private void UnlockTheDoor()
    {
        doorUnlocked = true;

        Debug.Log("Both puzzles completed. Door unlocked!");

        if (unlockSound != null)
        {
            AudioSource.PlayClipAtPoint(unlockSound, transform.position);
        }

        if (lockedText != null)
            lockedText.SetActive(false);

        if (unlockedText != null)
            unlockedText.SetActive(true);

        if (doorCollider != null)
            doorCollider.enabled = true;

        OpenDoor openDoor = door.GetComponent<OpenDoor>();

        if (openDoor != null)
        {
            openDoor.Open();
        }
    }
}