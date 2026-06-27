using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AutoAssignTeleportProvider : MonoBehaviour
{
    void Start()
    {
        TeleportationProvider provider = FindObjectOfType<TeleportationProvider>();

        TeleportationArea[] areas = FindObjectsOfType<TeleportationArea>();

        foreach (TeleportationArea area in areas)
        {
            area.teleportationProvider = provider;
        }

        Debug.Log("Assigned Teleportation Provider to all Teleportation Areas");
    }
}