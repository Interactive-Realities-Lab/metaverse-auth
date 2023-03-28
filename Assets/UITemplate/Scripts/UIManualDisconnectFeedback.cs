using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManualDisconnectFeedback : MonoBehaviour
{
    [SerializeField] private HandleConnectionState connectionState;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text textField;
    private string text = "Disconnecting in: ";


    private void OnEnable()
    {
        canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        
        if(connectionState.inputDisconnect.action.IsPressed())
        {

            canvasGroup.alpha += Mathf.Min(1, Time.deltaTime*2);

            //var result = Mathf.InverseLerp(connectionState.HeldThreasholdValue, 0, connectionState.HeldValue);
            var result = Remap(0, connectionState.HeldThreasholdValue, connectionState.HeldThreasholdValue, 0, connectionState.HeldValue);
            textField.text = text + (int)result + " s";
            
        }
        else
            canvasGroup.alpha -= Mathf.Max(0, Time.deltaTime*2);

    }

    float Remap(float origFrom, float origTo, float targetFrom, float targetTo, float value)
    {
        float rel = Mathf.InverseLerp(origFrom, origTo, value);
        return Mathf.Lerp(targetFrom, targetTo, rel);
    }

}
