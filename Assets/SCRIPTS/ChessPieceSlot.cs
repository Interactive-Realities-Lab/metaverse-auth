using UnityEngine;

public class ChessPieceSlot : MonoBehaviour
{
    [Header("Correct piece for this position")]
    [SerializeField] private GameObject requiredPiece;

    [Header("Optional")]
    [SerializeField] private bool snapPieceIntoPosition = true;
    [SerializeField] private Transform snapPoint;

    public bool IsCorrectPiecePlaced { get; private set; }

    private GameObject currentPiece;

    private bool pieceLocked = false;

    private void Awake()
    {
        // Use this object's position when no separate snap point is assigned.
        if (snapPoint == null)
            snapPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject enteredObject = FindPieceRoot(other);

        Debug.Log(
            $"Slot {name} detected {enteredObject.name}. " +
            $"Required piece is {requiredPiece.name}"
        );

        if (enteredObject != requiredPiece)
        {
            Debug.LogWarning(
                $"{enteredObject.name} entered {name}, but this slot requires {requiredPiece.name}"
            );
            return;
        }

        currentPiece = enteredObject;
        IsCorrectPiecePlaced = true;

        if (snapPieceIntoPosition)
        {
            SnapPiece();
            pieceLocked = true;
        }

        Debug.Log($"{requiredPiece.name} placed in the correct chess slot.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (pieceLocked)
            return;

        GameObject exitedObject = FindPieceRoot(other);

        if (exitedObject != requiredPiece)
            return;

        currentPiece = null;
        IsCorrectPiecePlaced = false;

        Debug.Log(requiredPiece.name + " removed from its chess slot.");
    }

    private GameObject FindPieceRoot(Collider pieceCollider)
    {
        // This allows the collider to be on a child object.
        if (pieceCollider.attachedRigidbody != null)
            return pieceCollider.attachedRigidbody.gameObject;

        return pieceCollider.gameObject;
    }

    private void SnapPiece()
    {
        if (currentPiece == null)
            return;

        Rigidbody pieceRigidbody = currentPiece.GetComponent<Rigidbody>();

        if (pieceRigidbody != null && !pieceRigidbody.isKinematic)
        {
            pieceRigidbody.linearVelocity = Vector3.zero;
            pieceRigidbody.angularVelocity = Vector3.zero;
        }

        currentPiece.transform.SetPositionAndRotation(
            snapPoint.position,
            snapPoint.rotation
        );
    }
}