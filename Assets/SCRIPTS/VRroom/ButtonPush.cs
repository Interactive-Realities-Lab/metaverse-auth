using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>   
/// This scripts allows the player to push the final button to complete the escape room.
/// </summary>
public class ButtonPush : MonoBehaviour
{
    /// <summary>
    /// Makes the Right Controller a collidable and detectable object
    /// -Used to interact with other objects (ie detects when near to a door or interactable object)
    /// </summary>
    public Collider RController;

    /// <summary>
    /// Makes the Left Controller a collidable and detectable object
    /// -Used to interact with other objects (ie detects when near to a door or interactable object)
    /// </summary>
    public Collider LController;

    /// <summary>
    /// Button object found in game.
    /// </summary>
    public GameObject button;

    /// <summary>
    /// The original color of the button before it is pressed.
    /// </summary>
    public Material originalButton;

    /// <summary>
    /// The color of the button after it is pressed.
    /// </summary>
    public Material redButton;

    /// <summary>
    /// The particles that fill the room once the button is pressed.
    /// </summary>
    public GameObject particles;

    /// <summary>
    /// An attempt to change the direction of the light once the button is pressed.
    /// This is a WIP and needs more attention
    /// </summary>
    public GameObject directionalLight;

    /// <summary>
    /// The sound of the button when it is pressed.
    /// </summary>
    public AudioSource buttonPress;

    /// <summary>
    /// The trigger to play the sound of the buttons press.
    /// </summary>
    bool playSound = true;

    /// <summary>
    /// Allows us to apply a new material when button is pressed.
    /// </summary>
    MeshRenderer buttonMesh;

    /// <summary>
    /// The range in which the user can interact with the button.
    /// Collides with one of the controllers.
    /// </summary>
    SphereCollider buttonSphere;

    /// <summary>
    /// The ambient light the button gives off.
    /// </summary>
    Light buttonLight;

    /// <summary>
    /// WIP
    /// </summary>
    Light lightShadow;

    /// <summary>
    /// The position of the light for the button.
    /// </summary>
    Transform lightPos;
    
    /// <summary>
    /// The sound that plays when the chess piece is hovered over the interactive panel.
    /// </summary>
    AudioSource playButtonPress;


    /// <summary>
    /// On Awake all of these variables will be instantiated with the corresponding component.
    /// </summary>
    void Awake()
    {
        buttonMesh = button.GetComponent<MeshRenderer>();
        buttonSphere = button.GetComponent<SphereCollider>();
        buttonLight = button.GetComponent<Light>();
        lightShadow = directionalLight.GetComponent<Light>();
        lightPos = directionalLight.GetComponent<Transform>();
        playButtonPress = buttonPress.GetComponent<AudioSource>();
    }

    /// <summary>   
    /// Continuously running code that checks if the player is close enough to the button.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("GameController"))
        {
            pushButton();
        }
    }

    /// <summary>   
    /// If player is close and presses the button, then this method is launched.
    /// </summary>
    public void pushButton()
    {
        if (playSound)
        {
            playButtonPress.Play();
            playSound = false;
        }
        buttonMesh.material = redButton;
        buttonLight.color = Color.red;
        particles.SetActive(true);
    }
}
