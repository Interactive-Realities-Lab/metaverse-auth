using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ShootingUserActivityDetector : MonoBehaviour
{
    [Header("State Machine")]
    [SerializeField] private ShootingUserStateMachine stateMachine;

    [Header("Player References (Auto-detected)")]
    [SerializeField] private Camera headsetCamera;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("Gun References (Auto-detected)")]
    [SerializeField] private XRGrabInteractable gunGrab;
    [SerializeField] private Transform gunMuzzle;
    [SerializeField] private FireBulletOnActivate gunFire;

    [Header("Target (Auto-detected)")]
    [SerializeField] private TargetSpawner targetSpawner;

    [Header("Reaching Detection")]
    [SerializeField] private float reachingDistance = 0.5f;
    [SerializeField] private float reachingSpeedThreshold = 0.05f;

    [Header("Aiming Detection")]
    [SerializeField] private float aimingAngleThreshold = 10f;

    [Header("State Durations")]
    [SerializeField] private float holdingDuration = 0.3f;
    [SerializeField] private float firingDuration = 0.15f;
    [SerializeField] private float postFiringDuration = 0.3f;
    [SerializeField] private float gunReleasedDuration = 0.2f;

    [Header("Automatic Discovery")]
    [SerializeField] private float discoveryInterval = 1f;

    private Vector3 previousLeftPosition;
    private Vector3 previousRightPosition;

    private float previousLeftDistance;
    private float previousRightDistance;

    private bool leftInitialized;
    private bool rightInitialized;

    private bool wasGunHeld;

    private float firingEndTime;
    private float postFiringEndTime;

    private bool firingActive;
    private bool postFiringActive;

    private float nextDiscoveryTime;

    private void Awake()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<ShootingUserStateMachine>();
    }

    private void OnEnable()
    {
        DiscoverReferences();
    }

    private void Start()
    {
        DiscoverReferences();

        wasGunHeld = gunGrab != null && gunGrab.isSelected;
    }

    private void OnDisable()
    {
        if (gunFire != null)
            gunFire.OnBulletFired -= HandleBulletFired;
    }

    private void Update()
    {
        // Retry discovery because XR Origin may be loaded
        // from another additive scene.

        if (Time.time >= nextDiscoveryTime)
        {
            DiscoverReferences();
            nextDiscoveryTime = Time.time + discoveryInterval;
        }

        if (stateMachine == null || gunGrab == null)
            return;

        bool isGunHeld = gunGrab.isSelected;

        // ----------------------------------
        // 1. Gun released
        // ----------------------------------

        if (wasGunHeld && !isGunHeld)
        {
            firingActive = false;
            postFiringActive = false;

            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.GunReleased
            );

            wasGunHeld = false;
            return;
        }

        // ----------------------------------
        // 2. Gun not held
        // ----------------------------------

        if (!isGunHeld)
        {
            if (stateMachine.CurrentState ==
                ShootingUserStateMachine.ShootingState.GunReleased &&
                stateMachine.StateDuration < gunReleasedDuration)
            {
                UpdatePreviousPositions();
                return;
            }

            DetectReaching();
            wasGunHeld = false;
            return;
        }

        // ----------------------------------
        // 3. Gun grabbed
        // ----------------------------------

        if (!wasGunHeld)
        {
            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.Holding
            );

            wasGunHeld = true;
            return;
        }

        wasGunHeld = true;

        // ----------------------------------
        // 4. Firing
        // ----------------------------------

        if (firingActive)
        {
            if (Time.time >= firingEndTime)
            {
                firingActive = false;
                postFiringActive = true;

                postFiringEndTime =
                    Time.time + postFiringDuration;

                stateMachine.ChangeState(
                    ShootingUserStateMachine.ShootingState.PostFiring
                );
            }

            return;
        }

        // ----------------------------------
        // 5. PostFiring
        // ----------------------------------

        if (postFiringActive)
        {
            if (Time.time < postFiringEndTime)
                return;

            postFiringActive = false;
        }

        // ----------------------------------
        // 6. Holding
        // ----------------------------------

        if (stateMachine.CurrentState ==
            ShootingUserStateMachine.ShootingState.Holding &&
            stateMachine.StateDuration < holdingDuration)
        {
            return;
        }

        // ----------------------------------
        // 7. Target detection
        // ----------------------------------

        GameObject target =
            targetSpawner != null
                ? targetSpawner.CurrentTarget
                : null;

        if (target == null)
        {
            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.Searching
            );

            return;
        }

        // ----------------------------------
        // 8. Target visibility
        // ----------------------------------

        if (!IsTargetVisible(target.transform))
        {
            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.Searching
            );

            return;
        }

        // ----------------------------------
        // 9. Aiming detection
        // ----------------------------------

        if (IsAimingAtTarget(target.transform))
        {
            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.Aiming
            );
        }
        else
        {
            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.TargetLocated
            );
        }
    }

    // =====================================
    // AUTOMATIC REFERENCE DISCOVERY
    // =====================================

    private void DiscoverReferences()
    {
        if (headsetCamera == null)
            headsetCamera = Camera.main;

        if (targetSpawner == null)
            targetSpawner = FindFirstObjectByType<TargetSpawner>();

        if (gunFire == null)
        {
            FireBulletOnActivate foundGun =
                FindFirstObjectByType<FireBulletOnActivate>();

            if (foundGun != null)
            {
                gunFire = foundGun;
                gunGrab = gunFire.GetComponent<XRGrabInteractable>();
                gunMuzzle = gunFire.spawnPoint;
            }
        }

        if (gunFire != null)
        {
            gunFire.OnBulletFired -= HandleBulletFired;
            gunFire.OnBulletFired += HandleBulletFired;
        }

        // Find controller transforms through XR Direct Interactors.
        // This assumes your controller hierarchy uses names
        // containing "Left" and "Right".

        XRDirectInteractor[] interactors =
            FindObjectsByType<XRDirectInteractor>(
                FindObjectsSortMode.None
            );

        foreach (XRDirectInteractor interactor in interactors)
        {
            string objectName = interactor.gameObject.name.ToLowerInvariant();

            if (leftController == null && objectName.Contains("left"))
            {
                leftController = interactor.transform;
                leftInitialized = false;
            }

            if (rightController == null && objectName.Contains("right"))
            {
                rightController = interactor.transform;
                rightInitialized = false;
            }
        }
    }

    // =====================================
    // REACHING DETECTION
    // =====================================

    private void DetectReaching()
    {
        bool leftReaching = IsHandReaching(
            leftController,
            ref previousLeftPosition,
            ref previousLeftDistance,
            ref leftInitialized
        );

        bool rightReaching = IsHandReaching(
            rightController,
            ref previousRightPosition,
            ref previousRightDistance,
            ref rightInitialized
        );

        if (leftReaching || rightReaching)
        {
            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.Reaching
            );
        }
        else
        {
            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.Idle
            );
        }
    }

    private bool IsHandReaching(
        Transform controller,
        ref Vector3 previousPosition,
        ref float previousDistance,
        ref bool initialized)
    {
        if (controller == null || gunGrab == null)
            return false;

        float distance = Vector3.Distance(
            controller.position,
            gunGrab.transform.position
        );

        if (!initialized)
        {
            previousPosition = controller.position;
            previousDistance = distance;
            initialized = true;
            return false;
        }

        float speed = Vector3.Distance(
            controller.position,
            previousPosition
        ) / Mathf.Max(Time.deltaTime, 0.0001f);

        bool approaching = distance < previousDistance;

        bool reaching =
            distance < reachingDistance &&
            speed > reachingSpeedThreshold &&
            approaching;

        previousPosition = controller.position;
        previousDistance = distance;

        return reaching;
    }

    private void UpdatePreviousPositions()
    {
        if (leftController != null)
        {
            previousLeftPosition = leftController.position;
            previousLeftDistance = Vector3.Distance(
                leftController.position,
                gunGrab.transform.position
            );
            leftInitialized = true;
        }

        if (rightController != null)
        {
            previousRightPosition = rightController.position;
            previousRightDistance = Vector3.Distance(
                rightController.position,
                gunGrab.transform.position
            );
            rightInitialized = true;
        }
    }

    // =====================================
    // TARGET VISIBILITY
    // =====================================

    private bool IsTargetVisible(Transform target)
    {
        if (headsetCamera == null)
            return false;

        Vector3 viewportPoint =
            headsetCamera.WorldToViewportPoint(target.position);

        return viewportPoint.z > 0 &&
               viewportPoint.x >= 0 &&
               viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 &&
               viewportPoint.y <= 1;
    }

    // =====================================
    // AIMING
    // =====================================

    private bool IsAimingAtTarget(Transform target)
    {
        if (gunMuzzle == null)
            return false;

        Vector3 direction =
            (target.position - gunMuzzle.position).normalized;

        float angle = Vector3.Angle(
            gunMuzzle.forward,
            direction
        );

        return angle <= aimingAngleThreshold;
    }

    // =====================================
    // FIRING EVENT
    // =====================================

    private void HandleBulletFired()
    {
        if (stateMachine == null ||
            gunGrab == null ||
            !gunGrab.isSelected)
            return;

        firingActive = true;
        postFiringActive = false;

        firingEndTime = Time.time + firingDuration;

        stateMachine.ChangeState(
            ShootingUserStateMachine.ShootingState.Firing
        );
    }
}