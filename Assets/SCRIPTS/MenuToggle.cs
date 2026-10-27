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

    [Header("Interaction Manager")]
    public BowlingInteractionManager bowlingInteractionManager;

    void Start()
    {
        // Synchronize interaction state with initial menu visibility
        if (bowlingInteractionManager != null && MenuCanvas != null)
        {
            bowlingInteractionManager.SetMenuOpen(MenuCanvas.activeSelf);
        }
    }

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
        if (MenuCanvas == null)
            return;

        bool show = !MenuCanvas.activeSelf;

        MenuCanvas.SetActive(show);

        if (show)
        {
            PlaceMenuInFrontOfUser();
        }

        // Enable or disable rays depending on menu visibility
        if (bowlingInteractionManager != null)
        {
            bowlingInteractionManager.SetMenuOpen(show);
        }
    }

    public void CloseMenu()
    {
        if (MenuCanvas != null)
        {
            MenuCanvas.SetActive(false);
        }

        // Restore normal interaction mode
        if (bowlingInteractionManager != null)
        {
            bowlingInteractionManager.SetMenuOpen(false);
        }
    }

    void PlaceMenuInFrontOfUser()
    {
        if (headsetCamera == null)
            return;

        Vector3 forward = headsetCamera.forward;
        forward.y = 0;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

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