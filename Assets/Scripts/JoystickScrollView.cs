using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class JoystickScrollView : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollSpeed = 0.8f;
    public float deadZone = 0.2f;      //small joystick movement ignored
    public bool invertScroll = false;

    private InputDevice rightController;

    void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    void Start()
    {
        FindRightController();
    }

    void Update()
    {
        if (scrollRect == null) return;

        if (!rightController.isValid)
            FindRightController();

        if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 input))
        {
            if (Mathf.Abs(input.y) < deadZone) return;

            float y = invertScroll ? -input.y : input.y;

            scrollRect.verticalNormalizedPosition += y * scrollSpeed * Time.deltaTime;
            scrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
    }

    void FindRightController()
    {
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }
}