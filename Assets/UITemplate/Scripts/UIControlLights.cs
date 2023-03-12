using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControlLights : MonoBehaviour
{
    [SerializeField] private GameObject[] lights = new GameObject[4];
    [SerializeField] private ColorVariable current;
    [SerializeField] private ColorVariable finished;

    private void OnEnable()
    {
        ResetLights();
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ResetLights()
    {
        foreach (GameObject g in lights)
        {
            g.SetActive(false);
        }
    }

    public void SetLights(int index)
    {
        if (index >= lights.Length) return;

        if(index > 0)
            SetFinished(index-1);

        SetCurrent(index);
    }

    public void SetCurrent(int index)
    {
        lights[index].SetActive(true);
        lights[index].GetComponent<Image>().color = current.color;
    }

    public void SetFinished(int index)
    {
        lights[index].GetComponent<Image>().color = finished.color;
    }


}
