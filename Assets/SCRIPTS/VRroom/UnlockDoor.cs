using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

/// <summary>   
/// This script keeps the door locked until the player solves the puzzle, which will unlock the door.
/// </summary>
public class UnlockDoor : MonoBehaviour
{
    /// <summary>   
    /// The image to display on the console. A clue for the player.
    /// </summary>
    public Image knightImage;

    /// <summary>   
    /// The GameObject the player needs to hold over the console to unlock the door.
    /// </summary>
    public GameObject knightPiece;

    /// <summary>   
    /// Text that appears on the console when the door is still locked.
    /// </summary>
    public GameObject lockedText;

    /// <summary>   
    /// Text that appears on the console when the door has been unlocked.
    /// </summary>
    public GameObject unlockedText;

    /// <summary>   
    /// The locked door GameObject.
    /// </summary>
    public GameObject door;

    /// <summary>   
    /// The sound that plays once the chess piece is held over the console and the door is unlocked.
    /// </summary>
    public AudioSource unlockSound;

    /// <summary>   
    /// The trigger to play the door unlocking sound.
    /// </summary>
    bool playSound = true;

    /// <summary>   
    /// The area above the console that the chess piece needs to enter in order to unlock the door.
    /// </summary>
    CapsuleCollider imageCollider;

    /// <summary>   
    /// The collider for the chess piece that is held over the console.
    /// </summary>
    BoxCollider knightCollider;

    /// <summary>   
    /// The area the player needs to be within to open the door.
    /// </summary>
    BoxCollider doorCollider;

    /// <summary>   
    /// The audio source for the door unlocking sound.
    /// </summary>
    AudioSource unlockSoundAudioSource;

    /// <summary>   
    /// Method that instantiates variables with associated components.
    /// </summary>   
    void Awake()
    {
        imageCollider = knightImage.GetComponent<CapsuleCollider>();
        knightCollider = knightPiece.GetComponent<BoxCollider>();
        doorCollider = door.GetComponent<BoxCollider>();
        if (unlockSound != null)
            unlockSoundAudioSource = unlockSound.GetComponent<AudioSource>();
    }

    /// <summary>   
    /// Code that constantly checks if the chess piece has crossed the bounds of the console's collider.
    /// </summary>
    void FixedUpdate()
    {
        if (knightCollider.bounds.Intersects(imageCollider.bounds))
            doorUnlock();
    }

    /// <summary>   
    /// Method that unlocks the door if conditions in FixedUpdate() are met.
    /// </summary>
    void doorUnlock()
    {
        if (playSound)
        {
            unlockSoundAudioSource.Play();
            playSound = false;
        }
        lockedText.SetActive(false);
        knightImage.enabled = false;
        unlockedText.SetActive(true);
        // Door is unlocked by turning on its collider component
        doorCollider.enabled = true;
               
        OpenDoor openDoor = door.GetComponent<OpenDoor>();

        if (openDoor != null)
        {
            openDoor.Open();
        }        
    }
}
