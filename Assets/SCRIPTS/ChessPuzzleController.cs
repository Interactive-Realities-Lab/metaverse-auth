using UnityEngine;
using Unity.XR.CoreUtils;

public class ChessPuzzleController : MonoBehaviour
{
    [Header("The three missing chess positions")]
    [SerializeField] private ChessPieceSlot[] pieceSlots;

    [Header("XR Player")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Destination in next room")]
    [SerializeField] private Transform nextRoomDestination;

    [Header("Optional")]
    [SerializeField] private GameObject incompleteMessage;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

    private bool puzzleCompleted;

    public void PressGreenButton()
    {
        if (puzzleCompleted)
            return;

        if (!AreAllPiecesCorrect())
        {
            Debug.Log("All three missing chess pieces have not been placed.");

            if (incompleteMessage != null)
                incompleteMessage.SetActive(true);

            if (failureClip != null)
                AudioSource.PlayClipAtPoint(failureClip, transform.position);

            return;
        }

        puzzleCompleted = true;

        if (incompleteMessage != null)
            incompleteMessage.SetActive(false);

        if (successClip != null)
            AudioSource.PlayClipAtPoint(successClip, nextRoomDestination.position);

        TeleportToNextRoom();
    }

    private bool AreAllPiecesCorrect()
    {
        if (pieceSlots == null || pieceSlots.Length == 0)
        {
            Debug.LogError("No chess slots assigned.");
            return false;
        }

        for (int i = 0; i < pieceSlots.Length; i++)
        {
            if (pieceSlots[i] == null)
            {
                Debug.LogError($"Chess slot {i} is not assigned.");
                return false;
            }

            Debug.Log(
                $"Slot {i}: {pieceSlots[i].name}, " +
                $"Correct placed = {pieceSlots[i].IsCorrectPiecePlaced}"
            );

            if (!pieceSlots[i].IsCorrectPiecePlaced)
                return false;
        }

        return true;
    }

    private void TeleportToNextRoom()
    {
        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError("No XROrigin found in any loaded scene.");
            return;
        }

        if (nextRoomDestination == null)
        {
            Debug.LogError("Next Room Destination has not been assigned.");
            return;
        }

        Camera xrCamera = xrOrigin.Camera;

        if (xrCamera == null)
        {
            Debug.LogError("The XR Origin camera could not be found.");
            return;
        }

        // Calculate the headset's horizontal offset from the XR Origin.
        Vector3 cameraOffset = xrCamera.transform.position -
                               xrOrigin.transform.position;

        cameraOffset.y = 0f;

        // Move the XR Origin so the headset arrives exactly at the destination.
        xrOrigin.transform.position =
            nextRoomDestination.position - cameraOffset;

        // Rotate the player to face the destination direction.
        Vector3 currentForward = xrCamera.transform.forward;
        currentForward.y = 0f;

        Vector3 targetForward = nextRoomDestination.forward;
        targetForward.y = 0f;

        if (currentForward.sqrMagnitude > 0.001f &&
            targetForward.sqrMagnitude > 0.001f)
        {
            float rotationDifference = Vector3.SignedAngle(
                currentForward,
                targetForward,
                Vector3.up
            );

            xrOrigin.transform.RotateAround(
                xrCamera.transform.position,
                Vector3.up,
                rotationDifference
            );
        }

        Debug.Log("Chess puzzle completed. Player teleported.");
    }
}