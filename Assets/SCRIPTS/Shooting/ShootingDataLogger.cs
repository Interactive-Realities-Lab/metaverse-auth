using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ShootingDataLogger : MonoBehaviour
{
    [Header("State Machine")]
    [SerializeField] private ShootingUserStateMachine stateMachine;

    [Header("XR References - Auto Detected")]
    [SerializeField] private Transform headset;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("Shooting References - Auto Detected")]
    [SerializeField] private XRGrabInteractable gunGrab;
    [SerializeField] private FireBulletOnActivate gunFire;
    [SerializeField] private TargetSpawner targetSpawner;

    [Header("Logging")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private float discoveryInterval = 1f;

    private StreamWriter writer;
    private string filePath;

    private bool isLogging;
    private float nextDiscoveryTime;
    private float sessionStartTime;

    private int shotCount;
    private int hitCount;

    private bool previousGunHeld;
    private bool gunHeldInitialized;

    private FireBulletOnActivate subscribedGun;

    private static readonly CultureInfo CI =
        CultureInfo.InvariantCulture;

    public string FilePath => filePath;
    public bool IsLogging => isLogging;

    private void Awake()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<ShootingUserStateMachine>();
    }

    private void Start()
    {
        DiscoverReferences();

        if (startAutomatically)
            StartLogging();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextDiscoveryTime)
        {
            DiscoverReferences();

            nextDiscoveryTime =
                Time.unscaledTime + Mathf.Max(0.1f, discoveryInterval);
        }

        if (!isLogging)
            return;

        // One row per Unity frame.
        WriteRow();
    }

    private void DiscoverReferences()
    {
        if (headset == null && Camera.main != null)
            headset = Camera.main.transform;

        if (targetSpawner == null)
            targetSpawner =
                FindFirstObjectByType<TargetSpawner>();

        if (gunFire == null)
            gunFire =
                FindFirstObjectByType<FireBulletOnActivate>();

        if (gunFire != null && gunGrab == null)
        {
            gunGrab =
                gunFire.GetComponent<XRGrabInteractable>();

            if (gunGrab == null)
                gunGrab =
                    gunFire.GetComponentInParent<XRGrabInteractable>();
        }

        // Subscribe only when the gun reference changes.
        if (subscribedGun != gunFire)
        {
            if (subscribedGun != null)
                subscribedGun.OnBulletFired -= HandleBulletFired;

            subscribedGun = gunFire;

            if (subscribedGun != null)
                subscribedGun.OnBulletFired += HandleBulletFired;
        }

        if (leftController == null || rightController == null)
        {
            XRDirectInteractor[] interactors =
                FindObjectsByType<XRDirectInteractor>(
                    FindObjectsSortMode.None
                );

            foreach (XRDirectInteractor interactor in interactors)
            {
                string hierarchyName =
                    GetHierarchyName(interactor.transform)
                    .ToLowerInvariant();

                if (leftController == null &&
                    hierarchyName.Contains("left"))
                {
                    leftController = interactor.transform;
                }

                if (rightController == null &&
                    hierarchyName.Contains("right"))
                {
                    rightController = interactor.transform;
                }
            }
        }
    }

    private string GetHierarchyName(Transform t)
    {
        string result = "";

        while (t != null)
        {
            result = t.name + "/" + result;
            t = t.parent;
        }

        return result;
    }

    private string GetActiveHand()
    {
        if (gunGrab == null || !gunGrab.isSelected)
            return "None";

        var interactor = gunGrab.firstInteractorSelecting;

        if (interactor == null)
            return "Unknown";

        Transform t = interactor.transform;

        while (t != null)
        {
            string name = t.name.ToLowerInvariant();

            if (name.Contains("left"))
                return "Left";

            if (name.Contains("right"))
                return "Right";

            t = t.parent;
        }

        return "Unknown";
    }

    public void StartLogging()
    {
        if (isLogging)
            return;

        string directory = Path.Combine(
            Application.persistentDataPath,
            "ShootingLogs"
        );

        Directory.CreateDirectory(directory);

        string filename =
            "Shooting_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") +
            ".csv";

        filePath = Path.Combine(directory, filename);

        try
        {
            writer = new StreamWriter(filePath, false);

            writer.WriteLine(
    "Frame,Time,ParentState,State,GunEvent,ActiveHand,GunHeld,TargetVisible,AimAngle,ShotCount,HitCount," +

    "HeadPositionX,HeadPositionY,HeadPositionZ," +
    "HeadRotationX,HeadRotationY,HeadRotationZ,HeadRotationW," +

    "LeftPositionX,LeftPositionY,LeftPositionZ," +
    "LeftRotationX,LeftRotationY,LeftRotationZ,LeftRotationW," +

    "RightPositionX,RightPositionY,RightPositionZ," +
    "RightRotationX,RightRotationY,RightRotationZ,RightRotationW," +

    "GunPositionX,GunPositionY,GunPositionZ," +
    "GunRotationX,GunRotationY,GunRotationZ,GunRotationW," +

    "TargetPositionX,TargetPositionY,TargetPositionZ"
);

            sessionStartTime = Time.unscaledTime;       
            shotCount = 0;
            hitCount = 0;

            previousGunHeld = gunGrab != null && gunGrab.isSelected;
            gunHeldInitialized = true;

            isLogging = true;

            if (stateMachine != null)
                stateMachine.OnStateChanged += HandleStateChanged;

            Debug.Log("[Shooting Logger] Started: " + filePath);
        }
        catch (Exception e)
        {
            isLogging = false;
            writer?.Dispose();
            writer = null;

            Debug.LogError(
                "[Shooting Logger] Could not start: " + e.Message
            );
        }
    }

    public void StopLogging()
    {
        if (!isLogging)
            return;

        isLogging = false;

        if (stateMachine != null)
            stateMachine.OnStateChanged -= HandleStateChanged;

        try
        {
            writer?.Flush();
            writer?.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "[Shooting Logger] Close error: " + e.Message
            );
        }

        writer = null;

        Debug.Log("[Shooting Logger] Saved: " + filePath);
    }

    private void HandleStateChanged(
        ShootingUserStateMachine.ShootingState newState)
    {
        // The state will be recorded in LateUpdate().
    }

    private void HandleBulletFired()
    {
        shotCount++;
    }

    public void RegisterTargetHit()
    {
        hitCount++;
    }

    private string GetParentState()
    {
        if (stateMachine == null)
            return "Unknown";

        switch (stateMachine.CurrentState)
        {
            case ShootingUserStateMachine.ShootingState.Idle:
            case ShootingUserStateMachine.ShootingState.Reaching:
                return "GunNotHeld";

            default:
                return "GunHeld";
        }
    }

    private void WriteRow()
    {
        if (!isLogging || writer == null)
            return;

        GameObject target =
            targetSpawner != null
                ? targetSpawner.CurrentTarget
                : null;

        Transform targetTransform =
            target != null ? target.transform : null;

        Transform gunTransform =
            gunGrab != null ? gunGrab.transform : null;

        bool gunHeld =
            gunGrab != null && gunGrab.isSelected;

        string gunEvent = "None";

        if (!gunHeldInitialized)
        {
            previousGunHeld = gunHeld;
            gunHeldInitialized = true;
        }
        else
        {
            if (!previousGunHeld && gunHeld)
            {
                gunEvent = "GunGrabbed";
            }
            else if (previousGunHeld && !gunHeld)
            {
                gunEvent = "GunReleased";
            }
        }

        previousGunHeld = gunHeld;

        bool targetVisible = false;
        float aimAngle = float.NaN;

        Camera cam = headset != null
            ? headset.GetComponent<Camera>()
            : null;

        if (cam != null && targetTransform != null)
        {
            Vector3 viewport =
                cam.WorldToViewportPoint(targetTransform.position);

            targetVisible =
                viewport.z > 0f &&
                viewport.x >= 0f &&
                viewport.x <= 1f &&
                viewport.y >= 0f &&
                viewport.y <= 1f;
        }

        if (gunHeld &&
            gunFire != null &&
            gunFire.spawnPoint != null &&
            targetTransform != null)
        {
            Transform muzzle = gunFire.spawnPoint;

            Vector3 direction =
                targetTransform.position - muzzle.position;

            if (direction.sqrMagnitude > 0.000001f)
            {
                aimAngle = Vector3.Angle(
                    muzzle.forward,
                    direction.normalized
                );
            }
        }

        string stateName =
            stateMachine != null
                ? stateMachine.CurrentStateName
                : "Unknown";

        string row = string.Join(",", new string[]
        {
            Time.frameCount.ToString(CI),
            F(Time.time),

            GetParentState(),
            stateName,
            gunEvent,

            GetActiveHand(),
            gunHeld ? "1" : "0",
            targetVisible ? "1" : "0",
            F(aimAngle),

            shotCount.ToString(CI),
            hitCount.ToString(CI),

            TransformValues(headset),
            TransformValues(leftController),
            TransformValues(rightController),
            TransformValues(gunTransform),

            PositionValues(targetTransform)
        });

        try
        {
            writer.WriteLine(row);
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[Shooting Logger] Write error: " + e.Message
            );

            StopLogging();
        }
    }

    private static string F(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? ""
            : value.ToString("F6", CI);
    }

    private static string PositionValues(Transform t)
    {
        if (t == null)
            return ",,";

        Vector3 p = t.position;

        return string.Join(",", new string[]
        {
            F(p.x), F(p.y), F(p.z)
        });
    }

    private static string TransformValues(Transform t)
    {
        if (t == null)
            return ",,,,,,";

        Vector3 p = t.position;
        Quaternion q = t.rotation;

        return string.Join(",", new string[]
        {
            F(p.x), F(p.y), F(p.z),
            F(q.x), F(q.y), F(q.z), F(q.w)
        });
    }

    private void OnDisable()
    {
        StopLogging();

        if (subscribedGun != null)
            subscribedGun.OnBulletFired -= HandleBulletFired;

        subscribedGun = null;
    }

    private void OnDestroy()
    {
        StopLogging();
    }

    private void OnApplicationQuit()
    {
        StopLogging();
    }
}