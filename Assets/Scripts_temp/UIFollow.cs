using UnityEngine;

public class UIFollow : MonoBehaviour
{
    public float distanceFromCamera = 2f;
    public float heightOffset = 0f;
    public float smoothSpeed = 5f;
    public bool keepFollowing = true;

    private Transform cam;

    private void OnEnable()
    {
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
            SnapInFrontOfCamera();
        }
    }

    private void Update()
    {
        if (!keepFollowing || cam == null) return;

        Vector3 forward = cam.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 targetPos = cam.position + forward * distanceFromCamera;
        targetPos += Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

        Quaternion targetRot = Quaternion.LookRotation(forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
    }

    public void SnapInFrontOfCamera()
    {
        if (cam == null) return;

        Vector3 forward = cam.forward;
        forward.y = 0f;
        forward.Normalize();

        transform.position = cam.position + forward * distanceFromCamera;
        transform.position += Vector3.up * heightOffset;

        transform.rotation = Quaternion.LookRotation(forward);
    }
}