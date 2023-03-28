using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CheckHeadMovements : MonoBehaviour
{

    [SerializeField] private Animator stateMachine;
    [SerializeField] private Transform head;

    List<float> timeStamp = new List<float>();
    List<Vector3> positions = new List<Vector3>();
    List<Quaternion> rotations = new List<Quaternion>();
    private float interval = 2f;
    private float maxForNormalization = 1f;
    private float minForNormalization = 0f;

    private bool isEnabled = false;

    private bool allowPauseSampling = false;

    [SerializeField] public bool IsRotating { get; private set; }


    private void LateUpdate()
    {
        if (!isEnabled)
        {
            ClearValues();
            return;
        }


        float intervalRotationIntensity = 0;
        UpdateValues(head);
        //intervalShakeIntensity += GetShakeIntensity();
        intervalRotationIntensity += GetRotationIntensity(); 

        float rotIntensityNormalized = NormalizedIntensity(intervalRotationIntensity);

        if (rotIntensityNormalized <= 0f)
        {
            if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Paused") && allowPauseSampling)
                stateMachine.SetTrigger("GotoPaused");
        }
        else
        {
            if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Sampling"))
                stateMachine.SetTrigger("GotoSampling");

        }
        //IsRotating = true;
        ////Debug.Log( "Intensity: " + intervalRotationIntensity.ToString("F4") + "\n" + "Delay: " + rotIntensityNormalized.ToString("F4"));
        //Debug.Log(IsRotating);
    }

    public void EnableCheck(bool var)
    {
        isEnabled = var;
    }

    /// Insert and maintain the desires time interval in the lists
    public void UpdateValues(Transform obj)
    {
        timeStamp.Add(Time.time);
        positions.Add(obj.localPosition);
        rotations.Add(obj.localRotation);

        while (timeStamp.Count > 2 && (timeStamp[timeStamp.Count - 1] - timeStamp[0]) > interval)
        {
            timeStamp.RemoveAt(0);
            positions.RemoveAt(0);
            rotations.RemoveAt(0);
        }
    }

    private void ClearValues()
    {
        timeStamp.Clear();
        positions.Clear();
        rotations.Clear();
    }



    /// Movement intensity based on the sum of discrete acceleration values in the interval
    public float GetPositionIntensity()
    {
        float intensity = 0;
        for (int i = 0; i < positions.Count - 2; i++)
        {
            float acc = Mathf.Abs((positions[i + 2] - positions[i + 1]).magnitude - (positions[i + 1] - positions[i]).magnitude);
            acc = acc / ((timeStamp[i + 2] - timeStamp[i]) * 0.5f);
            intensity += acc;
        }
        intensity /= positions.Count;
        return intensity;
    }

    /// Rotation intensity based on the sum of discrete acceleration values in the interval
    public float GetRotationIntensity()
    {
        float intensity = 0;
        for (int i = 0; i < rotations.Count - 2; i++)
        {
            float acc = Quaternion.Angle(rotations[i + 2], rotations[i]);
            acc = acc / ((timeStamp[i + 2] - timeStamp[i]) * 0.5f);
            intensity += acc;
        }
        intensity /= positions.Count;
        return intensity;
    }


    private float NormalizedIntensity(float intervalShakeIntensity)
    {
        return Mathf.Clamp((intervalShakeIntensity - minForNormalization) / (maxForNormalization - minForNormalization), 0, 1);
    }

    public void TooglePauseSampling(bool value)
    {
        allowPauseSampling = value;
    }

}
