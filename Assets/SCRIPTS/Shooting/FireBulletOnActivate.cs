using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FireBulletOnActivate : MonoBehaviour
{
    [Header("Bullet Settings")]
    public GameObject bullet;
    public Transform spawnPoint;
    public float fireSpeed = 20f;

    public event Action OnBulletFired;

    private XRGrabInteractable grabbable;

    private void Awake()
    {
        grabbable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.activated.AddListener(FireBullet);
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.activated.RemoveListener(FireBullet);
    }

    public void FireBullet(ActivateEventArgs arg)
    {
        if (bullet == null || spawnPoint == null)
        {
            Debug.LogWarning("Shooting: Bullet or Spawn Point missing.");
            return;
        }

        GameObject spawnedBullet =
            Instantiate(bullet, spawnPoint.position, spawnPoint.rotation);

        Rigidbody rb = spawnedBullet.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = spawnPoint.forward * fireSpeed;
        }

        Destroy(spawnedBullet, 5f);

        OnBulletFired?.Invoke();

        Debug.Log("[Shooting] Bullet fired!");
    }
}