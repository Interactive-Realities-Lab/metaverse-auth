using System;
using System.IO;
using UnityEngine;

public class BowlingDataLogger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BowlingUserStateMachine stateMachine;
    [SerializeField] private BowlingUserActivityDetector activityDetector;

    [Header("Optional References")]
    [SerializeField] private Transform headset;
    [SerializeField] private Rigidbody ballRigidbody;

    [Header("Bowling Score")]
    [SerializeField] private Scoreboard scoreboard;

    private Transform leftController;
    private Transform rightController;

    private StreamWriter writer;
    private string filePath;
    private bool isLogging;

    private void Start()
    {
        GameObject headObj = GameObject.FindGameObjectWithTag("MainCamera");

        if (headObj != null)
            headset = headObj.transform;
                
        // Find controllers from Authentication scene
        GameObject leftObj =
            GameObject.FindGameObjectWithTag("LeftController");

        GameObject rightObj =
            GameObject.FindGameObjectWithTag("RightController");

        if (leftObj != null)
            leftController = leftObj.transform;

        if (rightObj != null)
            rightController = rightObj.transform;

        if (scoreboard == null)
        {
            scoreboard = FindFirstObjectByType<Scoreboard>();
        }

        Debug.Log(
            "SCOREBOARD FOUND: " + (scoreboard != null)
        );

        // Create unique CSV file
        string fileName =
            "BowlingData_" +
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") +
            ".csv";

        filePath = Path.Combine(
            Application.persistentDataPath,
            fileName
        );

        writer = new StreamWriter(filePath);

        // CSV header
        writer.WriteLine(
            "Frame,Time,State,ActiveHand,Score," +

            "HeadPosX,HeadPosY,HeadPosZ," +
            "HeadRotX,HeadRotY,HeadRotZ," +

            "LeftPosX,LeftPosY,LeftPosZ," +
            "LeftRotX,LeftRotY,LeftRotZ," +

            "RightPosX,RightPosY,RightPosZ," +
            "RightRotX,RightRotY,RightRotZ," +

            "BallPosX,BallPosY,BallPosZ," +
            "BallVelX,BallVelY,BallVelZ"
        );

        isLogging = true;

        Debug.Log("BOWLING DATA LOGGING STARTED");
        Debug.Log("CSV FILE: " + filePath);
    }

    private void Update()
    {
        if (!isLogging || writer == null)
            return;

        LogFrame();
    }

    private void LogFrame()
    {
        string state =
            stateMachine != null
            ? stateMachine.GetCurrentStateName()
            : "Unknown";

        string hand =
            activityDetector != null
            ? activityDetector.CurrentHand.ToString()
            : "None";

        Vector3 headPos =
            headset != null ? headset.position : Vector3.zero;

        Vector3 headRot =
            headset != null ? headset.eulerAngles : Vector3.zero;

        Vector3 leftPos =
            leftController != null
            ? leftController.position
            : Vector3.zero;

        Vector3 leftRot =
            leftController != null
            ? leftController.eulerAngles
            : Vector3.zero;

        Vector3 rightPos =
            rightController != null
            ? rightController.position
            : Vector3.zero;

        Vector3 rightRot =
            rightController != null
            ? rightController.eulerAngles
            : Vector3.zero;

        Vector3 ballPos =
            ballRigidbody != null
            ? ballRigidbody.position
            : Vector3.zero;

        Vector3 ballVel =
            ballRigidbody != null
            ? ballRigidbody.linearVelocity
            : Vector3.zero;

        int currentScore = scoreboard != null
            ? scoreboard.CurrentScore
            : 0;

        writer.WriteLine(
            $"{Time.frameCount}," +
            $"{Time.time:F4}," +
            $"{state}," +
            $"{hand}," +
            $"{currentScore}," + 

            $"{headPos.x:F5},{headPos.y:F5},{headPos.z:F5}," +
            $"{headRot.x:F5},{headRot.y:F5},{headRot.z:F5}," +

            $"{leftPos.x:F5},{leftPos.y:F5},{leftPos.z:F5}," +
            $"{leftRot.x:F5},{leftRot.y:F5},{leftRot.z:F5}," +

            $"{rightPos.x:F5},{rightPos.y:F5},{rightPos.z:F5}," +
            $"{rightRot.x:F5},{rightRot.y:F5},{rightRot.z:F5}," +

            $"{ballPos.x:F5},{ballPos.y:F5},{ballPos.z:F5}," +
            $"{ballVel.x:F5},{ballVel.y:F5},{ballVel.z:F5}"
        );
    }

    private void OnDestroy()
    {
        StopLogging();
    }

    private void OnApplicationQuit()
    {
        StopLogging();
    }

    private void StopLogging()
    {
        if (writer == null)
            return;

        writer.Flush();
        writer.Close();
        writer = null;

        Debug.Log("BOWLING DATA SAVED: " + filePath);
    }
}