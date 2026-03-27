using System;
using System.IO;
using System.Text;
using UnityEngine;

public class HeadsetMotionCsvLogger : MonoBehaviour
{
    [Header("CSV Logging")]
    public bool enableCsvLogging = true;
    public float logEverySeconds = 1f;
    public string imuPlacementLabel = "TopFront";
    public string sessionId = "S01";
    public bool usePersistentDataPath = true;

    StreamWriter _csv;
    readonly StringBuilder _sb = new StringBuilder(512);
    float _nextLogAt = 0f;
    bool _isLogging = false;
    string _csvPath = "";

    public bool ShouldWriteRow(float time)
    {
        if (!_isLogging) return false;
        if (time < _nextLogAt) return false;

        _nextLogAt = time + Mathf.Max(0.01f, logEverySeconds);
        return true;
    }

    public void StartCsvLogging()
    {
        if (!enableCsvLogging) return;
        if (_isLogging) return;

        string dir = usePersistentDataPath ? Application.persistentDataPath : Application.dataPath;
        Directory.CreateDirectory(dir);

        string fileName = $"HeadsetIMU_{sessionId}_{imuPlacementLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        _csvPath = Path.Combine(dir, fileName);

        _csv = new StreamWriter(_csvPath, false, Encoding.UTF8);

        _csv.WriteLine(
            "utc_iso,unity_time,imu_placement," +
            "questYaw,questPitch,questRoll," +
            "imuPitch,imuRoll,imuYaw," +
            "imu_map_pitchQ,imu_map_yawQ,imu_map_rollQ," +
            "deltaYaw,deltaPitch,deltaRoll," +
            "parity_built,parity_cos"
        );

        _csv.Flush();
        _nextLogAt = Time.time;
        _isLogging = true;

        Debug.Log($"CSV logging STARTED: {_csvPath}");
    }

    public void StopCsvLogging()
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

        Debug.Log($"CSV logging STOPPED: {_csvPath}");
    }

    public void WriteRow(
        float trialTime,
        float questYaw, float questPitch, float questRoll,
        float imuP, float imuR, float imuY,
        float imuPitchQ, float imuYawQ, float imuRollQ,
        float dy, float dp, float dr,
        bool parityBuilt,
        float parityCos)
    {
        if (_csv == null) return;

        string utc = DateTime.UtcNow.ToString("o");

        _sb.Length = 0;
        _sb.Append(utc).Append(',');
        _sb.Append(trialTime.ToString("F2")).Append(',');
        _sb.Append(imuPlacementLabel).Append(',');

        _sb.Append(questYaw.ToString("F2")).Append(',');
        _sb.Append(questPitch.ToString("F2")).Append(',');
        _sb.Append(questRoll.ToString("F2")).Append(',');

        _sb.Append(imuP.ToString("F2")).Append(',');
        _sb.Append(imuR.ToString("F2")).Append(',');
        _sb.Append(imuY.ToString("F2")).Append(',');

        _sb.Append(imuPitchQ.ToString("F2")).Append(',');
        _sb.Append(imuYawQ.ToString("F2")).Append(',');
        _sb.Append(imuRollQ.ToString("F2")).Append(',');

        _sb.Append(dy.ToString("F2")).Append(',');
        _sb.Append(dp.ToString("F2")).Append(',');
        _sb.Append(dr.ToString("F2")).Append(',');

        _sb.Append(parityBuilt ? "1" : "0").Append(',');
        _sb.Append(parityCos.ToString("F2")).Append(',');

        _csv.WriteLine(_sb.ToString());

        if (Time.frameCount % 60 == 0) _csv.Flush();
    }

    void OnApplicationQuit()
    {
        StopCsvLogging();
    }
}