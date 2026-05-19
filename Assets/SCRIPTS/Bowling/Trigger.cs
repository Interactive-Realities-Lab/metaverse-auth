using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Trigger : MonoBehaviour
{
    [SerializeField] EventType _eventType;

    [Header(nameof(Gizmos))]
    [SerializeField] private Color _color = Color.white;
    [SerializeField][Range(0.0f, 1.0f)] private float _alpha = 0.2f;

    public EventType EventType => _eventType;

    private void OnDrawGizmos()
    {
        Color color = Gizmos.color;
        Matrix4x4 matrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        Gizmos.color = _color;
        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        Gizmos.color = new Color(_color.r, _color.g, _color.b, _alpha);
        Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        Gizmos.color = color;
        Gizmos.matrix = matrix;
    }
}
