using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Logs PARITY STATE TRANSITIONS (not every frame), so you can see exactly
/// where (in head/IMU orientation) parity gets lost, how long it stays lost,
/// and where it re-establishes.
///
/// Events written:
///   BUILT        -> transition into Matched (parity established)
///   LOST         -> transition out of Matched (parity lost)
///   STILL_LOST   -> heartbeat row while remaining in NotMatched (optional, every N seconds)
///   REAUTH_TIMEOUT -> parity stayed lost long enough to force logout
///
/// Wire this up from HeadsetMotion.cs at the same points where it already
/// logs "HeadsetMotion: MATCHED" / "HeadsetMotion: LOST" / timeout.
/// </summary>
public class ParityEventLogger : MonoBehaviour
{
    [Header("Logging")]
    public bool enableLogging = true;
    public string sessionId = "S01";
    public bool usePersistentDataPath = true;

    [Header("Heartbeat while LOST")]
    [Tooltip("If true, writes a STILL_LOST row periodically while parity remains lost, so you can see the range of positions it stays broken in.")]
    public bool logStillLostHeartbeat = true;
    public float stillLostHeartbeatSeconds = 1.0f;

    StreamWriter _csv;
    readonly StringBuilder _sb = new StringBuilder(512);
    string _csvPath = "";
    bool _isLogging = false;

    // transition bookkeeping
    float _lastEventAt = -1f;
    float _nextHeartbeatAt = 0f;
    string _currentEventState = "NONE"; // "BUILT" or "LOST" (last transition type)

    private string _currentLossNote = "";

    public void StartLogging()
    {
        if (!enableLogging) return;
        if (_isLogging) return;

        string dir = usePersistentDataPath ? Application.persistentDataPath : Application.dataPath;
        Directory.CreateDirectory(dir);

        string fileName = $"ParityEvents_{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        _csvPath = Path.Combine(dir, fileName);

        _csv = new StreamWriter(_csvPath, false, Encoding.UTF8);

        _csv.WriteLine(
            "utc_iso,unity_time,trial_time,event," +
            "seconds_since_last_event," +
            "questYaw,questPitch,questRoll," +
            "imuYawQ,imuPitchQ,imuRollQ," +
            "confidence,parity_cos,gyro_score,note"
        );
        _csv.Flush();

        _isLogging = true;
        _lastEventAt = Time.time;
        _currentEventState = "NONE";
        _nextHeartbeatAt = Time.time + stillLostHeartbeatSeconds;

        Debug.Log($"ParityEventLogger STARTED: {_csvPath}");
    }

    public void StopLogging()
    {
        if (!_isLogging) return;
        _isLogging = false;

        try
        {
            _csv?.Flush();
            _csv?.Close();
            _csv = null;
        }
        catch { }

        Debug.Log($"ParityEventLogger STOPPED: {_csvPath}");
    }

    /// <summary>
    /// Call this exactly at the moment parity transitions to BUILT or LOST
    /// (or for REAUTH_TIMEOUT). Also resets the "since last event" clock.
    /// </summary>
    public void LogEvent(
        string eventType, // "BUILT", "LOST", "REAUTH_TIMEOUT"
        float trialTime,
        float questYaw, float questPitch, float questRoll,
        float imuYawQ, float imuPitchQ, float imuRollQ,
        float confidence, float parityCos, float gyroScore,
        string note = "")
    {
        if (!_isLogging || _csv == null) return;

        float sinceLast = (_lastEventAt < 0f) ? 0f : (Time.time - _lastEventAt);

        WriteRow(eventType, trialTime, questYaw, questPitch, questRoll,
                 imuYawQ, imuPitchQ, imuRollQ, confidence, parityCos, gyroScore,
                 sinceLast, note);

        _lastEventAt = Time.time;
        _nextHeartbeatAt = Time.time + stillLostHeartbeatSeconds;

        if (eventType == "BUILT")
            _currentEventState = "BUILT";
        else if (eventType == "LOST" || eventType == "REAUTH_TIMEOUT")
            _currentEventState = "LOST";

        if (eventType == "LOST" || eventType == "REAUTH_TIMEOUT")
        _currentLossNote = note;
        
        else if (eventType == "BUILT")
        _currentLossNote = "";
    }

    /// <summary>
    /// Call this every frame (or every Update) from HeadsetMotion. It only
    /// actually writes a row if we're currently in the LOST state AND the
    /// heartbeat interval has elapsed. Cheap to call unconditionally.
    /// </summary>
    public void TickStillLostHeartbeat(
        float trialTime,
        float questYaw, float questPitch, float questRoll,
        float imuYawQ, float imuPitchQ, float imuRollQ,
        float confidence, float parityCos, float gyroScore)
    {
        if (!_isLogging || _csv == null) return;
        if (!logStillLostHeartbeat) return;
        if (_currentEventState != "LOST") return;
        if (Time.time < _nextHeartbeatAt) return;

        _nextHeartbeatAt = Time.time + stillLostHeartbeatSeconds;

        float sinceLast = (_lastEventAt < 0f) ? 0f : (Time.time - _lastEventAt);

        WriteRow("STILL_LOST", trialTime, questYaw, questPitch, questRoll,
                 imuYawQ, imuPitchQ, imuRollQ, confidence, parityCos, gyroScore,
                 sinceLast, _currentLossNote);
    }

    void WriteRow(
        string eventType, float trialTime,
        float questYaw, float questPitch, float questRoll,
        float imuYawQ, float imuPitchQ, float imuRollQ,
        float confidence, float parityCos, float gyroScore,
        float secondsSinceLastEvent, string note)
    {
        string utc = DateTime.UtcNow.ToString("o");

        _sb.Length = 0;
        _sb.Append(utc).Append(',');
        _sb.Append(Time.time.ToString("F2")).Append(',');
        _sb.Append(trialTime.ToString("F2")).Append(',');
        _sb.Append(eventType).Append(',');
        _sb.Append(secondsSinceLastEvent.ToString("F2")).Append(',');

        _sb.Append(questYaw.ToString("F2")).Append(',');
        _sb.Append(questPitch.ToString("F2")).Append(',');
        _sb.Append(questRoll.ToString("F2")).Append(',');

        _sb.Append(imuYawQ.ToString("F2")).Append(',');
        _sb.Append(imuPitchQ.ToString("F2")).Append(',');
        _sb.Append(imuRollQ.ToString("F2")).Append(',');

        _sb.Append(confidence.ToString("F2")).Append(',');
        _sb.Append(parityCos.ToString("F2")).Append(',');
        _sb.Append(gyroScore.ToString("F2")).Append(',');

        _sb.Append(EscapeCsv(note));

        _csv.WriteLine(_sb.ToString());
        _csv.Flush(); // events are rare, flush immediately so nothing is lost on crash
    }

    void OnApplicationQuit()
    {
        StopLogging();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value = value.Replace("\"", "\"\"");

        return $"\"{value}\"";
    }
}