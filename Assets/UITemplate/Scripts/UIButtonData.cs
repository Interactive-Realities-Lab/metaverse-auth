using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    [CreateAssetMenu(fileName = "New UI Button Template", menuName = "AlertEvaluation/UI/UIButtonTemplate")]
    public class UIButtonData : ScriptableObject
    {
        [Header("Button Sprite")]
        public Sprite buttonStandardSprite;

        [Header("Button Colors")]
        public ColorVariable defautButtonColor;
        public ColorVariable hoverButtonColor;
        public ColorVariable selectedButtonColor;
        public ColorVariable pressedButtonColor;
        public ColorVariable disabledButtonColor;

        [Header("Text")]
        public TMP_FontAsset fontType;
        public ColorVariable fontColor;
        public float fontSize;
    }
}