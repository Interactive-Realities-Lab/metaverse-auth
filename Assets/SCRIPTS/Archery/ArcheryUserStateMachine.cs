using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ArcheryUserStateMachine : MonoBehaviour
{
    public enum Activity
    {
        Idle,
        ReachingBow,
        HoldingOnlyBow,
        ReachingArrow,
        HoldingArrow,
        Nocking,
        Drawing,
        Aiming,
        Firing,
        PostFiring,
        ObservingTarget
    }

    [Header("Animator")]
    [SerializeField] private Animator activityAnimator;

    [Header("Existing Archery Components")]
    [SerializeField] private XRGrabInteractable bow;
    [SerializeField] private Puller puller;
    [SerializeField] private Transform notch;
    [SerializeField] private Transform quiverZone;

    [Header("Player Tracking")]
    [SerializeField] private Transform headset;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("Targets")]
    [SerializeField] private Transform[] starTargets;

    [Header("Thresholds")]
    [SerializeField] private float reachBowDistance = 0.45f;
    [SerializeField] private float reachArrowDistance = 0.45f;
    [SerializeField] private float nockDistance = 0.18f;
    [SerializeField] private float drawingThreshold = 0.08f;
    [SerializeField] private float aimingPullThreshold = 0.65f;
    [SerializeField] private float aimingStabilityTolerance = 0.04f;
    [SerializeField] private float aimingHoldSeconds = 0.4f;
    [SerializeField] private float firingStateSeconds = 0.12f;
    [SerializeField] private float postFiringSeconds = 0.35f;
    [SerializeField] private float observingAngle = 15f;
    [SerializeField] private float observingHoldSeconds = 0.5f;

    public Activity CurrentActivity { get; private set; } = Activity.Idle;

    public event Action<Activity, Activity> OnActivityChanged;

    // The active unfired arrow is supplied by BackQuiverGesture.
    private Arrow activeArrow;
    private bool shotPending;
    private float shotTime;
    private float lastPull;
    private float stableDrawTime;
    private float lookingAtTargetTime;

    private void Start()
    {
        if (activityAnimator == null)
            activityAnimator = GetComponent<Animator>();

        if (headset == null && Camera.main != null)
            headset = Camera.main.transform;

        SetActivity(Activity.Idle);
    }

    private void Update()
    {
        DetectPlayerReferences();

        bool bowHeld = bow != null && bow.isSelected;

        if (activityAnimator != null)
            activityAnimator.SetBool("BowHeld", bowHeld);

        if (!bowHeld)
        {
            activeArrow = null;
            shotPending = false;
            stableDrawTime = 0f;
            lookingAtTargetTime = 0f;

            SetActivity(IsReachingBow()
                ? Activity.ReachingBow
                : Activity.Idle);

            return;
        }

        // Firing and post-firing are timed states.
        if (shotPending)
        {
            float elapsed = Time.time - shotTime;

            if (elapsed < firingStateSeconds)
            {
                SetActivity(Activity.Firing);
                return;
            }

            if (elapsed < firingStateSeconds + postFiringSeconds)
            {
                SetActivity(Activity.PostFiring);
                return;
            }

            shotPending = false;
        }

        bool reachingArrow = IsReachingArrow();

        // A new reach takes priority over target observation.
        if (activeArrow == null && reachingArrow)
        {
            lookingAtTargetTime = 0f;
            SetActivity(Activity.ReachingArrow);
            return;
        }

        if (activeArrow == null)
        {
            UpdateTargetObservation();

            if (lookingAtTargetTime >= observingHoldSeconds)
            {
                SetActivity(Activity.ObservingTarget);
                return;
            }

            // Preserve the observation state until the player
            // stops looking at the target or reaches for an arrow.
            if (CurrentActivity == Activity.ObservingTarget &&
                IsLookingAtTarget())
            {
                return;
            }

            SetActivity(Activity.HoldingOnlyBow);
            return;
        }

        float pullAmount = puller != null
            ? puller.PullAmount
            : 0f;

        bool pulling = puller != null && puller.isSelected;

        bool arrowNocked = IsArrowNearNotch();

        if (pulling && pullAmount >= drawingThreshold)
        {
            if (pullAmount >= aimingPullThreshold &&
                Mathf.Abs(pullAmount - lastPull) <= aimingStabilityTolerance)
            {
                stableDrawTime += Time.deltaTime;
            }
            else
            {
                stableDrawTime = 0f;
            }

            SetActivity(stableDrawTime >= aimingHoldSeconds
                ? Activity.Aiming
                : Activity.Drawing);
        }
        else if (arrowNocked)
        {
            stableDrawTime = 0f;
            SetActivity(Activity.Nocking);
        }
        else
        {
            stableDrawTime = 0f;
            SetActivity(Activity.HoldingArrow);
        }

        lastPull = pullAmount;
    }

    // Call this after the back quiver successfully spawns an arrow.
    public void NotifyArrowRetrieved(Arrow arrow)
    {
        if (arrow == null || bow == null || !bow.isSelected)
            return;

        activeArrow = arrow;
        shotPending = false;
        lookingAtTargetTime = 0f;

        SetActivity(Activity.HoldingArrow);
    }

    // Call this when the arrow is actually launched.
    public void NotifyArrowFired(Arrow arrow)
    {
        if (arrow == null || arrow != activeArrow)
            return;

        activeArrow = null;
        shotPending = true;
        shotTime = Time.time;
        stableDrawTime = 0f;
        lookingAtTargetTime = 0f;

        SetActivity(Activity.Firing);
    }

    public void NotifyArrowRemoved(Arrow arrow)
    {
        if (arrow == activeArrow)
        {
            activeArrow = null;
            stableDrawTime = 0f;
        }
    }

    private bool IsArrowNearNotch()
    {
        if (activeArrow == null || notch == null)
            return false;

        return Vector3.Distance(
            activeArrow.transform.position,
            notch.position
        ) <= nockDistance;
    }

    private bool IsReachingBow()
    {
        if (bow == null)
            return false;

        return IsNear(leftController, bow.transform.position,
                   reachBowDistance) ||
               IsNear(rightController, bow.transform.position,
                   reachBowDistance);
    }

    private bool IsReachingArrow()
    {
        if (quiverZone == null || headset == null)
            return false;

        return IsHandBehindHeadNearQuiver(leftController) ||
               IsHandBehindHeadNearQuiver(rightController);
    }

    private bool IsHandBehindHeadNearQuiver(Transform controller)
    {
        if (controller == null)
            return false;

        Vector3 forward = headset.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return false;

        forward.Normalize();

        bool behind = Vector3.Dot(
            forward,
            controller.position - headset.position
        ) < -0.05f;

        return behind &&
               IsNear(controller, quiverZone.position,
                   reachArrowDistance);
    }

    private bool IsNear(Transform controller, Vector3 position,
        float threshold)
    {
        return controller != null &&
               Vector3.Distance(controller.position, position)
                   <= threshold;
    }

    private bool IsLookingAtTarget()
    {
        if (headset == null || starTargets == null)
            return false;

        foreach (Transform target in starTargets)
        {
            if (target == null)
                continue;

            Vector3 direction =
                target.position - headset.position;

            if (direction.sqrMagnitude < 0.001f)
                continue;

            float angle = Vector3.Angle(
                headset.forward,
                direction.normalized
            );

            if (angle <= observingAngle)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateTargetObservation()
    {
        if (IsLookingAtTarget())
            lookingAtTargetTime += Time.deltaTime;
        else
            lookingAtTargetTime = 0f;
    }

    private void DetectPlayerReferences()
    {
        if (headset == null && Camera.main != null)
            headset = Camera.main.transform;

        if (leftController == null)
        {
            GameObject obj =
                GameObject.FindGameObjectWithTag("LeftController");

            if (obj != null)
                leftController = obj.transform;
        }

        if (rightController == null)
        {
            GameObject obj =
                GameObject.FindGameObjectWithTag("RightController");

            if (obj != null)
                rightController = obj.transform;
        }
    }

    private void SetActivity(Activity next)
    {
        if (CurrentActivity == next)
            return;

        Activity previous = CurrentActivity;
        CurrentActivity = next;

        if (activityAnimator != null)
        {
            activityAnimator.SetInteger(
                "Activity",
                (int)CurrentActivity
            );
        }

        Debug.Log(
            $"ARCHERY STATE: {previous} -> {next}"
        );

        OnActivityChanged?.Invoke(previous, next);
    }
}