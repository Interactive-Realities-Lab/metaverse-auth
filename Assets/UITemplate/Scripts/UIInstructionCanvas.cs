using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    public class UIInstructionCanvas : MonoBehaviour
    {
        public UICanvasInstructionsData skinCanvas;
        RectTransform transform;
        //TextMeshProUGUI text;
        //public string displayText;

        private void Awake()
        {
            OnSkinUI();
        }

        private void OnSkinUI()
        {
            transform = GetComponent<RectTransform>();
            transform.localPosition = skinCanvas.distanceFromWorldOrigin;
            transform.sizeDelta = skinCanvas.canvasSize;
            transform.localScale = skinCanvas.scale;
        }
    }
}