using UnityEngine;


public class AutoAssignTeleportProvider : MonoBehaviour
{
    void Start()
    {
        UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider provider = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();

        UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea[] areas = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();

        foreach (UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea area in areas)
        {
            area.teleportationProvider = provider;
        }

        Debug.Log("Assigned Teleportation Provider to all Teleportation Areas");
    }
}