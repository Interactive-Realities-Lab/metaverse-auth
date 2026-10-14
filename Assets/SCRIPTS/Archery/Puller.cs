using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Bulk of XR code taken from https://learn.unity.com/tutorial/creating-bow-and-arrow-gameplay-in-vr-from-vr-with-andrew?projectId=5fbc5135edbc2a0139266a9a

public class Puller : XRBaseInteractable
{
    public float PullAmount { get; private set; } = 0.0f;

    public Transform start = null;
    public Transform end = null;

    private IXRSelectInteractor pullingInteractor = null;

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        base.OnSelectEntering(args);

        pullingInteractor = args.interactorObject;
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);

        if (pullingInteractor == args.interactorObject)
        {
            pullingInteractor = null;
            PullAmount = 0.0f;
        }
    }

    public override void ProcessInteractable(
        XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected && pullingInteractor != null &&
                start != null && end != null)
            {
                Vector3 pullPosition =
                    pullingInteractor.transform.position;

                PullAmount = CalculatePull(pullPosition);
            }
        }
    }

    private float CalculatePull(Vector3 pullPosition)
    {
        Vector3 pullDirection = pullPosition - start.position;
        Vector3 targetDirection = end.position - start.position;

        float maxLength = targetDirection.magnitude;

        if (maxLength <= Mathf.Epsilon)
            return 0f;

        targetDirection.Normalize();

        float pullValue =
            Vector3.Dot(pullDirection, targetDirection) / maxLength;

        return Mathf.Clamp01(pullValue);
    }
}


