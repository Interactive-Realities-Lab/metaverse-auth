using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ArcheryDataLogger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArcheryUserStateMachine stateMachine;
    [SerializeField] private XRGrabInteractable bow;
    [SerializeField] private Puller puller;

    [Header("XR Tracking")]
    [SerializeField] private Transform headset;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("Logging Settings")]
    [SerializeField] private bool logEveryFrame = true;
    [SerializeField] private bool logStateChanges = true;
    [SerializeField] private bool flushEveryFrame = false;

    private StreamWriter writer;
    private string filePath;
    private bool isLogging;
    private float startTime;
    private string previousState = "";

    // Tracks the arrow involved in the current shot.
    private Arrow activeArrow;

    // A one-frame event flag.
    private bool arrowFiredThisFrame;

    private void Start()
    {
        DetectReferences();
        StartLogging();
    }

    private void LateUpdate()
    {
        DetectReferences();

        if (!isLogging || !logEveryFrame)
            return;

        WriteFrame();
    }

    private void DetectReferences()
    {
        if (stateMachine == null)
            stateMachine = FindFirstObjectByType<ArcheryUserStateMachine>();

        if (bow == null)
            bow = FindFirstObjectByType<Bow>();

        if (puller == null)
            puller = FindFirstObjectByType<Puller>();

        if (headset == null && Camera.main != null)
            headset = Camera.main.transform;

        if (leftController == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("LeftController");

            if (obj != null)
                leftController = obj.transform;
        }

        if (rightController == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("RightController");

            if (obj != null)
                rightController = obj.transform;
        }
    }

    private void StartLogging()
    {
        if (isLogging)
            return;

        string folder = Path.Combine(
            Application.persistentDataPath,
            "ArcheryLogs"
        );

        Directory.CreateDirectory(folder);

        string fileName =
            "Archery_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".csv";

        filePath = Path.Combine(folder, fileName);

        writer = new StreamWriter(filePath, false, Encoding.UTF8);

        writer.WriteLine(
            "Frame,TimestampUTC,ElapsedTime,Event,ActivityState," +
            "BowHeld,BowHoldingHand,ArrowHoldingHand,DrawingHand,ArrowFired," +
            "PullerSelected,PullAmount," +
            "HeadPosX,HeadPosY,HeadPosZ," +
            "HeadRotX,HeadRotY,HeadRotZ,HeadRotW," +
            "LeftPosX,LeftPosY,LeftPosZ," +
            "LeftRotX,LeftRotY,LeftRotZ,LeftRotW," +
            "RightPosX,RightPosY,RightPosZ," +
            "RightRotX,RightRotY,RightRotZ,RightRotW"
        );

        startTime = Time.time;
        isLogging = true;

        SubscribeToEvents();

        Debug.Log("ARCHERY LOGGER STARTED: " + filePath);
    }

    private void SubscribeToEvents()
    {
        // Arrow retrieval/firing notifications are provided
        // by the existing gameplay scripts.
        ArcheryLoggerEvents.ArrowRetrieved += OnArrowRetrieved;
        ArcheryLoggerEvents.ArrowFired += OnArrowFired;
    }

    private void UnsubscribeFromEvents()
    {
        ArcheryLoggerEvents.ArrowRetrieved -= OnArrowRetrieved;
        ArcheryLoggerEvents.ArrowFired -= OnArrowFired;
    }

    private void OnArrowRetrieved(Arrow arrow)
    {
        activeArrow = arrow;
    }

    private void OnArrowFired(Arrow arrow)
    {
        if (arrow == activeArrow)
            activeArrow = null;

        arrowFiredThisFrame = true;
    }

    private void WriteFrame()
    {
        string currentState = stateMachine != null
            ? stateMachine.CurrentActivity.ToString()
            : "Unknown";

        string eventName = "Frame";

        if (arrowFiredThisFrame)
        {
            eventName = "ArrowFired";
        }
        else if (logStateChanges && currentState != previousState)
        {
            eventName = "StateChanged";
        }

        previousState = currentState;

        WriteRow(eventName, currentState);

        // Reset after writing the event.
        arrowFiredThisFrame = false;
    }

    private void WriteRow(string eventName, string activityState)
    {
        if (writer == null)
            return;

        StringBuilder row = new StringBuilder(512);

        row.Append(Time.frameCount).Append(",");
        row.Append(DateTime.UtcNow.ToString("O")).Append(",");
        row.Append(F(Time.time - startTime)).Append(",");
        row.Append(eventName).Append(",");
        row.Append(activityState).Append(",");

        bool bowHeld = bow != null && bow.isSelected;

        row.Append(bowHeld ? 1 : 0).Append(",");
        row.Append(GetHoldingHand(bow)).Append(",");
        row.Append(GetHoldingHand(activeArrow)).Append(",");
        row.Append(GetDrawingHand()).Append(",");
        row.Append(arrowFiredThisFrame ? 1 : 0).Append(",");

        row.Append(puller != null && puller.isSelected ? 1 : 0).Append(",");
        row.Append(F(puller != null ? puller.PullAmount : 0f)).Append(",");

        AppendTransform(row, headset);
        row.Append(",");

        AppendTransform(row, leftController);
        row.Append(",");

        AppendTransform(row, rightController);

        writer.WriteLine(row.ToString());

        if (flushEveryFrame)
            writer.Flush();
    }

    private string GetHoldingHand(XRGrabInteractable interactable)
    {
        if (interactable == null || !interactable.isSelected)
            return "None";

        foreach (var interactor in interactable.interactorsSelecting)
        {
            string hand = IdentifyHand(interactor.transform);

            if (hand != "None")
                return hand;
        }

        return "None";
    }

    private string GetDrawingHand()
    {
        if (puller == null || !puller.isSelected)
            return "None";

        foreach (var interactor in puller.interactorsSelecting)
        {
            string hand = IdentifyHand(interactor.transform);

            if (hand != "None")
                return hand;
        }

        return "None";
    }

    private string IdentifyHand(Transform interactorTransform)
    {
        if (interactorTransform == null)
            return "None";

        if (leftController != null &&
            (interactorTransform == leftController ||
             interactorTransform.IsChildOf(leftController.parent)))
        {
            return "Left";
        }

        if (rightController != null &&
            (interactorTransform == rightController ||
             interactorTransform.IsChildOf(rightController.parent)))
        {
            return "Right";
        }

        return "None";
    }

    private void AppendTransform(StringBuilder row, Transform target)
    {
        if (target == null)
        {
            row.Append(",,,,,,");
            return;
        }

        Vector3 p = target.position;
        Quaternion q = target.rotation;

        row.Append(F(p.x)).Append(",");
        row.Append(F(p.y)).Append(",");
        row.Append(F(p.z)).Append(",");
        row.Append(F(q.x)).Append(",");
        row.Append(F(q.y)).Append(",");
        row.Append(F(q.z)).Append(",");
        row.Append(F(q.w));
    }

    private string F(float value)
    {
        return value.ToString("F6", CultureInfo.InvariantCulture);
    }

    private void OnDisable()
    {
        StopLogging();
    }

    private void OnDestroy()
    {
        StopLogging();
    }

    private void StopLogging()
    {
        if (!isLogging)
            return;

        UnsubscribeFromEvents();

        isLogging = false;

        writer?.Flush();
        writer?.Close();
        writer = null;

        Debug.Log("ARCHERY LOGGER SAVED: " + filePath);
    }
}