using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    [RequireComponent(typeof(Image))]

    public class UIBodyCanvas : UITemplateBase
    {
        [Header("Template")]
        [SerializeField] private UICanvasData skinCanvas;

        Image image;
        //TextMeshProUGUI[] textFields;

        protected override void OnSkinUI()
        {
            base.OnSkinUI();
            
            image = GetComponent<Image>();
            if (image != null)
                image.color = skinCanvas.backgroundColor.color;

            //textFields = GetComponentsInChildren<TextMeshProUGUI>();
            //if(textFields != null)
            //{
            //    foreach(var textField in textFields)
            //    {
            //        textField.font = skinCanvas.fontType;
            //        textField.color = skinCanvas.fontColor.color;
            //        textField.fontSize = skinCanvas.fontSize;
            //        textField.material = skinCanvas.customFontMaterial;

            //    }
            //}
        }
    }
}