using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShootingUserActivityDetector : MonoBehaviour
{
    [Header("State Machine")]
    [SerializeField]
    private ShootingUserStateMachine stateMachine;

    [Header("XR References")]
    [SerializeField] private Transform headsetCamera;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("Gun")]
    [SerializeField] private XRGrabInteractable gunGrab;
    [SerializeField] private Transform gunMuzzle;
    [SerializeField] private FireBulletOnActivate gunFire;

    [Header("Target")]
    [SerializeField] private TargetSpawner targetSpawner;

    [Header("Reaching Detection")]
    [SerializeField] private float reachingDistance = 0.5f;
    [SerializeField] private float reachingSpeedThreshold = 0.05f;

    [Header("Aiming Detection")]
    [SerializeField] private float aimingAngleThreshold = 10f;

    [Header("Searching Detection")]
    [SerializeField] private float searchingAngularSpeed = 15f;
    [SerializeField] private float searchingStopDelay = 0.5f;

    [Header("State Durations")]
    [SerializeField] private float holdingDuration = 0.3f;
    [SerializeField] private float firingDuration = 0.15f;
    [SerializeField] private float postFiringDuration = 0.3f;

    [Header("Discovery")]
    [SerializeField] private float discoveryInterval = 1f;

    private bool wasGunHeld;

    private bool firingActive;
    private bool postFiringActive;

    private float firingEndTime;
    private float postFiringEndTime;

    private float nextDiscoveryTime;

    private Quaternion previousHeadRotation;
    private bool hasPreviousHeadRotation;

    private Vector3 previousLeftPosition;
    private Vector3 previousRightPosition;

    private bool hasPreviousControllerPositions;

    private float lastSearchingMotionTime = -100f;

    private void Start()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<ShootingUserStateMachine>();

        DiscoverReferences();

        if (gunGrab != null)
            wasGunHeld = gunGrab.isSelected;
    }

    private void OnDestroy()
    {
        if (gunFire != null)
            gunFire.OnBulletFired -= HandleBulletFired;
    }

    private void DiscoverReferences()
    {
        if (headsetCamera == null && Camera.main != null)
            headsetCamera = Camera.main.transform;

        if (targetSpawner == null)
            targetSpawner = FindFirstObjectByType<TargetSpawner>();

        if (gunFire == null)
            gunFire = FindFirstObjectByType<FireBulletOnActivate>();

        if (gunFire != null)
        {
            if (gunGrab == null)
                gunGrab = gunFire.GetComponent<XRGrabInteractable>();

            if (gunMuzzle == null)
                gunMuzzle = gunFire.spawnPoint;

            gunFire.OnBulletFired -= HandleBulletFired;
            gunFire.OnBulletFired += HandleBulletFired;
        }

        XRDirectInteractor[] interactors =
            FindObjectsByType<XRDirectInteractor>(
                FindObjectsSortMode.None
            );

        foreach (XRDirectInteractor interactor in interactors)
        {
            Transform current = interactor.transform;

            while (current != null)
            {
                string objectName = current.name.ToLowerInvariant();

                if (objectName.Contains("left"))
                {
                    if (leftController == null)
                        leftController = interactor.transform;

                    break;
                }

                if (objectName.Contains("right"))
                {
                    if (rightController == null)
                        rightController = interactor.transform;

                    break;
                }

                current = current.parent;
            }
        }
    }

    private void Update()
    {
        if (stateMachine == null)
            return;

        if (Time.time >= nextDiscoveryTime)
        {
            nextDiscoveryTime = Time.time + discoveryInterval;
            DiscoverReferences();
        }

        bool isGunHeld =
            gunGrab != null && gunGrab.isSelected;

        bool isSearching = DetectSearching();

        bool isReaching = DetectReaching();

        // Gun released: event, not state
        if (wasGunHeld && !isGunHeld)
        {
            Debug.Log(
                "[SHOOTING EVENT] GunReleased | Frame: " +
                Time.frameCount
            );

            firingActive = false;
            postFiringActive = false;

            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.Idle
            );
        }

        // Gun grabbed
        if (!wasGunHeld && isGunHeld)
        {
            Debug.Log(
                "[SHOOTING EVENT] GunGrabbed | Frame: " +
                Time.frameCount
            );

            firingActive = false;
            postFiringActive = false;

            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.HoldingIdle
            );

            wasGunHeld = true;
            return;
        }

        wasGunHeld = isGunHeld;

        // Gun not held
        if (!isGunHeld)
        {
            if (isReaching)
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

            return;
        }

        // Initial holding period
        if (stateMachine.CurrentState ==
            ShootingUserStateMachine.ShootingState.HoldingIdle &&
            stateMachine.StateDuration < holdingDuration)
        {
            return;
        }

        // Firing
        if (firingActive)
        {
            if (Time.time < firingEndTime)
                return;

            firingActive = false;
            postFiringActive = true;

            postFiringEndTime =
                Time.time + postFiringDuration;

            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.PostFiring
            );

            return;
        }

        // PostFiring
        if (postFiringActive)
        {
            if (Time.time < postFiringEndTime)
                return;

            postFiringActive = false;
        }

        GameObject target =
            targetSpawner != null
                ? targetSpawner.CurrentTarget
                : null;

        // No target available
        if (target == null)
        {
            SetSearchingOrHoldingIdle(isSearching);
            return;
        }

        // Target outside headset view
        if (!IsTargetVisible(target.transform))
        {
            SetSearchingOrHoldingIdle(isSearching);
            return;
        }

        // Target visible and gun aligned
        if (IsAimingAtTarget(target.transform))
        {
            stateMachine.ChangeState(
                ShootingUserStateMachine.ShootingState.Aiming
            );

            return;
        }

        // Target visible, but gun not aligned
        stateMachine.ChangeState(
            ShootingUserStateMachine.ShootingState.TargetLocated
        );
    }

    private void SetSearchingOrHoldingIdle(bool isSearching)
    {
        stateMachine.ChangeState(
            isSearching
                ? ShootingUserStateMachine.ShootingState.Searching
                : ShootingUserStateMachine.ShootingState.HoldingIdle
        );
    }

    private bool DetectSearching()
    {
        if (headsetCamera == null)
            return false;

        Quaternion currentRotation = headsetCamera.rotation;

        if (!hasPreviousHeadRotation)
        {
            previousHeadRotation = currentRotation;
            hasPreviousHeadRotation = true;
            return false;
        }

        float angle = Quaternion.Angle(
            previousHeadRotation,
            currentRotation
        );

        float angularSpeed =
            angle / Mathf.Max(Time.deltaTime, 0.0001f);

        previousHeadRotation = currentRotation;

        if (angularSpeed >= searchingAngularSpeed)
            lastSearchingMotionTime = Time.time;

        return Time.time - lastSearchingMotionTime
            <= searchingStopDelay;
    }

    private bool DetectReaching()
    {
        if (gunGrab == null)
            return false;

        Vector3 gunPosition = gunGrab.transform.position;

        bool reaching = false;

        if (leftController != null)
        {
            Vector3 current = leftController.position;

            if (hasPreviousControllerPositions)
            {
                float distance =
                    Vector3.Distance(current, gunPosition);

                float previousDistance =
                    Vector3.Distance(
                        previousLeftPosition,
                        gunPosition
                    );

                float speed =
                    Vector3.Distance(
                        current,
                        previousLeftPosition
                    ) / Mathf.Max(Time.deltaTime, 0.0001f);

                if (distance < reachingDistance &&
                    distance < previousDistance &&
                    speed > reachingSpeedThreshold)
                {
                    reaching = true;
                }
            }

            previousLeftPosition = current;
        }

        if (rightController != null)
        {
            Vector3 current = rightController.position;

            if (hasPreviousControllerPositions)
            {
                float distance =
                    Vector3.Distance(current, gunPosition);

                float previousDistance =
                    Vector3.Distance(
                        previousRightPosition,
                        gunPosition
                    );

                float speed =
                    Vector3.Distance(
                        current,
                        previousRightPosition
                    ) / Mathf.Max(Time.deltaTime, 0.0001f);

                if (distance < reachingDistance &&
                    distance < previousDistance &&
                    speed > reachingSpeedThreshold)
                {
                    reaching = true;
                }
            }

            previousRightPosition = current;
        }

        hasPreviousControllerPositions = true;

        return reaching;
    }

    private bool IsTargetVisible(Transform target)
    {
        if (headsetCamera == null || target == null)
            return false;

        Camera camera =
            headsetCamera.GetComponent<Camera>();

        if (camera == null)
            return false;

        Vector3 viewportPosition =
            camera.WorldToViewportPoint(target.position);

        return viewportPosition.z > 0 &&
               viewportPosition.x >= 0 &&
               viewportPosition.x <= 1 &&
               viewportPosition.y >= 0 &&
               viewportPosition.y <= 1;
    }

    private bool IsAimingAtTarget(Transform target)
    {
        if (gunMuzzle == null || target == null)
            return false;

        Vector3 direction =
            target.position - gunMuzzle.position;

        if (direction.sqrMagnitude < 0.000001f)
            return false;

        float angle = Vector3.Angle(
            gunMuzzle.forward,
            direction.normalized
        );

        return angle <= aimingAngleThreshold;
    }

    private void HandleBulletFired()
    {
        if (gunGrab == null || !gunGrab.isSelected)
            return;

        firingActive = true;
        postFiringActive = false;

        firingEndTime = Time.time + firingDuration;

        stateMachine.ChangeState(
            ShootingUserStateMachine.ShootingState.Firing
        );
    }
}