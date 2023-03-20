using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITooltipController : MonoBehaviour
{
    [SerializeField] private TMP_Text tooltip1, tooltip2;

    private int currentActiveIndex;
    private void OnEnable()
    {

        ActivateTT1();

    }

    public void ActivateTT1()
    {
        tooltip1.gameObject.SetActive(true);
        tooltip2.gameObject.SetActive(false);
    }

    public void ActivateTT2()
    {
        tooltip1.gameObject.SetActive(false);
        tooltip2.gameObject.SetActive(true);
    }
}
