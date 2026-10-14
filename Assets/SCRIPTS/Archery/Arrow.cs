using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Bulk of XR code taken from https://learn.unity.com/tutorial/creating-bow-and-arrow-gameplay-in-vr-from-vr-with-andrew?projectId=5fbc5135edbc2a0139266a9a

public class Arrow : XRGrabInteractable
{
    [Header("Arrow Settings")]
    public float speed = 60f;
    public Transform tip = null;

    private bool inAir = false;
    private Vector3 lastPosition = Vector3.zero;

    private Rigidbody rigidBody = null;
    private Collider arrowCollider = null;
    private Light arrowLight = null;

    protected override void Awake()
    {
        base.Awake();

        rigidBody = GetComponent<Rigidbody>();
        arrowCollider = GetComponent<Collider>();
        arrowLight = GetComponent<Light>();

        if (rigidBody == null)
            Debug.LogError("Arrow: Rigidbody component missing!");

        if (tip == null)
            Debug.LogError("Arrow: Tip Transform is not assigned!");
    }

    private void FixedUpdate()
    {
        if (inAir && tip != null)
        {
            CheckForCollision();

            if (inAir)
                lastPosition = tip.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Star"))
        {
            StarManager star = other.GetComponent<StarManager>();

            if (star != null)
            {
                star.ChangeMaterial();
            }
        }
    }

    private void CheckForCollision()
    {
        Vector3 direction = tip.position - lastPosition;
        float distance = direction.magnitude;

        if (distance <= 0.0001f)
            return;

        RaycastHit[] hits = Physics.RaycastAll(
            lastPosition,
            direction.normalized,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.MaxValue;
        RaycastHit closestHit = default;
        bool foundHit = false;

        foreach (RaycastHit hit in hits)
        {
            // Ignore every collider belonging to this arrow.
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }
        }

        if (foundHit)
        {
            Debug.Log(
                "ARROW COLLISION | Hit: " +
                closestHit.collider.name
            );

            Stop();
        }
    }

    private void Stop()
    {
        inAir = false;
        SetPhysics(false);
    }

    public void Release(float pullValue)
    {
        Debug.Log(
            "ARROW BEFORE RELEASE | Selected=" + isSelected +
            " | Selecting Interactors=" + interactorsSelecting.Count
        );
        
        Debug.Log("ARROW RELEASE CALLED!");

        Debug.Log("Pull Value: " + pullValue);

        inAir = true;

        SetPhysics(true);

        Debug.Log("Arrow Is Kinematic: " + rigidBody.isKinematic);

        MaskAndFire(pullValue);

        StartCoroutine(RotateWithVelocity());

        if (arrowLight != null)
            arrowLight.enabled = true;

        lastPosition = tip.position;
    }

    private void SetPhysics(bool usePhysics)
    {
        if (rigidBody == null)
            return;

        rigidBody.isKinematic = !usePhysics;
        rigidBody.useGravity = usePhysics;
    }

    private void MaskAndFire(float power)
    {
        interactionLayers = 0;

        if (arrowCollider != null)
            arrowCollider.enabled = false;

        float launchSpeed = Mathf.Clamp01(power) * speed;

        rigidBody.linearVelocity =
            transform.forward * launchSpeed;

        Debug.Log(
            "ARROW FIRED | Speed: " + launchSpeed +
            " | Velocity: " + rigidBody.linearVelocity
        );

        Debug.Log(
            "ARROW AFTER LAUNCH | Selected=" + isSelected +
            " | Selecting Interactors=" + interactorsSelecting.Count
        );
    }

    private IEnumerator RotateWithVelocity()
    {
        yield return new WaitForFixedUpdate();

        while (inAir)
        {
            if (rigidBody != null &&
                rigidBody.linearVelocity.sqrMagnitude > 0.001f)
            {
                Quaternion newRotation = Quaternion.LookRotation(
                    rigidBody.linearVelocity,
                    transform.up
                );

                transform.rotation = newRotation;
            }

            yield return null;
        }
    }

    private void DestroyArrow()
    {
        Destroy(gameObject, 3.0f);
    }
}
