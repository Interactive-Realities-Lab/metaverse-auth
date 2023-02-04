using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CheckHeadMovements : MonoBehaviour
{
    public GameObject a, b;

    [SerializeField] private Animator stateMachine;
    [SerializeField] private Transform head;
    private float rotDiff;

    [Range(0f, 1f)]
    [SerializeField] private float threashold = 1;
    private Quaternion prevRot;

    private int currSample;
    private int samplingRate = 200;
    private bool isEnabled = false;

    [SerializeField] public bool IsRotating { get; private set; }

    // Update is called once per frame
    //void FixedUpdate()
    //{
    //    if (!isEnabled) return;

    //    var currentRot = head.rotation;

    //    rotDiff = Mathf.Abs( Quaternion.Dot(currentRot, prevRot));

    //    if (rotDiff < threashold)
    //    {
    //        if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Sampling"))
    //            stateMachine.SetTrigger("GotoSampling");
    //    }
    //    else
    //    {
    //        if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Paused"))
    //            stateMachine.SetTrigger("GotoPaused");
    //    }

    //    if (currSample % samplingRate == 0)
    //        prevRot = currentRot;

    //    currSample++;
    //}

    private void Start()
    {
        
    }

    private void LateUpdate()
    {
        float intervalShakeIntensity = 0;
        UpdateValues(head);
        intervalShakeIntensity += GetShakeIntensity();
        

        float timedelay = 1 - NormalizedShakeIntensity(intervalShakeIntensity);


        Debug.Log( "Intensity: " + intervalShakeIntensity.ToString("F4") + "\n" + "Delay: " + timedelay.ToString("F4"));


        //performanceBarMaterial.color = lowPerformanceColor * timedelay + highPerformanceColor * (1 - timedelay);
        //performanceBar.transform.localScale = new Vector3((1 - timedelay) * 0.9f + 0.1f, 1, 1);
        //performanceBar.transform.localPosition = new Vector3(timedelay * 0.45f, 0, 0);
    }


    //private void Update()
    //{
    //    Debug.Log(Diffa(a.transform.rotation, b.transform.rotation));
    //}
    public void DisableCheck()
    {
        isEnabled = false;
    }

    public void EnableCheck()
    {
        isEnabled = true;
    }


    List<float> timeStamp = new List<float>();
    List<Vector3> positions = new List<Vector3>();
    List<Quaternion> rotations = new List<Quaternion>();
    public float interval = 0.3f;

    [Tooltip("Max shaking score that yields no control latency.")]
    public float maxForNormalization = 1;
    [Tooltip("Min shaking score to trigger a movement with the maximum control latency.")]
    public float minForNormalization = 0.5f;

    /// <summary>
    /// Insert and maintain the desires time interval in the lists
    /// </summary>
    /// <returns></returns>
    public void UpdateValues(Transform obj)
    {
        timeStamp.Add(Time.time);
        positions.Add(obj.localPosition);

        while (timeStamp.Count > 2 && (timeStamp[timeStamp.Count - 1] - timeStamp[0]) > interval)
        {
            timeStamp.RemoveAt(0);
            positions.RemoveAt(0);
        }
    }


    /// <summary>
    /// Shake intensity based on the sum of discrete acceleration values in the interval
    /// </summary>
    /// <returns></returns>
    public float GetShakeIntensity()
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

    private float NormalizedShakeIntensity(float intervalShakeIntensity)
    {
        return Mathf.Clamp((intervalShakeIntensity - minForNormalization) / (maxForNormalization - minForNormalization), 0, 1);
    }

    public Quaternion Diffa( Quaternion to, Quaternion from)
    {
        return to * Quaternion.Inverse(from);
    }


}
