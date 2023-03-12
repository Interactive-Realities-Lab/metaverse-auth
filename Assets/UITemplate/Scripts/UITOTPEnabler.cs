using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITOTPEnabler : MonoBehaviour
{

    [SerializeField] private GameObject TOTPController;

    private void OnEnable()
    {
        TOTPController.SetActive(true);
    }

    private void OnDisable()
    {
        TOTPController.SetActive(false);
    }
}
