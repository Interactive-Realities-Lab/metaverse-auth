using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ArcheryBowRequirement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRGrabInteractable bow;
    [SerializeField] private BackQuiverGesture backQuiver;

    public bool IsHoldingBow =>
        bow != null && bow.isSelected;

    private bool wasHoldingBow;

    private void Update()
    {
        if (bow == null)
            return;

        bool holdingBow = bow.isSelected;

        if (wasHoldingBow && !holdingBow)
        {
            Debug.Log("ARCHERY: Bow dropped. Remove current arrow.");

            if (backQuiver != null)
            {
                backQuiver.RemoveCurrentArrow();
            }
        }

        wasHoldingBow = holdingBow;
    }
}