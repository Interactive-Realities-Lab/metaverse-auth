using UnityEngine;

public class DetectDisable : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.LogError($"{name} got disabled!", this);
        Debug.Break();
    }

    private void OnTransformParentChanged()
    {
        Debug.Log($"{name} parent changed", this);
    }
}