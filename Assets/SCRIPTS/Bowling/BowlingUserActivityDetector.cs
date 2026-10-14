using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowlingUserActivityDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BowlingUserStateMachine stateMachine;
    [SerializeField] private XRGrabInteractable grabInteractable;

    [Header("Controller Transforms")]
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("Reaching Detection")]
    [SerializeField] private float reachingDistance = 0.5f;

    [Header("Post Throw")]
    [SerializeField] private float postThrowDuration = 1.0f;

    private float postThrowTimer = 0f;

    // How much closer the controller must move between frames
    // before we consider it approaching the ball.
    [SerializeField] private float approachThreshold = 0.001f;

    private float previousLeftDistance;
    private float previousRightDistance;

    private bool isHolding = false;

    public enum ActiveHand
    {
        None,
        Left,
        Right
    }

    public ActiveHand CurrentHand { get; private set; } = ActiveHand.None;


    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
            Debug.LogError("XRGrabInteractable NOT FOUND!");

        if (stateMachine == null)
            Debug.LogError("BowlingUserStateMachine NOT ASSIGNED!");
    }


    private void Start()
    {
        // Find controllers from the Authentication scene
        if (leftController == null)
        {
            GameObject leftObject =
                GameObject.FindGameObjectWithTag("LeftController");

            if (leftObject != null)
                leftController = leftObject.transform;
        }

        if (rightController == null)
        {
            GameObject rightObject =
                GameObject.FindGameObjectWithTag("RightController");

            if (rightObject != null)
                rightController = rightObject.transform;
        }

        // Check that both were found
        if (leftController == null)
        {
            Debug.LogError(
                "Left controller was not found. " +
                "Check the LeftController tag."
            );
        }

        if (rightController == null)
        {
            Debug.LogError(
                "Right controller was not found. " +
                "Check the RightController tag."
            );
        }

        // Initialize distances
        if (leftController != null)
        {
            previousLeftDistance =
                Vector3.Distance(
                    leftController.position,
                    transform.position
                );
        }

        if (rightController != null)
        {
            previousRightDistance =
                Vector3.Distance(
                    rightController.position,
                    transform.position
                );
        }

        Debug.Log(
            $"CONTROLLERS FOUND | " +
            $"Left={leftController != null} | " +
            $"Right={rightController != null}"
        );
    }


    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnBallGrabbed);
            grabInteractable.selectExited.AddListener(OnBallReleased);
        }
    }


    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnBallGrabbed);
            grabInteractable.selectExited.RemoveListener(OnBallReleased);
        }
    }


    private void Update()
    {
        DetectReaching();

        // Temporary PostThrow -> Idle rule
        if (stateMachine.CurrentState ==
            BowlingUserStateMachine.UserState.PostThrow)
        {
            postThrowTimer += Time.deltaTime;

            if (postThrowTimer >= postThrowDuration)
            {
                postThrowTimer = 0f;
                CurrentHand = ActiveHand.None;

                stateMachine.ReturnToIdle();

                Debug.Log("POST THROW FINISHED -> IDLE");
            }
        }
    }


    private void DetectReaching()
    {
        if (leftController == null || rightController == null)
            return;

        // Don't perform reaching detection while ball is held.
        if (isHolding)
            return;

        float leftDistance =
            Vector3.Distance(leftController.position, transform.position);

        float rightDistance =
            Vector3.Distance(rightController.position, transform.position);

        float leftChange = previousLeftDistance - leftDistance;
        float rightChange = previousRightDistance - rightDistance;

        // Debug.Log(
        //     $"REACH DEBUG | State={stateMachine.CurrentState} | " +
        //     $"L={leftDistance:F3} change={leftChange:F4} | " +
        //     $"R={rightDistance:F3} change={rightChange:F4}"
        // );

        bool leftApproaching =
            leftDistance < previousLeftDistance - approachThreshold;

        bool rightApproaching =
            rightDistance < previousRightDistance - approachThreshold;


        bool leftReaching =
            leftDistance <= reachingDistance &&
            leftApproaching;

        bool rightReaching =
            rightDistance <= reachingDistance &&
            rightApproaching;


        if (stateMachine.CurrentState ==
            BowlingUserStateMachine.UserState.Idle)
        {
            if (leftReaching || rightReaching)
            {
                // If both qualify, use whichever hand is closer.
                if (leftReaching &&
                    (!rightReaching || leftDistance < rightDistance))
                {
                    CurrentHand = ActiveHand.Left;
                }
                else
                {
                    CurrentHand = ActiveHand.Right;
                }

                stateMachine.StartReaching();

                Debug.Log(
                    $"USER REACHING with {CurrentHand} hand | " +
                    $"L={leftDistance:F3}m R={rightDistance:F3}m"
                );
            }
        }


        previousLeftDistance = leftDistance;
        previousRightDistance = rightDistance;
    }


    private void OnBallGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("===== BALL GRABBED =====");

        isHolding = true;

        // Determine which XR interactor grabbed the ball.
        Transform interactorTransform =
            args.interactorObject.transform;

        float leftDistance =
            Vector3.Distance(
                interactorTransform.position,
                leftController.position
            );

        float rightDistance =
            Vector3.Distance(
                interactorTransform.position,
                rightController.position
            );

        CurrentHand =
            leftDistance < rightDistance
            ? ActiveHand.Left
            : ActiveHand.Right;


        // Grab event
        stateMachine.StartGrabbing();

        Debug.Log($"USER STATE -> Grabbing ({CurrentHand})");

        // For now, immediately transition to Holding.
        stateMachine.StartHolding();

        Debug.Log($"USER STATE -> Holding ({CurrentHand})");
    }


    private void OnBallReleased(SelectExitEventArgs args)
    {
        Debug.Log("===== BALL RELEASED =====");

        isHolding = false;

        postThrowTimer = 0f;

        stateMachine.StartPostThrow();

        Debug.Log($"USER STATE -> PostThrow ({CurrentHand})");
    }
}