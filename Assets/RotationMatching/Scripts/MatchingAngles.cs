using UnityEngine;

public class MatchingAngles : MonoBehaviour
{

    [SerializeField] private GameObject object1, object2;

    [SerializeField] private float currentDistance = 0f;
    [SerializeField] private float prevDistance = 0f;
    [SerializeField] private float referenceValue = 0f;

    [Range(.001f, 1f)]
    [SerializeField] private float threashold = .1f;

    private int match = 0;
    private int matchMax = 500;

    private int samplingInterval = 500;
    private int sampling = 0;


    private bool matched = false;

    void Start()
    {
    }

    void Update()
    {


        var obj1matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), object1.transform.rotation, new Vector3(1.0f, 1.0f, 1.0f));
        var obj2matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), object2.transform.rotation, new Vector3(1.0f, 1.0f, 1.0f));

        currentDistance = Utils.DistMatrices(obj1matrix, obj2matrix);

        var absdiffCurrPrev = Mathf.Abs(prevDistance - currentDistance);


        if (match > matchMax)
        {
            referenceValue = currentDistance;
            matched = true;
            match = 0;
        }

        if (!matched)
        {
            if (absdiffCurrPrev < threashold)
            {
                match++;
                Debug.Log("SAMPLING");
            }
            else
            {
                match = 0;
                Debug.Log("NOT MATCHING");
            }

            if (sampling % samplingInterval == 0)
                prevDistance = currentDistance;

            sampling++;
 
        }
        else
        {
            
            var currentAbsdiff = Mathf.Abs(referenceValue - currentDistance);

            if (currentAbsdiff > threashold)
                matched = false;
            Debug.Log("    MATCHING --- Diff: " + currentAbsdiff);

        }

    }
}
