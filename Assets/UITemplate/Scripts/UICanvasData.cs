using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UITemplate
{
    [CreateAssetMenu(fileName = "New UI Canvas Template", menuName = "AlertEvaluation/UI/UICanvasTemplate")]
    public class UICanvasData : ScriptableObject
    {
        [Header("Color")]
        public ColorVariable backgroundColor;

        [Header("Text")]
        public TMP_FontAsset fontType;
        public Material customFontMaterial;
        public ColorVariable fontColor;
        public float fontSize;

    }
}