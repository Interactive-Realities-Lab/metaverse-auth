using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    [RequireComponent(typeof(Image))]

    public class UIFooterCanvas : UITemplateBase
    {
        [Header("Template")]
        [SerializeField] private UICanvasData skinCanvas;

        Image image;
        //TextMeshProUGUI text;
        //public string displayText;

        protected override void OnSkinUI()
        {
            base.OnSkinUI();
            image = GetComponent<Image>();
            image.color = skinCanvas.backgroundColor.color;

        }
    }
}