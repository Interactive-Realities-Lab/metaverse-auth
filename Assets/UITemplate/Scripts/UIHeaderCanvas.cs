using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    [RequireComponent(typeof(Image))]

    public class UIHeaderCanvas : UITemplateBase
    {
        [Header("Template")]
        [SerializeField] private UICanvasData skinCanvas;

        [Header("UI Attributes")]
        Image image;
        TextMeshProUGUI text;
        public string displayText;

        protected override void OnSkinUI()
        {
            base.OnSkinUI();
            image = GetComponent<Image>();
            text = GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
            {
                if(!string.IsNullOrEmpty(displayText))
                    text.text = displayText;
                text.font = skinCanvas.fontType;
                text.color = skinCanvas.fontColor.color;
                text.fontSize = skinCanvas.fontSize;
                text.material = skinCanvas.customFontMaterial;
            }

            image.color = skinCanvas.backgroundColor.color;

        }
    }
}