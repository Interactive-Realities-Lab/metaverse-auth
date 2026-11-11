using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BackQuiverGesture : MonoBehaviour
{
    [Header("Arrow Prefab")]
    public GameObject arrowPrefab;

    [Header("Headset")]
    public Transform headset;

    [Header("Quiver Zone")]
    public SphereCollider quiverZone;

    [Header("Auto Detection")]
    public float discoveryInterval = 1f;
    private float nextDiscoveryTime;

    [Header("Controller Transforms")]
    public Transform leftController;
    public Transform rightController;

    [Header("Direct Interactors")]
    public XRDirectInteractor leftDirectInteractor;
    public XRDirectInteractor rightDirectInteractor;

    [Header("Grip Input Actions")]
    public InputActionProperty leftGripAction;
    public InputActionProperty rightGripAction;

    [Header("Interaction Manager")]
    public XRInteractionManager interactionManager;

    [Header("Settings")]
    public float spawnCooldown = 0.5f;

    private float lastSpawnTime = -10f;

    private bool leftGripWasPressed;
    private bool rightGripWasPressed;

    private void Start()
    {
        DetectReferences();
    }

    private void DetectReferences()
    {
        if (headset == null && Camera.main != null)
        {
            headset = Camera.main.transform;
        }

        if (leftController == null)
        {
            GameObject left =
                GameObject.FindGameObjectWithTag("LeftController");

            if (left != null)
                leftController = left.transform;
        }

        if (rightController == null)
        {
            GameObject right =
                GameObject.FindGameObjectWithTag("RightController");

            if (right != null)
                rightController = right.transform;
        }

        if (leftDirectInteractor == null && leftController != null)
        {
            Transform leftHand = leftController.parent;

            if (leftHand != null)
            {
                leftDirectInteractor =
                    leftHand.GetComponentInChildren<XRDirectInteractor>();
            }
        }

        if (rightDirectInteractor == null && rightController != null)
        {
            Transform rightHand = rightController.parent;

            if (rightHand != null)
            {
                rightDirectInteractor =
                    rightHand.GetComponentInChildren<XRDirectInteractor>();
            }
        }

        if (interactionManager == null)
        {
            interactionManager =
                FindFirstObjectByType<XRInteractionManager>();
        }
    }
    private void OnEnable()
    {
        leftGripAction.action?.Enable();
        rightGripAction.action?.Enable();
    }

    private void Update()
    {
        if (Time.time >= nextDiscoveryTime)
        {
            DetectReferences();

            nextDiscoveryTime = Time.time + discoveryInterval;
        }        
        
        if (headset == null ||
            quiverZone == null ||
            arrowPrefab == null ||
            interactionManager == null)
            return;

        bool leftGripPressed =
            leftGripAction.action != null &&
            leftGripAction.action.IsPressed();

        bool rightGripPressed =
            rightGripAction.action != null &&
            rightGripAction.action.IsPressed();

        bool leftGripStarted =
            leftGripPressed && !leftGripWasPressed;

        bool rightGripStarted =
            rightGripPressed && !rightGripWasPressed;

        if (leftGripStarted)
            Debug.Log("BACK QUIVER: LEFT GRIP DETECTED");

        if (rightGripStarted)
            Debug.Log("BACK QUIVER: RIGHT GRIP DETECTED");

        leftGripWasPressed = leftGripPressed;
        rightGripWasPressed = rightGripPressed;


        if (Time.time - lastSpawnTime < spawnCooldown)
            return;

        if (leftGripStarted &&
            IsInsideQuiver(leftController))
        {
            SpawnArrow(leftDirectInteractor);
        }
        else if (rightGripStarted &&
                 IsInsideQuiver(rightController))
        {
            SpawnArrow(rightDirectInteractor);
        }
    }

    private bool IsInsideQuiver(Transform controller)
    {
        if (controller == null)
            return false;

        Vector3 center =
            quiverZone.transform.TransformPoint(
                quiverZone.center
            );

        Vector3 scale =
            quiverZone.transform.lossyScale;

        float maxScale = Mathf.Max(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z)
        );

        float radius =
            quiverZone.radius * maxScale;

        float distance =
            Vector3.Distance(
                controller.position,
                center
            );

        // Require the hand to be behind the headset.
        Vector3 horizontalForward = headset.forward;
        horizontalForward.y = 0f;

        if (horizontalForward.sqrMagnitude < 0.001f)
            return false;

        horizontalForward.Normalize();

        Vector3 toController =
            controller.position - headset.position;

        bool behindHead =
            Vector3.Dot(
                horizontalForward,
                toController
            ) < -0.05f;

        bool insideSphere = distance <= radius;

        if (insideSphere)
        {
            Debug.Log(
                "BACK QUIVER: Controller inside sphere! " +
                controller.name +
                " | Distance: " + distance.ToString("F2") +
                " | Radius: " + radius.ToString("F2") +
                " | Behind Head: " + behindHead
            );
        }

        return insideSphere && behindHead;
    }

    private void SpawnArrow(XRDirectInteractor interactor)
    {
        if (interactor == null)
            return;

        // Do not create another arrow if this hand
        // is already holding an interactable.
        if (interactor.hasSelection)
            return;

        GameObject arrowObject = Instantiate(
            arrowPrefab,
            interactor.transform.position,
            interactor.transform.rotation
        );

        Arrow arrow =
            arrowObject.GetComponent<Arrow>();

        if (arrow == null)
        {
            Debug.LogError(
                "BACK QUIVER: Arrow component missing!"
            );

            Destroy(arrowObject);
            return;
        }

        // Attach arrow through XR Interaction Toolkit.
        interactionManager.SelectEnter(
            (IXRSelectInteractor)interactor,
            (IXRSelectInteractable)arrow
        );

        if (!arrow.isSelected)
        {
            Debug.LogWarning(
                "BACK QUIVER: Arrow was not selected."
            );

            Destroy(arrowObject);
            return;
        }

        lastSpawnTime = Time.time;

        Debug.Log(
            "BACK QUIVER: Arrow retrieved!"
        );
    }
}