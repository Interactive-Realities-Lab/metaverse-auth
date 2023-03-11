using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]

    public class UIButton : UITemplateBase
    {
        [Header("Template")]
        [SerializeField] private UIButtonData skinButton;

        [Header("UI Attributes")]
        Button button;
        Image image;
        TextMeshProUGUI text;
        public string displayText;

        protected override void OnSkinUI()
        {
            base.OnSkinUI();
            image = GetComponent<Image>();
            button = GetComponent<Button>();
            text = GetComponentInChildren<TextMeshProUGUI>();

            button.transition = Selectable.Transition.ColorTint;

            if (text != null)
            {
                if (!string.IsNullOrEmpty(displayText))
                    text.text = displayText;
                text.font = skinButton.fontType;
                text.color = skinButton.fontColor.color;
                text.fontSize = skinButton.fontSize;
            }

            var colors = button.colors;
            colors.normalColor = skinButton.defautButtonColor.color;
            colors.highlightedColor = skinButton.hoverButtonColor.color;
            colors.selectedColor = skinButton.selectedButtonColor.color;
            colors.pressedColor = skinButton.pressedButtonColor.color;
            colors.disabledColor = skinButton.disabledButtonColor.color;

            GetComponent<Button>().colors = colors;
            image.sprite = skinButton.buttonStandardSprite;

        }
    }
}