using UnityEngine;

public class MatchingAngles : MonoBehaviour
{
    [SerializeField] private Animator stateMachine;

    [SerializeField] private GameObject object1, object2, objectFallback;

    //The current rotation difference between object1 and object2
    private float currentDistance = 0f;

    //The previous sampled rotation difference between the 2 objects
    private float prevDistance = 0f;

    //The reference rotation distance used to test against the currentDistance 
    private float referenceValue = 0f;

    //Rotation diff tolerance  
    [Range(.001f, 1f)]
    [SerializeField] private float threashold = .1f;


    //Controls how long 2 rotations need to match until we consider that they are actually matching 
    public float currentMatchPeriod { get; private set; } = 0;

    //[Range(100, 1000)]
    private int maxMatchPeriod = 1;
    public int maxMatchSamples { get => maxMatchPeriod; private set => maxMatchPeriod = value; }


    //Sampling rate for the previous rotation diff
    private int rotSamplingInterval = 100;
    private int currentRotSampling = 0;


    private bool matched = false;
    private bool allowDisconnect;
    private bool isSamplingPaused;

    public bool isDisconnected { get; set; } = true;

    [Range(0f, 10f)]
    [SerializeField] private float progressSpeed;

    private Transform currentTracker;

    void Update()
    {
        if (isDisconnected) return;

        //UGLY remove this after demo


        currentTracker = (object2.transform.rotation == Quaternion.identity) ? objectFallback.transform : object2.transform;

        var obj1matrix = Matrix4x4.Rotate(object1.transform.rotation);
        var obj2matrix = Matrix4x4.Rotate(currentTracker.transform.rotation);
        currentDistance = Utils.DistMatrices(obj1matrix, obj2matrix);

        //currentDistance = Mathf.Abs(Quaternion.Dot(object1.transform.rotation, object2.transform.rotation));

        var absdiffCurrPrev = Mathf.Abs(prevDistance - currentDistance);

        // Check if rotations are matching for some time 
        if (currentMatchPeriod >= maxMatchSamples && !matched)
        {
            referenceValue = currentDistance;
            matched = true;
            currentMatchPeriod = maxMatchSamples;
            if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Matched"))
                stateMachine.SetTrigger("GotoMatched");
        }

        if (currentMatchPeriod <= 0 && allowDisconnect)
        {
            Disconnect();
            return;
        }

        // If rotations are not matched 
        if (!matched)
        {
            if (isSamplingPaused) return;

            //if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Sampling"))
            //    stateMachine.SetTrigger("GotoSampling");

            // Start sampling if rotartions are starting to match
            if (absdiffCurrPrev < threashold)
                currentMatchPeriod = Mathf.Min(maxMatchSamples, currentMatchPeriod += Time.deltaTime * progressSpeed);

            // Lost matching... reset sampling
            else
                currentMatchPeriod = Mathf.Max(0, currentMatchPeriod -= Time.deltaTime * progressSpeed * 2f);



            // avoid sampling every frame... 
            if (currentRotSampling % rotSamplingInterval == 0)
                prevDistance = currentDistance;

            currentRotSampling++;
            return;
        }



        // Once sampling is done and rotations are matched

        // Start observing if rotations lost matching...
        var currentAbsdiff = Mathf.Abs(referenceValue - currentDistance);

        if (currentAbsdiff > threashold)
        {
            currentMatchPeriod = Mathf.Max(0, currentMatchPeriod -= Time.deltaTime * progressSpeed * 2f);
            matched = false;
            if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Sampling"))
                stateMachine.SetTrigger("GotoSampling");
        }

    }

    private void Disconnect()
    {
        Reset();

        stateMachine.SetTrigger("GotoNotMatched");
    }

    public void Reset()
    {
        currentMatchPeriod = 0;
        matched = false;
        isDisconnected = true;
        currentRotSampling = 0;
        prevDistance = 0;
        allowDisconnect = false;
    }

    public void AllowDisconnection()
    {
        allowDisconnect = true;
    }

    public void PauseSampling(bool val)
    {
        isSamplingPaused = val;
    }
    //public void ResumeSampling()
    //{
    //    isSamplingPaused = false;
    //}

}
