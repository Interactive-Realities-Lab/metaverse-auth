using UnityEngine;

public class MatchingAngles : MonoBehaviour
{

    [SerializeField] private GameObject object1, object2;

    [SerializeField] private float currentValue = 0f;
    [SerializeField] private float referenceValue = 0f;

    [Range(.001f, 1f)]
    [SerializeField] private float threashold = .1f;

    private int sample = 0;
    private int maxSample = 500;
    private float prevValue = 0f;


    void Start()
    {
    }

    void Update()
    {

        //if (Input.GetKeyDown(KeyCode.V))
        //    referenceValue = currentValue;

        var obj1matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), object1.transform.rotation, new Vector3(1.0f, 1.0f, 1.0f));
        var obj2matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), object2.transform.rotation, new Vector3(1.0f, 1.0f, 1.0f));

        currentValue = Utils.DistMatrices(obj1matrix, obj2matrix);
        
        var absdiff = Mathf.Abs(referenceValue - currentValue);

        prevValue = currentValue;
        var absdiffCurrPrev = Mathf.Abs(prevValue - currentValue);


        if (absdiffCurrPrev < threashold)
            sample++;
        else
            sample = 0;

        Debug.Log(sample);

        if (sample > maxSample)
            referenceValue = currentValue;

        if (absdiff > threashold)
        {

            Debug.Log("NOT MATCHING --- Diff: " + absdiff);
            return;
        }

        Debug.Log("    MATCHING --- Diff: " + absdiff);
    }
}
