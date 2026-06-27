using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void ToggleDoor()
    {
        anim.SetBool("open_door", !anim.GetBool("open_door"));
    }

    public void Open()
    {
        anim.SetBool("open_door", true);
    }

    public void Close()
    {
        anim.SetBool("open_door", false);
    }
}