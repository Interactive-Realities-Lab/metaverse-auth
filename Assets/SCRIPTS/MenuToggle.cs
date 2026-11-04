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

    [Header("Interaction Manager")]
    public VRInteractionManager interactionManager;

    [Header("Menu Placement")]
    public float distanceFromFace = 1.5f;
    public float heightOffset = 0f;

    private bool isMenuOpen = false;

    private void OnEnable()
    {
        if (bButtonAction.action != null)
        {
            bButtonAction.action.Enable();
            bButtonAction.action.performed += OnBButtonPressed;
        }
    }

    private void OnDisable()
    {
        if (bButtonAction.action != null)
        {
            bButtonAction.action.performed -= OnBButtonPressed;
        }
    }

    private void Start()
    {
        if (MenuCanvas != null)
            MenuCanvas.SetActive(false);

        interactionManager?.SetMenuOpen(false);
    }

    private void OnBButtonPressed(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (MenuCanvas != null)
        {
            MenuCanvas.SetActive(isMenuOpen);

            if (isMenuOpen && headsetCamera != null)
            {
                Vector3 forward = headsetCamera.forward;

                forward.y = 0f;

                if (forward.sqrMagnitude < 0.001f)
                    forward = headsetCamera.forward;

                forward.Normalize();

                MenuCanvas.transform.position =
                    headsetCamera.position +
                    forward * distanceFromFace +
                    Vector3.up * heightOffset;

                MenuCanvas.transform.rotation =
                    Quaternion.LookRotation(forward);
            }
        }

        // IMPORTANT: Notify the interaction manager.
        if (interactionManager != null)
        {
            interactionManager.SetMenuOpen(isMenuOpen);
        }

        Debug.Log("B Button Pressed | Menu: " + isMenuOpen);
    }

    public void CloseMenu()
    {
        isMenuOpen = false;

        if (MenuCanvas != null)
            MenuCanvas.SetActive(false);

        // Disable rays when menu closes
        if (interactionManager != null)
            interactionManager.SetMenuOpen(false);

        Debug.Log("Menu Closed | Rays Disabled");
    }
}