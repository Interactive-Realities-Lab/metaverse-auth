using UnityEngine;
using UnityEngine.InputSystem;

public class MenuToggle : MonoBehaviour
{
    [Header("Menu")]
    public GameObject MenuCanvas;

    [Header("Headset Camera")]
    public Transform headsetCamera;

    [Header("Controller Button")]
    public InputActionProperty bButtonAction;

    [Header("Menu Placement")]
    public float distanceFromFace = 1.5f;
    public float heightOffset = 0f;

    void Update()
    {
        if (bButtonAction.action != null &&
            bButtonAction.action.WasPressedThisFrame())
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        bool show = !MenuCanvas.activeSelf;
        MenuCanvas.SetActive(show);

        if (show)
        {
            PlaceMenuInFrontOfUser();
        }
    }

    public void CloseMenu()
    {
        MenuCanvas.SetActive(false);
    }

    void PlaceMenuInFrontOfUser()
    {
        Vector3 forward = headsetCamera.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 position =
            headsetCamera.position +
            forward * distanceFromFace +
            Vector3.up * heightOffset;

        MenuCanvas.transform.position = position;

        MenuCanvas.transform.rotation =
            Quaternion.LookRotation(forward);
    }
}