using UnityEngine;

public class MatchingAngles : MonoBehaviour
{

    [SerializeField] private GameObject rhand, lhand;

    [SerializeField] private float currentValue = 0f;
    [SerializeField] private float referenceValue = 0f;

    [Range(.001f, 1f)]
    [SerializeField] private float threashold = .1f;


    void Start()
    {
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.V))
            referenceValue = currentValue;

        var rhandmatrix = Matrix4x4.TRS(new Vector3(0, 0, 0), rhand.transform.rotation, new Vector3(1.0f, 1.0f, 1.0f));
        var lhandmatrix = Matrix4x4.TRS(new Vector3(0, 0, 0), lhand.transform.rotation, new Vector3(1.0f, 1.0f, 1.0f));

        currentValue = Utils.DistMatrices(rhandmatrix, lhandmatrix);
        
        var diff = referenceValue - currentValue;

        if (Mathf.Abs(diff) > threashold)
        {
            Debug.Log("NOT MATCHING --- Diff: " + diff);
            return;
        }



        Debug.Log("    MATCHING --- Diff: " + diff);
    }
}
