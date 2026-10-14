using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Bulk of XR code taken from https://learn.unity.com/tutorial/creating-bow-and-arrow-gameplay-in-vr-from-vr-with-andrew?projectId=5fbc5135edbc2a0139266a9a

public class Notch : XRSocketInteractor
{
    private Puller puller = null;
    private Arrow currentArrow = null;

    private float lastPullAmount = 0f;

    protected override void Awake()
    {
        base.Awake();

        puller = GetComponent<Puller>();

        if (puller == null)
        {
            Debug.LogError("Notch: Puller component not found!");
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (puller != null)
        {
            puller.selectExited.AddListener(TryToReleaseArrow);
        }
    }

    protected override void OnDisable()
    {
        if (puller != null)
        {
            puller.selectExited.RemoveListener(TryToReleaseArrow);
        }

        base.OnDisable();
    }

    private void Update()
    {
        if (puller != null && puller.isSelected)
        {
            lastPullAmount = puller.PullAmount;
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        StoreArrow(args.interactableObject as XRBaseInteractable);
    }

    private void StoreArrow(XRBaseInteractable interactable)
    {
        if (interactable is Arrow arrow)
        {
            currentArrow = arrow;
            lastPullAmount = 0f;
        }
    }

    private void TryToReleaseArrow(SelectExitEventArgs args)
    {
        if (currentArrow == null)
            return;

        float releaseStrength = lastPullAmount;

        Arrow arrowToRelease = currentArrow;

        currentArrow = null;

        // Prevent the socket from immediately selecting
        // the arrow again during launch.
        socketActive = false;

        ForceDeselect(arrowToRelease);

        arrowToRelease.Release(releaseStrength);

        lastPullAmount = 0f;

        StartCoroutine(ReactivateSocket());
    }

    private System.Collections.IEnumerator ReactivateSocket()
    {
        yield return new WaitForSeconds(0.5f);

        socketActive = true;
    }

    private void ForceDeselect(Arrow arrow)
    {
        if (arrow == null || interactionManager == null)
            return;

        interactionManager.SelectExit(
            (UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)this,
            (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)arrow
        );
    }

    public override XRBaseInteractable.MovementType?
        selectedInteractableMovementTypeOverride
    {
        get
        {
            return XRBaseInteractable.MovementType.Instantaneous;
        }
    }
}