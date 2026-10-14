using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Quiver : XRSocketInteractor
{
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;

    [Header("Respawn Settings")]
    public float respawnDelay = 0.3f;
    public float removalDistance = 0.25f;

    private Arrow currentArrow;
    private Coroutine respawnCoroutine;

    private Vector3 attachOffset = Vector3.zero;

    protected override void Awake()
    {
        base.Awake();
        SetAttachOffset();
    }

    protected override void Start()
    {
        base.Start();
        SpawnArrow();
    }

    private void Update()
    {
        if (currentArrow == null)
            return;

        // Only consider an arrow removed when the player
        // has selected it and moved it away from the Quiver.
        if (currentArrow.isSelected)
        {
            bool heldByController = false;

            foreach (var interactor in currentArrow.interactorsSelecting)
            {
                // Ignore socket selection.
                if (interactor is not XRSocketInteractor)
                {
                    heldByController = true;
                    break;
                }
            }

            if (heldByController)
            {
                float distance = Vector3.Distance(
                    currentArrow.transform.position,
                    transform.position
                );

                if (distance > removalDistance)
                {
                    Debug.Log("QUIVER: Arrow taken by player!");

                    currentArrow = null;

                    if (respawnCoroutine == null)
                    {
                        respawnCoroutine =
                            StartCoroutine(RespawnArrow());
                    }
                }
            }
        }
    }

    private IEnumerator RespawnArrow()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (isActiveAndEnabled)
            SpawnArrow();

        respawnCoroutine = null;
    }

    private void SpawnArrow()
    {
        if (arrowPrefab == null)
        {
            Debug.LogError("QUIVER: Arrow Prefab missing!");
            return;
        }

        GameObject arrowObject = Instantiate(
            arrowPrefab,
            transform.position - transform.rotation * attachOffset,
            transform.rotation
        );

        currentArrow = arrowObject.GetComponent<Arrow>();

        if (currentArrow == null)
        {
            Debug.LogError("QUIVER: Arrow.cs missing!");

            Destroy(arrowObject);
            return;
        }

        Debug.Log("QUIVER: New arrow spawned!");
    }

    private void SetAttachOffset()
    {
        if (arrowPrefab == null)
            return;

        XRGrabInteractable interactable =
            arrowPrefab.GetComponent<XRGrabInteractable>();

        if (interactable != null &&
            interactable.attachTransform != null)
        {
            attachOffset =
                interactable.attachTransform.localPosition;
        }
    }

    protected override void OnDisable()
    {
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        base.OnDisable();
    }
}