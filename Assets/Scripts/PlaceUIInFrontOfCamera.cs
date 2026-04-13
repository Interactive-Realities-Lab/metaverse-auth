using UnityEngine;

public class PlaceUIInFrontOfCamera : MonoBehaviour
{
    public float distance = 2f;
    public float heightOffset = 0f;

    private void OnEnable()
    {
        PlaceNow();
    }

    public void PlaceNow()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        transform.position = cam.transform.position + forward * distance;
        transform.position += Vector3.up * heightOffset;

        transform.LookAt(cam.transform.position);
        transform.Rotate(0f, 180f, 0f);
    }
}