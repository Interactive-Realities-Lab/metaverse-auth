using UnityEngine;

public class BackQuiverFollower : MonoBehaviour
{
    [Header("Headset Reference")]
    public Transform headset;

    [Header("Quiver Position")]
    public float rightOffset = 0.25f;
    public float backOffset = 0.30f;
    public float downOffset = 0.25f;

    private void Start()
    {
        if (headset == null && Camera.main != null)
        {
            headset = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (headset == null)
        {
            if (Camera.main != null)
                headset = Camera.main.transform;

            return;
        }

        // Horizontal headset direction
        Vector3 forward = headset.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return;

        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);

        // Position behind the right shoulder
        transform.position =
            headset.position
            + right * rightOffset
            - forward * backOffset
            - Vector3.up * downOffset;

        // Rotate horizontally with the headset
        transform.rotation = Quaternion.LookRotation(
            forward,
            Vector3.up
        );
    }
}