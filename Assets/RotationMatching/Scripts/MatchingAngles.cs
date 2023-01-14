using UnityEngine;

public class MatchingAngles : MonoBehaviour
{
    [SerializeField] private Animator stateMachine;

    [SerializeField] private GameObject object1, object2;

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
    private int match = 0;
    private int matchMax = 500;


    //Sampling rate for the previous rotation diff
    private int samplingInterval = 500;
    private int sampling = 0;

    private bool matched = false;

    void Update()
    {


        var obj1matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), object1.transform.rotation, new Vector3(1.0f, 1.0f, 1.0f));
        var obj2matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), object2.transform.rotation, new Vector3(1.0f, 1.0f, 1.0f));

        currentDistance = Utils.DistMatrices(obj1matrix, obj2matrix);

        var absdiffCurrPrev = Mathf.Abs(prevDistance - currentDistance);

        // Check if rotations are matching for some time 
        if (match > matchMax)
        {
            referenceValue = currentDistance;
            matched = true;
            match = 0;
            if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Matched"))
                stateMachine.SetTrigger("GotoMatched");
        }

        // If rotations are not matched 
        if (!matched)
        {
            // Start sampling if rotartions are starting to match
            if (absdiffCurrPrev < threashold)
            {
                match++;

                if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("Sampling"))
                    stateMachine.SetTrigger("GotoSampling");

            }
            else
            {
                // Lost matching... reset sampling (start over)
                match = 0;

                if (!stateMachine.GetCurrentAnimatorStateInfo(0).IsName("NotMatched"))
                    stateMachine.SetTrigger("GotoNotMatched");
            }

            // avoid sampling every frame... 
            if (sampling % samplingInterval == 0)
                prevDistance = currentDistance;

            sampling++;
            return;
        }
            
        // Once sampling is done and rotations are matched

        // Start observing if rotations lost matching...
        var currentAbsdiff = Mathf.Abs(referenceValue - currentDistance);

        if (currentAbsdiff > threashold)
            matched = false;

    }
}
