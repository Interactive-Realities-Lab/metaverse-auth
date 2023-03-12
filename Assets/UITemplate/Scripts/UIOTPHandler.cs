using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIOTPHandler : MonoBehaviour
{

    [SerializeField] private List<GameObject> otpPanels;

    private int currentEnabled;

    private void OnEnable()
    {
        DisableAllPanels();
        EnablePanel(0);
    }

    private void DisableAllPanels()
    {
        foreach (var panel in otpPanels)
        {
            panel.gameObject.SetActive(false);
        }
    }

    private void EnablePanel(int index)
    {
        if (index >= otpPanels.Count) return;

        otpPanels[index].gameObject.SetActive(true);
        currentEnabled = index;
    }


    public void EnableNextPanel()
    {
            
        DisableAllPanels();
        EnablePanel(++currentEnabled);
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
