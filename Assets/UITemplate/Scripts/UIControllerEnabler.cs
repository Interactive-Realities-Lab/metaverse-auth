using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIControllerEnabler : MonoBehaviour
{

    [SerializeField] private GameObject Controller;

    private void OnEnable()
    {
        Controller.SetActive(true);
    }

    private void OnDisable()
    {
        Controller.SetActive(false);
    }
}
